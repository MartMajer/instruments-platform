<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { env } from '$env/dynamic/public';
	import { ApiError } from '$lib/api/client';
	import { createLoginUrlFromEnv } from '$lib/api/session-headers';
	import { createRegistrationApi } from '$lib/api/registration';

	const registrationApi = createRegistrationApi();
	const signInUrl = createLoginUrlFromEnv(env);

	let organizationName = $state('');
	let accessCode = $state('');
	let errorMessage = $state('');
	let sessionErrorMessage = $state('');
	let statusMessage = $state('');
	let isSubmitting = $state(false);
	let pendingEmail = $state('');
	let sessionState = $state<'checking' | 'signed-out' | 'ready'>('checking');
	let registrationSignInUrl = $state('');
	let switchAccountUrl = $state('');

	onMount(() => {
		const registerReturnUrl = encodeURIComponent(absoluteWebUrl(resolve('/register')));
		registrationSignInUrl = resolveAuthRedirectUrl(
			`/auth/login?registration=1&returnUrl=${registerReturnUrl}`
		);
		switchAccountUrl = resolveAuthRedirectUrl(
			`/auth/logout?provider=1&returnUrl=${encodeURIComponent(absoluteWebUrl(`${resolve('/')}?postLogout=register`))}`
		);
		void loadRegistrationSession();
	});

	async function loadRegistrationSession() {
		errorMessage = '';
		sessionErrorMessage = '';

		try {
			const session = await registrationApi.getSession();
			pendingEmail = session.email;
			sessionState = 'ready';
		} catch (error) {
			if (error instanceof ApiError && error.status === 401) {
				sessionState = 'signed-out';
				return;
			}

			sessionState = 'signed-out';
			sessionErrorMessage =
				'We could not confirm the account step. Continue with account setup again before creating the workspace.';
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

			statusMessage = 'Workspace created. Opening your app.';
			window.location.assign(response.appUrl);
		} catch (error) {
			errorMessage = toRegistrationError(error);
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

	function toRegistrationError(error: unknown) {
		if (error instanceof ApiError) {
			const problem = error.body as { title?: unknown; detail?: unknown } | null;
			const code = typeof problem?.title === 'string' ? problem.title : '';
			const detail = typeof problem?.detail === 'string' ? problem.detail : '';

			if (code === 'registration.disabled') {
				return 'Private beta sign-up is not open on this environment. Sign in if you already have a workspace.';
			}

			if (error.status === 401) {
				return 'Your account step expired. Continue with account setup again, then create the workspace.';
			}

			if (error.status === 403) {
				return 'This account cannot create a workspace. Sign out and use the approved owner account, or ask for beta access.';
			}

			if (error.status === 409) {
				return 'This account or workspace is already set up. Sign in instead.';
			}

			if (code === 'registration.invalid_access_code') {
				return 'That access code does not match the private beta list.';
			}

			if (code === 'registration.organization_invalid') {
				return 'Enter the workspace or organization name you want to use.';
			}

			if (detail) {
				return detail;
			}
		}

		return 'We could not create the workspace. Check the beta access code and try again.';
	}
</script>

<svelte:head>
	<title>Create workspace | Instruments Platform</title>
	<meta
		name="description"
		content="Create a private beta workspace for Instruments Platform and finish sign-in with your identity provider."
	/>
</svelte:head>

<section class="registration-page" aria-labelledby="registration-title">
	<div class="registration-page__glow registration-page__glow--one" aria-hidden="true"></div>
	<div class="registration-page__glow registration-page__glow--two" aria-hidden="true"></div>

	<header class="registration-nav">
		<a class="launchpad-brand" href={resolve('/')}>
			<span class="launchpad-brand__mark" aria-hidden="true">IP</span>
			<span>
				<strong>Instruments Platform</strong>
				<small>Private beta workspace</small>
			</span>
		</a>
		<nav class="registration-nav__links" aria-label="Registration actions">
			<a href={resolve('/')}>Product</a>
			<a href={signInUrl}>Sign in</a>
		</nav>
	</header>

	<div class="registration-grid">
		<section class="registration-copy">
			<p class="launchpad-kicker">Private beta access</p>
			<h1 id="registration-title">Create your workspace.</h1>
			<p>
				Use the email that should own the workspace. Password and MFA stay with the identity provider; this page only names the workspace and checks beta access.
			</p>
			<div class="registration-steps" aria-label="Registration steps">
				<div>
					<span>01</span>
					<strong>Create or choose account</strong>
					<p>Set the owner email and password with the identity provider.</p>
				</div>
				<div>
					<span>02</span>
					<strong>Name workspace</strong>
					<p>Return here signed in as the owner and enter the beta code.</p>
				</div>
				<div>
					<span>03</span>
					<strong>Open the app</strong>
					<p>The new workspace opens immediately with the owner session.</p>
				</div>
			</div>
		</section>

		<section class="registration-panel" aria-label="Create workspace form">
			<div class="registration-panel__header">
				<span>Workspace signup</span>
				<strong>Account first, workspace second</strong>
				<p>Already have a workspace? <a href={signInUrl}>Sign in instead</a>.</p>
			</div>

			{#if sessionState === 'checking'}
				<p class="registration-alert" role="status">Checking account status...</p>
			{:else if sessionState === 'signed-out'}
				<div class="registration-form">
					{#if sessionErrorMessage}
						<p class="registration-alert registration-alert--error" role="alert">{sessionErrorMessage}</p>
					{:else}
						<p class="registration-alert" role="status">
							Continue with the owner account. After sign-in, you will return here to enter the workspace name and beta code.
						</p>
					{/if}
					{#if registrationSignInUrl}
						<a class="registration-submit" href={registrationSignInUrl}>Continue with owner account</a>
					{:else}
						<button class="registration-submit" type="button" disabled>
							Preparing account link...
						</button>
					{/if}
				</div>
			{:else}
				<form class="registration-form" onsubmit={submitWorkspace}>
					<div class="registration-alert" role="status">
						Account ready: <strong>{pendingEmail}</strong>. This email becomes the workspace owner.
					</div>
					{#if switchAccountUrl}
						<a class="secondary-button" href={switchAccountUrl}>Switch account</a>
					{/if}

					<label>
						<span>Workspace name</span>
						<input
							type="text"
							bind:value={organizationName}
							autocomplete="organization"
							placeholder="Your lab, team, or company"
							required
						/>
					</label>

					<label>
						<span>Beta access code</span>
						<input
							type="password"
							bind:value={accessCode}
							autocomplete="one-time-code"
							placeholder="Access code"
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
						{isSubmitting ? 'Creating workspace...' : 'Create workspace'}
					</button>
				</form>
			{/if}

			<div class="registration-boundary">
				<strong>Private beta</strong>
				<p>Use demo or owner-controlled data only until production review is closed.</p>
			</div>
		</section>
	</div>
</section>





