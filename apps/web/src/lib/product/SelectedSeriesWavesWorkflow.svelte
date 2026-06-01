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
	const wavesUi = $derived(wavesWorkflowCopy.component);
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
			interpretation.isValidated
				? wavesUi.reviewedInterpretation
				: wavesUi.notReviewedInterpretation,
			interpretation.isOfficial
				? wavesUi.officialInterpretation
				: wavesUi.notOfficialInterpretation
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
					refreshWarning = wavesUi.errors.refreshFailed;
				}
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, wavesUi.errors.actionFailed)
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

	function pathStateLabel(state: SelectedSeriesWavesPathStepState) {
		if (state === 'done') {
			return wavesUi.state.done;
		}

		if (state === 'current') {
			return wavesUi.state.current;
		}

		return wavesUi.state.blocked;
	}

	function inactivePathTitle() {
		if (wavesPath.mode === 'group_trend') {
			return wavesUi.linkedChecksNotNeeded;
		}

		return wavesUi.linkedChecksNotActiveYet;
	}

	function waveStatusBadge(status: string) {
		const normalized = status.toLowerCase();
		if (normalized.includes('closed') || normalized.includes('complete')) {
			return 'ready';
		}
		if (normalized.includes('draft') || normalized.includes('pending')) {
			return 'not_available';
		}
		return 'pending';
	}

	function formatNullableScoreValue(value: number | null) {
		return value === null ? wavesUi.suppressed : value.toFixed(2);
	}

	function humanize(value: string | null | undefined) {
		return value ? value.replaceAll('_', ' ') : wavesUi.notAvailable;
	}
</script>

