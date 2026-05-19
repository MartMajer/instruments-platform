<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { env } from '$env/dynamic/public';
	import { ApiError } from '$lib/api/client';
	import {
		createLoginUrlFromEnv,
		normalizeTenantId,
		readLastTenantId,
		readLastWorkspaceEmail,
		rememberLastTenantId,
		rememberLastWorkspaceEmail
	} from '$lib/api/session-headers';
	import { createRegistrationApi } from '$lib/api/registration';

	const registrationApi = createRegistrationApi();

	let email = $state('');
	let rememberedEmail = $state('');
	let rememberedSignInUrl = $state('');
	let errorMessage = $state('');
	let statusMessage = $state('');
	let isSubmitting = $state(false);

	onMount(() => {
		const tenantId = readLastTenantId(window.localStorage);
		const storedEmail = readLastWorkspaceEmail(window.localStorage);

		rememberedEmail = storedEmail;
		email = storedEmail;

		if (tenantId) {
			rememberedSignInUrl = createLoginUrlFromEnv(env, tenantId, storedEmail);
		}
	});

	async function submitExistingWorkspaceSignIn(event: SubmitEvent) {
		event.preventDefault();
		errorMessage = '';
		statusMessage = '';
		isSubmitting = true;

		try {
			const response = await registrationApi.createExistingWorkspaceSignIn({
				email,
				returnUrl: absoluteWebUrl(resolve('/app'))
			});
			const loginUrl = resolveAuthRedirectUrl(response.loginUrl);
			rememberTenantFromLoginUrl(loginUrl, email);
			statusMessage = 'Opening workspace sign-in.';
			window.location.assign(loginUrl);
		} catch (error) {
			errorMessage = toWorkspaceSignInError(error);
			isSubmitting = false;
		}
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

	function rememberTenantFromLoginUrl(loginUrl: string, email: string) {
		try {
			const parsedUrl = new URL(loginUrl, window.location.origin);
			const tenantId = normalizeTenantId(parsedUrl.searchParams.get('tenantId'));
			if (!tenantId) {
				return;
			}

			rememberLastTenantId(window.localStorage, tenantId);
			rememberLastWorkspaceEmail(window.localStorage, email);
		} catch {
			return;
		}
	}

	function toWorkspaceSignInError(error: unknown) {
		if (error instanceof ApiError) {
			const problem = error.body as { title?: unknown; detail?: unknown } | null;
			const code = typeof problem?.title === 'string' ? problem.title : '';
			const detail = typeof problem?.detail === 'string' ? problem.detail : '';

			if (code === 'registration.workspace_not_found') {
				return 'No workspace exists for this email yet. Create a workspace first.';
			}

			if (code === 'registration.email_invalid') {
				return 'Enter the email used for the workspace.';
			}

			if (detail) {
				return detail;
			}
		}

		return 'We could not find a workspace for this email.';
	}
</script>

<svelte:head>
	<title>Sign in | Instruments Platform</title>
	<meta
		name="description"
		content="Find your Instruments Platform workspace before signing in with the identity provider."
	/>
</svelte:head>

<section class="registration-page" aria-labelledby="signin-title">
	<div class="registration-page__glow registration-page__glow--one" aria-hidden="true"></div>
	<div class="registration-page__glow registration-page__glow--two" aria-hidden="true"></div>

	<header class="registration-nav">
		<a class="launchpad-brand" href={resolve('/')}>
			<span class="launchpad-brand__mark" aria-hidden="true">IP</span>
			<span>
				<strong>Instruments Platform</strong>
				<small>Workspace sign-in</small>
			</span>
		</a>
		<nav class="registration-nav__links" aria-label="Sign-in actions">
			<a href={resolve('/')}>Product</a>
			<a href={resolve('/register')}>Create workspace</a>
		</nav>
	</header>

	<div class="registration-grid">
		<section class="registration-copy">
			<p class="launchpad-kicker">Workspace access</p>
			<h1 id="signin-title">Sign in to your workspace.</h1>
			<p>
				Enter the email used for the workspace. We find the workspace first, then send you
				to the identity provider for password and MFA.
			</p>
			<div class="registration-steps" aria-label="Sign-in steps">
				<div>
					<span>01</span>
					<strong>Find workspace</strong>
					<p>Use the same email that owns or belongs to the workspace.</p>
				</div>
				<div>
					<span>02</span>
					<strong>Sign in</strong>
					<p>Auth0 handles password, account selection, and MFA.</p>
				</div>
				<div>
					<span>03</span>
					<strong>Open app</strong>
					<p>The platform opens only after matching the Auth0 account to workspace membership.</p>
				</div>
			</div>
		</section>

		<section class="registration-panel" aria-label="Workspace sign-in form">
			<div class="registration-panel__header">
				<span>Existing workspace</span>
				<strong>Continue with your workspace email</strong>
				<p>Need a new workspace? <a href={resolve('/register')}>Create one instead</a>.</p>
			</div>

			{#if rememberedSignInUrl}
				<div class="registration-alert" role="status">
					<strong>Recent workspace found</strong>
					<span>
						{#if rememberedEmail}
							Continue as {rememberedEmail}, or enter another workspace email below.
						{:else}
							Continue to your recent workspace, or enter another workspace email below.
						{/if}
					</span>
				</div>
				<a class="registration-submit" href={rememberedSignInUrl}>Continue to recent workspace</a>
			{/if}

			<form class="registration-form" onsubmit={submitExistingWorkspaceSignIn}>
				<label>
					<span>Email</span>
					<input
						type="email"
						bind:value={email}
						autocomplete="email"
						placeholder="you@example.com"
						required
					/>
				</label>

				{#if errorMessage}
					<p class="registration-alert registration-alert--error" role="alert">{errorMessage}</p>
				{/if}

				{#if statusMessage}
					<p class="registration-alert registration-alert--success" role="status">{statusMessage}</p>
				{/if}

				<button class="registration-submit" type="submit" disabled={isSubmitting}>
					{isSubmitting ? 'Finding workspace...' : 'Continue to sign in'}
				</button>
			</form>

			<div class="registration-boundary">
				<strong>Private beta</strong>
				<p>Use demo or owner-controlled data only until production review is closed.</p>
			</div>
		</section>
	</div>
</section>
