#!/usr/bin/env node

import { mkdir, readdir, readFile, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { gzipSync } from 'node:zlib';

const KB = 1024;

/**
 * @typedef {'js' | 'css'} AssetType
 * @typedef {'pass' | 'fail' | 'warning'} GateStatus
 * @typedef {{ gzipBytes: number, rawBytes: number }} AssetMeasurement
 * @typedef {Record<string, AssetMeasurement>} AssetMeasurements
 * @typedef {{ file?: string, src?: string, name?: string, imports?: string[], dynamicImports?: string[], css?: string[] }} ClientManifestEntry
 * @typedef {Record<string, ClientManifestEntry>} ClientManifest
 * @typedef {{ layouts?: (number | null)[], leaf?: number }} ServerRoutePage
 * @typedef {{ id: string, page?: ServerRoutePage }} ServerRoute
 * @typedef {{ _: { client?: { imports?: string[] }, routes?: ServerRoute[] } }} ServerManifest
 * @typedef {{ id: string, label: string, routes: string[], jsBudgetGzipBytes: number | null, cssWarningGzipBytes: number | null, forbiddenInitialDependencies?: string[], forbiddenLazyDependencies?: string[], warnLazyDependencies?: string[] }} RouteGroup
 * @typedef {{ file: string, manifestKey: string, type: AssetType, gzipBytes: number, rawBytes: number, dependencies: string[] }} BundleAsset
 * @typedef {{ routeGroupId: string, routeId: string, severity: 'error' | 'warning', code: string, message: string, dependency: string | null, assets: string[] }} Finding
 * @typedef {{ js: { gzipBytes: number, budgetBytes: number | null, status: 'pass' | 'fail' }, css: { gzipBytes: number, warningBytes: number | null, status: 'pass' | 'warning' } }} RouteBudgets
 * @typedef {{ id: string, status: 'pass' | 'fail', initialAssets: BundleAsset[], lazyAssets: BundleAsset[], budgets: RouteBudgets, failures: Finding[], warnings: Finding[] }} RouteReport
 * @typedef {{ id: string, label: string, routes: RouteReport[], status: 'pass' | 'fail' }} RouteGroupReport
 * @typedef {{ status: 'pass' | 'fail', generatedAt: string, routeGroups: RouteGroupReport[], failures: Finding[], warnings: Finding[] }} BundleBudgetReport
 * @typedef {{ entries: Map<string, ClientManifestEntry>, resolve(ref: string): string | null }} ClientGraph
 * @typedef {{ bootstrapRefs: string[], nodeRefs: string[] }} RouteEntryRefs
 * @typedef {{ initialAssets: Map<string, BundleAsset>, lazyAssets: Map<string, BundleAsset> }} CollectedRouteAssets
 */

/** @type {RouteGroup[]} */
export const DEFAULT_ROUTE_GROUPS = [
	{
		id: 'respondent-entry',
		label: 'Respondent entry',
		routes: ['/r/[token]'],
		jsBudgetGzipBytes: 150 * KB,
		cssWarningGzipBytes: 80 * KB,
		forbiddenInitialDependencies: ['echarts', 'survey-core', 'survey-js-ui', 'survey-creator'],
		forbiddenLazyDependencies: ['echarts', 'survey-creator']
	},
	{
		id: 'respondent-survey-runtime',
		label: 'Respondent survey runtime',
		routes: ['/r/[token]'],
		jsBudgetGzipBytes: null,
		cssWarningGzipBytes: 80 * KB,
		forbiddenInitialDependencies: ['echarts', 'survey-creator'],
		forbiddenLazyDependencies: ['echarts', 'survey-creator']
	},
	{
		id: 'tenant-setup-admin',
		label: 'Tenant setup/admin',
		routes: [
			'/app',
			'/app/campaign-series',
			'/app/campaign-series/[seriesId]',
			'/app/campaign-series/[seriesId]/setup',
			'/app/campaign-series/[seriesId]/operations'
		],
		jsBudgetGzipBytes: null,
		cssWarningGzipBytes: 80 * KB,
		forbiddenInitialDependencies: ['echarts', 'survey-core', 'survey-js-ui', 'survey-creator'],
		forbiddenLazyDependencies: ['survey-core', 'survey-js-ui', 'survey-creator'],
		warnLazyDependencies: ['echarts']
	},
	{
		id: 'dashboard-reporting',
		label: 'Dashboard/reporting',
		routes: ['/app/campaign-series/[seriesId]/reports', '/app/campaign-series/[seriesId]/waves'],
		jsBudgetGzipBytes: 800 * KB,
		cssWarningGzipBytes: 80 * KB,
		forbiddenInitialDependencies: ['echarts', 'survey-core', 'survey-js-ui', 'survey-creator'],
		forbiddenLazyDependencies: ['survey-core', 'survey-js-ui', 'survey-creator']
	},
	{
		id: 'future-designer',
		label: 'Future designer',
		routes: [],
		jsBudgetGzipBytes: null,
		cssWarningGzipBytes: 80 * KB,
		forbiddenInitialDependencies: ['echarts', 'survey-core', 'survey-js-ui', 'survey-creator'],
		forbiddenLazyDependencies: ['echarts', 'survey-core', 'survey-js-ui', 'survey-creator']
	}
];

/**
 * @param {{ clientManifest: ClientManifest, serverManifest: ServerManifest, assetMeasurements: AssetMeasurements, routeGroups?: RouteGroup[], generatedAt?: string }} options
 * @returns {BundleBudgetReport}
 */
export function createBundleBudgetReport({
	clientManifest,
	serverManifest,
	assetMeasurements,
	routeGroups = DEFAULT_ROUTE_GROUPS,
	generatedAt = new Date().toISOString()
}) {
	const graph = createClientGraph(clientManifest);
	const manifestRoutes = serverManifest?._?.routes ?? [];
	const serverRoutes = new Map(
		manifestRoutes.filter((route) => route.page).map((route) => [route.id, route])
	);
	/** @type {Finding[]} */
	const failures = [];
	/** @type {Finding[]} */
	const warnings = [];

	/** @type {RouteGroupReport[]} */
	const reportedRouteGroups = routeGroups.map((group) => {
		const routes = group.routes.map((routeId) => {
			const route = serverRoutes.get(routeId);

			if (!route) {
				/** @type {Finding} */
				const failure = {
					routeGroupId: group.id,
					routeId,
					severity: 'error',
					code: 'route-not-found',
					message: `${group.label} route ${routeId} is not present in the SvelteKit server manifest.`,
					dependency: null,
					assets: []
				};
				failures.push(failure);
				return emptyRouteReport(routeId, [failure]);
			}

			const routeReport = createRouteReport({
				group,
				route,
				graph,
				serverManifest,
				assetMeasurements
			});

			failures.push(...routeReport.failures);
			warnings.push(...routeReport.warnings);

			return routeReport;
		});

		return {
			id: group.id,
			label: group.label,
			routes,
			status: /** @type {'pass' | 'fail'} */ (
				routes.some((route) => route.status === 'fail') ? 'fail' : 'pass'
			)
		};
	});

	return {
		status: failures.length > 0 ? 'fail' : 'pass',
		generatedAt,
		routeGroups: reportedRouteGroups,
		failures,
		warnings
	};
}

/**
 * @param {{ rootDir?: string, routeGroups?: RouteGroup[], outputPath?: string }} [options]
 * @returns {Promise<BundleBudgetReport>}
 */
export async function runBundleBudgetCheck({
	rootDir = process.cwd(),
	routeGroups = DEFAULT_ROUTE_GROUPS,
	outputPath = path.join(rootDir, 'artifacts', 'bundle-budgets', 'report.json')
} = {}) {
	const buildOutputDir = path.join(rootDir, '.svelte-kit', 'output');
	const clientDir = path.join(buildOutputDir, 'client');
	const clientManifestPath = path.join(clientDir, '.vite', 'manifest.json');
	const serverManifestPath = path.join(buildOutputDir, 'server', 'manifest.js');

	await assertBuildOutputExists(clientManifestPath, serverManifestPath);

	const clientManifest = JSON.parse(await readFile(clientManifestPath, 'utf8'));
	const serverManifestModule = await import(
		`${pathToFileURL(serverManifestPath).href}?bundleBudget=${Date.now()}`
	);
	const assetMeasurements = await measureClientAssets(clientDir);

	const report = createBundleBudgetReport({
		clientManifest,
		serverManifest: serverManifestModule.manifest,
		assetMeasurements,
		routeGroups
	});

	await mkdir(path.dirname(outputPath), { recursive: true });
	await writeFile(outputPath, `${JSON.stringify(report, null, 2)}\n`, 'utf8');

	printReport(report, outputPath);

	if (report.status === 'fail') {
		process.exitCode = 1;
	}

	return report;
}

/**
 * @param {{ group: RouteGroup, route: ServerRoute, graph: ClientGraph, serverManifest: ServerManifest, assetMeasurements: AssetMeasurements }} options
 * @returns {RouteReport}
 */
function createRouteReport({ group, route, graph, serverManifest, assetMeasurements }) {
	const entryRefs = routeEntryRefs(route, serverManifest);
	const collected = collectRouteAssets(entryRefs, graph, assetMeasurements);
	const initialAssets = sortAssets([...collected.initialAssets.values()]);
	const lazyAssets = sortAssets([...collected.lazyAssets.values()]);
	const jsGzipBytes = sumAssets(initialAssets, 'js');
	const cssGzipBytes = sumAssets(initialAssets, 'css');
	const failures = [
		...dependencyFindings({
			severity: 'error',
			code: 'forbidden-initial-dependency',
			message: /** @param {string} dependency */ (dependency) =>
				`${group.label} first-load assets include forbidden dependency ${dependency}.`,
			routeGroupId: group.id,
			routeId: route.id,
			assets: initialAssets,
			dependencies: group.forbiddenInitialDependencies ?? []
		}),
		...dependencyFindings({
			severity: 'error',
			code: 'forbidden-lazy-dependency',
			message: /** @param {string} dependency */ (dependency) =>
				`${group.label} lazy assets include forbidden dependency ${dependency}.`,
			routeGroupId: group.id,
			routeId: route.id,
			assets: lazyAssets,
			dependencies: group.forbiddenLazyDependencies ?? []
		})
	];
	const warnings = dependencyFindings({
		severity: 'warning',
		code: 'conditional-lazy-dependency',
		message: /** @param {string} dependency */ (dependency) =>
			`${group.label} lazy assets advertise dependency ${dependency}; keep this informational unless route-smoke coverage observes a runtime request.`,
		routeGroupId: group.id,
		routeId: route.id,
		assets: lazyAssets,
		dependencies: group.warnLazyDependencies ?? []
	});

	if (group.jsBudgetGzipBytes !== null && jsGzipBytes > group.jsBudgetGzipBytes) {
		failures.push({
			routeGroupId: group.id,
			routeId: route.id,
			severity: 'error',
			code: 'js-budget-exceeded',
			message: `${group.label} first-load JS is ${jsGzipBytes} gzip bytes, above budget ${group.jsBudgetGzipBytes}.`,
			dependency: null,
			assets: []
		});
	}

	if (group.cssWarningGzipBytes !== null && cssGzipBytes > group.cssWarningGzipBytes) {
		warnings.push({
			routeGroupId: group.id,
			routeId: route.id,
			severity: 'warning',
			code: 'css-budget-warning',
			message: `${group.label} first-load CSS is ${cssGzipBytes} gzip bytes, above warning threshold ${group.cssWarningGzipBytes}.`,
			dependency: null,
			assets: []
		});
	}

	return {
		id: route.id,
		status: failures.length > 0 ? 'fail' : 'pass',
		initialAssets,
		lazyAssets,
		budgets: {
			js: {
				gzipBytes: jsGzipBytes,
				budgetBytes: group.jsBudgetGzipBytes,
				status:
					group.jsBudgetGzipBytes === null || jsGzipBytes <= group.jsBudgetGzipBytes
						? 'pass'
						: 'fail'
			},
			css: {
				gzipBytes: cssGzipBytes,
				warningBytes: group.cssWarningGzipBytes,
				status:
					group.cssWarningGzipBytes === null || cssGzipBytes <= group.cssWarningGzipBytes
						? 'pass'
						: 'warning'
			}
		},
		failures,
		warnings
	};
}

/**
 * @param {string} routeId
 * @param {Finding[]} failures
 * @returns {RouteReport}
 */
function emptyRouteReport(routeId, failures) {
	return {
		id: routeId,
		status: 'fail',
		initialAssets: [],
		lazyAssets: [],
		budgets: {
			js: { gzipBytes: 0, budgetBytes: null, status: 'fail' },
			css: { gzipBytes: 0, warningBytes: null, status: 'pass' }
		},
		failures,
		warnings: []
	};
}

/**
 * @param {ServerRoute} route
 * @param {ServerManifest} serverManifest
 * @returns {RouteEntryRefs}
 */
function routeEntryRefs(route, serverManifest) {
	const bootstrapRefs = [...(serverManifest?._?.client?.imports ?? [])];
	/** @type {string[]} */
	const nodeRefs = [];
	const page = route.page;

	if (!page) {
		return { bootstrapRefs, nodeRefs };
	}

	const indexes = [...(page.layouts ?? []), page.leaf].filter((index) => Number.isInteger(index));

	for (const index of indexes) {
		nodeRefs.push(`.svelte-kit/generated/client-optimized/nodes/${index}.js`);
	}

	return { bootstrapRefs, nodeRefs };
}

/**
 * @param {RouteEntryRefs} entryRefs
 * @param {ClientGraph} graph
 * @param {AssetMeasurements} assetMeasurements
 * @returns {CollectedRouteAssets}
 */
function collectRouteAssets(entryRefs, graph, assetMeasurements) {
	/** @type {Map<string, BundleAsset>} */
	const initialAssets = new Map();
	/** @type {Map<string, BundleAsset>} */
	const lazyAssets = new Map();
	/** @type {Set<string>} */
	const visitedInitialEntries = new Set();
	/** @type {Set<string>} */
	const visitedLazyEntries = new Set();

	/** @type {(dynamicRef: string, inheritedDependencies: Set<string>) => void} */
	const collectLazyEntry = (dynamicRef, inheritedDependencies) => {
		collectEntry({
			ref: dynamicRef,
			graph,
			assetMeasurements,
			targetAssets: lazyAssets,
			visitedEntries: visitedLazyEntries,
			inheritedDependencies,
			onDynamicImport: collectLazyEntry
		});
	};

	for (const entryRef of entryRefs.bootstrapRefs) {
		collectEntry({
			ref: entryRef,
			graph,
			assetMeasurements,
			targetAssets: initialAssets,
			visitedEntries: visitedInitialEntries,
			inheritedDependencies: new Set(),
			onDynamicImport: () => {}
		});
	}

	for (const entryRef of entryRefs.nodeRefs) {
		collectEntry({
			ref: entryRef,
			graph,
			assetMeasurements,
			targetAssets: initialAssets,
			visitedEntries: visitedInitialEntries,
			inheritedDependencies: new Set(),
			onDynamicImport: collectLazyEntry
		});
	}

	return { initialAssets, lazyAssets };
}

/**
 * @param {{ ref: string, graph: ClientGraph, assetMeasurements: AssetMeasurements, targetAssets: Map<string, BundleAsset>, visitedEntries: Set<string>, inheritedDependencies: Set<string>, onDynamicImport?: (dynamicRef: string, inheritedDependencies: Set<string>) => void }} options
 * @returns {void}
 */
function collectEntry({
	ref,
	graph,
	assetMeasurements,
	targetAssets,
	visitedEntries,
	inheritedDependencies,
	onDynamicImport = (_dynamicRef, _dependencies) => {}
}) {
	const manifestKey = graph.resolve(ref);

	if (!manifestKey || visitedEntries.has(manifestKey)) {
		return;
	}

	visitedEntries.add(manifestKey);

	const entry = graph.entries.get(manifestKey);

	if (!entry) {
		return;
	}

	const dependencies = new Set([
		...inheritedDependencies,
		...dependencyTagsForEntry(manifestKey, entry)
	]);

	addEntryFileAsset(entry, manifestKey, dependencies, assetMeasurements, targetAssets);
	addCssAssets(entry, dependencies, assetMeasurements, targetAssets);

	for (const importRef of entry.imports ?? []) {
		collectEntry({
			ref: importRef,
			graph,
			assetMeasurements,
			targetAssets,
			visitedEntries,
			inheritedDependencies: dependencies,
			onDynamicImport
		});
	}

	for (const dynamicRef of entry.dynamicImports ?? []) {
		onDynamicImport(dynamicRef, dependencies);
	}
}

/**
 * @param {ClientManifestEntry} entry
 * @param {string} manifestKey
 * @param {Set<string>} dependencies
 * @param {AssetMeasurements} assetMeasurements
 * @param {Map<string, BundleAsset>} targetAssets
 * @returns {void}
 */
function addEntryFileAsset(entry, manifestKey, dependencies, assetMeasurements, targetAssets) {
	if (!entry.file) {
		return;
	}

	addAsset({
		file: normalizePath(entry.file),
		manifestKey,
		dependencies,
		assetMeasurements,
		targetAssets
	});
}

/**
 * @param {ClientManifestEntry} entry
 * @param {Set<string>} dependencies
 * @param {AssetMeasurements} assetMeasurements
 * @param {Map<string, BundleAsset>} targetAssets
 * @returns {void}
 */
function addCssAssets(entry, dependencies, assetMeasurements, targetAssets) {
	for (const cssFile of entry.css ?? []) {
		addAsset({
			file: normalizePath(cssFile),
			manifestKey: cssFile,
			dependencies,
			assetMeasurements,
			targetAssets
		});
	}
}

/**
 * @param {{ file: string, manifestKey: string, dependencies: Set<string>, assetMeasurements: AssetMeasurements, targetAssets: Map<string, BundleAsset> }} options
 * @returns {void}
 */
function addAsset({ file, manifestKey, dependencies, assetMeasurements, targetAssets }) {
	const existing = targetAssets.get(file);
	const measurement = assetMeasurements[file] ?? { gzipBytes: 0, rawBytes: 0 };

	if (existing) {
		for (const dependency of dependencies) {
			existing.dependencies.push(dependency);
		}
		existing.dependencies = [...new Set(existing.dependencies)].sort();
		return;
	}

	targetAssets.set(file, {
		file,
		manifestKey,
		type: file.endsWith('.css') ? 'css' : 'js',
		gzipBytes: measurement.gzipBytes,
		rawBytes: measurement.rawBytes,
		dependencies: [...dependencies].sort()
	});
}

/**
 * @param {{ severity: 'error' | 'warning', code: string, message: (dependency: string) => string, routeGroupId: string, routeId: string, assets: BundleAsset[], dependencies: string[] }} options
 * @returns {Finding[]}
 */
function dependencyFindings({
	severity,
	code,
	message,
	routeGroupId,
	routeId,
	assets,
	dependencies
}) {
	const findings = [];

	for (const dependency of dependencies) {
		const matchingAssets = assets
			.filter((asset) => asset.dependencies.includes(dependency))
			.map((asset) => asset.file)
			.sort();

		if (matchingAssets.length > 0) {
			findings.push({
				routeGroupId,
				routeId,
				severity,
				code,
				message: message(dependency),
				dependency,
				assets: matchingAssets
			});
		}
	}

	return findings;
}

/**
 * @param {ClientManifest} clientManifest
 * @returns {ClientGraph}
 */
function createClientGraph(clientManifest) {
	const entries = new Map(Object.entries(clientManifest));
	const fileToKey = new Map();

	for (const [key, entry] of entries) {
		if (entry.file) {
			fileToKey.set(normalizePath(entry.file), key);
		}
	}

	return {
		entries,
		resolve(ref) {
			const normalizedRef = normalizePath(ref);

			if (entries.has(normalizedRef)) {
				return normalizedRef;
			}

			if (fileToKey.has(normalizedRef)) {
				return fileToKey.get(normalizedRef);
			}

			return null;
		}
	};
}

/**
 * @param {string} manifestKey
 * @param {ClientManifestEntry} entry
 * @returns {string[]}
 */
function dependencyTagsForEntry(manifestKey, entry) {
	const text = `${manifestKey} ${entry.src ?? ''} ${entry.name ?? ''}`;
	const dependencies = [];

	if (/node_modules\/echarts\//.test(text) || /node_modules\/zrender\//.test(text)) {
		dependencies.push('echarts');
	}

	if (/node_modules\/survey-core\//.test(text) || /survey-core/.test(text)) {
		dependencies.push('survey-core');
	}

	if (/node_modules\/survey-js-ui\//.test(text) || /survey-js-ui/.test(text)) {
		dependencies.push('survey-js-ui');
	}

	if (/node_modules\/survey-creator/.test(text) || /survey-creator/.test(text)) {
		dependencies.push('survey-creator');
	}

	return dependencies;
}

/**
 * @param {string} clientManifestPath
 * @param {string} serverManifestPath
 * @returns {Promise<void>}
 */
async function assertBuildOutputExists(clientManifestPath, serverManifestPath) {
	const missing = [];

	for (const filePath of [clientManifestPath, serverManifestPath]) {
		try {
			await stat(filePath);
		} catch {
			missing.push(filePath);
		}
	}

	if (missing.length > 0) {
		throw new Error(
			`Bundle budget check requires SvelteKit build output. Run "npm run build" first. Missing: ${missing.join(', ')}`
		);
	}
}

/**
 * @param {string} clientDir
 * @returns {Promise<AssetMeasurements>}
 */
async function measureClientAssets(clientDir) {
	const files = await listFiles(clientDir);
	/** @type {AssetMeasurements} */
	const measurements = {};

	for (const filePath of files) {
		if (!filePath.endsWith('.js') && !filePath.endsWith('.css')) {
			continue;
		}

		const buffer = await readFile(filePath);
		const relativePath = normalizePath(path.relative(clientDir, filePath));

		measurements[relativePath] = {
			rawBytes: buffer.byteLength,
			gzipBytes: gzipSync(buffer, { level: 9 }).byteLength
		};
	}

	return measurements;
}

/**
 * @param {string} directory
 * @returns {Promise<string[]>}
 */
async function listFiles(directory) {
	/** @type {string[]} */
	const result = [];
	const entries = await readdir(directory, { withFileTypes: true });

	for (const entry of entries) {
		const entryPath = path.join(directory, entry.name);

		if (entry.isDirectory()) {
			result.push(...(await listFiles(entryPath)));
		} else if (entry.isFile()) {
			result.push(entryPath);
		}
	}

	return result;
}

/**
 * @param {BundleBudgetReport} report
 * @param {string} outputPath
 * @returns {void}
 */
function printReport(report, outputPath) {
	console.log('Bundle budget report');
	console.log(
		'Surface                         Route                                            JS gzip   CSS gzip   Status'
	);
	console.log(
		'------------------------------  ----------------------------------------------  --------  ---------  ------'
	);

	for (const group of report.routeGroups) {
		for (const route of group.routes) {
			console.log(
				[
					pad(group.label, 30),
					pad(route.id, 46),
					pad(formatBytes(route.budgets.js.gzipBytes), 8),
					pad(formatBytes(route.budgets.css.gzipBytes), 9),
					route.status
				].join('  ')
			);
		}
	}

	for (const warning of report.warnings) {
		console.warn(`warning ${warning.code}: ${warning.message}`);
	}

	for (const failure of report.failures) {
		console.error(`error ${failure.code}: ${failure.message}`);
	}

	console.log(`Wrote ${path.relative(process.cwd(), outputPath)}`);
}

/**
 * @param {number} bytes
 * @returns {string}
 */
function formatBytes(bytes) {
	return `${(bytes / KB).toFixed(1)} KB`;
}

/**
 * @param {unknown} value
 * @param {number} width
 * @returns {string}
 */
function pad(value, width) {
	const text = String(value);
	return text.length >= width ? text : `${text}${' '.repeat(width - text.length)}`;
}

/**
 * @param {BundleAsset[]} assets
 * @returns {BundleAsset[]}
 */
function sortAssets(assets) {
	return assets
		.map((asset) => ({
			...asset,
			dependencies: [...asset.dependencies].sort()
		}))
		.sort((left, right) => left.file.localeCompare(right.file));
}

/**
 * @param {BundleAsset[]} assets
 * @param {AssetType} type
 * @returns {number}
 */
function sumAssets(assets, type) {
	return assets
		.filter((asset) => asset.type === type)
		.reduce((total, asset) => total + asset.gzipBytes, 0);
}

/**
 * @param {string} value
 * @returns {string}
 */
function normalizePath(value) {
	return value.replaceAll('\\', '/');
}

const isMain =
	process.argv[1] && pathToFileURL(path.resolve(process.argv[1])).href === import.meta.url;

if (isMain) {
	runBundleBudgetCheck().catch((error) => {
		console.error(error instanceof Error ? error.message : String(error));
		process.exitCode = 1;
	});
}
