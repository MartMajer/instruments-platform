<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { ApiError, createApiClient } from '$lib/api/client';
	import {
		createLoginUrlFromEnv,
		createLogoutUrlFromEnv,
		createSessionHeadersFromEnv,
		readLastWorkspaceEmail,
		readLastTenantId,
		rememberLastWorkspaceEmail,
		rememberLastTenantId,
		normalizeTenantId
	} from '$lib/api/session-headers';
	import { createSetupApi, type AuthSessionResponse } from '$lib/api/setup';
	import AppShell from '$lib/components/AppShell.svelte';
	import { createProductAuthContext, setProductAuthContext } from '$lib/product/auth-context';
	import { toSessionProfileView } from '$lib/product/session-profile';

	type AuthState = 'checking' | 'authenticated' | 'unauthenticated' | 'forbidden' | 'failed';

	const initialTenantIdFromUrl = normalizeTenantId(page.url.searchParams.get('tenantId'));
	const tenantIdFromUrl = $derived(normalizeTenantId(page.url.searchParams.get('tenantId')));
	let loginUrl = $state(createLoginUrlFromEnv(env, initialTenantIdFromUrl));
	const logoutUrl = createLogoutUrlFromEnv(env);
	const pendingRegistrationLoginUrlKey = 'instruments-platform.pending-registration-login-url';
	const pendingRegistrationStage = 'auth0-sign-in';
	const pendingRegistrationMaxAgeMs = 15 * 60 * 1000;
	const pendingRegistrationClockSkewMs = 60 * 1000;
	const hasTenantLoginTarget = $derived(/[?&]tenantId=/.test(loginUrl));
	const primaryAuthActionUrl = $derived(hasTenantLoginTarget ? loginUrl : resolve('/signin'));
	const primaryAuthActionLabel = $derived(
		hasTenantLoginTarget ? 'Sign in' : 'Sign in to existing workspace'
	);
	const completeLogoutUrl = $derived(createProviderLogoutUrl(resolve('/')));
	const authFailedPrimaryUrl = $derived(pendingRegistrationLoginUrl || primaryAuthActionUrl);
	const emailVerificationSignInUrl = $derived(withPromptLogin(authFailedPrimaryUrl));
	const authFailedPrimaryLabel = $derived(
		pendingRegistrationLoginUrl
			? 'Try registration sign-in again'
			: hasTenantLoginTarget
			? 'Sign in to existing workspace'
			: primaryAuthActionLabel
	);
	const authFailedHasPendingRegistration = $derived(pendingRegistrationLoginUrl.length > 0);

	const setupApi = createSetupApi(
		createApiClient({
			defaultHeaders: createSessionHeadersFromEnv(env),
			csrf: true
		})
	);
	const authContext = setProductAuthContext(createProductAuthContext());

	let { children } = $props();

	let authState = $state<AuthState>('checking');
	let authSession = $state<AuthSessionResponse | null>(null);
	let authMessage = $state<string | null>(null);
	let pendingRegistrationLoginUrl = $state('');
	const workspaceLogoutUrl = $derived(createWorkspaceLogoutUrl(authSession?.tenantId));
	const sessionProfile = $derived(authSession ? toSessionProfileView(authSession) : null);
	const authFailureReason = $derived(page.url.searchParams.get('auth'));
	const authFailedRedirect = $derived(authFailureReason === 'failed');
	const authEmailUnverifiedRedirect = $derived(authFailureReason === 'email_unverified');
	const authEmailMismatchRedirect = $derived(authFailureReason === 'email_mismatch');
	const authRecoveryRedirect = $derived(
		authFailedRedirect || authEmailUnverifiedRedirect || authEmailMismatchRedirect
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

		loginUrl = createLoginUrlFromEnv(env, tenantId, loginHint);
		loadPendingRegistrationLoginUrl();
		void checkSession();
	});

	async function checkSession() {
		authState = 'checking';
		authMessage = null;

		try {
			authSession = await setupApi.getCurrentSession();
			authContext.session.set(authSession);
			rememberLastTenantId(window.localStorage, authSession.tenantId);
			rememberLastWorkspaceEmail(window.localStorage, authSession.email);
			loginUrl = createLoginUrlFromEnv(env, authSession.tenantId, authSession.email);
			authState = 'authenticated';
			clearAuthFailureMarker();
			clearPendingRegistrationLoginUrl();
		} catch (error) {
			authSession = null;
			authContext.session.set(null);

			if (error instanceof ApiError) {
				if (error.status === 401) {
					authState = 'unauthenticated';
					return;
				}

				if (error.status === 403) {
					authState = 'forbidden';
					return;
				}
			}

			authState = 'failed';
			authMessage = formatError(error);
		}
	}

	function formatError(error: unknown) {
		if (error instanceof ApiError) {
			if (error.status >= 500) {
				return 'The API could not confirm workspace access. Retry, then sign out and sign in again if it continues.';
			}

			return 'The app could not confirm workspace access. Sign out and sign in again if retry does not recover.';
		}

		if (error instanceof Error) {
			return error.message;
		}

		return 'Session check failed.';
	}

	function clearAuthFailureMarker() {
		if (!authRecoveryRedirect) {
			return;
		}

		const nextUrl = new URL(page.url);
		nextUrl.searchParams.delete('auth');

		window.history.replaceState(
			window.history.state,
			'',
			`${nextUrl.pathname}${nextUrl.search}${nextUrl.hash}`
		);
	}

	function loadPendingRegistrationLoginUrl() {
		const loginUrl = readPendingRegistrationLoginUrl();
		pendingRegistrationLoginUrl = loginUrl;

		if (!loginUrl) {
			window.sessionStorage.removeItem(pendingRegistrationLoginUrlKey);
		}
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
			const url = new URL(loginUrl, page.url.origin);
			return url.searchParams.has('registrationToken') && url.searchParams.has('returnUrl');
		} catch {
			return false;
		}
	}

	function clearPendingRegistrationLoginUrl() {
		pendingRegistrationLoginUrl = '';
		window.sessionStorage.removeItem(pendingRegistrationLoginUrlKey);
	}

	function withPromptLogin(url: string) {
		try {
			const parsedUrl = new URL(url, page.url.origin);
			if (parsedUrl.pathname !== '/auth/login') {
				return url;
			}

			parsedUrl.searchParams.set('prompt', 'login');

			if (/^https?:\/\//i.test(url)) {
				return parsedUrl.toString();
			}

			return `${parsedUrl.pathname}${parsedUrl.search}${parsedUrl.hash}`;
		} catch {
			return url;
		}
	}

	function createProviderLogoutUrl(returnPath: string) {
		const providerLogoutUrl = new URL(logoutUrl, page.url.origin);
		const returnUrl = new URL(returnPath, page.url.origin).toString();
		providerLogoutUrl.searchParams.set('provider', '1');
		providerLogoutUrl.searchParams.set('returnUrl', returnUrl);

		if (/^https?:\/\//i.test(logoutUrl)) {
			return providerLogoutUrl.toString();
		}

		return `${providerLogoutUrl.pathname}${providerLogoutUrl.search}${providerLogoutUrl.hash}`;
	}

	function createWorkspaceLogoutUrl(tenantId: string | null | undefined) {
		const workspaceLogoutUrl = new URL(logoutUrl, page.url.origin);
		const returnUrl = new URL(resolve('/'), page.url.origin);
		const normalizedTenantId = normalizeTenantId(tenantId);

		if (normalizedTenantId) {
			returnUrl.searchParams.set('tenantId', normalizedTenantId);
		}

		workspaceLogoutUrl.searchParams.set('returnUrl', returnUrl.toString());

		if (/^https?:\/\//i.test(logoutUrl)) {
			return workspaceLogoutUrl.toString();
		}

		return `${workspaceLogoutUrl.pathname}${workspaceLogoutUrl.search}${workspaceLogoutUrl.hash}`;
	}
