<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { env } from '$env/dynamic/public';
	import { ApiError } from '$lib/api/client';
	import {
		createLoginUrlFromEnv,
		readLastWorkspaceEmail,
		readLastTenantId,
		rememberLastWorkspaceEmail,
		rememberLastTenantId
	} from '$lib/api/session-headers';
	import { createRegistrationApi } from '$lib/api/registration';
	import { appLocaleFromPageData, localizedHref } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';

	const registrationApi = createRegistrationApi();
	let signInUrl = $state<string>(resolve('/signin'));
	const pendingRegistrationLoginUrlKey = 'instruments-platform.pending-registration-login-url';
	const pendingRegistrationStage = 'auth0-sign-in';
	const pendingRegistrationMaxAgeMs = 15 * 60 * 1000;
	const pendingRegistrationClockSkewMs = 60 * 1000;

	let email = $state('');
	let organizationName = $state('');
	let accessCode = $state('');
	let errorMessage = $state('');
	let sessionErrorMessage = $state('');
	let statusMessage = $state('');
	let isSubmitting = $state(false);
	let pendingEmail = $state('');
	let sessionState = $state<'checking' | 'signed-out' | 'ready'>('checking');
	let pendingRegistrationLoginUrl = $state('');
	let existingWorkspaceSignInUrl = $state('');
	const emailVerificationRequired = $derived(
		page.url.searchParams.get('auth') === 'email_unverified'
	);
	const emailMismatchRequired = $derived(page.url.searchParams.get('auth') === 'email_mismatch');
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const englishLocaleHref = $derived(localizedHref(page.url, 'en'));
	const croatianLocaleHref = $derived(localizedHref(page.url, 'hr-HR'));

	onMount(() => {
		const tenantId = readLastTenantId(window.localStorage);
		signInUrl = tenantId
			? createLoginUrlFromEnv(env, tenantId, readLastWorkspaceEmail(window.localStorage))
			: resolve('/signin');
		loadPendingRegistrationLoginUrl();
		void loadRegistrationSession();
	});

	async function loadRegistrationSession() {
		errorMessage = '';
		sessionErrorMessage = '';

		try {
			const session = await registrationApi.getSession();
			pendingEmail = session.email;
			sessionState = 'ready';
			clearPendingRegistrationLoginUrl();
		} catch (error) {
			if (error instanceof ApiError && error.status === 401) {
				sessionState = 'signed-out';
				return;
			}

			sessionState = 'signed-out';
			sessionErrorMessage =
				text.register.sessionError;
		}
	}

	async function submitRegistrationIntent(event: SubmitEvent) {
		event.preventDefault();
		errorMessage = '';
		statusMessage = '';
		existingWorkspaceSignInUrl = '';
		isSubmitting = true;

		try {
			const response = await registrationApi.createIntent({
				email,
				organizationName,
				accessCode,
				returnUrl: absoluteWebUrl(resolve('/app'))
			});
			const loginUrl = resolveAuthRedirectUrl(response.loginUrl);

			storePendingRegistrationLoginUrl(toRegistrationContinuationUrl(loginUrl));
			statusMessage = text.register.openingAccount;
			window.location.assign(loginUrl);
		} catch (error) {
			const signInUrl = getExistingWorkspaceSignInUrl(error);
			if (signInUrl) {
				existingWorkspaceSignInUrl = signInUrl;
			}
			errorMessage = toRegistrationError(error);
			isSubmitting = false;
		}
	}

	async function submitWorkspace(event: SubmitEvent) {
		event.preventDefault();
		errorMessage = '';
		statusMessage = '';
		isSubmitting = true;

		try {
			const response = await registrationApi.createWorkspace({
				organizationName,
				accessCode,
				returnUrl: absoluteWebUrl(resolve('/app'))
			});

			rememberLastTenantId(window.localStorage, response.tenantId);
			rememberLastWorkspaceEmail(window.localStorage, response.email);
			statusMessage = text.register.workspaceCreated;
			window.location.assign(withTenantId(response.appUrl, response.tenantId));
		} catch (error) {
			errorMessage = toRegistrationError(error);
			isSubmitting = false;
		}
	}

	function withTenantId(url: string, tenantId: string) {
		const parsedUrl = new URL(url, window.location.origin);
		parsedUrl.searchParams.set('tenantId', tenantId);

		if (/^https?:\/\//i.test(url)) {
			return parsedUrl.toString();
		}

		return `${parsedUrl.pathname}${parsedUrl.search}${parsedUrl.hash}`;
	}

	function loadPendingRegistrationLoginUrl() {
		const loginUrl = readPendingRegistrationLoginUrl();
		pendingRegistrationLoginUrl = loginUrl;

		if (!loginUrl) {
			window.sessionStorage.removeItem(pendingRegistrationLoginUrlKey);
		}
	}

	function storePendingRegistrationLoginUrl(loginUrl: string) {
		pendingRegistrationLoginUrl = loginUrl;
		window.sessionStorage.setItem(
			pendingRegistrationLoginUrlKey,
			JSON.stringify({
				loginUrl,
				createdAt: Date.now(),
				stage: pendingRegistrationStage
			})
		);
	}

	function readPendingRegistrationLoginUrl() {
		const value = window.sessionStorage.getItem(pendingRegistrationLoginUrlKey);
		if (!value) {
			return '';
		}

		try {
			const metadata = JSON.parse(value) as {
				loginUrl?: unknown;
				createdAt?: unknown;
				stage?: unknown;
			};

			if (
				typeof metadata.loginUrl === 'string' &&
				typeof metadata.createdAt === 'number' &&
				metadata.stage === pendingRegistrationStage &&
				isRecentPendingRegistration(metadata.createdAt) &&
				isStructurallyValidPendingRegistrationUrl(metadata.loginUrl)
			) {
				return metadata.loginUrl;
			}
		} catch {
			return '';
		}

		return '';
	}

	function isRecentPendingRegistration(createdAt: number) {
		const ageMs = Date.now() - createdAt;
		return (
			Number.isFinite(createdAt) &&
			ageMs >= -pendingRegistrationClockSkewMs &&
			ageMs <= pendingRegistrationMaxAgeMs
		);
	}

	function isStructurallyValidPendingRegistrationUrl(loginUrl: string) {
		try {
			const url = new URL(loginUrl, window.location.origin);
			return url.searchParams.has('registrationToken') && url.searchParams.has('returnUrl');
		} catch {
			return false;
		}
	}

	function clearPendingRegistrationLoginUrl() {
		pendingRegistrationLoginUrl = '';
		window.sessionStorage.removeItem(pendingRegistrationLoginUrlKey);
	}

	function clearExistingWorkspaceSignIn() {
		existingWorkspaceSignInUrl = '';
		errorMessage = '';
		statusMessage = '';
	}

	function restartRegistration() {
		clearPendingRegistrationLoginUrl();
		clearExistingWorkspaceSignIn();
		errorMessage = '';
		sessionErrorMessage = '';
		statusMessage = '';
	}

	function absoluteWebUrl(path: string) {
		return new URL(path, window.location.origin).toString();
	}

	function resolveAuthRedirectUrl(loginUrl: string) {
		if (/^https?:\/\//i.test(loginUrl)) {
			return loginUrl;
		}

		const authOrigin = absoluteOrigin(env.PUBLIC_AUTH_LOGIN_URL);
		if (authOrigin) {
			return new URL(loginUrl, authOrigin).toString();
		}

		const apiOrigin = absoluteOrigin(env.PUBLIC_API_BASE_URL);
		if (apiOrigin) {
			return new URL(loginUrl, apiOrigin).toString();
		}

		return loginUrl;
	}

	function toRegistrationContinuationUrl(loginUrl: string) {
		try {
			const url = new URL(loginUrl, window.location.origin);
			url.searchParams.delete('screen_hint');
			return url.toString();
		} catch {
			return loginUrl;
		}
	}

	function getExistingWorkspaceSignInUrl(error: unknown) {
		if (!(error instanceof ApiError)) {
			return '';
		}

		const problem = error.body as { title?: unknown; loginUrl?: unknown } | null;
		if (
			typeof problem?.title === 'string' &&
			problem.title === 'registration.email_exists' &&
			typeof problem.loginUrl === 'string' &&
			problem.loginUrl.length > 0
		) {
			return resolveAuthRedirectUrl(problem.loginUrl);
		}

		return '';
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

	function toRegistrationError(error: unknown) {
		if (error instanceof ApiError) {
			const problem = error.body as { title?: unknown; detail?: unknown } | null;
			const code = typeof problem?.title === 'string' ? problem.title : '';
			const detail = typeof problem?.detail === 'string' ? problem.detail : '';

			if (code === 'registration.disabled') {
				return text.register.disabled;
			}

			if (error.status === 401) {
				return text.register.expired;
			}

			if (error.status === 403) {
				return text.register.forbidden;
			}

			if (error.status === 409) {
				return text.register.conflict;
			}

			if (code === 'registration.invalid_access_code') {
				return text.register.invalidCode;
			}

			if (code === 'registration.email_exists') {
				return text.register.emailExists;
			}

			if (code === 'registration.organization_invalid') {
				return text.register.organizationInvalid;
			}

			if (detail) {
				return detail;
			}
		}

		return text.register.fallbackError;
	}
</script>

<svelte:head>
	<title>{text.register.metaTitle}</title>
	<meta
		name="description"
		content={text.register.metaDescription}
	/>
</svelte:head>

<section class="registration-page" aria-labelledby="registration-title">
	<div class="registration-page__glow registration-page__glow--one" aria-hidden="true"></div>
	<div class="registration-page__glow registration-page__glow--two" aria-hidden="true"></div>

	<header class="public-nav">
		<a class="launchpad-brand" href={resolve('/')}>
			<img class="launchpad-brand__mark" src="/brand/validated-scale-mark.svg" alt="" aria-hidden="true" />
			<span>
				<strong>Validated Scale</strong>
				<small>{text.register.brandSubtitle}</small>
			</span>
		</a>
		<div class="public-nav__actions">
			<nav class="public-nav__links" aria-label={text.register.navAria}>
				<a href={resolve('/')}>{text.common.product}</a>
				<a href={signInUrl}>{text.common.signIn}</a>
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
		</div>
	</header>

	<div class="registration-grid">
		<section class="registration-copy">
			<p class="launchpad-kicker">{text.register.eyebrow}</p>
			<h1 id="registration-title">{text.register.title}</h1>
			<p>
				{text.register.body}
			</p>
			<div class="registration-steps" aria-label={text.register.stepsAria}>
				<div>
					<span>01</span>
					<strong>{text.register.stepCreate}</strong>
					<p>{text.register.stepCreateBody}</p>
				</div>
				<div>
					<span>02</span>
					<strong>{text.register.stepVerify}</strong>
					<p>
						{text.register.stepVerifyBody}
					</p>
				</div>
				<div>
					<span>03</span>
					<strong>{text.register.stepOpen}</strong>
					<p>
						{text.register.stepOpenBody}
					</p>
				</div>
			</div>
		</section>

		<section class="registration-panel" aria-label={text.register.formAria}>
			<div class="registration-panel__header">
				<span>{text.register.panelEyebrow}</span>
				<strong>{text.register.panelTitle}</strong>
				<p>{text.register.alreadyHave} <a href={signInUrl}>{text.register.signInInstead}</a>.</p>
			</div>

			{#if sessionState === 'checking'}
				<p class="registration-alert" role="status">{text.register.checkingAccount}</p>
			{:else if sessionState === 'signed-out'}
				<form class="registration-form" onsubmit={submitRegistrationIntent}>
					{#if sessionErrorMessage}
						<p class="registration-alert registration-alert--error" role="alert">
							{sessionErrorMessage}
						</p>
					{/if}

					{#if (emailVerificationRequired || emailMismatchRequired) && pendingRegistrationLoginUrl}
						<div class="registration-alert" role="status">
							<strong>
								{emailMismatchRequired
									? text.register.chooseStartedAccount
									: text.register.verifyThenSignIn}
							</strong>
							<span>
								{emailMismatchRequired
									? text.register.mismatchBody
									: text.register.verifyBody}
							</span>
						</div>
						<a class="registration-submit" href={pendingRegistrationLoginUrl}
							>{text.register.retryRegistration}</a
						>
						<button class="secondary-button" type="button" onclick={restartRegistration}>
							{text.register.startOver}
						</button>
					{:else}
						<label>
							<span>{text.common.email}</span>
							<input
								type="email"
								bind:value={email}
								autocomplete="email"
								placeholder="you@example.com"
								required
							/>
						</label>

						<label>
							<span>{text.register.workspaceName}</span>
							<input
								type="text"
								bind:value={organizationName}
								autocomplete="organization"
								placeholder={text.register.workspacePlaceholder}
								required
							/>
						</label>

						<label>
							<span>{text.register.betaAccessCode}</span>
							<input
								type="password"
								bind:value={accessCode}
								autocomplete="one-time-code"
								placeholder={text.register.accessCodePlaceholder}
								required
							/>
						</label>

						{#if errorMessage}
							<p class="registration-alert registration-alert--error" role="alert">
								{errorMessage}
							</p>
						{/if}

						{#if statusMessage}
							<p class="registration-alert registration-alert--success" role="status">
								{statusMessage}
							</p>
						{/if}

						{#if existingWorkspaceSignInUrl}
							<a class="registration-submit" href={existingWorkspaceSignInUrl}>{text.register.signInInstead}</a>
							<button class="secondary-button" type="button" onclick={clearExistingWorkspaceSignIn}>
								{text.register.useDifferentEmail}
							</button>
						{:else}
							<button class="registration-submit" type="submit" disabled={isSubmitting}>
								{isSubmitting ? text.register.openingAccountButton : text.register.createAccount}
							</button>
						{/if}
					{/if}
				</form>
			{:else}
				<form class="registration-form" onsubmit={submitWorkspace}>
					<div class="registration-alert" role="status">
						{text.register.accountReady(pendingEmail)}
					</div>

					<label>
						<span>{text.register.workspaceName}</span>
						<input
							type="text"
							bind:value={organizationName}
							autocomplete="organization"
							placeholder={text.register.workspacePlaceholder}
							required
						/>
					</label>

					<label>
						<span>{text.register.betaAccessCode}</span>
						<input
							type="password"
							bind:value={accessCode}
							autocomplete="one-time-code"
							placeholder={text.register.accessCodePlaceholder}
							required
						/>
					</label>

					{#if errorMessage}
						<p class="registration-alert registration-alert--error" role="alert">{errorMessage}</p>
					{/if}

					{#if statusMessage}
						<p class="registration-alert registration-alert--success" role="status">
							{statusMessage}
						</p>
					{/if}

					<button class="registration-submit" type="submit" disabled={isSubmitting}>
						{isSubmitting ? text.register.creatingWorkspace : text.common.createWorkspace}
					</button>
				</form>
			{/if}

			<div class="registration-boundary">
				<strong>{text.register.boundaryTitle}</strong>
				<p>
					{text.register.boundaryBody}
				</p>
			</div>
		</section>
	</div>
</section>
