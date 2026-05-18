<script lang="ts">
	import type { Snippet } from 'svelte';
	import { AlertTriangle, CheckCircle2, Info } from 'lucide-svelte';

	type AlertVariant = 'info' | 'success' | 'warning' | 'danger';

	let {
		variant = 'info',
		title,
		message,
		children
	}: {
		variant?: AlertVariant;
		title: string;
		message?: string | null;
		children?: Snippet;
	} = $props();

	const role = $derived(variant === 'danger' ? 'alert' : 'status');
</script>

<section class="inline-alert" data-variant={variant} {role} aria-live="polite">
	<span class="inline-alert__icon" aria-hidden="true">
		{#if variant === 'success'}
			<CheckCircle2 size={18} />
		{:else if variant === 'warning' || variant === 'danger'}
			<AlertTriangle size={18} />
		{:else}
			<Info size={18} />
		{/if}
	</span>
	<div class="min-w-0">
		<p class="inline-alert__title">{title}</p>
		{#if message}
			<p class="inline-alert__message">{message}</p>
		{/if}
		{#if children}
			<div class="inline-alert__content">
				{@render children()}
			</div>
		{/if}
	</div>
</section>
