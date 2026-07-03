<script lang="ts">
	/**
	 * The signature motif: a calibrated rail with wave marks.
	 * Ticks are the fine track; each wave is an engraved mark whose fill encodes
	 * state (done = filled ink, live = stain pulse, planned = hollow).
	 */
	type WaveMark = {
		id: string;
		label: string;
		state: 'done' | 'live' | 'planned';
	};

	let {
		marks,
		dark = false
	}: {
		marks: WaveMark[];
		dark?: boolean;
	} = $props();
</script>

<div class="waverail" class:dark role="list" aria-label="Waves">
	<div class="track rail-h" aria-hidden="true"></div>
	{#each marks as mark (mark.id)}
		<div class="mark" role="listitem">
			<span class="pip {mark.state}" aria-hidden="true"></span>
			<span class="label datum">{mark.label}</span>
			<span class="sr-only">{mark.state === 'done' ? 'completed' : mark.state}</span>
		</div>
	{/each}
</div>

<style>
	.waverail {
		position: relative;
		display: flex;
		align-items: flex-start;
		gap: 2rem;
		padding-top: 0.375rem;
	}

	.track {
		position: absolute;
		inset: 0 0 auto 0;
		height: 7px;
		--rail-pitch: 7px;
	}

	.dark .track {
		--rail-ink: var(--color-console-line);
	}

	.mark {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.375rem;
		min-width: 2.5rem;
	}

	.pip {
		width: 11px;
		height: 11px;
		border-radius: 999px;
		border: 2px solid var(--color-ink);
		background: transparent;
		margin-top: -0.6rem;
		z-index: 1;
	}

	.dark .pip {
		border-color: var(--color-console-ink);
	}

	.pip.done {
		background: var(--color-ink);
	}

	.dark .pip.done {
		background: var(--color-console-ink);
	}

	.pip.live {
		border-color: var(--color-stain);
		background: var(--color-stain);
		box-shadow: 0 0 0 4px var(--color-stain-wash);
	}

	.dark .pip.live {
		border-color: var(--color-stain-bright);
		background: var(--color-stain-bright);
		box-shadow: 0 0 0 4px color-mix(in oklab, var(--color-stain-bright) 25%, transparent);
	}

	.label {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.dark .label {
		color: var(--color-console-dim);
	}

	.sr-only {
		position: absolute;
		width: 1px;
		height: 1px;
		overflow: hidden;
		clip-path: inset(50%);
	}
</style>
