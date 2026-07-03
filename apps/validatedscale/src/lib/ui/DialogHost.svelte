<script lang="ts">
	import { dialogState, settleDialog } from './dialog.svelte';

	let value = $state('');

	$effect(() => {
		if (dialogState.current) {
			value = dialogState.current.initialValue ?? '';
		}
	});

	function confirm() {
		if (!dialogState.current) return;
		if (dialogState.current.kind === 'prompt') {
			if (!value.trim()) return;
			settleDialog(value.trim());
		} else {
			settleDialog(true);
		}
	}

	function onkeydown(event: KeyboardEvent) {
		if (!dialogState.current) return;
		if (event.key === 'Escape') settleDialog(null);
		if (event.key === 'Enter' && dialogState.current.kind === 'prompt') confirm();
	}
</script>

<svelte:window {onkeydown} />

{#if dialogState.current}
	<div class="scrim" role="presentation" onclick={() => settleDialog(null)}>
		<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
		<div
			class="dialog panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			aria-label={dialogState.current.title}
			onclick={(event) => event.stopPropagation()}
			onkeydown={(event) => { if (event.key === 'Escape') settleDialog(null); }}
		>
			<h2 class="doc-title">{dialogState.current.title}</h2>
			{#if dialogState.current.body}<p class="body">{dialogState.current.body}</p>{/if}
			{#if dialogState.current.kind === 'prompt'}
				<!-- svelte-ignore a11y_autofocus -->
				<input
					autofocus
					bind:value
					placeholder={dialogState.current.placeholder}
					aria-label={dialogState.current.title}
				/>
			{/if}
			<div class="actions">
				<button class="btn btn-ghost" onclick={() => settleDialog(null)}>
					{dialogState.current.cancelLabel}
				</button>
				<button
					class="btn"
					class:btn-stain={!dialogState.current.danger}
					class:btn-danger={dialogState.current.danger}
					disabled={dialogState.current.kind === 'prompt' && !value.trim()}
					onclick={confirm}
				>
					{dialogState.current.confirmLabel}
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.scrim {
		position: fixed;
		inset: 0;
		background: rgb(21 28 37 / 0.45);
		display: flex;
		align-items: flex-start;
		justify-content: center;
		padding: 18vh 1rem 0;
		z-index: 100;
	}

	.dialog {
		width: min(26rem, 100%);
		padding: 1.5rem;
		border-top: 3px solid var(--color-stain);
		display: flex;
		flex-direction: column;
		gap: 0.875rem;
	}

	.dialog h2 {
		font-size: 1.25rem;
	}

	.body {
		font-size: 0.875rem;
		line-height: 1.55;
		color: var(--color-ink-2);
	}

	.dialog input {
		font: inherit;
		padding: 0.625rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
	}

	.actions {
		display: flex;
		justify-content: flex-end;
		gap: 0.625rem;
	}

	.btn-danger {
		background: var(--color-danger);
		color: #fff;
	}
</style>
