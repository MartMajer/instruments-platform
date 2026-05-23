<script lang="ts">
	import { page } from '$app/state';
	import { env } from '$env/dynamic/public';
	import { GitCompareArrows, LoaderCircle, RefreshCw } from 'lucide-svelte';
	import type { CampaignSeriesWavesWorkspaceResponse } from '$lib/api/product';
	import type {
		CampaignSeriesTwoWaveProofResponse,
		CampaignSeriesWaveComparisonProofResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import SelectedSeriesWaveComparisonSnapshot from '$lib/product/SelectedSeriesWaveComparisonSnapshot.svelte';
	import {
		toSelectedSeriesGroupTrendPlan,
		toSelectedSeriesWaveScoreMethodReview,
		toSelectedSeriesWaveComparisonReview,
		toSelectedSeriesWavePlan,
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
	const appLocale = $derived(appLocaleFromPageData(page.data));
	const wavesWorkflowCopy = $derived(routePageCopy(appLocale).selectedStudy.wavesWorkflow);
	const wavePlan = $derived(toSelectedSeriesWavePlan(workspace, wavesWorkflowCopy));
	const groupTrendPlan = $derived(toSelectedSeriesGroupTrendPlan(workspace, wavesWorkflowCopy));
	const comparisonReview = $derived(toSelectedSeriesWaveComparisonReview(workspace, wavesWorkflowCopy));
	const methodReview = $derived(
		toSelectedSeriesWaveScoreMethodReview(workspace, waveComparisonProofResult, wavesWorkflowCopy)
	);
	const wavesPath = $derived(toSelectedSeriesWavesPath(workspace, localState, wavesWorkflowCopy));
	const workflowActions = $derived(wavesPath.steps);
	const currentAction = $derived(wavesPath.currentAction);
	const resultsHref = $derived(`/app/campaign-series/${workspace.series.id}/reports`);
	const setupHref = $derived(`/app/campaign-series/${workspace.series.id}/setup`);

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
			humanize(interpretation.status),
			humanize(interpretation.source),
			interpretation.isValidated ? 'reviewed' : 'not reviewed',
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

	function inactivePathTitle() {
		if (wavesPath.mode === 'group_trend') {
			return 'Linked-change checks not needed';
		}

		return 'Linked-change checks not active yet';
	}

	function formatNullableScoreValue(value: number | null) {
		return value === null ? 'suppressed' : value.toFixed(2);
	}

	function humanize(value: string | null | undefined) {
		return value ? value.replaceAll('_', ' ') : 'Not available';
	}
</script>

<section class="product-panel" role="group" aria-label="Waves action workflow">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Study flow · Waves</p>
			<h3 class="product-title">Repeat the study and compare waves</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Create follow-up waves from Setup, collect responses from Collection, then compare closed waves here.
			</p>
		</div>
		<StatusBadge status={wavePlan.status} label={wavePlan.title} />
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}

	<article class="record-row setup-current-task" role="region" aria-label="Wave plan">
		<div class="setup-current-task__header">
			<div>
				<p class="record-field__label">Where waves fit</p>
				<h4 class="setup-current-task__title">{wavePlan.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{wavePlan.description}</p>
			</div>
			<StatusBadge status={wavePlan.status} />
		</div>
		<ul class="grid gap-2 text-sm leading-6 text-[var(--color-text-muted)]">
			{#each wavePlan.guidance as item}
				<li>{item}</li>
			{/each}
		</ul>
		<div class="action-row">
			{#if wavePlan.primaryHref}
				<a class="primary-button" href={wavePlan.primaryHref}>{wavePlan.primaryLabel}</a>
			{:else}
				<p class="step-pill" data-state="idle">{wavePlan.primaryLabel}</p>
			{/if}
			{#if wavePlan.secondaryHref && wavePlan.secondaryLabel}
				<a class="secondary-button" href={wavePlan.secondaryHref}>{wavePlan.secondaryLabel}</a>
			{/if}
		</div>
	</article>

	<article class="questionnaire-blueprint-review questionnaire-blueprint-review--section" role="region" aria-label="Wave comparison plan">
		<div class="questionnaire-blueprint-review__header">
			<div>
				<p class="product-kicker">Comparison plan</p>
				<h4 class="setup-current-task__title">{comparisonReview.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{comparisonReview.description}</p>
			</div>
			<StatusBadge status={comparisonReview.status} />
		</div>
		<div class="questionnaire-blueprint-review__grid">
			{#each comparisonReview.items as item (item.id)}
				<section
					class="questionnaire-blueprint-review__item"
					data-state={item.status}
					aria-label={item.label}
				>
					<div class="questionnaire-blueprint-review__item-header">
						<p class="record-field__label">{item.label}</p>
						<StatusBadge status={item.status} />
					</div>
					<p class="record-row__title">{item.summary}</p>
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">{item.detail}</p>
				</section>
			{/each}
		</div>
	</article>

	<article class="questionnaire-blueprint-review questionnaire-blueprint-review--section" role="region" aria-label="Wave score method review">
		<div class="questionnaire-blueprint-review__header">
			<div>
				<p class="product-kicker">Score method</p>
				<h4 class="setup-current-task__title">{methodReview.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{methodReview.description}</p>
			</div>
			<StatusBadge status={methodReview.status} />
		</div>
		<div class="questionnaire-blueprint-review__grid">
			{#each methodReview.items as item (item.id)}
				<section
					class="questionnaire-blueprint-review__item"
					data-state={item.status}
					aria-label={item.label}
				>
					<div class="questionnaire-blueprint-review__item-header">
						<p class="record-field__label">{item.label}</p>
						<StatusBadge status={item.status} />
					</div>
					<p class="record-row__title">{item.summary}</p>
					<p class="text-sm leading-6 text-[var(--color-text-muted)]">{item.detail}</p>
				</section>
			{/each}
		</div>
	</article>

	<article class="record-row setup-current-task" role="region" aria-label="Group trend review">
		<div class="setup-current-task__header">
			<div>
				<p class="record-field__label">Group trend</p>
				<h4 class="setup-current-task__title">{groupTrendPlan.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{groupTrendPlan.description}</p>
			</div>
			<StatusBadge status={groupTrendPlan.status} />
		</div>
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">First wave</dt>
				<dd class="record-field__value">{groupTrendPlan.baselineName ?? 'Missing'}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">First wave responses</dt>
				<dd class="record-field__value">{groupTrendPlan.baselineResponseCount ?? 0}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Second wave</dt>
				<dd class="record-field__value">{groupTrendPlan.comparisonName ?? 'Missing'}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">Second wave responses</dt>
				<dd class="record-field__value">{groupTrendPlan.comparisonResponseCount ?? 0}</dd>
			</div>
			{#each groupTrendPlan.safetyRows as row}
				<div class="record-field">
					<dt class="record-field__label">{row.label}</dt>
					<dd class="record-field__value">{row.value}</dd>
				</div>
			{/each}
		</dl>
		<ul class="grid gap-2 text-sm leading-6 text-[var(--color-text-muted)]">
			{#each groupTrendPlan.guidance as item}
				<li>{item}</li>
			{/each}
		</ul>
		<div class="action-row">
			{#if groupTrendPlan.primaryHref}
				<a class="primary-button" href={groupTrendPlan.primaryHref}>{groupTrendPlan.primaryLabel}</a>
			{:else}
				<p class="step-pill" data-state="idle">{groupTrendPlan.primaryLabel}</p>
			{/if}
			{#if groupTrendPlan.secondaryHref && groupTrendPlan.secondaryLabel}
				<a class="secondary-button" href={groupTrendPlan.secondaryHref}>
					{groupTrendPlan.secondaryLabel}
				</a>
			{/if}
		</div>
	</article>

	{#if wavesPath.showWorkflow}
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
						{wavesPath.completedCount} of {wavesPath.totalCount} comparison tasks done
					</p>
					<h4 class="setup-current-task__title">Current comparison task</h4>
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
							<dt class="record-field__label">Repeated waves</dt>
							<dd class="record-field__value">{workspace.summary.longitudinalWaveCount}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Potential complete trajectories</dt>
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
							<dd class="record-field__value">{humanize(workspace.comparison.compatibilityState)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Disclosure</dt>
							<dd class="record-field__value">{humanize(workspace.comparison.disclosureState)}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Minimum group size</dt>
							<dd class="record-field__value">
								{workspace.comparison.disclosureKMin ?? 'Not configured'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Suppressed comparisons</dt>
							<dd class="record-field__value">{workspace.summary.suppressedComparisonCount}</dd>
						</div>
					</dl>
					<SelectedSeriesWaveComparisonSnapshot {workspace} embedded={true} />
					{#if waveComparisonProofResult}
						{@render WaveComparisonProofResult()}
					{/if}
					{@render ActionFooter({
						id: 'waveComparisonProof',
						label: 'Review comparison',
						resultLabel: 'Comparison',
						resultValue: waveComparisonProofResult ? 'Reviewed' : null,
						onclick: viewWaveComparisonProof
					})}
				{/if}
			</div>
		</article>
	{:else}
		<article class="record-row setup-current-task" role="region" aria-label="Linked change task status">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Linked-change workflow</p>
					<h4 class="setup-current-task__title">{inactivePathTitle()}</h4>
					<p class="text-sm text-[var(--color-text-muted)]">{wavesPath.inactiveReason}</p>
				</div>
				<StatusBadge status={comparisonReview.status} />
			</div>
		</article>
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
				<span>{twoWaveProofResult.launchedWaveCount} launched waves</span>
				<span>{twoWaveProofResult.submittedWaveCount} waves with responses</span>
				<span>{twoWaveProofResult.linkedTrajectoryCount} linked trajectories</span>
				<span>{twoWaveProofResult.completeTrajectoryCount} complete trajectories</span>
			</div>
			<div class="record-list">
				{#each twoWaveProofResult.waves as wave (wave.campaignId)}
					<article class="record-row" aria-label={`Wave ${wave.name}`}>
						<div class="record-row__header">
							<h5 class="record-row__title">{wave.name}</h5>
							<StatusBadge status="ready" label={humanize(wave.status)} />
						</div>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">Response mode</dt>
								<dd class="record-field__value">{humanize(wave.responseIdentityMode)}</dd>
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
		{#if id === 'waveComparisonProof'}
			<a class="secondary-button" href={resultsHref}>Back to results</a>
			<a class="secondary-button" href={setupHref}>Set up next wave</a>
		{/if}
	</div>
	{#if actionErrors[id]}
		<p class="error-line">{actionErrors[id]}</p>
	{/if}
{/snippet}

{#snippet ResultLine({ label, value }: { label: string; value: string | null | undefined })}
	{#if value}
		<p class="result-line">
			<span>{label}</span>
			<span>{value}</span>
		</p>
	{/if}
{/snippet}
