import { describe, expect, it } from 'vitest';

import { createBundleBudgetReport } from '../../../scripts/check-bundle-budgets.mjs';

describe('bundle budget checker', () => {
	it('keeps respondent survey runtime chunks out of the respondent entry budget', () => {
		const report = createBundleBudgetReport({
			clientManifest: fakeClientManifest,
			serverManifest: fakeServerManifest,
			assetMeasurements: fakeAssetMeasurements,
			routeGroups: [
				{
					id: 'respondent-entry',
					label: 'Respondent entry',
					routes: ['/r/[token]'],
					jsBudgetGzipBytes: 20_000,
					cssWarningGzipBytes: 80_000,
					forbiddenInitialDependencies: ['echarts', 'survey-core', 'survey-js-ui'],
					forbiddenLazyDependencies: ['echarts']
				}
			],
			generatedAt: '2026-05-09T00:00:00.000Z'
		});

		const respondentRoute = report.routeGroups[0].routes[0];

		expect(report.status).toBe('pass');
		expect(respondentRoute.initialAssets.map((asset) => asset.file).sort()).toEqual([
			'_app/immutable/chunks/public-client.js',
			'_app/immutable/chunks/runtime.js',
			'_app/immutable/entry/app.js',
			'_app/immutable/entry/start.js',
			'_app/immutable/nodes/respondent-page.js',
			'_app/immutable/nodes/root-layout.js'
		]);
		expect(respondentRoute.lazyAssets.map((asset) => asset.file).sort()).toEqual([
			'_app/immutable/assets/survey-core.css',
			'_app/immutable/chunks/survey-core.js',
			'_app/immutable/chunks/survey-ui.js'
		]);
		expect(respondentRoute.budgets.js.gzipBytes).toBe(17_000);
	});

	it('fails when ECharts is in a non-dashboard first-load route graph', () => {
		const report = createBundleBudgetReport({
			clientManifest: fakeClientManifest,
			serverManifest: fakeServerManifest,
			assetMeasurements: fakeAssetMeasurements,
			routeGroups: [
				{
					id: 'tenant-setup',
					label: 'Tenant setup',
					routes: ['/app/campaign-series/[seriesId]/setup'],
					jsBudgetGzipBytes: null,
					cssWarningGzipBytes: 80_000,
					forbiddenInitialDependencies: ['echarts', 'survey-core', 'survey-js-ui'],
					forbiddenLazyDependencies: ['survey-core', 'survey-js-ui']
				}
			],
			generatedAt: '2026-05-09T00:00:00.000Z'
		});

		expect(report.status).toBe('fail');
		expect(report.failures).toEqual([
			{
				routeGroupId: 'tenant-setup',
				routeId: '/app/campaign-series/[seriesId]/setup',
				severity: 'error',
				code: 'forbidden-initial-dependency',
				message: 'Tenant setup first-load assets include forbidden dependency echarts.',
				dependency: 'echarts',
				assets: ['_app/immutable/chunks/echarts-core.js']
			}
		]);
	});

	it('allows ECharts as lazy dashboard/reporting assets', () => {
		const report = createBundleBudgetReport({
			clientManifest: fakeClientManifest,
			serverManifest: fakeServerManifest,
			assetMeasurements: fakeAssetMeasurements,
			routeGroups: [
				{
					id: 'dashboard-reporting',
					label: 'Dashboard/reporting',
					routes: ['/app/campaign-series/[seriesId]/reports'],
					jsBudgetGzipBytes: 120_000,
					cssWarningGzipBytes: 80_000,
					forbiddenInitialDependencies: ['survey-core', 'survey-js-ui'],
					forbiddenLazyDependencies: ['survey-core', 'survey-js-ui']
				}
			],
			generatedAt: '2026-05-09T00:00:00.000Z'
		});

		expect(report.status).toBe('pass');
		expect(report.routeGroups[0].routes[0].lazyAssets.map((asset) => asset.file).sort()).toEqual([
			'_app/immutable/chunks/echarts-charts.js',
			'_app/immutable/chunks/echarts-core.js'
		]);
	});

	it('warns when a non-dashboard route only advertises conditional lazy ECharts imports', () => {
		const report = createBundleBudgetReport({
			clientManifest: fakeClientManifest,
			serverManifest: fakeServerManifest,
			assetMeasurements: fakeAssetMeasurements,
			routeGroups: [
				{
					id: 'tenant-operations',
					label: 'Tenant operations',
					routes: ['/app/campaign-series/[seriesId]/operations'],
					jsBudgetGzipBytes: null,
					cssWarningGzipBytes: 80_000,
					forbiddenInitialDependencies: ['echarts', 'survey-core', 'survey-js-ui'],
					forbiddenLazyDependencies: ['survey-core', 'survey-js-ui'],
					warnLazyDependencies: ['echarts']
				}
			],
			generatedAt: '2026-05-09T00:00:00.000Z'
		});

		expect(report.status).toBe('pass');
		expect(report.warnings).toEqual([
			{
				routeGroupId: 'tenant-operations',
				routeId: '/app/campaign-series/[seriesId]/operations',
				severity: 'warning',
				code: 'conditional-lazy-dependency',
				message:
					'Tenant operations lazy assets advertise dependency echarts; keep this informational unless route-smoke coverage observes a runtime request.',
				dependency: 'echarts',
				assets: ['_app/immutable/chunks/echarts-charts.js', '_app/immutable/chunks/echarts-core.js']
			}
		]);
	});
});

