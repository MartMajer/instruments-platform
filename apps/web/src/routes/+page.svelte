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
	let mobileEntryMenuOpen = $state(false);
	let loginUrl = $state(
		initialTenantIdFromUrl ? createLoginUrlFromEnv(env, initialTenantIdFromUrl) : workspaceSignInUrl
	);

	onMount(() => {
		const storedTenantId = readLastTenantId(window.localStorage);
		const tenantId = tenantIdFromUrl || storedTenantId;
		const loginHint =
			tenantId && tenantId === storedTenantId ? readLastWorkspaceEmail(window.localStorage) : '';

		if (tenantIdFromUrl) {
			rememberLastTenantId(window.localStorage, tenantIdFromUrl);
		}

		loginUrl = tenantId ? createLoginUrlFromEnv(env, tenantId, loginHint) : workspaceSignInUrl;

		if (page.url.searchParams.get('postLogout') !== 'register') {
			return;
		}

		const registrationReturnUrl = encodeURIComponent(absoluteWebUrl(resolve('/register')));
		window.location.replace(
			resolveAuthRedirectUrl(`/auth/login?registration=1&returnUrl=${registrationReturnUrl}`)
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
	<title>Instruments Platform | Research study operations</title>
	<meta
		name="description"
		content="EU-hosted private-beta workspace for study setup, response collection, results review, and exports."
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
				<small>Research studies and wellbeing programs</small>
			</span>
		</a>
		<nav class="launchpad-nav__links" aria-label="Product entry actions">
			<a href="#workflow">Workflow</a>
			<a href="#trust">Trust model</a>
			<a href={resolve('/register')}>Create workspace</a>
			<a href={loginUrl}>Sign in</a>
		</nav>
		<button
			type="button"
			class="launchpad-nav__menu"
			aria-label={mobileEntryMenuOpen ? 'Close menu' : 'Open menu'}
			aria-expanded={mobileEntryMenuOpen}
			onclick={() => (mobileEntryMenuOpen = !mobileEntryMenuOpen)}
		>
			Menu
		</button>
	</header>
	{#if mobileEntryMenuOpen}
		<nav class="launchpad-mobile-menu" aria-label="Mobile product entry actions">
			<a href="#workflow">Workflow</a>
			<a href="#trust">Trust model</a>
			<a href={resolve('/register')}>Create workspace</a>
			<a href={loginUrl}>Sign in</a>
		</nav>
	{/if}

	<section class="launchpad-hero">
		<div class="launchpad-hero__copy">
			<p class="launchpad-kicker">EU-hosted workspace for research and wellbeing studies</p>
			<h1 id="product-entry-title">Run research studies from setup to defensible results.</h1>
			<p>
				Build questionnaires, collect anonymous or identified responses, review scoring context, and
				export datasets without stitching together forms, spreadsheets, scripts, and screenshots.
			</p>
			<div class="launchpad-actions">
				<a class="launchpad-button launchpad-button--primary" href={resolve('/register')}
					>Create workspace</a
				>
				<a class="launchpad-button launchpad-button--secondary" href={loginUrl}>Sign in</a>
			</div>
		</div>

		<div class="showcase" aria-label="Product preview">
			<div class="showcase__chrome" aria-hidden="true">
				<span></span><span></span><span></span>
				<strong>workspace / study operations</strong>
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
							<path
								class="chart-area"
								d="M24 184C70 176 90 154 128 148C176 141 190 96 238 102C298 110 322 62 372 70C420 78 440 42 496 36V204H24Z"
							/>
							<path
								class="chart-line"
								d="M24 184C70 176 90 154 128 148C176 141 190 96 238 102C298 110 322 62 372 70C420 78 440 42 496 36"
							/>
							<circle cx="496" cy="36" r="7" />
						</svg>
					</section>

					<div class="showcase-grid">
						<section class="showcase-panel" aria-label="Preparation state">
							<span>Prepare</span>
							<strong>Launch checklist</strong>
							<p>
								Questionnaire, scoring, audience, and collection settings stay visible before
								launch.
							</p>
						</section>
						<section class="showcase-panel showcase-panel--dark" aria-label="Export state">
							<span>Export</span>
							<strong>Dataset + codebook</strong>
							<p>Exports keep source, finality, and suppression context attached to the file.</p>
						</section>
					</div>
				</main>
			</div>
			<div class="floating-card floating-card--receipt" aria-hidden="true">
				<span>Respondent</span>
				<strong>Private response receipt</strong>
				<small>Completion state without exposing respondent identity</small>
			</div>
			<div class="floating-card floating-card--proof" aria-hidden="true">
				<span>Governance</span>
				<strong>Study context preserved</strong>
				<small>Consent, retention, finality, and provenance</small>
			</div>
		</div>
	</section>

	<section class="proof-ribbon" id="trust" aria-label="Trust model">
		<div>
			<span>Workflow</span><strong>Setup, collection, scoring, reports, and exports</strong>
		</div>
		<div><span>Access</span><strong>Tenant-scoped authenticated workspaces</strong></div>
		<div>
			<span>Data controls</span><strong>Consent, retention, finality, and export provenance</strong>
		</div>
		<div><span>Product stage</span><strong>Private beta with staged onboarding</strong></div>
	</section>

	<section class="suite-map" aria-labelledby="suite-map-title">
		<div class="suite-map__intro">
			<p class="launchpad-kicker">Workspace overview</p>
			<h2 id="suite-map-title">See what is ready, blocked, live, or ready to export.</h2>
			<p>
				Keep every study's next action visible: preparation gaps, live collection, result review,
				and export readiness.
			</p>
		</div>

		<div class="suite-console" aria-label="App preview">
			<section class="command-card">
				<span>Next action</span>
				<strong>What should the team do next?</strong>
				<div class="command-card__input">Launch readiness: review retention setting</div>
				<ul>
					<li><b>Review setting</b><small>Retention date needs confirmation</small></li>
					<li><b>Audience prepared</b><small>Directory group resolves 1,240 subjects</small></li>
					<li>
						<b>Export plan defined</b><small>Dataset and codebook outputs are selected</small>
					</li>
				</ul>
			</section>

			<section class="module-stack">
				<span>App areas</span>
				<div class="module-grid">
					<a href={resolve('/app/campaign-series')}
						><b>Studies</b><small>portfolio + lifecycle</small></a
					>
					<a href={resolve('/app/instruments')}
						><b>Instruments</b><small>available study content</small></a
					>
					<a href={resolve('/app/directory')}><b>Directory</b><small>subjects + hierarchy</small></a
					>
					<a href={resolve('/app/team')}><b>Team</b><small>roles + capabilities</small></a>
					<a href={resolve('/app/exports')}><b>Exports</b><small>files + provenance</small></a>
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
						<p>Questionnaire ready</p>
						<p>Scoring configured</p>
						<p class="is-warning">Retention review</p>
					</div>
					<div>
						<h3>Collect</h3>
						<p>Wave 2 live</p>
						<p>412 submitted</p>
						<p>Delivery monitored</p>
					</div>
					<div>
						<h3>Review</h3>
						<p>Aggregate reports</p>
						<p>Codebook export</p>
						<p>Wave comparison</p>
					</div>
				</div>
			</section>
		</div>
	</section>
	<section class="workflow" id="workflow" aria-labelledby="workflow-title">
		<div class="workflow__intro">
			<p class="launchpad-kicker">Study workflow</p>
			<h2 id="workflow-title">A clear path from study design to reusable evidence.</h2>
		</div>
		<div class="workflow-lane">
			<a href={resolve('/app/campaign-series')}>
				<span>01</span>
				<strong>Portfolio</strong>
				<p>Create, compare, and return to active study programs from one workspace.</p>
			</a>
			<a href={resolve('/app/campaign-series')}>
				<span>02</span>
				<strong>Prepare</strong>
				<p>Define questionnaire, scoring, policies, audience, and launch checks.</p>
			</a>
			<a href={resolve('/app')}>
				<span>03</span>
				<strong>Collect</strong>
				<p>Open links or invite lists, track response progress, and monitor delivery.</p>
			</a>
			<a href={resolve('/app/exports')}>
				<span>04</span>
				<strong>Review</strong>
				<p>Inspect reports, compare waves, and export datasets with provenance.</p>
			</a>
		</div>
	</section>
</section>
