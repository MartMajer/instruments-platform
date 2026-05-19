<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import {
		createLoginUrlFromEnv,
		readLastWorkspaceEmail,
		readLastTenantId,
		rememberLastTenantId,
		normalizeTenantId
	} from '$lib/api/session-headers';

	const initialTenantIdFromUrl = normalizeTenantId(page.url.searchParams.get('tenantId'));
	const tenantIdFromUrl = $derived(normalizeTenantId(page.url.searchParams.get('tenantId')));
	const workspaceSignInUrl = resolve('/signin');
	let loginUrl = $state(
		initialTenantIdFromUrl ? createLoginUrlFromEnv(env, initialTenantIdFromUrl) : workspaceSignInUrl
	);

	onMount(() => {
		const storedTenantId = readLastTenantId(window.localStorage);
		const tenantId = tenantIdFromUrl || storedTenantId;
		const loginHint = tenantId && tenantId === storedTenantId
			? readLastWorkspaceEmail(window.localStorage)
			: '';

		if (tenantIdFromUrl) {
			rememberLastTenantId(window.localStorage, tenantIdFromUrl);
		}

		loginUrl = tenantId ? createLoginUrlFromEnv(env, tenantId, loginHint) : workspaceSignInUrl;

		if (page.url.searchParams.get('postLogout') !== 'register') {
			return;
		}

		const registrationReturnUrl = encodeURIComponent(absoluteWebUrl(resolve('/register')));
		window.location.replace(
			resolveAuthRedirectUrl(
				`/auth/login?registration=1&returnUrl=${registrationReturnUrl}`
			)
		);
	});

	function absoluteWebUrl(path: string) {
		return new URL(path, window.location.origin).toString();
	}

	function resolveAuthRedirectUrl(authUrl: string) {
		if (/^https?:\/\//i.test(authUrl)) {
			return authUrl;
		}

		const authOrigin = absoluteOrigin(env.PUBLIC_AUTH_LOGIN_URL);
		if (authOrigin) {
			return new URL(authUrl, authOrigin).toString();
		}

		const apiOrigin = absoluteOrigin(env.PUBLIC_API_BASE_URL);
		if (apiOrigin) {
			return new URL(authUrl, apiOrigin).toString();
		}

		return authUrl;
	}

	function absoluteOrigin(value: string | undefined) {
		if (!value || !/^https?:\/\//i.test(value)) {
			return null;
		}

		try {
			return new URL(value).origin;
		} catch {
			return null;
		}
	}
</script>

<svelte:head>
	<title>Instruments Platform | Study management platform</title>
	<meta
		name="description"
		content="Tenant-scoped app for survey setup, response collection, reports, and exports."
	/>
</svelte:head>

