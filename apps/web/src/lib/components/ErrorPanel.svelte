<script lang="ts">
	import { AlertTriangle, RefreshCw } from 'lucide-svelte';

	let {
		title = 'Something went wrong',
		message,
		retryLabel = 'Retry',
		onRetry
	}: {
		title?: string;
		message: string;
		retryLabel?: string;
		onRetry?: () => void;
	} = $props();
</script>

<section class="error-panel" role="alert" aria-live="assertive">
	<AlertTriangle size={20} aria-hidden="true" />
	<div class="min-w-0">
		<h2 class="error-panel__title">{title}</h2>
		<p class="error-panel__message">{message}</p>
		{#if onRetry}
			<button type="button" class="secondary-button" onclick={onRetry}>
				<RefreshCw size={15} aria-hidden="true" />
				<span>{retryLabel}</span>
			</button>
		{/if}
	</div>
</section>
