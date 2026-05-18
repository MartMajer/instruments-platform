<script lang="ts">
	import type { Snippet } from 'svelte';
	import { ArrowRight, CircleDashed } from 'lucide-svelte';

	let {
		title,
		description,
		actionHref,
		actionLabel,
		children
	}: {
		title: string;
		description: string;
		actionHref?: string;
		actionLabel?: string;
		children?: Snippet;
	} = $props();
</script>

<section class="empty-state" aria-label={title}>
	<CircleDashed size={22} aria-hidden="true" />
	<div class="min-w-0">
		<h2 class="empty-state__title">{title}</h2>
		<p class="empty-state__description">{description}</p>
		{#if children}
			<div class="empty-state__content">
				{@render children()}
			</div>
		{/if}
		{#if actionHref && actionLabel}
			<a class="secondary-button" href={actionHref}>
				<span>{actionLabel}</span>
				<ArrowRight size={15} aria-hidden="true" />
			</a>
		{/if}
	</div>
</section>
