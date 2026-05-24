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
	import { appLocaleFromPageData, localizedHref } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';

	const initialTenantIdFromUrl = normalizeTenantId(page.url.searchParams.get('tenantId'));
	const tenantIdFromUrl = $derived(normalizeTenantId(page.url.searchParams.get('tenantId')));
	const workspaceSignInUrl = resolve('/signin');
	let mobileEntryMenuOpen = $state(false);
	let loginUrl = $state(
		initialTenantIdFromUrl ? createLoginUrlFromEnv(env, initialTenantIdFromUrl) : workspaceSignInUrl
	);
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const englishLocaleHref = $derived(localizedHref(page.url, 'en'));
	const croatianLocaleHref = $derived(localizedHref(page.url, 'hr-HR'));

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
	<title>{text.publicEntry.metaTitle}</title>
	<meta
		name="description"
		content={text.publicEntry.metaDescription}
	/>
</svelte:head>

<section class="launchpad" aria-labelledby="product-entry-title">
	<div class="launchpad__glow launchpad__glow--one" aria-hidden="true"></div>
	<div class="launchpad__glow launchpad__glow--two" aria-hidden="true"></div>

	<header class="public-nav public-nav--home">
		<a class="launchpad-brand" href={resolve('/')}>
			<span class="launchpad-brand__mark" aria-hidden="true">IP</span>
			<span>
				<strong>Instruments Platform</strong>
				<small>{text.publicEntry.brandSubtitle}</small>
			</span>
		</a>
		<div class="public-nav__actions">
			<nav class="public-nav__links" aria-label={text.publicEntry.navAria}>
				<a href="#workflow">{text.publicEntry.workflow}</a>
				<a href="#trust">{text.publicEntry.trustModel}</a>
				<a href={resolve('/register')}>{text.common.createWorkspace}</a>
				<a href={loginUrl}>{text.common.signIn}</a>
			</nav>
			<div class="public-language-switcher" aria-label={text.publicEntry.languageSwitchAria}>
				<a
					class={`public-language-switcher__option${locale === 'en' ? ' public-language-switcher__option--active' : ''}`}
					href={englishLocaleHref}
					hreflang="en"
					aria-current={locale === 'en' ? 'true' : undefined}>EN</a
				>
				<a
					class={`public-language-switcher__option${locale === 'hr-HR' ? ' public-language-switcher__option--active' : ''}`}
					href={croatianLocaleHref}
					hreflang="hr-HR"
					aria-current={locale === 'hr-HR' ? 'true' : undefined}>HR</a
				>
			</div>
			<button
				type="button"
				class="public-nav__menu"
				aria-label={mobileEntryMenuOpen ? text.publicEntry.closeMenu : text.publicEntry.openMenu}
				aria-expanded={mobileEntryMenuOpen}
				onclick={() => (mobileEntryMenuOpen = !mobileEntryMenuOpen)}
			>
				<span class="public-nav__menu-icon" aria-hidden="true">
					<span></span>
					<span></span>
					<span></span>
				</span>
			</button>
		</div>
	</header>
	{#if mobileEntryMenuOpen}
		<nav class="public-mobile-menu" aria-label={text.publicEntry.mobileNavAria}>
			<a href="#workflow">{text.publicEntry.workflow}</a>
			<a href="#trust">{text.publicEntry.trustModel}</a>
			<a href={resolve('/register')}>{text.common.createWorkspace}</a>
			<a href={loginUrl}>{text.common.signIn}</a>
		</nav>
	{/if}

	<section class="launchpad-hero">
		<div class="launchpad-hero__copy">
			<p class="launchpad-kicker">{text.publicEntry.heroKicker}</p>
			<h1 id="product-entry-title">{text.publicEntry.heroTitle}</h1>
			<p>
				{text.publicEntry.heroBody}
			</p>
			<div class="launchpad-actions">
				<a class="launchpad-button launchpad-button--primary" href={resolve('/register')}
					>{text.common.createWorkspace}</a
				>
				<a class="launchpad-button launchpad-button--secondary" href={loginUrl}>{text.common.signIn}</a>
			</div>
		</div>

		<div class="showcase" aria-label={text.publicEntry.previewAria}>
			<div class="showcase__chrome" aria-hidden="true">
				<span></span><span></span><span></span>
				<strong>{text.publicEntry.previewChrome}</strong>
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
							<span>{text.publicEntry.selectedStudy}</span>
							<h2>{text.publicEntry.previewStudyName}</h2>
						</div>
						<strong>{text.publicEntry.liveCollection}</strong>
					</div>

					<section class="showcase-panel showcase-panel--chart" aria-label={text.publicEntry.responseSignal}>
						<div class="showcase-panel__heading">
							<span>{text.publicEntry.responseSignal}</span>
							<strong>{text.publicEntry.responseProgress}</strong>
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
							<span>{text.publicEntry.prepare}</span>
							<strong>{text.publicEntry.launchChecklist}</strong>
							<p>
								{text.publicEntry.prepareBody}
							</p>
						</section>
						<section class="showcase-panel showcase-panel--dark" aria-label="Export state">
							<span>{text.publicEntry.export}</span>
							<strong>{text.publicEntry.datasetCodebook}</strong>
							<p>{text.publicEntry.exportBody}</p>
						</section>
					</div>
				</main>
			</div>
			<div class="floating-card floating-card--receipt" aria-hidden="true">
				<span>Respondent</span>
				<strong>Response receipt</strong>
				<small>Completion state without exposing identity</small>
			</div>
			<div class="floating-card floating-card--proof" aria-hidden="true">
				<span>Governance</span>
				<strong>Study context preserved</strong>
				<small>Consent, retention, finality, and provenance</small>
			</div>
		</div>
	</section>

	<section class="proof-ribbon" id="trust" aria-label={text.publicEntry.trustAria}>
		<div>
			<span>{text.publicEntry.workflow}</span><strong>{text.publicEntry.workflowRibbon}</strong>
		</div>
		<div><span>{text.publicEntry.access}</span><strong>{text.publicEntry.accessRibbon}</strong></div>
		<div>
			<span>{text.publicEntry.dataControls}</span><strong>{text.publicEntry.dataControlsRibbon}</strong>
		</div>
		<div><span>{text.publicEntry.productStage}</span><strong>{text.publicEntry.productStageRibbon}</strong></div>
	</section>

	<section class="suite-map" aria-labelledby="suite-map-title">
		<div class="suite-map__intro">
			<p class="launchpad-kicker">{text.publicEntry.workspaceOverview}</p>
			<h2 id="suite-map-title">{text.publicEntry.suiteTitle}</h2>
			<p>
				{text.publicEntry.suiteBody}
			</p>
		</div>

		<div class="suite-console" aria-label={text.publicEntry.previewAria}>
			<section class="command-card">
				<span>{text.publicEntry.nextAction}</span>
				<strong>{text.publicEntry.nextActionQuestion}</strong>
				<div class="command-card__input">Launch readiness: review retention setting</div>
				<ul>
					<li><b>Review setting</b><small>Retention date needs confirmation</small></li>
					<li><b>Audience prepared</b><small>Directory group resolves 1,240 people</small></li>
					<li>
						<b>Export plan defined</b><small>Dataset and codebook outputs are selected</small>
					</li>
				</ul>
			</section>

			<section class="module-stack">
				<span>{text.publicEntry.appAreas}</span>
				<div class="module-grid">
					<a href={resolve('/app/campaign-series')}
						><b>Studies</b><small>portfolio + lifecycle</small></a
					>
					<a href={resolve('/app/instruments')}
						><b>Instruments</b><small>available study content</small></a
					>
					<a href={resolve('/app/directory')}><b>Directory</b><small>people + groups</small></a
					>
					<a href={resolve('/app/team')}><b>Team</b><small>roles + capabilities</small></a>
					<a href={resolve('/app/exports')}><b>Exports</b><small>files + provenance</small></a>
					<a href={resolve('/app/settings')}><b>Settings</b><small>study defaults</small></a>
				</div>
			</section>

			<section class="board-preview">
				<div class="board-preview__heading">
					<span>{text.publicEntry.studyStatus}</span>
					<strong>{text.publicEntry.studyStatus}</strong>
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
			<p class="launchpad-kicker">{text.publicEntry.workflow}</p>
			<h2 id="workflow-title">{text.publicEntry.workflowTitle}</h2>
		</div>
		<div class="workflow-lane">
			<a href={resolve('/app/campaign-series')}>
				<span>01</span>
				<strong>{text.publicEntry.portfolio}</strong>
				<p>{text.publicEntry.portfolioBody}</p>
			</a>
			<a href={resolve('/app/campaign-series')}>
				<span>02</span>
				<strong>{text.publicEntry.prepare}</strong>
				<p>{text.publicEntry.prepareStepBody}</p>
			</a>
			<a href={resolve('/app')}>
				<span>03</span>
				<strong>{text.publicEntry.previewCollect}</strong>
				<p>{text.publicEntry.collectStepBody}</p>
			</a>
			<a href={resolve('/app/exports')}>
				<span>04</span>
				<strong>{text.publicEntry.previewResults}</strong>
				<p>{text.publicEntry.reviewStepBody}</p>
			</a>
		</div>
	</section>
</section>
