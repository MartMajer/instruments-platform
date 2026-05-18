<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { ApiError, createApiClient } from '$lib/api/client';
	import { createLoginUrlFromEnv, createSessionHeadersFromEnv } from '$lib/api/session-headers';
	import { createSetupApi, type AuthSessionResponse } from '$lib/api/setup';
	import { createProductAuthContext, setProductAuthContext } from '$lib/product/auth-context';
	import { toSessionProfileView } from '$lib/product/session-profile';

	type AuthState = 'checking' | 'authenticated' | 'unauthenticated' | 'forbidden' | 'failed';

	const loginUrl = createLoginUrlFromEnv(env);
	const logoutUrl = env.PUBLIC_AUTH_LOGOUT_URL || '/auth/logout';
	const hasTenantLoginTarget = /[?&]tenantId=/.test(loginUrl);
	const unauthenticatedActionUrl = hasTenantLoginTarget ? loginUrl : resolve('/register');
	const unauthenticatedActionLabel = hasTenantLoginTarget ? 'Sign in' : 'Create workspace';

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
	const sessionProfile = $derived(authSession ? toSessionProfileView(authSession) : null);

	onMount(() => {
		void checkSession();
	});

	async function checkSession() {
		authState = 'checking';
		authMessage = null;

		try {
			authSession = await setupApi.getCurrentSession();
			authContext.session.set(authSession);
			authState = 'authenticated';
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
			return `Session check failed with status ${error.status}.`;
		}

		if (error instanceof Error) {
			return error.message;
		}

		return 'Session check failed.';
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
				<p class="setup-panel__eyebrow">Tenant session</p>
				<h1 id="auth-boundary-title" class="setup-panel__title">Checking access</h1>
			</div>
		</div>
		<p class="text-sm text-[var(--color-text-muted)]">Loading authenticated workspace access.</p>
	</section>
{:else if authState === 'authenticated' && authSession}
	<div class="grid gap-6">
		<section class="setup-callout" aria-label="Authenticated tenant session">
			{#if sessionProfile}
				<div class="session-callout__header">
					<div>
						<p class="setup-callout__key">Tenant session</p>
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
				<p class="setup-panel__eyebrow">Tenant session</p>
				<h1 id="auth-boundary-title" class="setup-panel__title">Sign in required</h1>
			</div>
		</div>
		<p class="text-sm text-[var(--color-text-muted)]">
			{hasTenantLoginTarget
				? 'Sign in before opening tenant product surfaces.'
				: 'Create a workspace first, then the app will open with your tenant session.'}
		</p>
		<a class="primary-button" href={unauthenticatedActionUrl}>{unauthenticatedActionLabel}</a>
	</section>
{:else if authState === 'forbidden'}
	<section class="setup-panel" aria-labelledby="auth-boundary-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Tenant session</p>
				<h1 id="auth-boundary-title" class="setup-panel__title">Tenant access unavailable</h1>
			</div>
		</div>
		<p class="text-sm text-[var(--color-text-muted)]">
			Your session does not have access to this tenant workspace.
		</p>
		<button type="button" class="secondary-button" onclick={checkSession}
			>Retry session check</button
		>
	</section>
{:else}
	<section class="setup-panel" aria-labelledby="auth-boundary-title" role="alert">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Tenant session</p>
				<h1 id="auth-boundary-title" class="setup-panel__title">Session check failed</h1>
			</div>
		</div>
		<p class="text-sm text-[var(--color-text-muted)]">{authMessage}</p>
		<button type="button" class="secondary-button" onclick={checkSession}
			>Retry session check</button
		>
	</section>
{/if}
