<script lang="ts">
	import { onMount } from 'svelte';
	import { env } from '$env/dynamic/public';
	import { createRegistrationApi } from '$lib/api/registration';
	import { ApiError } from '$lib/api/client';
	import {
		rememberLastTenantId,
		rememberLastWorkspaceEmail,
		resolveApiUrl
	} from '$lib/api/session-headers';
	import { api } from '$lib/core/client';
	import { problemMessage } from '$lib/core/problem';

	const registration = createRegistrationApi(api());

	let phase = $state<'checking' | 'start' | 'complete'>('checking');
	let pendingEmail = $state('');
	let email = $state('');
	let organizationName = $state('');
	let accessCode = $state('');
	let busy = $state(false);
	let error = $state<string | null>(null);

	onMount(async () => {
		try {
			const session = await registration.getSession();
			pendingEmail = session.email;
			phase = 'complete';
		} catch {
			phase = 'start';
		}
	});

	function describe(cause: unknown): string {
		if (cause instanceof ApiError && cause.status === 409) {
			return 'This email already belongs to a workspace. Sign in instead.';
		}
		if (cause instanceof ApiError && cause.status === 429) {
			return 'Too many attempts. Wait a minute, then try again.';
		}
		return problemMessage(
			cause,
			{
				'registration.invalid_access_code': 'That access code was not accepted. Check it and try again.',
				'registration.email_invalid': 'That email address does not look valid. Check it and try again.',
				'registration.organization_invalid': 'That organization name was not accepted. Try a plainer one.',
				'registration.invalid_return_url': 'Something went wrong with the sign-in redirect. Reload the page and try again.'
			},
			'Registration is unavailable right now. Try again shortly.'
		);
	}

	async function start(event: SubmitEvent) {
		event.preventDefault();
		if (busy) return;
		busy = true;
		error = null;

		try {
			const response = await registration.createIntent({
				email: email.trim(),
				organizationName: organizationName.trim(),
				accessCode: accessCode.trim(),
				returnUrl: `${location.origin}/register`
			});
			location.assign(resolveApiUrl(env, response.loginUrl));
		} catch (cause) {
			busy = false;
			error = describe(cause);
		}
	}

	async function complete(event: SubmitEvent) {
		event.preventDefault();
		if (busy) return;
		busy = true;
		error = null;

		try {
			const response = await registration.createWorkspace({
				organizationName: organizationName.trim(),
				accessCode: accessCode.trim(),
				returnUrl: `${location.origin}/app`
			});
			rememberLastTenantId(globalThis.localStorage, response.tenantId);
			rememberLastWorkspaceEmail(globalThis.localStorage, response.email);
			location.assign(response.appUrl);
		} catch (cause) {
			busy = false;
			error = describe(cause);
		}
	}
</script>

<svelte:head><title>Create a workspace — ValidatedScale</title></svelte:head>

<div class="gate">
	<a class="eyebrow brand" href="/">ValidatedScale</a>

	<main class="panel card">
		{#if phase === 'checking'}
			<p class="hint" role="status">Checking your registration…</p>
		{:else if phase === 'complete'}
			<h1 class="doc-title">Finish your workspace</h1>
			<p class="hint">
				Signed in as <strong class="datum">{pendingEmail}</strong>. Name the workspace and
				confirm your access code.
			</p>

			<form onsubmit={complete}>
				<label class="eyebrow" for="org">Organization</label>
				<input id="org" required bind:value={organizationName} />

				<label class="eyebrow" for="code">Access code</label>
				<input id="code" required bind:value={accessCode} autocomplete="off" />

				{#if error}<p class="error" role="alert">{error}</p>{/if}

				<button class="btn btn-ink" type="submit" disabled={busy}>
					{busy ? 'Creating workspace…' : 'Create workspace'}
				</button>
			</form>
		{:else}
			<h1 class="doc-title">Create a workspace</h1>
			<p class="hint">
				A workspace holds your studies, people and evidence — isolated per organization. You
				verify your email in the next step.
			</p>

			<form onsubmit={start}>
				<label class="eyebrow" for="email">Work email</label>
				<input
					id="email"
					type="email"
					autocomplete="email"
					required
					bind:value={email}
					placeholder="name@institution.org"
				/>

				<label class="eyebrow" for="org">Organization</label>
				<input id="org" required bind:value={organizationName} />

				<label class="eyebrow" for="code">Access code</label>
				<input id="code" required bind:value={accessCode} autocomplete="off" />

				{#if error}<p class="error" role="alert">{error}</p>{/if}

				<button class="btn btn-ink" type="submit" disabled={busy}>
					{busy ? 'Opening verification…' : 'Continue'}
				</button>
			</form>

			<p class="alt">
				Already registered? <a href="/signin">Sign in</a>
			</p>
		{/if}
	</main>
</div>

<style>
	.gate {
		min-height: 100dvh;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 2.5rem;
		padding: 3.5rem 1.25rem;
	}

	.brand {
		color: var(--color-ink);
		font-size: 0.875rem;
		text-decoration: none;
	}

	.card {
		width: min(26rem, 100%);
		padding: 2rem;
		border-top: 3px solid var(--color-stain);
	}

	h1 {
		font-size: 1.75rem;
	}

	.hint {
		margin-top: 0.375rem;
		font-size: 0.875rem;
		color: var(--color-ink-2);
	}

	form {
		margin-top: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 0.625rem;
	}

	label {
		margin-top: 0.375rem;
	}

	input {
		font: inherit;
		padding: 0.625rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	input:focus-visible {
		outline-offset: 0;
		border-color: var(--color-stain);
	}

	button {
		margin-top: 0.75rem;
		justify-content: center;
	}

	.error {
		font-size: 0.8125rem;
		color: var(--color-danger);
	}

	.alt {
		margin-top: 1.25rem;
		font-size: 0.8125rem;
		color: var(--color-ink-3);
	}

	.alt a {
		color: var(--color-stain);
	}
</style>
