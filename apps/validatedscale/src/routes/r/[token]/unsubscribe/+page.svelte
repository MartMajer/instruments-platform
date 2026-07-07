<script lang="ts">
	import { page } from '$app/state';
	import { createSetupApi } from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { respondentCopy, respondentLocale } from '$lib/respondent/copy';

	const setup = createSetupApi(api());
	const token = $derived(page.params.token!);

	let phase = $state<'ask' | 'busy' | 'done' | 'failed'>('ask');
	let doneScope = $state<'study' | 'workspace'>('study');

	const locale = $derived(respondentLocale('en', page.url.searchParams.get('locale')));
	const copy = $derived(respondentCopy[locale]);

	async function confirm(workspaceWide: boolean) {
		phase = 'busy';
		try {
			const result = await setup.unsubscribeEmailInvitation(token, workspaceWide);
			doneScope = result.scope === 'workspace' ? 'workspace' : 'study';
			phase = 'done';
		} catch {
			phase = 'failed';
		}
	}
</script>

<svelte:head><title>ValidatedScale</title></svelte:head>

<div class="notice">
	<p class="eyebrow">ValidatedScale</p>
	{#if phase === 'done'}
		<h1 class="doc-title">
			{doneScope === 'workspace' ? copy.unsubscribeDoneWorkspace : copy.unsubscribeDone}
		</h1>
	{:else if phase === 'failed'}
		<h1 class="doc-title">{copy.notFound}</h1>
	{:else}
		<h1 class="doc-title">{copy.unsubscribeTitle}</h1>
		<button class="btn btn-ink" disabled={phase === 'busy'} onclick={() => confirm(false)}>
			{copy.unsubscribeConfirm}
		</button>
		<button class="link-btn" disabled={phase === 'busy'} onclick={() => confirm(true)}>
			{copy.unsubscribeWorkspace}
		</button>
	{/if}
</div>

<style>
	.notice {
		min-height: 80dvh;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 1.25rem;
		text-align: center;
		padding: 1rem;
	}

	.notice h1 {
		font-size: 1.5rem;
		max-width: 28ch;
	}

	.link-btn {
		font: inherit;
		font-size: 0.875rem;
		background: none;
		border: 0;
		color: var(--color-stain, #555);
		text-decoration: underline;
		cursor: pointer;
	}

	.link-btn:disabled {
		opacity: 0.5;
	}
</style>
