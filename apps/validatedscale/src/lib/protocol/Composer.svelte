<script lang="ts">
	import { onMount } from 'svelte';
	import { createSetupApi, type InstrumentSummaryResponse } from '$lib/api/setup';
	import { api } from '$lib/core/client';

	let {
		seriesId,
		seriesName,
		onDone
	}: {
		seriesId: string;
		seriesName: string;
		onDone: () => void;
	} = $props();

	const setup = createSetupApi(api());

	let instruments = $state<InstrumentSummaryResponse[]>([]);
	let instrumentId = $state<string>('');

	/* Original example item sets — generic wording, no named-instrument text. */
	const presets = [
		{
			key: 'workload',
			label: 'Workload & recovery (8 items)',
			name: 'Workload and recovery questionnaire',
			low: 'Strongly disagree',
			high: 'Strongly agree',
			calculation: 'mean' as const,
			items: [
				{ text: 'I have enough time to complete my assigned tasks.', reverse: false },
				{ text: 'My workload is predictable from week to week.', reverse: false },
				{ text: 'I often have to skip breaks to keep up with work.', reverse: true },
				{ text: 'After a working day, I recover before the next one begins.', reverse: false },
				{ text: 'I feel drained at the end of a typical shift.', reverse: true },
				{ text: 'I can influence how my work is scheduled.', reverse: false },
				{ text: 'Urgent requests regularly disrupt my planned work.', reverse: true },
				{ text: 'I have the resources I need to do my job well.', reverse: false }
			]
		},
		{
			key: 'wellbeing',
			label: 'Wellbeing check-in (5 items)',
			name: 'Wellbeing check-in',
			low: 'Never',
			high: 'Always',
			calculation: 'mean' as const,
			items: [
				{ text: 'Over the past two weeks, I have felt calm and relaxed.', reverse: false },
				{ text: 'I have felt cheerful and in good spirits.', reverse: false },
				{ text: 'I have woken up feeling fresh and rested.', reverse: false },
				{ text: 'My daily life has been filled with things that interest me.', reverse: false },
				{ text: 'I have felt worried or tense.', reverse: true }
			]
		},
		{
			key: 'ergonomics',
			label: 'Workstation comfort (7 items)',
			name: 'Workstation comfort survey',
			low: 'Never',
			high: 'Very often',
			calculation: 'mean' as const,
			items: [
				{ text: 'I experience neck or shoulder discomfort during work.', reverse: true },
				{ text: 'I experience lower-back discomfort during work.', reverse: true },
				{ text: 'I experience wrist or hand discomfort during work.', reverse: true },
				{ text: 'My screen, chair and desk are adjusted to suit me.', reverse: false },
				{ text: 'I can vary my working posture during the day.', reverse: false },
				{ text: 'I experience eye strain during screen work.', reverse: true },
				{ text: 'I take short movement breaks during the working day.', reverse: false }
			]
		}
	];

	function applyPreset(key: string) {
		const preset = presets.find((candidate) => candidate.key === key);
		if (!preset) return;
		templateName = preset.name;
		lowAnchor = preset.low;
		highAnchor = preset.high;
		calculation = preset.calculation;
		scaleMin = 1;
		scaleMax = 5;
		items = preset.items.map((item) => ({ kind: 'likert' as ItemKind, options: '', ...item }));
	}

	onMount(async () => {
		instruments = await setup.listInstruments().catch(() => []);
	});

	type ItemKind = 'likert' | 'single' | 'multi' | 'text';
	type ItemRow = { text: string; reverse: boolean; kind: ItemKind; options: string };

	let templateName = $state('');
	let locale = $state('en');
	let scaleMin = $state(1);
	let scaleMax = $state(5);
	let lowAnchor = $state('Strongly disagree');
	let highAnchor = $state('Strongly agree');
	let calculation = $state<'mean' | 'sum'>('mean');
	const blankItem = (): ItemRow => ({ text: '', reverse: false, kind: 'likert', options: '' });
	let items = $state<ItemRow[]>([blankItem(), blankItem(), blankItem()]);

	let busy = $state(false);
	let step = $state<string | null>(null);
	let error = $state<string | null>(null);

	const validItems = $derived(items.filter((item) => item.text.trim().length > 0));

	function code(index: number): string {
		return `Q${String(index + 1).padStart(2, '0')}`;
	}

	function slug(): string {
		const base = (templateName || seriesName)
			.toLowerCase()
			.replace(/[^a-z0-9]+/g, '_')
			.replace(/^_+|_+$/g, '')
			.slice(0, 40);
		return `${base || 'study'}_score`;
	}

	function scoringDocument(rows: { code: string; reverse: boolean }[]): string {
		// rows here are the scale-backed items only
		const reverseItems = rows.filter((row) => row.reverse).map((row) => row.code);
		const hasReverse = reverseItems.length > 0;
		const aggregateInput = hasReverse ? 'total_scored_answers' : 'total_answers';
		const nodes: unknown[] = [{ id: 'total_answers', op: 'select_answers', input: 'total_items' }];
		if (hasReverse) {
			nodes.push({
				id: 'total_scored_answers',
				op: 'reverse_code',
				input: 'total_answers',
				scale: 'default_rating',
				reverse_flag_source: 'explicit_list',
				explicit_reverse_items: reverseItems
			});
		}
		nodes.push({
			id: 'total_score',
			op: calculation,
			input: aggregateInput,
			missing_data: { strategy: 'require_all' }
		});

		return JSON.stringify({
			rule_id: slug(),
			rule_version: '1.0.0',
			schema_version: '1.0.0',
			engine_min_version: '1.0.0',
			scale_defaults: { default_rating: { min: scaleMin, max: scaleMax } },
			inputs: [{ id: 'total_items', kind: 'answers', items: rows.map((row) => row.code) }],
			nodes,
			outputs: [{ code: 'total', node: 'total_score' }],
			missing_data: { defaults: { strategy: 'require_all' } }
		});
	}

	async function compose(event: SubmitEvent) {
		event.preventDefault();
		if (busy || validItems.length === 0) return;
		busy = true;
		error = null;

		const rows = validItems.map((item, index) => ({
			code: code(index),
			text: item.text.trim(),
			reverse: item.kind === 'likert' ? item.reverse : false,
			kind: item.kind,
			options: item.options
				.split(/[\n;,]+/)
				.map((option) => option.trim())
				.filter((option) => option.length > 0)
		}));
		const scaleRows = rows.filter((row) => row.kind === 'likert');
		if (scaleRows.length === 0) {
			error = 'At least one scale item is needed so the study produces a score.';
			busy = false;
			return;
		}
		const anchors = JSON.stringify([
			{ value: scaleMin, label: lowAnchor.trim() },
			{ value: scaleMax, label: highAnchor.trim() }
		]);

		try {
			step = 'Creating questionnaire…';
			const template = await setup.createTemplateVersion({
				templateName: templateName.trim() || `${seriesName} questionnaire`,
				semver: '1.0.0',
				defaultLocale: locale,
				instrumentId: instrumentId || null,
				sections: [{ ordinal: 1, code: 'MAIN', titleDefault: 'Questions' }],
				scales: scaleRows.map((row) => ({
					code: `S_${row.code}`,
					type: 'likert',
					minValue: scaleMin,
					maxValue: scaleMax,
					step: 1,
					naAllowed: false,
					anchors
				})),
				questions: rows.map((row, index) => ({
					ordinal: index + 1,
					code: row.code,
					type: row.kind,
					textDefault: row.text,
					sectionCode: 'MAIN',
					scaleCode: row.kind === 'likert' ? `S_${row.code}` : null,
					required: row.kind !== 'text',
					reverseCoded: row.reverse,
					measurementLevel: null,
					payload:
						row.kind === 'single' || row.kind === 'multi'
							? JSON.stringify({
									options: row.options.map((label, optionIndex) => ({
										code: `o${String(optionIndex + 1).padStart(2, '0')}`,
										label
									}))
								})
							: '{}',
					missingCodes: '[]'
				}))
			});

			if (template.status?.toLowerCase() === 'draft') {
				step = 'Publishing…';
				await setup.publishTemplateVersion(template.templateVersionId);
			}

			step = 'Binding scoring rule…';
			await setup.createScoringRule({
				templateVersionId: template.templateVersionId,
				ruleKey: slug(),
				ruleVersion: '1.0.0',
				schemaVersion: '1.0.0',
				engineMinVersion: '1.0.0',
				document: scoringDocument(scaleRows),
				produces: JSON.stringify({ scores: ['total'] }),
				compatibility: '{}'
			});

			step = 'Attaching to study…';
			await setup.selectCampaignSeriesSetupTemplate(seriesId, {
				templateVersionId: template.templateVersionId
			});

			onDone();
		} catch {
			error = 'Composing failed at: ' + (step ?? 'start') + ' Fix the fields and try again.';
		} finally {
			busy = false;
			step = null;
		}
	}
