<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		state,
		error = null,
		emptyTitle = 'Nothing here yet',
		emptyBody = '',
		children
	}: {
		state: 'loading' | 'error' | 'empty' | 'ready';
		error?: string | null;
		emptyTitle?: string;
		emptyBody?: string;
		children?: Snippet;
	} = $props();
</script>

{#if state === 'loading'}
	<div class="panel state" role="status" aria-live="polite">
		<span class="pulse" aria-hidden="true"></span>
		<span class="eyebrow">Reading</span>
	</div>
{:else if state === 'error'}
	<div class="panel state error" role="alert">
		<span class="eyebrow" style="color: var(--color-danger)">Could not load</span>
		<p>{error ?? 'The workspace did not respond. Reload to try again.'}</p>
	</div>
{:else if state === 'empty'}
	<div class="panel state">
		<span class="eyebrow">{emptyTitle}</span>
		{#if emptyBody}<p>{emptyBody}</p>{/if}
	</div>
{:else if children}
	{@render children()}
{/if}

<style>
	.state {
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: 0.5rem;
		padding: 2rem;
	}

	.state p {
		font-size: 0.875rem;
		color: var(--color-ink-2);
		max-width: 44ch;
	}

	.pulse {
		width: 34px;
		height: 7px;
		background-image: repeating-linear-gradient(
			to right,
			var(--color-line-2) 0 1px,
			transparent 1px 7px
		);
		animation: sweep 1.1s linear infinite;
	}

	@keyframes sweep {
		from {
			background-position-x: 0;
		}
		to {
			background-position-x: 7px;
		}
	}
</style>
