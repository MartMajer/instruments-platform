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
	let statusMessage = $state('');
	let isSubmitting = $state(false);
	let pendingEmail = $state('');
	let sessionState = $state<'checking' | 'signed-out' | 'ready'>('checking');
	let registrationSignInUrl = $state('');

	onMount(() => {
		registrationSignInUrl = resolveAuthRedirectUrl(
			`/auth/login?registration=1&returnUrl=${encodeURIComponent(absoluteWebUrl(resolve('/register')))}`
		);
		void loadRegistrationSession();
	});

	async function loadRegistrationSession() {
		errorMessage = '';

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
			errorMessage = 'Registration session check failed. Start account sign-up again.';
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

		return 'Registration is unavailable right now. Try again or sign in if you already have a workspace.';
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
			<h1 id="registration-title">Create the workspace you will use for studies.</h1>
			<p>
				Create or choose your account first, then name the workspace that account will own.
			</p>
			<div class="registration-steps" aria-label="Registration steps">
				<div>
					<span>01</span>
					<strong>Create account</strong>
					<p>Use Auth0 to create or choose the owner identity.</p>
				</div>
				<div>
					<span>02</span>
					<strong>Name workspace</strong>
					<p>Return here to enter workspace name and beta access code.</p>
				</div>
				<div>
					<span>03</span>
					<strong>Start setup</strong>
					<p>Prepare studies, team access, collection, reports, and exports.</p>
				</div>
			</div>
		</section>

		<section class="registration-panel" aria-label="Create workspace form">
			<div class="registration-panel__header">
				<span>Workspace signup</span>
				<strong>Create your account, then your workspace</strong>
				<p>Already have a workspace? <a href={signInUrl}>Sign in instead</a>.</p>
			</div>

			{#if sessionState === 'checking'}
				<p class="registration-alert" role="status">Checking account status...</p>
			{:else if sessionState === 'signed-out'}
				<div class="registration-form">
					<p class="registration-alert" role="status">
						Start with your identity provider. You will create or choose the email account there, then return here to name the workspace.
					</p>
					<a class="registration-submit" href={registrationSignInUrl || signInUrl}>Create account</a>
				</div>
			{:else}
				<form class="registration-form" onsubmit={submitWorkspace}>
					<div class="registration-alert" role="status">
						Signed in as <strong>{pendingEmail}</strong>. This email will become the workspace owner.
					</div>

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
				<strong>Beta boundary</strong>
				<p>Use demo or owner-controlled data only until production review is closed.</p>
			</div>
		</section>
	</div>
</section>





