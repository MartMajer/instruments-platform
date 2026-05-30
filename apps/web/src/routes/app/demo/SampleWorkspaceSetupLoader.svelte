<script lang="ts">
	import { onMount } from 'svelte';
	import { Check, LoaderCircle } from 'lucide-svelte';

	let {
		kicker,
		title,
		body,
		progressLabel,
		stepsLabel,
		steps
	}: {
		kicker: string;
		title: string;
		body: string;
		progressLabel: string;
		stepsLabel: string;
		steps: string[];
	} = $props();

	let progress = $state(8);
	let stepIndex = $state(0);

	const safeSteps = $derived(steps.length > 0 ? steps : [title]);
	const currentStep = $derived(safeSteps[Math.min(stepIndex, safeSteps.length - 1)] ?? title);

	onMount(() => {
		const startedAt = Date.now();
		const interval = window.setInterval(() => {
			const elapsed = Date.now() - startedAt;
			const nextProgress =
				elapsed < 600
					? Math.min(24, progress + 8)
					: elapsed < 1_600
						? Math.min(52, progress + 5)
						: elapsed < 3_200
							? Math.min(76, progress + 4)
							: Math.min(90, progress + 2);

			progress = Math.max(progress, nextProgress);
			stepIndex = progress < 30 ? 0 : progress < 58 ? 1 : progress < 82 ? 2 : 3;
		}, 220);

		return () => {
			window.clearInterval(interval);
		};
	});

	function stepClass(index: number) {
		const classes = ['sample-setup-loader__step'];

		if (index < stepIndex) {
			classes.push('sample-setup-loader__step--complete');
		} else if (index === stepIndex) {
			classes.push('sample-setup-loader__step--active');
		}

		return classes.join(' ');
	}
</script>

<section class="sample-setup-loader" role="status" aria-live="polite" aria-label={title}>
	<div class="sample-setup-loader__header">
		<span class="sample-setup-loader__spinner" aria-hidden="true">
			<LoaderCircle size={22} class="animate-spin" />
		</span>
		<div>
			<p class="workspace-home-kicker">{kicker}</p>
			<h2>{title}</h2>
			<p>{body}</p>
		</div>
	</div>

	<div class="sample-setup-loader__progress">
		<div class="sample-setup-loader__progress-header">
			<span>{currentStep}</span>
			<span>{Math.round(progress)}%</span>
		</div>
		<div
			class="sample-setup-loader__track"
			role="progressbar"
			aria-label={progressLabel}
			aria-valuemin="0"
			aria-valuemax="100"
			aria-valuenow={Math.round(progress)}
		>
			<span class="sample-setup-loader__bar" style={`width: ${Math.round(progress)}%`}></span>
		</div>
	</div>

	<ol class="sample-setup-loader__steps" aria-label={stepsLabel}>
		{#each safeSteps as step, index}
			<li class={stepClass(index)}>
				<span class="sample-setup-loader__step-icon" aria-hidden="true">
					{#if index < stepIndex}
						<Check size={14} />
					{:else}
						{index + 1}
					{/if}
				</span>
				<span>{step}</span>
			</li>
		{/each}
	</ol>
</section>

<style>
	.sample-setup-loader {
		display: grid;
		gap: 1.25rem;
		width: min(100%, 760px);
		margin: 0 auto;
		padding: 1.5rem;
		border: 1px solid color-mix(in srgb, var(--color-border, #d4d7dd) 82%, transparent);
		border-radius: 8px;
		background:
			linear-gradient(135deg, color-mix(in srgb, #eef7f3 72%, transparent), transparent 44%),
			var(--color-surface, #ffffff);
		color: var(--color-text, #141821);
	}

	.sample-setup-loader__header {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr);
		gap: 0.9rem;
		align-items: start;
	}

	.sample-setup-loader__spinner,
	.sample-setup-loader__step-icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		border-radius: 999px;
	}

	.sample-setup-loader__spinner {
		width: 2.75rem;
		height: 2.75rem;
		background: color-mix(in srgb, var(--color-accent, #2364aa) 12%, transparent);
		color: var(--color-accent, #2364aa);
	}

	.sample-setup-loader h2 {
		margin: 0.1rem 0 0.35rem;
		font-size: clamp(1.25rem, 1.1rem + 0.4vw, 1.55rem);
		font-weight: 700;
		letter-spacing: 0;
	}

	.sample-setup-loader p {
		margin: 0;
		max-width: 60ch;
		color: var(--color-text-muted, #566173);
		line-height: 1.6;
	}

	.sample-setup-loader__progress {
		display: grid;
		gap: 0.55rem;
	}

	.sample-setup-loader__progress-header {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		font-size: 0.9rem;
		font-weight: 650;
		color: var(--color-text, #141821);
	}

	.sample-setup-loader__track {
		position: relative;
		height: 0.65rem;
		overflow: hidden;
		border-radius: 999px;
		background: color-mix(in srgb, var(--color-border, #d4d7dd) 58%, white);
	}

	.sample-setup-loader__bar {
		position: absolute;
		inset: 0 auto 0 0;
		min-width: 0.8rem;
		border-radius: inherit;
		background: linear-gradient(90deg, #2364aa, #1f8f70);
		transition: width 220ms ease;
	}

	.sample-setup-loader__steps {
		display: grid;
		grid-template-columns: repeat(4, minmax(0, 1fr));
		gap: 0.65rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}

	.sample-setup-loader__step {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		min-width: 0;
		padding: 0.65rem;
		border: 1px solid color-mix(in srgb, var(--color-border, #d4d7dd) 78%, transparent);
		border-radius: 8px;
		background: color-mix(in srgb, white 84%, transparent);
		color: var(--color-text-muted, #566173);
		font-size: 0.82rem;
		line-height: 1.35;
	}

	.sample-setup-loader__step--active {
		border-color: color-mix(in srgb, var(--color-accent, #2364aa) 42%, var(--color-border, #d4d7dd));
		color: var(--color-text, #141821);
	}

	.sample-setup-loader__step--complete {
		color: var(--color-text, #141821);
	}

	.sample-setup-loader__step-icon {
		flex: 0 0 auto;
		width: 1.35rem;
		height: 1.35rem;
		background: color-mix(in srgb, var(--color-accent, #2364aa) 10%, transparent);
		color: var(--color-accent, #2364aa);
		font-size: 0.72rem;
		font-weight: 700;
	}

	@media (max-width: 720px) {
		.sample-setup-loader {
			padding: 1rem;
		}

		.sample-setup-loader__steps {
			grid-template-columns: 1fr;
		}
	}
</style>
