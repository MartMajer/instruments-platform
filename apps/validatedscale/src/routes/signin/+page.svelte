<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { createRegistrationApi } from '$lib/api/registration';
	import { ApiError } from '$lib/api/client';
	import {
		readLastWorkspaceEmail,
		rememberLastWorkspaceEmail,
		resolveApiUrl
	} from '$lib/api/session-headers';
	import { api } from '$lib/core/client';
	import { problemMessage } from '$lib/core/problem';

	const registration = createRegistrationApi(api());
	const devAuth = env.PUBLIC_DEV_AUTH_ENABLED === 'true';

	let email = $state('');
	let busy = $state(false);
	let error = $state<string | null>(null);

	$effect(() => {
		email = readLastWorkspaceEmail(globalThis.localStorage) || email;
	});

	async function signIn(event: SubmitEvent) {
		event.preventDefault();
		if (busy) return;

		busy = true;
		error = null;

		try {
			const result = await registration.createExistingWorkspaceSignIn({
				email: email.trim(),
				returnUrl: `${location.origin}/app`
			});
			rememberLastWorkspaceEmail(globalThis.localStorage, email.trim());
			location.assign(resolveApiUrl(env, result.loginUrl));
		} catch (cause) {
			busy = false;
			if (cause instanceof ApiError && cause.status === 404) {
				error = 'No workspace uses this email. Check the address, or create a workspace.';
			} else if (cause instanceof ApiError && cause.status === 429) {
				error = 'Too many attempts. Wait a minute, then try again.';
			} else {
				error = problemMessage(
					cause,
					{
						'registration.email_invalid': 'That email address does not look valid. Check it and try again.'
					},
					'Sign-in is unavailable right now. Try again shortly.'
				);
			}
		}
	}
</script>

<svelte:head><title>Sign in — ValidatedScale</title></svelte:head>

<div class="gate">
	<a class="eyebrow brand" href="/">ValidatedScale</a>

	<main class="panel card">
		{#if devAuth}
			<div class="dev-gate">
				<span class="eyebrow">Local development</span>
				<p class="hint">
					Dev auth is on — no real sign-in is configured locally. Enter the local workspace
					directly.
				</p>
				<a class="btn btn-stain dev-enter" href="/app">Enter local workspace</a>
			</div>
		{/if}

		<h1 class="doc-title">Sign in</h1>
		<p class="hint">Enter your work email and we take you to your workspace.</p>

		<form onsubmit={signIn}>
			<label class="eyebrow" for="email">Email</label>
			<input
				id="email"
				type="email"
				autocomplete="email"
				required
				bind:value={email}
				placeholder="name@institution.org"
			/>

			{#if error}
				<p class="error" role="alert">{error}</p>
			{/if}

			<button class="btn btn-ink" type="submit" disabled={busy}>
				{busy ? 'Opening workspace…' : 'Continue'}
			</button>
		</form>

		<p class="alt">
			New here? <a href="/register">Create a workspace</a>
		</p>
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

	.dev-gate {
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: 0.5rem;
		padding: 1rem;
		margin-bottom: 1.75rem;
		background: var(--color-stain-wash);
		border: 1px solid var(--color-stain-line);
		border-radius: var(--radius-instrument);
	}

	.dev-gate .hint {
		margin-top: 0;
	}

	.dev-enter {
		margin-top: 0.375rem;
		text-decoration: none;
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
		margin-top: 0.5rem;
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