</script>

<form class="composer" onsubmit={compose}>
	<div class="presets">
		<span class="eyebrow">Start from an example</span>
		<div class="preset-row">
			{#each presets as preset (preset.key)}
				<button type="button" class="preset" onclick={() => applyPreset(preset.key)}>
					{preset.label}
				</button>
			{/each}
		</div>
	</div>

	<div class="row">
		<div class="field grow">
			<label class="eyebrow" for="tpl-name">Questionnaire name</label>
			<input id="tpl-name" bind:value={templateName} placeholder={`${seriesName} questionnaire`} />
		</div>
		<div class="field">
			<label class="eyebrow" for="tpl-locale">Language</label>
			<select id="tpl-locale" bind:value={locale}>
				<option value="en">English</option>
				<option value="hr-HR">Hrvatski</option>
			</select>
		</div>
		{#if instruments.length > 0}
			<div class="field">
				<label class="eyebrow" for="tpl-instrument">Instrument (optional)</label>
				<select id="tpl-instrument" bind:value={instrumentId}>
					<option value="">None — custom items</option>
					{#each instruments as instrument (instrument.id)}
						<option value={instrument.id}>{instrument.code} — {instrument.fullName}</option>
					{/each}
				</select>
			</div>
		{/if}
	</div>

	<fieldset class="scale">
		<legend class="eyebrow">Response scale (applies to all items)</legend>
		<div class="row">
			<div class="field small">
				<label class="eyebrow" for="s-min">Min</label>
				<input id="s-min" class="datum" type="number" min="0" max="9" bind:value={scaleMin} />
			</div>
			<div class="field small">
				<label class="eyebrow" for="s-max">Max</label>
				<input id="s-max" class="datum" type="number" min="1" max="11" bind:value={scaleMax} />
			</div>
			<div class="field grow">
				<label class="eyebrow" for="s-low">Low anchor</label>
				<input id="s-low" bind:value={lowAnchor} />
			</div>
			<div class="field grow">
				<label class="eyebrow" for="s-high">High anchor</label>
				<input id="s-high" bind:value={highAnchor} />
			</div>
			<div class="field">
				<label class="eyebrow" for="s-calc">Score</label>
				<select id="s-calc" bind:value={calculation}>
					<option value="mean">Mean of items</option>
					<option value="sum">Sum of items</option>
				</select>
			</div>
		</div>
	</fieldset>

	<fieldset class="items">
		<legend class="eyebrow">Items</legend>
		{#each items as item, index (index)}
			<div class="item-row">
				<span class="datum item-code">{code(index)}</span>
				<input
					class="grow"
					bind:value={item.text}
					placeholder={item.kind === 'likert'
						? 'Statement the respondent rates on the scale'
						: item.kind === 'text'
							? 'Open question (free text, optional)'
							: 'Question with answer options'}
					aria-label={`Item ${index + 1} text`}
				/>
				<select bind:value={item.kind} aria-label={`Item ${index + 1} type`}>
					<option value="likert">Scale</option>
					<option value="single">Choice</option>
					<option value="multi">Multi-choice</option>
					<option value="text">Open text</option>
				</select>
				{#if item.kind === 'likert'}
					<label class="reverse" class:on={item.reverse} title="Reverse-coded item">
						<input type="checkbox" bind:checked={item.reverse} />
						R
					</label>
				{/if}
				<button
					type="button"
					class="remove"
					aria-label={`Remove item ${index + 1}`}
					onclick={() => (items = items.filter((_, i) => i !== index))}
				>
					×
				</button>
			</div>
			{#if item.kind === 'single' || item.kind === 'multi'}
				<input
					class="options-input"
					bind:value={item.options}
					placeholder="Options, comma-separated — Day shift, Night shift, Rotating"
					aria-label={`Item ${index + 1} options`}
				/>
			{/if}
		{/each}
		<button type="button" class="add" onclick={() => (items = [...items, blankItem()])}>
			+ Add item
		</button>
	</fieldset>

	{#if error}<p class="error" role="alert">{error}</p>{/if}

	<div class="actions">
		<button class="btn btn-stain" type="submit" disabled={busy || validItems.length === 0}>
			{busy ? (step ?? 'Working…') : `Create questionnaire (${validItems.length} items)`}
		</button>
		<p class="note">
			Creates the questionnaire, publishes it, binds a {calculation} score with reverse-coding,
			and attaches it to this study.
		</p>
	</div>
</form>

<style>
	.composer {
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
		padding: 1.25rem;
		border: 1px solid var(--color-stain-line);
		background: var(--color-stain-wash);
		border-radius: var(--radius-instrument);
	}

	.presets {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.preset-row {
		display: flex;
		gap: 0.5rem;
		flex-wrap: wrap;
	}

	.preset {
		background: var(--color-surface);
		border: 1px solid var(--color-stain-line);
		border-radius: 999px;
		font: inherit;
		font-size: 0.8125rem;
		font-weight: 540;
		color: var(--color-stain);
		padding: 0.4375rem 0.875rem;
		cursor: pointer;
	}

	.preset:hover {
		background: var(--color-stain);
		border-color: var(--color-stain);
		color: #fff;
	}

	fieldset {
		border: none;
		display: flex;
		flex-direction: column;
		gap: 0.625rem;
	}

	legend {
		margin-bottom: 0.625rem;
	}

	.row {
		display: flex;
		gap: 0.875rem;
		flex-wrap: wrap;
	}

	.field {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
	}

	.field.grow,
	input.grow {
		flex: 1;
		min-width: 10rem;
	}

	.field.small {
		width: 4.5rem;
	}

	input,
	select {
		font: inherit;
		font-size: 0.9375rem;
		padding: 0.5rem 0.625rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	.item-row {
		display: flex;
		align-items: center;
		gap: 0.625rem;
	}

	.item-code {
		font-size: 0.6875rem;
		color: var(--color-stain);
		width: 2rem;
		flex-shrink: 0;
	}

	.options-input {
		margin-left: 2.625rem;
		font-size: 0.8125rem;
	}

	.reverse {
		display: inline-flex;
		align-items: center;
		gap: 0.25rem;
		font-size: 0.75rem;
		font-weight: 600;
		color: var(--color-ink-3);
		border: 1px dashed var(--color-line-2);
		border-radius: var(--radius-instrument);
		padding: 0.4375rem 0.5rem;
		cursor: pointer;
	}

	.reverse.on {
		color: var(--color-stain);
		border-color: var(--color-stain);
		border-style: solid;
	}

	.reverse input {
		position: absolute;
		opacity: 0;
		pointer-events: none;
	}

	.remove {
		background: none;
		border: none;
		font-size: 1.125rem;
		color: var(--color-ink-3);
		cursor: pointer;
		padding: 0.25rem;
		line-height: 1;
	}

	.remove:hover {
		color: var(--color-danger);
	}

	.add {
		align-self: flex-start;
		background: none;
		border: none;
		font: inherit;
		font-size: 0.875rem;
		font-weight: 560;
		color: var(--color-stain);
		cursor: pointer;
		padding: 0.25rem 0;
	}

	.actions {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.actions .btn {
		align-self: flex-start;
	}

	.note {
		font-size: 0.75rem;
		color: var(--color-ink-3);
		max-width: 52ch;
	}

	.error {
		font-size: 0.8125rem;
		color: var(--color-danger);
	}
</style>
