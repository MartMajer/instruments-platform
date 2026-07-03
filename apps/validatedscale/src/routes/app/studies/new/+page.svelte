<script lang="ts">
	import { t } from '$lib/core/locale.svelte';
	import { createSetupApi } from '$lib/api/setup';
	import { api } from '$lib/core/client';

	const setup = createSetupApi(api());

	let name = $state('');
	let busy = $state(false);
	let error = $state<string | null>(null);

	async function create(event: SubmitEvent) {
		event.preventDefault();
		if (busy) return;
		busy = true;
		error = null;

		try {
			const created = await setup.createCampaignSeries({ name: name.trim() });
			location.assign(`/app/studies/${created.id}`);
		} catch {
			busy = false;
			error = 'The study could not be created. Try again.';
		}
	}
</script>

<svelte:head><title>New study — ValidatedScale</title></svelte:head>

<header class="head">
	<p class="eyebrow"><a href="/app/studies">Studies</a> / New</p>
	<h1 class="doc-title">{t('Register a study')}</h1>
	<p class="hint">
		{t('A study holds one protocol: an instrument, an identity mode, policies, and one or more waves. Name it the way you would in the paper.')}
	</p>
</header>

<form class="panel card" onsubmit={create}>
	<label class="eyebrow" for="name">{t('Study title')}</label>
	<input
		id="name"
		required
		minlength="3"
		bind:value={name}
		placeholder="Nurse burnout and workload, three-wave cohort"
	/>

	{#if error}<p class="error" role="alert">{error}</p>{/if}

	<button class="btn btn-ink" type="submit" disabled={busy}>
		{busy ? t('Registering…') : t('Register study')}
	</button>
	<p class="note">{t('You attach the instrument and policies in the protocol, before launch.')}</p>
</form>

<style>
	.head {
		margin-bottom: 2rem;
	}

	.head .eyebrow a {
		color: inherit;
		text-decoration: none;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.5rem;
	}

	.hint {
		margin-top: 0.5rem;
		font-size: 0.9375rem;
		color: var(--color-ink-2);
		max-width: 52ch;
	}

	.card {
		max-width: 34rem;
		padding: 1.75rem;
		display: flex;
		flex-direction: column;
		gap: 0.625rem;
		border-top: 3px solid var(--color-stain);
	}

	input {
		font-family: var(--font-doc);
		font-size: 1.125rem;
		padding: 0.625rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
	}

	input:focus-visible {
		outline-offset: 0;
		border-color: var(--color-stain);
	}

	button {
		margin-top: 0.75rem;
		align-self: flex-start;
	}

	.error {
		font-size: 0.8125rem;
		color: var(--color-danger);
	}

	.note {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}
</style>