<section class="launchpad" aria-labelledby="product-entry-title">
	<div class="launchpad__glow launchpad__glow--one" aria-hidden="true"></div>
	<div class="launchpad__glow launchpad__glow--two" aria-hidden="true"></div>

	<header class="launchpad-nav">
		<a class="launchpad-brand" href={resolve('/')}>
			<span class="launchpad-brand__mark" aria-hidden="true">IP</span>
			<span>
				<strong>Instruments Platform</strong>
				<small>Studies, surveys, results</small>
			</span>
		</a>
		<nav class="launchpad-nav__links" aria-label="Product entry actions">
			<a href="#workflow">Workflow</a>
			<a href="#proof">Beta boundary</a>
			<a href={resolve('/register')}>Create workspace</a>
			<a href={loginUrl}>Sign in</a>
		</nav>
	</header>

	<section class="launchpad-hero">
		<div class="launchpad-hero__copy">
			<p class="launchpad-kicker">Private beta · EU-hosted app · demo data only</p>
			<h1 id="product-entry-title">Run studies from setup to results without spreadsheets holding it together.</h1>
			<p>
				Prepare surveys, collect responses, review results, and export files without
				forcing researchers to stitch together forms, spreadsheets, scripts, and screenshots.
			</p>
			<div class="launchpad-actions">
				<a class="launchpad-button launchpad-button--primary" href={resolve('/register')}>Create workspace</a>
				<a class="launchpad-button launchpad-button--secondary" href={loginUrl}>Sign in</a>
			</div>
		</div>

		<div class="showcase" aria-label="Product preview">
			<div class="showcase__chrome" aria-hidden="true">
				<span></span><span></span><span></span>
				<strong>workspace / study cockpit</strong>
			</div>
			<div class="showcase__body">
				<aside class="showcase-rail" aria-hidden="true">
					<div class="showcase-rail__logo">IP</div>
					<span class="showcase-rail__link showcase-rail__link--active">Studies</span>
					<span class="showcase-rail__link">Collect</span>
					<span class="showcase-rail__link">Results</span>
					<span class="showcase-rail__link">Exports</span>
				</aside>

				<main class="showcase-main">
					<div class="showcase-main__topline">
						<div>
							<span>Selected study</span>
							<h2>Workplace wellbeing pulse</h2>
						</div>
						<strong>Live collection</strong>
					</div>

					<section class="showcase-panel showcase-panel--chart" aria-label="Response trend preview">
						<div class="showcase-panel__heading">
							<span>Response signal</span>
							<strong>412 responses · 33% complete</strong>
						</div>
						<svg viewBox="0 0 520 220" role="img" aria-label="Response trend chart">
							<defs>
								<linearGradient id="trend-fill" x1="0" x2="0" y1="0" y2="1">
									<stop offset="0%" stop-color="currentColor" stop-opacity="0.22" />
									<stop offset="100%" stop-color="currentColor" stop-opacity="0" />
								</linearGradient>
							</defs>
							<path class="chart-grid" d="M20 48H500M20 98H500M20 148H500M20 198H500" />
							<path class="chart-area" d="M24 184C70 176 90 154 128 148C176 141 190 96 238 102C298 110 322 62 372 70C420 78 440 42 496 36V204H24Z" />
							<path class="chart-line" d="M24 184C70 176 90 154 128 148C176 141 190 96 238 102C298 110 322 62 372 70C420 78 440 42 496 36" />
							<circle cx="496" cy="36" r="7" />
						</svg>
					</section>

					<div class="showcase-grid">
						<section class="showcase-panel" aria-label="Preparation state">
							<span>Prepare</span>
							<strong>4 / 5 checks ready</strong>
							<p>One policy gap blocks launch. Template, scoring, and audience are ready.</p>
						</section>
						<section class="showcase-panel showcase-panel--dark" aria-label="Export state">
							<span>Export</span>
							<strong>PDF + CSV ready</strong>
							<p>Exports keep source, finality, and suppression context.</p>
						</section>
					</div>
				</main>
			</div>
			<div class="floating-card floating-card--receipt" aria-hidden="true">
				<span>Respondent</span>
				<strong>Anonymous receipt</strong>
				<small>Tokenless return path · withdrawal ready</small>
			</div>
			<div class="floating-card floating-card--proof" aria-hidden="true">
				<span>Operations</span>
				<strong>Recovery posture ready</strong>
				<small>Backup · restore · rollback · release path</small>
			</div>
		</div>
	</section>

	<section class="proof-ribbon" id="proof" aria-label="Beta boundary">
		<div><span>Runtime</span><strong>Setup to collection, scoring, reports, and exports</strong></div>
		<div><span>Access</span><strong>Tenant-scoped authenticated workspace</strong></div>
		<div><span>Operations</span><strong>Backup, restore, and release recovery prepared</strong></div>
		<div><span>Beta boundary</span><strong>Demo and owner-controlled data only</strong></div>
	</section>

	<section class="suite-map" aria-labelledby="suite-map-title">
		<div class="suite-map__intro">
			<p class="launchpad-kicker">Workspace overview</p>
			<h2 id="suite-map-title">See what is ready, blocked, live, or ready to export.</h2>
			<p>
				Users should know what to fix next, where to go, and whether each study is still being prepared, collecting responses, or ready for results.
			</p>
		</div>

		<div class="suite-console" aria-label="App preview">
			<section class="command-card">
				<span>Needs attention</span>
				<strong>What needs attention before launch?</strong>
				<div class="command-card__input">Launch readiness: retention review date missing</div>
				<ul>
					<li><b>Policy gap</b><small>Retention review date missing</small></li>
					<li><b>Audience ready</b><small>Directory group resolves 1,240 subjects</small></li>
					<li><b>Export ready</b><small>CSV, codebook, and PDF exports available</small></li>
				</ul>
			</section>

			<section class="module-stack">
				<span>App areas</span>
				<div class="module-grid">
					<a href={resolve('/app/campaign-series')}><b>Studies</b><small>portfolio + lifecycle</small></a>
					<a href={resolve('/app/instruments')}><b>Instruments</b><small>rights + launch eligibility</small></a>
					<a href={resolve('/app/directory')}><b>Directory</b><small>subjects + hierarchy</small></a>
					<a href={resolve('/app/team')}><b>Team</b><small>roles + capabilities</small></a>
					<a href={resolve('/app/exports')}><b>Exports</b><small>artifacts + provenance</small></a>
					<a href={resolve('/app/settings')}><b>Settings</b><small>tenant profile</small></a>
				</div>
			</section>

			<section class="board-preview">
				<div class="board-preview__heading">
					<span>Study status</span>
					<strong>Study status</strong>
				</div>
				<div class="board-columns">
					<div>
						<h3>Prepare</h3>
						<p>Template ready</p>
						<p>Scoring ready</p>
						<p class="is-warning">Policy gap</p>
					</div>
					<div>
						<h3>Collect</h3>
						<p>Wave 2 live</p>
						<p>412 submitted</p>
						<p>Invite batch sent</p>
					</div>
					<div>
						<h3>Review</h3>
						<p>Safe aggregate reports</p>
						<p>PDF staged</p>
						<p>Wave comparison</p>
					</div>
				</div>
			</section>
		</div>
	</section>
	<section class="workflow" id="workflow" aria-labelledby="workflow-title">
		<div class="workflow__intro">
			<p class="launchpad-kicker">The app flow</p>
			<h2 id="workflow-title">Every screen should answer the obvious question first.</h2>
		</div>
		<div class="workflow-lane">
			<a href={resolve('/app/campaign-series')}>
				<span>01</span>
				<strong>Portfolio</strong>
				<p>Sample and own studies, with duplicate-to-edit instead of a separate demo island.</p>
			</a>
			<a href={resolve('/app/campaign-series')}>
				<span>02</span>
				<strong>Prepare</strong>
				<p>Instrument, scoring, policies, audience, launch readiness.</p>
			</a>
			<a href={resolve('/app')}>
				<span>03</span>
				<strong>Collect</strong>
				<p>Launch state, respondent access, delivery, response progress.</p>
			</a>
			<a href={resolve('/app/exports')}>
				<span>04</span>
				<strong>Review</strong>
				<p>Reports, suppression, PDFs, CSV/codebook, provenance.</p>
			</a>
		</div>
	</section>
</section>
