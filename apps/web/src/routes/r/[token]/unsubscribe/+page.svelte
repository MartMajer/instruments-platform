<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { LoaderCircle } from 'lucide-svelte';
	import { createSetupApiFromEnv } from '$lib/product/route-state';
	import { toProductApiErrorMessage } from '$lib/product/view-models';

	type UnsubscribeState = 'idle' | 'submitting' | 'done' | 'failed';

	const setupApi = createSetupApiFromEnv(env);
	const token = $derived(page.params.token ?? '');

	let unsubscribeState = $state<UnsubscribeState>('idle');
	let errorMessage = $state<string | null>(null);

	async function unsubscribe() {
		unsubscribeState = 'submitting';
		errorMessage = null;

		try {
			await setupApi.unsubscribeEmailInvitation(token);
			unsubscribeState = 'done';
		} catch (error) {
			unsubscribeState = 'failed';
			errorMessage = toProductApiErrorMessage(
				error,
				'This invitation could not be unsubscribed. The link may be invalid or already removed.'
			);
		}
	}
</script>

<svelte:head>
	<title>Unsubscribe from study invitations - Instruments Platform</title>
</svelte:head>

<main class="respondent-shell">
	<section class="respondent-card">
		<p class="product-kicker">Study invitation email</p>
		<h1 class="product-title">Unsubscribe from future invitations</h1>
		{#if unsubscribeState === 'idle'}
			<p class="mt-3 text-sm leading-6 text-[var(--color-text-muted)]">
				Use this page only if you want this email address added to the workspace
				do-not-contact list for study invitation emails.
			</p>
			<button type="button" class="primary-button mt-4" onclick={unsubscribe}>
				Unsubscribe this email address
			</button>
		{:else if unsubscribeState === 'submitting'}
			<p class="mt-3 flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
				<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
				Applying your do-not-contact request...
			</p>
		{:else if unsubscribeState === 'done'}
			<p class="mt-3 text-sm leading-6 text-[var(--color-text-muted)]">
				This email address has been added to the workspace do-not-contact list for future
				study invitation emails. You can close this page.
			</p>
		{:else if unsubscribeState === 'failed'}
			<p class="error-line mt-3" role="alert">{errorMessage}</p>
			<button type="button" class="secondary-button mt-4" onclick={unsubscribe}>Try again</button>
		{/if}
	</section>
</main>
