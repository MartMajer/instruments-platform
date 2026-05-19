<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { ApiError, createApiClient } from '$lib/api/client';
	import { createLoginUrlFromEnv, createSessionHeadersFromEnv } from '$lib/api/session-headers';
	import { createSetupApi, type AuthSessionResponse } from '$lib/api/setup';
	import { createProductAuthContext, setProductAuthContext } from '$lib/product/auth-context';
	import { toSessionProfileView } from '$lib/product/session-profile';

	type AuthState = 'checking' | 'authenticated' | 'unauthenticated' | 'forbidden' | 'failed';

	const loginUrl = createLoginUrlFromEnv(env);
	const logoutUrl = env.PUBLIC_AUTH_LOGOUT_URL || '/auth/logout';
	const pendingRegistrationLoginUrlKey = 'instruments-platform.pending-registration-login-url';
	const hasTenantLoginTarget = /[?&]tenantId=/.test(loginUrl);
	const primaryAuthActionUrl = hasTenantLoginTarget ? loginUrl : resolve('/register');
	const primaryAuthActionLabel = hasTenantLoginTarget ? 'Sign in' : 'Create workspace';
	const completeLogoutUrl = $derived(createProviderLogoutUrl());
	const authFailedPrimaryUrl = $derived(pendingRegistrationLoginUrl || primaryAuthActionUrl);
	const authFailedPrimaryLabel = $derived(
		pendingRegistrationLoginUrl
			? 'Continue workspace setup'
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
	const sessionProfile = $derived(authSession ? toSessionProfileView(authSession) : null);
	const authFailedRedirect = $derived(page.url.searchParams.get('auth') === 'failed');

	onMount(() => {
		loadPendingRegistrationLoginUrl();
		void checkSession();
	});

	async function checkSession() {
		authState = 'checking';
		authMessage = null;

		try {
			authSession = await setupApi.getCurrentSession();
			authContext.session.set(authSession);
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
		if (!authFailedRedirect) {
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
		pendingRegistrationLoginUrl =
			window.sessionStorage.getItem(pendingRegistrationLoginUrlKey) ?? '';
	}

	function clearPendingRegistrationLoginUrl() {
		pendingRegistrationLoginUrl = '';
		window.sessionStorage.removeItem(pendingRegistrationLoginUrlKey);
	}

	function createProviderLogoutUrl() {
		const providerLogoutUrl = new URL(logoutUrl, page.url.origin);
		const returnUrl = new URL(resolve('/'), page.url.origin).toString();
		providerLogoutUrl.searchParams.set('provider', '1');
		providerLogoutUrl.searchParams.set('returnUrl', returnUrl);

		if (/^https?:\/\//i.test(logoutUrl)) {
			return providerLogoutUrl.toString();
		}

		return `${providerLogoutUrl.pathname}${providerLogoutUrl.search}${providerLogoutUrl.hash}`;
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
	<section class="setup-panel" aria-labelledby="auth-boundary-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Workspace access</p>
				<h1 id="auth-boundary-title" class="setup-panel__title">Checking workspace access</h1>
			</div>
		</div>
		<p class="text-sm text-[var(--color-text-muted)]">Confirming your signed-in account and workspace membership.</p>
	</section>
{:else if authState === 'authenticated' && authSession}
	<div class="grid gap-6">
		<section class="setup-callout" aria-label="Authenticated tenant session">
			{#if sessionProfile}
				<div class="session-callout__header">
					<div>
						<p class="setup-callout__key">Workspace session</p>
						<p class="setup-callout__value">{sessionProfile.accountLabel}</p>
						<p class="setup-callout__note">{sessionProfile.permissionSummary}</p>
					</div>
					<a class="secondary-button" href={logoutUrl}>Sign out</a>
				</div>
				<div class="session-callout__badges" aria-label="Session permission posture">
					{#each sessionProfile.permissionBadges as badge}
						<span class="status-badge" data-status="ready">{badge}</span>
					{/each}
				</div>
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
		</section>

		{@render children()}
	</div>
{:else if authState === 'unauthenticated'}
	<section class="setup-panel" aria-labelledby="auth-boundary-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Workspace access</p>
				<h1 id="auth-boundary-title" class="setup-panel__title">Workspace sign-in needed</h1>
			</div>
		</div>
		{#if authFailedRedirect}
			<div
				class="email-verification-reminder"
				role="status"
				aria-label="Email verification reminder"
			>
				<div class="email-verification-reminder__icon" aria-hidden="true">!</div>
				<div class="email-verification-reminder__body">
					<h2 class="email-verification-reminder__title">
						{authFailedHasPendingRegistration ? 'Finish workspace setup' : 'Sign in with your workspace account'}
					</h2>
					{#if authFailedHasPendingRegistration}
						<p class="email-verification-reminder__text">
							Your account step exists, but this browser still needs to finish creating the
							workspace membership. Continue workspace setup with the same verified Auth0 email.
						</p>
						<p class="email-verification-reminder__note">
							If Auth0 keeps choosing the wrong account, sign out completely first, then continue
							workspace setup again.
						</p>
					{:else}
						<p class="email-verification-reminder__text">
							The account Auth0 returned does not have access to this workspace. Sign in with
							the email that owns the workspace, or create a new workspace from registration.
						</p>
						<p class="email-verification-reminder__note">
							If Auth0 keeps using the wrong account, sign out completely and choose the intended email.
						</p>
					{/if}
				</div>
			</div>
		{/if}
		<p class="text-sm text-[var(--color-text-muted)]">
			{authFailedRedirect
				? authFailedHasPendingRegistration
					? 'Use the saved registration link to finish workspace setup. Normal sign-in only works after the workspace membership exists.'
					: 'Use an account that already belongs to this workspace. If this is not the account you intended, sign out completely.'
				: hasTenantLoginTarget
					? 'Sign in with an account that belongs to this workspace before opening product screens.'
					: 'No workspace session is active. Create a workspace first; the app will open immediately after registration.'}
		</p>
		<div class="flex flex-wrap gap-3">
			{#if authFailedRedirect}
				<a class="primary-button" href={authFailedPrimaryUrl}>
					{authFailedPrimaryLabel}
				</a>
				<a class="secondary-button" href={completeLogoutUrl}>Sign out completely</a>
			{:else}
				<a class="primary-button" href={primaryAuthActionUrl}>{primaryAuthActionLabel}</a>
				<button type="button" class="secondary-button" onclick={checkSession}>Retry</button>
			{/if}
		</div>
	</section>
{:else if authState === 'forbidden'}
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
			<a class="secondary-button" href={logoutUrl}>Sign out</a>
			<a class="primary-button" href={resolve('/register')}>Create workspace</a>
		</div>
	</section>
{:else}
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
			<a class="secondary-button" href={logoutUrl}>Sign out</a>
		</div>
	</section>
{/if}

<style>
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