</script>

<svelte:head>
	<title>Workspace</title>
	<meta
		name="description"
		content="Authenticated campaign-series workspace for the Instruments Platform."
	/>
</svelte:head>

{#if authState === 'checking'}
	<main class="auth-boundary-shell" aria-label="Workspace access">
		<section class="setup-panel" aria-labelledby="auth-boundary-title">
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Workspace access</p>
					<h1 id="auth-boundary-title" class="setup-panel__title">Checking workspace access</h1>
				</div>
			</div>
			<p class="text-sm text-[var(--color-text-muted)]">Confirming your signed-in account and workspace membership.</p>
		</section>
	</main>
{:else if authState === 'authenticated' && authSession}
	<AppShell>
		<div class="grid gap-6">
			{#if authSession.emailVerificationRequired === true}
				<div class="email-verification-reminder" role="status" aria-label="Email verification required">
					<div class="email-verification-reminder__icon" aria-hidden="true">!</div>
					<div class="email-verification-reminder__body">
						<h2 class="email-verification-reminder__title">Verify your email</h2>
						<p class="email-verification-reminder__text">
							Open the verification email from your sign-in provider to keep access after signing out.
						</p>
					</div>
				</div>
			{/if}

			<section class="setup-callout" aria-label="Signed-in workspace account">
				{#if sessionProfile}
					<div class="session-callout__header">
						<div>
							<p class="setup-callout__key">Signed in as</p>
							<p class="setup-callout__value">{sessionProfile.accountLabel}</p>
							<p class="setup-callout__note">{sessionProfile.permissionSummary}</p>
						</div>
						<a class="secondary-button" href={workspaceLogoutUrl}>Sign out</a>
					</div>
					<div class="session-callout__badges" aria-label="Workspace access">
						{#each sessionProfile.permissionBadges as badge}
							<span class="status-badge" data-status="ready">{badge}</span>
						{/each}
					</div>
					{#if sessionProfile.technicalRows.length > 0}
						<details class="session-callout__details" aria-label="Session technical details">
							<summary>Technical details</summary>
							<dl class="session-callout__technical-list">
								{#each sessionProfile.technicalRows as row}
									<div class="session-callout__technical-row">
										<dt>{row.label}</dt>
										<dd>{row.value}</dd>
									</div>
								{/each}
							</dl>
						</details>
					{/if}
				{/if}
			</section>

			{@render children()}
		</div>
	</AppShell>
{:else if authState === 'unauthenticated'}
	<main class="auth-boundary-shell" aria-label="Workspace access">
		<section class="setup-panel" aria-labelledby="auth-boundary-title">
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Workspace access</p>
					<h1 id="auth-boundary-title" class="setup-panel__title">Workspace sign-in needed</h1>
				</div>
			</div>
			{#if authRecoveryRedirect}
				<div
					class="email-verification-reminder"
					role="status"
					aria-label="Email verification reminder"
				>
					<div class="email-verification-reminder__icon" aria-hidden="true">!</div>
					<div class="email-verification-reminder__body">
						<h2 class="email-verification-reminder__title">
							{authEmailUnverifiedRedirect
								? 'Verify email, then sign in'
								: authEmailMismatchRedirect
								? 'Choose the requested account'
								: authFailedHasPendingRegistration
								? 'Registration sign-in did not finish'
								: 'Sign in with your workspace account'}
						</h2>
						{#if authEmailUnverifiedRedirect}
							<p class="email-verification-reminder__text">
								Open the verification email from your sign-in provider, then sign in again with the same account.
							</p>
							<p class="email-verification-reminder__note">
								If the browser keeps choosing the wrong account, sign out completely and choose the intended email.
							</p>
						{:else if authEmailMismatchRedirect}
							<p class="email-verification-reminder__text">
								The selected sign-in account did not match the workspace email you entered.
								Sign out completely, then choose the same account again.
							</p>
							<p class="email-verification-reminder__note">
								This protects the workspace from stale provider sessions and wrong account selection.
							</p>
						{:else if authFailedHasPendingRegistration}
							<p class="email-verification-reminder__text">
								Retry the saved registration sign-in link if sign-in was interrupted.
							</p>
							<p class="email-verification-reminder__note">
								If the browser keeps choosing the wrong account, sign out completely first.
							</p>
						{:else}
							<p class="email-verification-reminder__text">
								The selected account does not have access to this workspace. Sign in with
								the email that owns the workspace, or create a new workspace from registration.
							</p>
							<p class="email-verification-reminder__note">
								If the browser keeps choosing the wrong account, sign out completely and choose the intended email.
							</p>
						{/if}
					</div>
				</div>
			{/if}
			<p class="text-sm text-[var(--color-text-muted)]">
				{authRecoveryRedirect
					? authEmailUnverifiedRedirect
						? 'Email verification is required before signing in again after sign-out.'
						: authEmailMismatchRedirect
						? 'Use the same email you entered on the workspace sign-in page.'
						: authFailedHasPendingRegistration
						? 'Use the saved registration link only when registration was interrupted before the workspace opened.'
						: 'Use an account that already belongs to this workspace. If this is not the account you intended, sign out completely.'
					: hasTenantLoginTarget
						? 'Sign in with an account that belongs to this workspace before opening product screens.'
						: 'No workspace session is active. Sign in with your workspace email, or create a workspace first.'}
			</p>
			<div class="flex flex-wrap gap-3">
				{#if authRecoveryRedirect}
					<a
						class="primary-button"
						href={authEmailUnverifiedRedirect || authEmailMismatchRedirect
							? emailVerificationSignInUrl
							: authFailedPrimaryUrl}
					>
						{authEmailUnverifiedRedirect
							? 'Sign in after verifying email'
							: authEmailMismatchRedirect
							? 'Choose account again'
							: authFailedPrimaryLabel}
					</a>
					<a class="secondary-button" href={completeLogoutUrl}>Sign out completely</a>
				{:else}
					<a class="primary-button" href={primaryAuthActionUrl}>{primaryAuthActionLabel}</a>
					{#if !hasTenantLoginTarget}
						<a class="secondary-button" href={resolve('/register')}>Create workspace</a>
					{/if}
					<button type="button" class="secondary-button" onclick={checkSession}>Retry</button>
				{/if}
			</div>
		</section>
	</main>
{:else if authState === 'forbidden'}
	<main class="auth-boundary-shell" aria-label="Workspace access">
		<section class="setup-panel" aria-labelledby="auth-boundary-title">
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Workspace access</p>
					<h1 id="auth-boundary-title" class="setup-panel__title">Workspace access unavailable</h1>
				</div>
			</div>
			<p class="text-sm text-[var(--color-text-muted)]">
				This account is signed in, but it is not a member of the workspace the app tried to open.
			</p>
			<div class="flex flex-wrap gap-3">
				<button type="button" class="secondary-button" onclick={checkSession}>Retry</button>
				<a class="secondary-button" href={workspaceLogoutUrl}>Sign out</a>
				<a class="primary-button" href={resolve('/register')}>Create workspace</a>
			</div>
		</section>
	</main>
{:else}
	<main class="auth-boundary-shell" aria-label="Workspace access">
		<section class="setup-panel" aria-labelledby="auth-boundary-title" role="alert">
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Workspace access</p>
					<h1 id="auth-boundary-title" class="setup-panel__title">Could not verify workspace access</h1>
				</div>
			</div>
			<p class="text-sm text-[var(--color-text-muted)]">{authMessage}</p>
			<div class="flex flex-wrap gap-3">
				<button type="button" class="secondary-button" onclick={checkSession}>Retry</button>
				<a class="secondary-button" href={workspaceLogoutUrl}>Sign out</a>
			</div>
		</section>
	</main>
{/if}

<style>
	.auth-boundary-shell {
		display: grid;
		min-height: 100svh;
		place-items: center;
		padding: clamp(1rem, 4vw, 3rem);
		background:
			radial-gradient(circle at 20% 10%, rgba(15, 118, 110, 0.16), transparent 30rem),
			radial-gradient(circle at 85% 20%, rgba(245, 158, 11, 0.13), transparent 26rem),
			linear-gradient(135deg, #f8faf7 0%, #eef4ef 100%);
	}

	.auth-boundary-shell :global(.setup-panel) {
		width: min(100%, 46rem);
		margin: 0;
	}

	.email-verification-reminder {
		display: grid;
		grid-template-columns: auto 1fr;
		gap: 1rem;
		align-items: start;
		margin: 0.25rem 0 0.35rem;
		padding: 1rem;
		border: 1px solid rgba(217, 119, 6, 0.34);
		border-radius: 1.35rem;
		background:
			radial-gradient(circle at 0% 0%, rgba(245, 158, 11, 0.24), transparent 42%),
			linear-gradient(135deg, rgba(255, 251, 235, 0.98), rgba(255, 247, 237, 0.94));
		box-shadow: 0 18px 45px rgba(120, 53, 15, 0.12);
		color: #451a03;
	}

	.email-verification-reminder__icon {
		display: grid;
		width: 2.5rem;
		height: 2.5rem;
		place-items: center;
		border-radius: 999px;
		background: #f59e0b;
		box-shadow: 0 10px 22px rgba(217, 119, 6, 0.26);
		color: #fff7ed;
		font-size: 1.3rem;
		font-weight: 900;
		line-height: 1;
	}

	.email-verification-reminder__body {
		display: grid;
		gap: 0.35rem;
	}

	.email-verification-reminder__title {
		margin: 0;
		color: #451a03;
		font-size: 1rem;
		font-weight: 800;
		letter-spacing: -0.015em;
	}

	.email-verification-reminder__text,
	.email-verification-reminder__note {
		margin: 0;
		color: #78350f;
		font-size: 0.92rem;
		line-height: 1.5;
	}

	.email-verification-reminder__note {
		color: #92400e;
	}

	@media (max-width: 640px) {
		.email-verification-reminder {
			grid-template-columns: 1fr;
		}

		.email-verification-reminder__icon {
			width: 2.2rem;
			height: 2.2rem;
		}
	}
</style>
