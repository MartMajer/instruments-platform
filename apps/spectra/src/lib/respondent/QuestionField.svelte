<script lang="ts">
	import type { RespondentQuestionResponse } from '$lib/api/setup';
	import { parseAnchors, parseOptions, scaleRange } from './answers';

	let {
		question,
		value = $bindable(),
		copy
	}: {
		question: RespondentQuestionResponse;
		value: unknown;
		copy: { naLabel: string; typeUnsupported: string };
	} = $props();

	const anchors = $derived(parseAnchors(question));
	const options = $derived(parseOptions(question));
	const range = $derived(scaleRange(question));

	function anchorFor(v: number): string | null {
		return anchors.find((anchor) => anchor.value === v)?.label ?? null;
	}

	const lowAnchor = $derived(anchorFor(range[0]));
	const highAnchor = $derived(anchorFor(range[range.length - 1]));

	function toggleMulti(code: string) {
		const current = Array.isArray(value) ? [...(value as string[])] : [];
		const index = current.indexOf(code);
		if (index >= 0) current.splice(index, 1);
		else current.push(code);
		value = current;
	}
</script>

{#if question.type === 'likert' || question.type === 'nps'}
	<div class="scale" role="radiogroup" aria-label={question.textDefault}>
		<div class="scale-buttons">
			{#each range as step (step)}
				<button
					type="button"
					role="radio"
					aria-checked={String(value) === String(step)}
					class="scale-btn datum"
					class:on={String(value) === String(step)}
					title={anchorFor(step) ?? undefined}
					onclick={() => (value = String(step))}
				>
					{step}
				</button>
			{/each}
		</div>
		{#if lowAnchor || highAnchor}
			<div class="scale-anchors">
				<span>{lowAnchor ?? ''}</span>
				<span>{highAnchor ?? ''}</span>
			</div>
		{/if}
		{#if question.scaleNaAllowed}
			<button
				type="button"
				class="na"
				class:on={value === 'NA'}
				onclick={() => (value = value === 'NA' ? '' : 'NA')}
			>
				{copy.naLabel}
			</button>
		{/if}
	</div>
{:else if question.type === 'single'}
	<div class="choices" role="radiogroup" aria-label={question.textDefault}>
		{#each options as option (option.code)}
			<label class="choice" class:on={value === option.code}>
				<input
					type="radio"
					name={question.id}
					value={option.code}
					checked={value === option.code}
					onchange={() => (value = option.code)}
				/>
				<span>{option.label}</span>
			</label>
		{/each}
	</div>
{:else if question.type === 'multi'}
	<div class="choices" role="group" aria-label={question.textDefault}>
		{#each options as option (option.code)}
			{@const selected = Array.isArray(value) && (value as string[]).includes(option.code)}
			<label class="choice" class:on={selected}>
				<input type="checkbox" checked={selected} onchange={() => toggleMulti(option.code)} />
				<span>{option.label}</span>
			</label>
		{/each}
	</div>
{:else if question.type === 'number'}
	<input
		class="field"
		type="number"
		inputmode="numeric"
		value={typeof value === 'string' ? value : ''}
		oninput={(event) => (value = event.currentTarget.value)}
	/>
{:else if question.type === 'date'}
	<input
		class="field"
		type="date"
		value={typeof value === 'string' ? value : ''}
		oninput={(event) => (value = event.currentTarget.value)}
	/>
{:else if question.type === 'comment'}
	<textarea
		class="field"
		rows="4"
		value={typeof value === 'string' ? value : ''}
		oninput={(event) => (value = event.currentTarget.value)}
	></textarea>
{:else}
	{#if question.type !== 'text'}
		<p class="unsupported">{copy.typeUnsupported}</p>
	{/if}
	<input
		class="field"
		type="text"
		value={typeof value === 'string' ? value : ''}
		oninput={(event) => (value = event.currentTarget.value)}
	/>
{/if}

<style>
	.scale-buttons {
		display: flex;
		flex-wrap: wrap;
		gap: 0.375rem;
	}

	.scale-btn {
		min-width: 2.75rem;
		height: 2.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
		font-size: 0.9375rem;
		cursor: pointer;
		color: var(--color-ink);
	}

	.scale-btn:hover {
		border-color: var(--color-stain);
	}

	.scale-btn.on {
		background: var(--color-stain);
		border-color: var(--color-stain);
		color: #fff;
	}

	.scale-anchors {
		display: flex;
		justify-content: space-between;
		margin-top: 0.5rem;
		font-size: 0.75rem;
		color: var(--color-ink-3);
		gap: 2rem;
	}

	.na {
		margin-top: 0.625rem;
		background: none;
		border: 1px dashed var(--color-line-2);
		border-radius: var(--radius-instrument);
		padding: 0.375rem 0.75rem;
		font: inherit;
		font-size: 0.8125rem;
		color: var(--color-ink-2);
		cursor: pointer;
	}

	.na.on {
		border-style: solid;
		border-color: var(--color-stain);
		color: var(--color-stain);
	}

	.choices {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.choice {
		display: flex;
		align-items: baseline;
		gap: 0.625rem;
		padding: 0.75rem 0.875rem;
		border: 1px solid var(--color-line);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
		font-size: 0.9375rem;
		cursor: pointer;
	}

	.choice:hover {
		border-color: var(--color-line-2);
	}

	.choice.on {
		border-color: var(--color-stain);
		background: var(--color-stain-wash);
	}

	.choice input {
		accent-color: var(--color-stain);
	}

	.field {
		font: inherit;
		width: 100%;
		max-width: 24rem;
		padding: 0.625rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	textarea.field {
		max-width: 100%;
		resize: vertical;
	}

	.field:focus-visible {
		outline-offset: 0;
		border-color: var(--color-stain);
	}

	.unsupported {
		font-size: 0.8125rem;
		color: var(--color-ink-3);
		margin-bottom: 0.375rem;
	}
</style>