<section class="product-panel" role="group" aria-label={wavesWorkflowCopy.surface.reviewActionsAria}>
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">{wavesWorkflowCopy.surface.flowKicker}</p>
			<h3 class="product-title">{wavesWorkflowCopy.surface.title}</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				{wavesWorkflowCopy.surface.description}
			</p>
		</div>
		<StatusBadge status={wavePlan.status} label={wavePlan.title} />
	</div>

	{#if refreshWarning}
		<p class="error-line">{refreshWarning}</p>
	{/if}
	<article class="record-row" role="region" aria-label={wavesUi.measurementsTimelineAria}>
		<div class="record-row__header">
			<div>
				<span>{wavesUi.measurements}</span>
				<strong>{workspace.series.name}</strong>
			</div>
			<StatusBadge status={wavePlan.status} />
		</div>
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.measurements}</dt>
				<dd class="record-field__value">{workspace.summary.campaignCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.submittedResponses}</dt>
				<dd class="record-field__value">{workspace.summary.submittedWaveCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.repeatedWaves}</dt>
				<dd class="record-field__value">{workspace.summary.longitudinalWaveCount}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.compatibility}</dt>
				<dd class="record-field__value">{humanize(workspace.comparison.compatibilityState)}</dd>
			</div>
		</dl>
		<div class="record-list" style="max-height: 24rem; overflow: auto;">
			{#each workspace.waves as wave (wave.id)}
				<article class="record-row" aria-label={wavesUi.waveAria(wave.name)}>
					<div class="record-row__header">
						<div>
							<span>{wave.name}</span>
							<strong>{humanize(wave.status)}</strong>
						</div>
						<StatusBadge status={waveStatusBadge(wave.status)} label={humanize(wave.status)} />
					</div>
					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">{wavesUi.submittedResponses}</dt>
							<dd class="record-field__value">{wave.submittedResponseCount}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">{wavesUi.responseMode}</dt>
							<dd class="record-field__value">{humanize(wave.responseIdentityMode)}</dd>
						</div>
					</dl>
				</article>
			{/each}
		</div>
		<div class="action-row">
			<a class="secondary-button" href={setupHref}>{wavesUi.setUpNextWave}</a>
			<a class="secondary-button" href={resultsHref}>{wavesUi.backToResults}</a>
		</div>
	</article>

	<article class="record-row setup-current-task" role="region" aria-label={wavesUi.waveComparisonPreviewAria}>
		<div class="setup-current-task__header">
			<div>
				<p class="record-field__label">{wavesUi.comparisonPlan}</p>
				<h4 class="setup-current-task__title">{comparisonReview.title}</h4>
				<p class="text-sm text-[var(--color-text-muted)]">{comparisonReview.description}</p>
			</div>
			<StatusBadge status={comparisonReview.status} />
		</div>
		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.baseline}</dt>
				<dd class="record-field__value">{workspace.selectedBaselineWave?.name ?? wavesUi.missing}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.firstWaveResponses}</dt>
				<dd class="record-field__value">{workspace.selectedBaselineWave?.submittedResponseCount ?? 0}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.comparison}</dt>
				<dd class="record-field__value">{workspace.selectedComparisonWave?.name ?? wavesUi.missing}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.secondWaveResponses}</dt>
				<dd class="record-field__value">{workspace.selectedComparisonWave?.submittedResponseCount ?? 0}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.disclosure}</dt>
				<dd class="record-field__value">{humanize(workspace.comparison.disclosureState)}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.suppressedComparisons}</dt>
				<dd class="record-field__value">{workspace.summary.suppressedComparisonCount}</dd>
			</div>
		</dl>

		{#if workspace.selectedBaselineWave && workspace.selectedComparisonWave}
			<SelectedSeriesWaveComparisonSnapshot {workspace} embedded={true} />
		{/if}

		{#if wavesPath.showWorkflow && workflowAction('waveComparisonProof').disabledReason}
			<p class="text-sm text-[var(--color-text-muted)]">
				{workflowAction('waveComparisonProof').disabledReason}
			</p>
		{/if}

		<div class="action-row">
			{#if wavesPath.showWorkflow}
				<button
					type="button"
					class="secondary-button"
					disabled={isActionDisabled('twoWaveProof')}
					title={workflowAction('twoWaveProof').disabledReason ?? undefined}
					onclick={refreshTwoWaveProof}
				>
					{#if actionStates.twoWaveProof === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<RefreshCw size={17} aria-hidden="true" />
					{/if}
					<span>{wavesUi.runLinkedTrajectoryCheck}</span>
				</button>
				<button
					type="button"
					class="primary-button"
					disabled={isActionDisabled('waveComparisonProof')}
					title={workflowAction('waveComparisonProof').disabledReason ?? undefined}
					onclick={viewWaveComparisonProof}
				>
					{#if actionStates.waveComparisonProof === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<GitCompareArrows size={17} aria-hidden="true" />
					{/if}
					<span>{wavesUi.reviewComparison}</span>
				</button>
			{:else if groupTrendPlan.primaryHref}
				<a class="primary-button" href={groupTrendPlan.primaryHref}>{groupTrendPlan.primaryLabel}</a>
			{:else}
				<a class="primary-button" href={setupHref}>{wavesUi.setUpNextWave}</a>
			{/if}
		</div>
		{#if actionErrors.twoWaveProof}
			<p class="error-line">{actionErrors.twoWaveProof}</p>
		{/if}
		{#if actionErrors.waveComparisonProof}
			<p class="error-line">{actionErrors.waveComparisonProof}</p>
		{/if}
	</article>

	<details class="record-row" open={Boolean(twoWaveProofResult || waveComparisonProofResult)}>
		<summary class="record-row__header">
			<span>{wavesUi.detailsDrawerTitle}</span>
			<StatusBadge status={methodReview.status} />
		</summary>

		{#if twoWaveProofResult}
			{@render TwoWaveProofResult()}
		{/if}
		{#if waveComparisonProofResult}
			{@render WaveComparisonProofResult()}
		{/if}

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

		<dl class="record-grid">
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.firstWave}</dt>
				<dd class="record-field__value">{groupTrendPlan.baselineName ?? wavesUi.missing}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.firstWaveResponses}</dt>
				<dd class="record-field__value">{groupTrendPlan.baselineResponseCount ?? 0}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.secondWave}</dt>
				<dd class="record-field__value">{groupTrendPlan.comparisonName ?? wavesUi.missing}</dd>
			</div>
			<div class="record-field">
				<dt class="record-field__label">{wavesUi.secondWaveResponses}</dt>
				<dd class="record-field__value">{groupTrendPlan.comparisonResponseCount ?? 0}</dd>
			</div>
			{#each groupTrendPlan.safetyRows as row}
				<div class="record-field">
					<dt class="record-field__label">{row.label}</dt>
					<dd class="record-field__value">{row.value}</dd>
				</div>
			{/each}
		</dl>

		{#if wavesPath.showWorkflow}
			<div class="setup-path" role="list" aria-label={wavesUi.wavesPathAria}>
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
			<p class="result-line">
				<span>{wavesUi.currentTaskTitle}</span>
				<span>{currentAction.title}</span>
			</p>
		{:else}
			<p class="result-line">
				<span>{inactivePathTitle()}</span>
				<span>{wavesPath.inactiveReason}</span>
			</p>
		{/if}
	</details></section>

{#snippet TwoWaveProofResult()}
	{#if twoWaveProofResult}
		<section class="score-result-panel report-proof-panel" aria-label={wavesUi.linkedTrajectoryCheckAria}>
			<div class="score-result-panel__header">
				<div>
					<p class="product-kicker">{wavesUi.waveReadiness}</p>
					<h4 class="record-row__title">{wavesUi.linkedTrajectoryCheck}</h4>
				</div>
				<StatusBadge status="ready" label={wavesUi.state.ready} />
			</div>
			<div class="response-lab__meta">
				<span>{wavesUi.launchedWaves(twoWaveProofResult.launchedWaveCount)}</span>
				<span>{wavesUi.wavesWithResponses(twoWaveProofResult.submittedWaveCount)}</span>
				<span>{wavesUi.linkedTrajectories(twoWaveProofResult.linkedTrajectoryCount)}</span>
				<span>{wavesUi.completeTrajectories(twoWaveProofResult.completeTrajectoryCount)}</span>
			</div>
			<div class="record-list">
				{#each twoWaveProofResult.waves as wave (wave.campaignId)}
					<article class="record-row" aria-label={wavesUi.waveAria(wave.name)}>
						<div class="record-row__header">
							<h5 class="record-row__title">{wave.name}</h5>
							<StatusBadge status="ready" label={humanize(wave.status)} />
						</div>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">{wavesUi.responseMode}</dt>
								<dd class="record-field__value">{humanize(wave.responseIdentityMode)}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{wavesUi.submittedResponses}</dt>
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
		<section class="score-result-panel report-proof-panel" aria-label={wavesUi.waveComparisonPreviewAria}>
			<div class="score-result-panel__header">
				<div>
					<p class="product-kicker">{wavesUi.waveComparison}</p>
					<h4 class="record-row__title">{wavesUi.disclosureGatedComparison}</h4>
				</div>
				<StatusBadge status="ready" label={wavesUi.state.ready} />
			</div>
			<div class="response-lab__meta">
				<span>{waveComparisonProofResult.interpretationStatus}</span>
				{#if waveComparisonProofResult.disclosurePolicy}
					<span>{wavesUi.disclosureK(waveComparisonProofResult.disclosurePolicy.kMin)}</span>
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
			<div class="score-card-list" aria-label={wavesUi.waveComparisonScoresAria}>
				{#each waveComparisonProofResult.scores as score (score.dimensionCode)}
					{@const baselineScoreMetadata =
						score.disclosure === 'visible'
							? formatScoreOutputMetadata(
									score.baselineNValidTotal,
									score.baselineNExpectedTotal,
									score.baselineMissingPolicyStatusSummary,
									{
										calculationLabel: score.baselineCalculationLabel,
										scoreRangeMin: score.baselineScoreRangeMin,
										scoreRangeMax: score.baselineScoreRangeMax
									}
								)
							: null}
					{@const comparisonScoreMetadata =
						score.disclosure === 'visible'
							? formatScoreOutputMetadata(
									score.comparisonNValidTotal,
									score.comparisonNExpectedTotal,
									score.comparisonMissingPolicyStatusSummary,
									{
										calculationLabel: score.comparisonCalculationLabel,
										scoreRangeMin: score.comparisonScoreRangeMin,
										scoreRangeMax: score.comparisonScoreRangeMax
									}
								)
							: null}
					<article class="score-card" aria-label={wavesUi.waveComparisonScoreAria(score.dimensionCode)}>
						<div>
							<p class="score-card__label">{score.displayLabel?.trim() || score.dimensionCode}</p>
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
							{wavesUi.pairedDelta(formatNullableScoreValue(score.pairedDeltaMean))}
						</p>
						{#if baselineScoreMetadata}
							<p class="score-card__interpretation">{wavesUi.baselineMeta(baselineScoreMetadata)}</p>
						{/if}
						{#if comparisonScoreMetadata}
							<p class="score-card__interpretation">{wavesUi.comparisonMeta(comparisonScoreMetadata)}</p>
						{/if}
						{#if score.baselineInterpretation}
							<p class="score-card__interpretation">
								{wavesUi.baselineBand(score.baselineInterpretation.label)}
							</p>
						{/if}
						{#if score.comparisonInterpretation}
							<p class="score-card__interpretation">
								{wavesUi.comparisonBand(score.comparisonInterpretation.label)}
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

