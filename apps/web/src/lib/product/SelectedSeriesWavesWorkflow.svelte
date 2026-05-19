<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { GitCompareArrows, LoaderCircle, RefreshCw } from 'lucide-svelte';
	import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
	import type {
		CampaignSeriesTwoWaveProofResponse,
		CampaignSeriesWaveComparisonProofResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import {
		toSelectedSeriesWavesPath,
		type SelectedSeriesWavesPathStepState,
		type SelectedSeriesWavesWorkflowActionId
	} from './waves-workflow';
	import { createSetupApiFromEnv } from './route-state';
	import { formatScoreOutputMetadata, toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';

	let {
		workspace,
		onWorkspaceRefresh
	}: {
		workspace: CampaignSeriesWavesWorkspaceResponse;
		onWorkspaceRefresh?: () => Promise<boolean>;
	} = $props();

	const setupApi = createSetupApiFromEnv(env);

	let twoWaveProofResult = $state<CampaignSeriesTwoWaveProofResponse | null>(null);
	let waveComparisonProofResult = $state<CampaignSeriesWaveComparisonProofResponse | null>(null);
	let refreshWarning = $state<string | null>(null);
	let actionStates = $state<Record<SelectedSeriesWavesWorkflowActionId, StepState>>({
		twoWaveProof: 'idle',
		waveComparisonProof: 'idle'
	});
	let actionErrors = $state<Record<SelectedSeriesWavesWorkflowActionId, string | null>>({
		twoWaveProof: null,
		waveComparisonProof: null
	});

	const localState = $derived({
		twoWaveProofViewed: Boolean(twoWaveProofResult),
		waveComparisonProofViewed: Boolean(waveComparisonProofResult)
	});
	const wavesPath = $derived(toSelectedSeriesWavesPath(workspace, localState));
	const workflowActions = $derived(wavesPath.steps);
	const currentAction = $derived(wavesPath.currentAction);
	const hasWavesResults = $derived(Boolean(twoWaveProofResult || waveComparisonProofResult));

	function scoreInterpretationMeta(
		interpretation:
			| {
					status: string;
					source: string;
					isValidated: boolean;
					isOfficial: boolean;
			  }
			| null
			| undefined
	) {
		if (!interpretation) {
			return null;
		}

		return [
			interpretation.status.replaceAll('_', ' '),
			interpretation.source.replaceAll('_', ' '),
			interpretation.isValidated ? 'validated' : 'not validated',
			interpretation.isOfficial ? 'official' : 'not official'
		].join(' / ');
	}

	async function refreshTwoWaveProof() {
		const result = await runAction(
			'twoWaveProof',
			() => setupApi.getCampaignSeriesTwoWaveProof(workspace.series.id),
			{ refreshAfter: true }
		);

		if (result) {
			twoWaveProofResult = result;
			waveComparisonProofResult = null;
		}
	}

	async function viewWaveComparisonProof() {
		const result = await runAction(
			'waveComparisonProof',
			() => setupApi.getCampaignSeriesWaveComparisonProof(workspace.series.id),
			{ refreshAfter: true }
		);

		if (result) {
			waveComparisonProofResult = result;
		}
	}

	async function runAction<T>(
		actionId: SelectedSeriesWavesWorkflowActionId,
		action: () => Promise<T>,
		options: { refreshAfter?: boolean } = {}
	) {
		actionStates = { ...actionStates, [actionId]: 'submitting' };
		actionErrors = { ...actionErrors, [actionId]: null };
		refreshWarning = null;

		try {
			const result = await action();
			actionStates = { ...actionStates, [actionId]: 'succeeded' };
			if (options.refreshAfter) {
				const refreshed = await onWorkspaceRefresh?.();
				if (refreshed === false) {
					refreshWarning = 'Waves action completed, but the waves workspace refresh failed.';
				}
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, 'Waves action failed.')
			};
			return null;
		}
	}

	function workflowAction(id: SelectedSeriesWavesWorkflowActionId) {
		return workflowActions.find((action) => action.id === id) ?? workflowActions[0];
	}

	function isActionDisabled(id: SelectedSeriesWavesWorkflowActionId) {
		const action = workflowAction(id);
		return !action.available || actionStates[id] === 'submitting';
	}

	function stepLabel(state: StepState) {
		if (state === 'submitting') {
			return 'Working';
		}

		if (state === 'succeeded') {
			return 'Viewed';
		}

		if (state === 'failed') {
			return 'Failed';
		}

		return 'Ready';
	}

	function pathStateLabel(state: SelectedSeriesWavesPathStepState) {
		if (state === 'done') {
			return 'Done';
		}

		if (state === 'current') {
			return 'Current';
		}

		return 'Blocked';
	}

	function formatNullableScoreValue(value: number | null) {
		return value === null ? 'suppressed' : value.toFixed(2);
	}
</script>

<section class="product-panel" role="group" aria-label="Waves action workflow">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Waves workflow</p>
			<h3 class="product-title">Compare waves</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Check linked trajectories and compare change over time between selected waves.
			</p>
		</div>
		<StatusBadge status={currentAction.status} label={currentAction.title} />
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}

	<div class="setup-path" role="list" aria-label="Waves path">
		{#each wavesPath.steps as action, index (action.id)}
			<div
				class="setup-path__item"
				data-state={action.pathState}
				role="listitem"
				aria-current={action.pathState === 'current' ? 'step' : undefined}
			>
				<span class="setup-path__marker" aria-hidden="true">{index + 1}</span>
				<div class="setup-path__content">
					<p class="setup-path__title">{action.title}</p>
					<p class="setup-path__description">{action.description}</p>
				</div>
				<span class="setup-path__state">{pathStateLabel(action.pathState)}</span>
			</div>
		{/each}
	</div>

	<article class="record-row setup-current-task" role="region" aria-label="Current waves task">
		<div class="setup-current-task__header">
			<div>
				<p class="record-field__label">
					{wavesPath.completedCount} of {wavesPath.totalCount} waves tasks done
				</p>
				<h4 class="setup-current-task__title">Current waves task</h4>
				<p class="record-row__title">{currentAction.title}</p>
				<p class="text-sm text-[var(--color-text-muted)]">{currentAction.description}</p>
			</div>
			<StatusBadge status={currentAction.status} />
		</div>
		{#if currentAction.disabledReason}
			<p class="text-sm text-[var(--color-text-muted)]">{currentAction.disabledReason}</p>
		{/if}

		<div class="setup-current-task__body">
			{#if currentAction.id === 'twoWaveProof'}
				<dl class="record-grid">
					<div class="record-field">
						<dt class="record-field__label">Selected series</dt>
						<dd class="record-field__value">{workspace.series.name}</dd>
					</div>
					<div class="record-field">
						<dt class="record-field__label">Longitudinal waves</dt>
						<dd class="record-field__value">{workspace.summary.longitudinalWaveCount}</dd>
					</div>
					<div class="record-field">
						<dt class="record-field__label">Complete trajectories</dt>
						<dd class="record-field__value">{workspace.summary.completeTrajectoryCount}</dd>
					</div>
				</dl>
				{#if twoWaveProofResult}
					{@render TwoWaveProofResult()}
				{/if}
				{@render ActionFooter({
					id: 'twoWaveProof',
					label: 'Run linked trajectory check',
					resultLabel: 'Study',
					resultValue: twoWaveProofResult ? workspace.series.name : null,
					onclick: refreshTwoWaveProof
				})}
			{:else}
				<dl class="record-grid">
					<div class="record-field">
						<dt class="record-field__label">Baseline</dt>
						<dd class="record-field__value">{workspace.selectedBaselineWave?.name ?? 'Missing'}</dd>
					</div>
					<div class="record-field">
						<dt class="record-field__label">Comparison</dt>
						<dd class="record-field__value">
							{workspace.selectedComparisonWave?.name ?? 'Missing'}
						</dd>
					</div>
					<div class="record-field">
						<dt class="record-field__label">Compatibility</dt>
						<dd class="record-field__value">{workspace.comparison.compatibilityState}</dd>
					</div>
				</dl>
				{#if waveComparisonProofResult}
					{@render WaveComparisonProofResult()}
				{/if}
				{@render ActionFooter({
					id: 'waveComparisonProof',
					label: 'View wave comparison preview',
					resultLabel: 'Study',
					resultValue: waveComparisonProofResult ? workspace.series.name : null,
					onclick: viewWaveComparisonProof
				})}
			{/if}
		</div>
	</article>

	{#if hasWavesResults}
		<details class="record-row" aria-label="Latest waves action details">
			<summary class="record-row__title">Latest action details</summary>
			<div class="record-list mt-4" aria-label="Latest waves results">
				{#if twoWaveProofResult}
					<article class="record-row" aria-label="Latest linked trajectory check result">
						{@render TwoWaveProofResult()}
					</article>
				{/if}
				{#if waveComparisonProofResult}
					<article class="record-row" aria-label="Latest wave comparison preview result">
						{@render WaveComparisonProofResult()}
					</article>
				{/if}
			</div>
		</details>
	{/if}
</section>

{#snippet TwoWaveProofResult()}
	{#if twoWaveProofResult}
		<section class="score-result-panel report-proof-panel" aria-label="Linked trajectory check">
			<div class="score-result-panel__header">
				<div>
					<p class="product-kicker">Wave readiness</p>
					<h4 class="record-row__title">Linked trajectory check</h4>
				</div>
				<StatusBadge status="ready" label="Ready" />
			</div>
			<div class="response-lab__meta">
				<span>launched waves {twoWaveProofResult.launchedWaveCount}</span>
				<span>submitted waves {twoWaveProofResult.submittedWaveCount}</span>
				<span>linked trajectories {twoWaveProofResult.linkedTrajectoryCount}</span>
				<span>complete trajectories {twoWaveProofResult.completeTrajectoryCount}</span>
			</div>
			<div class="record-list">
				{#each twoWaveProofResult.waves as wave (wave.campaignId)}
					<article class="record-row" aria-label={`Wave ${wave.name}`}>
						<div class="record-row__header">
							<h5 class="record-row__title">{wave.name}</h5>
							<StatusBadge status="proof_only" label={wave.status} />
						</div>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Identity mode</dt>
								<dd class="record-field__value">{wave.responseIdentityMode}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">Submitted responses</dt>
								<dd class="record-field__value">{wave.submittedResponseCount}</dd>
							</div>
						</dl>
					</article>
				{/each}
			</div>
		</section>
	{/if}
{/snippet}

{#snippet WaveComparisonProofResult()}
	{#if waveComparisonProofResult}
		<section class="score-result-panel report-proof-panel" aria-label="Wave comparison preview">
			<div class="score-result-panel__header">
				<div>
					<p class="product-kicker">Wave comparison</p>
					<h4 class="record-row__title">Disclosure-gated comparison</h4>
				</div>
				<StatusBadge status="ready" label="Ready" />
			</div>
			<div class="response-lab__meta">
				<span>{waveComparisonProofResult.interpretationStatus}</span>
				{#if waveComparisonProofResult.disclosurePolicy}
					<span>Disclosure k={waveComparisonProofResult.disclosurePolicy.kMin}</span>
				{/if}
			</div>
			{#if waveComparisonProofResult.baselineWave && waveComparisonProofResult.comparisonWave}
				<div class="response-lab__meta">
					<span>{waveComparisonProofResult.baselineWave.name}</span>
					<span>{waveComparisonProofResult.comparisonWave.name}</span>
					<span>{waveComparisonProofResult.baselineWave.scoringRuleVersion}</span>
					<span>{waveComparisonProofResult.comparisonWave.scoringRuleVersion}</span>
				</div>
			{/if}
			<div class="score-card-list" aria-label="Wave comparison scores">
				{#each waveComparisonProofResult.scores as score (score.dimensionCode)}
					{@const baselineScoreMetadata =
						score.disclosure === 'visible'
							? formatScoreOutputMetadata(
									score.baselineNValidTotal,
									score.baselineNExpectedTotal,
									score.baselineMissingPolicyStatusSummary
								)
							: null}
					{@const comparisonScoreMetadata =
						score.disclosure === 'visible'
							? formatScoreOutputMetadata(
									score.comparisonNValidTotal,
									score.comparisonNExpectedTotal,
									score.comparisonMissingPolicyStatusSummary
								)
							: null}
					<article class="score-card" aria-label={`Wave comparison ${score.dimensionCode}`}>
						<div>
							<p class="score-card__label">{score.dimensionCode}</p>
							<p
								class={score.disclosure === 'visible'
									? 'score-card__value'
									: 'score-card__interpretation'}
							>
								{formatNullableScoreValue(score.aggregateDelta)}
							</p>
						</div>
						<p class="score-card__meta">{score.compatibilityStatus}</p>
						<p class="score-card__interpretation">{score.disclosure}</p>
						<p class="score-card__interpretation">
							paired delta {formatNullableScoreValue(score.pairedDeltaMean)}
						</p>
						{#if baselineScoreMetadata}
							<p class="score-card__interpretation">baseline {baselineScoreMetadata}</p>
						{/if}
						{#if comparisonScoreMetadata}
							<p class="score-card__interpretation">comparison {comparisonScoreMetadata}</p>
						{/if}
						{#if score.baselineInterpretation}
							<p class="score-card__interpretation">
								baseline band {score.baselineInterpretation.label}
							</p>
						{/if}
						{#if score.comparisonInterpretation}
							<p class="score-card__interpretation">
								comparison band {score.comparisonInterpretation.label}
							</p>
						{/if}
						{#if score.baselineInterpretation || score.comparisonInterpretation}
							<p class="score-card__interpretation">
								{scoreInterpretationMeta(
									score.baselineInterpretation ?? score.comparisonInterpretation
								)}
							</p>
						{/if}
					</article>
				{/each}
			</div>
		</section>
	{/if}
{/snippet}

{#snippet ActionFooter({
	id,
	label,
	resultLabel,
	resultValue,
	onclick
}: {
	id: SelectedSeriesWavesWorkflowActionId;
	label: string;
	resultLabel: string;
	resultValue: string | null | undefined;
	onclick: () => void | Promise<void>;
})}
	<div class="action-row">
		<button
			type="button"
			class="primary-button"
			disabled={isActionDisabled(id)}
			title={workflowAction(id).disabledReason ?? undefined}
			{onclick}
		>
			{#if actionStates[id] === 'submitting'}
				<LoaderCircle size={17} aria-hidden="true" />
			{:else if id === 'twoWaveProof'}
				<RefreshCw size={17} aria-hidden="true" />
			{:else}
				<GitCompareArrows size={17} aria-hidden="true" />
			{/if}
			<span>{label}</span>
		</button>
		<p class="step-pill" data-state={actionStates[id]}>{stepLabel(actionStates[id])}</p>
		{@render ResultLine({ label: resultLabel, value: resultValue })}
	</div>
	{#if actionErrors[id]}
		<p class="error-line">{actionErrors[id]}</p>
	{/if}
{/snippet}

{#snippet ResultLine({ label, value }: { label: string; value: string | null | undefined })}
	{#if value}
		<p class="result-line">
			<span>{label}</span>
			<code>{value}</code>
		</p>
	{/if}
{/snippet}
