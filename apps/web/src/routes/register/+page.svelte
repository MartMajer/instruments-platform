<script lang="ts">
	import { resolve } from '$app/paths';
	import { env } from '$env/dynamic/public';
	import { ApiError } from '$lib/api/client';
	import { createLoginUrlFromEnv } from '$lib/api/session-headers';
	import { createRegistrationApi } from '$lib/api/registration';

	const registrationApi = createRegistrationApi();
	const signInUrl = createLoginUrlFromEnv(env);

	let email = $state('');
	let organizationName = $state('');
	let accessCode = $state('');
	let errorMessage = $state('');
	let statusMessage = $state('');
	let isSubmitting = $state(false);

	async function submitRegistration(event: SubmitEvent) {
		event.preventDefault();
		errorMessage = '';
		statusMessage = '';
		isSubmitting = true;

		try {
			const response = await registrationApi.createIntent({
				email,
				organizationName,
				accessCode,
				returnUrl: resolve('/app')
			});

			statusMessage = 'Workspace reserved. Redirecting to sign-in.';
			window.location.assign(resolveAuthRedirectUrl(response.loginUrl));
		} catch (error) {
			errorMessage = toRegistrationError(error);
			isSubmitting = false;
		}
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

			if (code === 'registration.email_invalid') {
				return 'Enter a valid work email address.';
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
				Use your beta access code, confirm your identity, and land in a tenant workspace with owner permissions already assigned.
			</p>
			<div class="registration-steps" aria-label="Registration steps">
				<div>
					<span>01</span>
					<strong>Reserve workspace</strong>
					<p>Name the tenant and bind it to your email.</p>
				</div>
				<div>
					<span>02</span>
					<strong>Sign in</strong>
					<p>Finish authentication with the configured identity provider.</p>
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
				<strong>Create a private beta workspace</strong>
				<p>Already have a workspace? <a href={signInUrl}>Sign in instead</a>.</p>
			</div>

			<form class="registration-form" onsubmit={submitRegistration}>
				<label>
					<span>Work email</span>
					<input
						type="email"
						bind:value={email}
						autocomplete="email"
						placeholder="name@organization.com"
						required
					/>
				</label>

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
					{isSubmitting ? 'Preparing sign-in...' : 'Continue to sign in'}
				</button>
			</form>

			<div class="registration-boundary">
				<strong>Beta boundary</strong>
				<p>Use demo or owner-controlled data only until production review is closed.</p>
			</div>
		</section>
	</div>
</section>





