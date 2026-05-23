<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { LoaderCircle } from 'lucide-svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { createSetupApiFromEnv } from '$lib/product/route-state';
	import { toProductApiErrorMessage } from '$lib/product/view-models';

	type UnsubscribeState = 'idle' | 'submitting' | 'done' | 'failed';

	const setupApi = createSetupApiFromEnv(env);
	const token = $derived(page.params.token ?? '');
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));

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
				text.unsubscribe.fallbackError
			);
		}
	}
</script>

<svelte:head>
	<title>{text.unsubscribe.metaTitle}</title>
</svelte:head>

<main class="respondent-shell">
	<section class="respondent-card">
		<p class="product-kicker">{text.unsubscribe.kicker}</p>
		<h1 class="product-title">{text.unsubscribe.title}</h1>
		{#if unsubscribeState === 'idle'}
			<p class="mt-3 text-sm leading-6 text-[var(--color-text-muted)]">
				{text.unsubscribe.body}
			</p>
			<button type="button" class="primary-button mt-4" onclick={unsubscribe}>
				{text.unsubscribe.button}
			</button>
		{:else if unsubscribeState === 'submitting'}
			<p class="mt-3 flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
				<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
				{text.unsubscribe.submitting}
			</p>
		{:else if unsubscribeState === 'done'}
			<p class="mt-3 text-sm leading-6 text-[var(--color-text-muted)]">
				{text.unsubscribe.done}
			</p>
		{:else if unsubscribeState === 'failed'}
			<p class="error-line mt-3" role="alert">{errorMessage}</p>
			<button type="button" class="secondary-button mt-4" onclick={unsubscribe}>{text.unsubscribe.retry}</button>
		{/if}
	</section>
</main>
