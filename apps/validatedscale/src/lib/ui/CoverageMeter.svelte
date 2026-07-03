<script lang="ts">
	import { t } from '$lib/core/locale.svelte';
	/**
	 * A bullet-style coverage meter for the Field console.
	 * Fill = submitted; hollow remainder = invited; the engraved tick = the
	 * k-anonymity reporting threshold. Meets/below is written in text — never
	 * color alone.
	 */
	let {
		label,
		submitted,
		invited,
		kMin,
		meets
	}: {
		label: string;
		submitted: number;
		invited: number;
		kMin: number;
		meets: boolean;
	} = $props();

	const max = $derived(Math.max(invited, submitted, kMin, 1));
	const fillPct = $derived(Math.min(100, (submitted / max) * 100));
	const invitedPct = $derived(Math.min(100, (invited / max) * 100));
	const kPct = $derived(Math.min(100, (kMin / max) * 100));
</script>

<div class="meter" role="group" aria-label={`${label}: ${submitted} of ${invited} responses, threshold ${kMin}`}>
	<div class="meter-head">
		<span class="meter-label">{label}</span>
		<span class="datum reading">
			{submitted}<span class="dim">/{invited}</span>
			<span class="verdict" class:below={!meets}>{meets ? t('reportable') : `${t('below')} k=${kMin}`}</span>
		</span>
	</div>
	<div class="track">
		<div class="invited" style={`width: ${invitedPct}%`}></div>
		<div class="fill" style={`width: ${fillPct}%`}></div>
		<div class="k" style={`left: ${kPct}%`} title={`k = ${kMin}`}></div>
	</div>
</div>

<style>
	.meter {
		display: flex;
		flex-direction: column;
		gap: 0.4375rem;
	}

	.meter-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 1rem;
	}

	.meter-label {
		font-size: 0.875rem;
		color: var(--color-console-ink);
	}

	.reading {
		font-size: 0.8125rem;
		color: var(--color-console-ink);
	}

	.dim {
		color: var(--color-console-dim);
	}

	.verdict {
		margin-left: 0.625rem;
		font-family: var(--font-ui);
		font-size: 0.6875rem;
		font-weight: 600;
		letter-spacing: 0.06em;
		text-transform: uppercase;
		color: var(--color-console-dim);
	}

	.verdict.below {
		color: #f0b429;
	}

	.track {
		position: relative;
		height: 10px;
		background: var(--color-console-3);
		border-radius: 2px;
		overflow: visible;
	}

	.invited {
		position: absolute;
		inset: 0 auto 0 0;
		border: 1px solid var(--color-console-line);
		border-radius: 2px;
	}

	.fill {
		position: absolute;
		inset: 0 auto 0 0;
		background: var(--color-chart-violet-dark);
		border-radius: 2px 4px 4px 2px;
		transition: width 400ms ease;
	}

	.k {
		position: absolute;
		top: -4px;
		bottom: -4px;
		width: 2px;
		background: var(--color-console-ink);
		transform: translateX(-1px);
	}
</style>