const fakeServerManifest = {
	_: {
		client: {
			imports: ['_app/immutable/entry/start.js', '_app/immutable/entry/app.js']
		},
		routes: [
			{
				id: '/r/[token]',
				page: { layouts: [0], leaf: 12 }
			},
			{
				id: '/app/campaign-series/[seriesId]/setup',
				page: { layouts: [0, 2], leaf: 9 }
			},
			{
				id: '/app/campaign-series/[seriesId]/operations',
				page: { layouts: [0, 2], leaf: 7 }
			},
			{
				id: '/app/campaign-series/[seriesId]/reports',
				page: { layouts: [0, 2], leaf: 8 }
			}
		]
	}
};

const fakeClientManifest = {
	'entry-start': {
		file: '_app/immutable/entry/start.js',
		imports: ['runtime']
	},
	'entry-app': {
		file: '_app/immutable/entry/app.js',
		imports: ['runtime'],
		dynamicImports: ['unrelated-echarts-route']
	},
	'.svelte-kit/generated/client-optimized/nodes/0.js': {
		file: '_app/immutable/nodes/root-layout.js',
		imports: ['runtime']
	},
	'.svelte-kit/generated/client-optimized/nodes/2.js': {
		file: '_app/immutable/nodes/app-layout.js',
		imports: ['runtime']
	},
	'.svelte-kit/generated/client-optimized/nodes/7.js': {
		file: '_app/immutable/nodes/operations-page.js',
		imports: ['shared-surface']
	},
	'.svelte-kit/generated/client-optimized/nodes/8.js': {
		file: '_app/immutable/nodes/reports-page.js',
		imports: ['chart-surface']
	},
	'.svelte-kit/generated/client-optimized/nodes/9.js': {
		file: '_app/immutable/nodes/setup-page.js',
		imports: ['echarts-core']
	},
	'.svelte-kit/generated/client-optimized/nodes/12.js': {
		file: '_app/immutable/nodes/respondent-page.js',
		imports: ['public-client'],
		dynamicImports: ['survey-core', 'survey-ui']
	},
	runtime: {
		file: '_app/immutable/chunks/runtime.js'
	},
	'public-client': {
		file: '_app/immutable/chunks/public-client.js'
	},
	'shared-surface': {
		file: '_app/immutable/chunks/shared-surface.js',
		dynamicImports: ['echarts-core']
	},
	'chart-surface': {
		file: '_app/immutable/chunks/chart-surface.js',
		dynamicImports: ['echarts-core']
	},
	'echarts-core': {
		file: '_app/immutable/chunks/echarts-core.js',
		src: 'node_modules/echarts/core.js',
		dynamicImports: ['echarts-charts']
	},
	'echarts-charts': {
		file: '_app/immutable/chunks/echarts-charts.js',
		src: 'node_modules/echarts/charts.js'
	},
	'unrelated-echarts-route': {
		file: '_app/immutable/nodes/unrelated-echarts-route.js',
		dynamicImports: ['echarts-core']
	},
	'survey-core': {
		file: '_app/immutable/chunks/survey-core.js',
		src: 'node_modules/survey-core/fesm/survey-core.mjs',
		css: ['_app/immutable/assets/survey-core.css']
	},
	'survey-ui': {
		file: '_app/immutable/chunks/survey-ui.js',
		src: 'node_modules/survey-js-ui/fesm/survey-js-ui.mjs',
		imports: ['survey-core']
	}
};

const fakeAssetMeasurements = {
	'_app/immutable/entry/start.js': { gzipBytes: 2_000, rawBytes: 6_000 },
	'_app/immutable/entry/app.js': { gzipBytes: 3_000, rawBytes: 9_000 },
	'_app/immutable/nodes/root-layout.js': { gzipBytes: 4_000, rawBytes: 12_000 },
	'_app/immutable/nodes/app-layout.js': { gzipBytes: 5_000, rawBytes: 15_000 },
	'_app/immutable/nodes/respondent-page.js': { gzipBytes: 6_000, rawBytes: 18_000 },
	'_app/immutable/nodes/setup-page.js': { gzipBytes: 7_000, rawBytes: 21_000 },
	'_app/immutable/nodes/operations-page.js': { gzipBytes: 8_000, rawBytes: 24_000 },
	'_app/immutable/nodes/reports-page.js': { gzipBytes: 9_000, rawBytes: 27_000 },
	'_app/immutable/chunks/runtime.js': { gzipBytes: 1_000, rawBytes: 3_000 },
	'_app/immutable/chunks/public-client.js': { gzipBytes: 1_000, rawBytes: 3_000 },
	'_app/immutable/chunks/shared-surface.js': { gzipBytes: 2_000, rawBytes: 6_000 },
	'_app/immutable/chunks/chart-surface.js': { gzipBytes: 3_000, rawBytes: 9_000 },
	'_app/immutable/chunks/echarts-core.js': { gzipBytes: 10_000, rawBytes: 30_000 },
	'_app/immutable/chunks/echarts-charts.js': { gzipBytes: 11_000, rawBytes: 33_000 },
	'_app/immutable/nodes/unrelated-echarts-route.js': { gzipBytes: 8_000, rawBytes: 24_000 },
	'_app/immutable/chunks/survey-core.js': { gzipBytes: 12_000, rawBytes: 36_000 },
	'_app/immutable/chunks/survey-ui.js': { gzipBytes: 13_000, rawBytes: 39_000 },
	'_app/immutable/assets/survey-core.css': { gzipBytes: 14_000, rawBytes: 42_000 }
};
