<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { onDestroy } from 'svelte';
	import { LoaderCircle, RefreshCw } from 'lucide-svelte';
	import type {
		CampaignSeriesHubResponse,
		CampaignSeriesOperationsWorkspaceResponse,
		CampaignSeriesReportsWidgetManifestResponse,
		CampaignSeriesReportsWorkspaceResponse,
		CampaignSeriesScoreRemediationResponse,
		CampaignSeriesSetupWorkspaceResponse,
		CampaignSeriesWavesWorkspaceResponse
	} from '$lib/api/product';
	import type { AuthSessionResponse } from '$lib/api/setup';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import RouteGuidancePanel from '$lib/components/RouteGuidancePanel.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import ProofWorkflowSurface from '$lib/product/ProofWorkflowSurface.svelte';
	import SelectedSeriesOperationsWorkflow from '$lib/product/SelectedSeriesOperationsWorkflow.svelte';
	import SelectedSeriesReportsWorkflow from '$lib/product/SelectedSeriesReportsWorkflow.svelte';
	import SelectedSeriesSetupWorkflow from '$lib/product/SelectedSeriesSetupWorkflow.svelte';
	import SelectedSeriesWavesWorkflow from '$lib/product/SelectedSeriesWavesWorkflow.svelte';
	import {
		createProductApiFromEnv,
		createProductRequestGate,
		toSelectedSeriesErrorMessage
	} from '$lib/product/route-state';
	import { toProductRouteGuidance } from '$lib/product/route-guidance';
	import {
		toCampaignSeriesOperationsWorkspaceView,
		toCampaignSeriesReportsWorkspaceView,
		toCampaignSeriesSetupWorkspaceView,
		toCampaignSeriesWavesWorkspaceView,
		toSelectedSeriesSurfaceView,
		type SelectedSeriesSurfaceId
	} from '$lib/product/view-models';
	import {
		getProductAuthContext,
		hasProductPermission,
		setupManagePermission
	} from '$lib/product/auth-context';

	type LoadState = 'loading' | 'ready' | 'error';

	let {
		seriesId,
		surface,
		ariaLabel
	}: {
		seriesId: string;
		surface: SelectedSeriesSurfaceId;
		ariaLabel: string;
	} = $props();

	type ActionState = 'idle' | 'submitting' | 'succeeded' | 'failed';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();
	const authContext = getProductAuthContext();
	const appLocale = $derived(appLocaleFromPageData(page.data));
	const copy = $derived(routePageCopy(appLocale).selectedStudy.surfaceChrome);

	let loadState = $state<LoadState>('loading');
	let authSession = $state<AuthSessionResponse | null>(null);
	let campaignSeriesHub = $state<CampaignSeriesHubResponse | null>(null);
	let setupWorkspace = $state<CampaignSeriesSetupWorkspaceResponse | null>(null);
	let operationsWorkspace = $state<CampaignSeriesOperationsWorkspaceResponse | null>(null);
	let reportsWorkspace = $state<CampaignSeriesReportsWorkspaceResponse | null>(null);
	let reportsWidgetManifest = $state<CampaignSeriesReportsWidgetManifestResponse | null>(null);
	let reportsWidgetManifestWarning = $state<string | null>(null);
	let wavesWorkspace = $state<CampaignSeriesWavesWorkspaceResponse | null>(null);
	let errorMessage = $state<string | null>(null);
	let scoreRemediationState = $state<ActionState>('idle');
	let scoreRemediationResult = $state<CampaignSeriesScoreRemediationResponse | null>(null);
	let scoreRemediationError = $state<string | null>(null);
	let scoreRemediationRefreshWarning = $state<string | null>(null);

	const unsubscribeAuth = authContext.session.subscribe((value) => {
		authSession = value;
	});
	onDestroy(unsubscribeAuth);

	const canManageSetup = $derived(hasProductPermission(authSession, setupManagePermission));
	const surfaceView = $derived(
		campaignSeriesHub ? toSelectedSeriesSurfaceView(campaignSeriesHub, surface, appLocale) : null
	);
	const setupWorkspaceView = $derived(
		setupWorkspace ? toCampaignSeriesSetupWorkspaceView(setupWorkspace, appLocale) : null
	);
	const operationsWorkspaceView = $derived(
		operationsWorkspace ? toCampaignSeriesOperationsWorkspaceView(operationsWorkspace, appLocale) : null
	);
	const reportsWorkspaceView = $derived(
		reportsWorkspace ? toCampaignSeriesReportsWorkspaceView(reportsWorkspace, appLocale) : null
	);
	const wavesWorkspaceView = $derived(
		wavesWorkspace ? toCampaignSeriesWavesWorkspaceView(wavesWorkspace, appLocale) : null
	);
	const setupRouteGuidance = $derived(
		setupWorkspaceView
			? toProductRouteGuidance('setup', {
					isSample: setupWorkspaceView.ownership.isSample,
					canManageSetup
				}, appLocale)
			: null
	);
	const scoreRemediationDisabledReason = $derived(
		!operationsWorkspace?.scoreCoverage
			? copy.scoreCoverageUnavailable
			: operationsWorkspace.scoreCoverage.unscoredSubmittedResponseCount <= 0
				? copy.noMissingScores
				: null
	);
	const canRemediateScores = $derived(
		Boolean(
			operationsWorkspace?.series.id &&
			!scoreRemediationDisabledReason &&
			scoreRemediationState !== 'submitting'
		)
	);
	const scoreRemediationStateLabel = $derived(
		actionStateLabel(scoreRemediationState, copy.actionStates)
	);

	$effect(() => {
		void loadSelectedSeries(seriesId);
	});

	async function loadSelectedSeries(selectedSeriesId: string = seriesId) {
		const requestId = requestGate.next();

		if (!selectedSeriesId) {
			campaignSeriesHub = null;
			setupWorkspace = null;
			operationsWorkspace = null;
			reportsWorkspace = null;
			reportsWidgetManifest = null;
			reportsWidgetManifestWarning = null;
			wavesWorkspace = null;
			resetScoreRemediationState();
			errorMessage = copy.missingStudy;
			loadState = 'error';
			return;
		}

		loadState = 'loading';
		campaignSeriesHub = null;
		setupWorkspace = null;
		operationsWorkspace = null;
		reportsWorkspace = null;
		reportsWidgetManifest = null;
		reportsWidgetManifestWarning = null;
		wavesWorkspace = null;
		resetScoreRemediationState();
		errorMessage = null;

		try {
			if (surface === 'setup') {
				const nextSetupWorkspace =
					await productApi.getCampaignSeriesSetupWorkspace(selectedSeriesId);
				if (!requestGate.isCurrent(requestId)) {
					return;
				}

				setupWorkspace = nextSetupWorkspace;
			} else if (surface === 'operations') {
				const nextOperationsWorkspace =
					await productApi.getCampaignSeriesOperationsWorkspace(selectedSeriesId);
				if (!requestGate.isCurrent(requestId)) {
					return;
				}

				operationsWorkspace = nextOperationsWorkspace;
			} else if (surface === 'reports') {
				const nextReportsWorkspace =
					await productApi.getCampaignSeriesReportsWorkspace(selectedSeriesId);
				if (!requestGate.isCurrent(requestId)) {
					return;
				}

				reportsWorkspace = nextReportsWorkspace;
				loadState = 'ready';
				errorMessage = null;
				void refreshReportsWidgetManifest(selectedSeriesId, requestId);
				return;
			} else if (surface === 'waves') {
				const nextWavesWorkspace =
					await productApi.getCampaignSeriesWavesWorkspace(selectedSeriesId);
				if (!requestGate.isCurrent(requestId)) {
					return;
				}

				wavesWorkspace = nextWavesWorkspace;
			} else {
				const nextCampaignSeriesHub = await productApi.getCampaignSeriesHub(selectedSeriesId);
				if (!requestGate.isCurrent(requestId)) {
					return;
				}

				campaignSeriesHub = nextCampaignSeriesHub;
			}

			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			campaignSeriesHub = null;
			setupWorkspace = null;
			operationsWorkspace = null;
			reportsWorkspace = null;
			reportsWidgetManifest = null;
			reportsWidgetManifestWarning = null;
			wavesWorkspace = null;
			resetScoreRemediationState();
			errorMessage = toSelectedSeriesErrorMessage(
				error,
				copy.surfaceUnavailableFallback
			);
			loadState = 'error';
		}
	}

	async function refreshSetupWorkspace(selectedSeriesId: string = seriesId) {
		const requestId = requestGate.next();

		if (!selectedSeriesId || surface !== 'setup') {
			return false;
		}

		try {
			const nextSetupWorkspace = await productApi.getCampaignSeriesSetupWorkspace(selectedSeriesId);
			if (!requestGate.isCurrent(requestId)) {
				return false;
			}

			setupWorkspace = nextSetupWorkspace;
			loadState = 'ready';
			errorMessage = null;
			return true;
		} catch {
			return false;
		}
	}

	async function refreshOperationsWorkspace(selectedSeriesId: string = seriesId) {
		const requestId = requestGate.next();

		if (!selectedSeriesId || surface !== 'operations') {
			return false;
		}

		try {
			const nextOperationsWorkspace =
				await productApi.getCampaignSeriesOperationsWorkspace(selectedSeriesId);
			if (!requestGate.isCurrent(requestId)) {
				return false;
			}

			operationsWorkspace = nextOperationsWorkspace;
			loadState = 'ready';
			errorMessage = null;
			return true;
		} catch {
			return false;
		}
	}

	async function refreshReportsWorkspace(selectedSeriesId: string = seriesId) {
		const requestId = requestGate.next();

		if (!selectedSeriesId || surface !== 'reports') {
			return false;
		}

		try {
			const nextReportsWorkspace =
				await productApi.getCampaignSeriesReportsWorkspace(selectedSeriesId);
			if (!requestGate.isCurrent(requestId)) {
				return false;
			}

			reportsWorkspace = nextReportsWorkspace;
			loadState = 'ready';
			errorMessage = null;
			void refreshReportsWidgetManifest(selectedSeriesId, requestId);
			return true;
		} catch {
			return false;
		}
	}

	async function refreshReportsWidgetManifest(selectedSeriesId: string, requestId: number) {
		reportsWidgetManifestWarning = null;

		try {
			const nextManifest =
				await productApi.getCampaignSeriesReportsWidgetManifest(selectedSeriesId);
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			reportsWidgetManifest = nextManifest;
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			reportsWidgetManifest = null;
			reportsWidgetManifestWarning = toSelectedSeriesErrorMessage(
				error,
				copy.resultsSummaryUnavailable
			);
		}
	}

	async function refreshWavesWorkspace(selectedSeriesId: string = seriesId) {
		const requestId = requestGate.next();

		if (!selectedSeriesId || surface !== 'waves') {
			return false;
		}

		try {
			const nextWavesWorkspace = await productApi.getCampaignSeriesWavesWorkspace(selectedSeriesId);
			if (!requestGate.isCurrent(requestId)) {
				return false;
			}

			wavesWorkspace = nextWavesWorkspace;
			loadState = 'ready';
			errorMessage = null;
			return true;
		} catch {
			return false;
		}
	}

	async function remediateScores() {
		if (!canManageSetup) {
			return;
		}

		const selectedSeriesId = operationsWorkspace?.series.id;
		if (!selectedSeriesId || scoreRemediationState === 'submitting') {
			return;
		}

		scoreRemediationState = 'submitting';
		scoreRemediationError = null;
		scoreRemediationRefreshWarning = null;

		try {
			scoreRemediationResult = await productApi.remediateCampaignSeriesScores(selectedSeriesId);
			scoreRemediationState = 'succeeded';

			const refreshed = await refreshOperationsWorkspace(selectedSeriesId);
			if (!refreshed) {
				scoreRemediationRefreshWarning =
					'Score remediation completed, but the coverage refresh did not finish.';
			}
		} catch (error) {
			scoreRemediationResult = null;
			scoreRemediationState = 'failed';
			scoreRemediationError = toSelectedSeriesErrorMessage(
				error,
				'Score remediation could not be completed.'
			);
		}
	}

	function resetScoreRemediationState() {
		scoreRemediationState = 'idle';
		scoreRemediationResult = null;
		scoreRemediationError = null;
		scoreRemediationRefreshWarning = null;
	}

	function actionStateLabel(state: ActionState, labels: typeof copy.actionStates) {
		if (state === 'submitting') {
			return labels.running;
		}

		if (state === 'succeeded') {
			return labels.done;
		}

		if (state === 'failed') {
			return labels.failed;
		}

		return labels.ready;
	}
</script>

<section class="product-stack" aria-label={ariaLabel}>
	<LoadingBoundary loading={loadState === 'loading'} label={copy.loadingContext(surface)}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={copy.errorTitle}
				message={errorMessage}
				retryLabel={copy.retry}
				onRetry={() => loadSelectedSeries()}
			/>
		{:else if setupWorkspaceView}
			{#if setupWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label={copy.readOnlyStateAria}>
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{copy.ownershipKicker}</p>
							<h2 class="product-title">{setupWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{setupWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={setupWorkspaceView.ownership.badgeStatus} label={copy.readOnly} />
					</div>
				</section>
			{/if}

			<section class="product-panel" aria-label={setupWorkspaceView.studyBriefContext.title}>
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{copy.studyBriefKicker}</p>
						<h2 class="product-title">{setupWorkspaceView.studyBriefContext.title}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{setupWorkspaceView.studyBriefContext.summary}
						</p>
					</div>
					<StatusBadge
						status={setupWorkspaceView.studyBriefContext.status}
						label={setupWorkspaceView.studyBriefContext.badgeLabel}
					/>
				</div>
				<p class="text-sm text-[var(--color-text-muted)]">
					{setupWorkspaceView.studyBriefContext.guidance}
				</p>
				<dl class="record-grid">
					{#each setupWorkspaceView.studyBriefContext.rows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			</section>

			{#if setupWorkspace}
				<SelectedSeriesSetupWorkflow
					workspace={setupWorkspace}
					{canManageSetup}
					onWorkspaceRefresh={() => refreshSetupWorkspace()}
				/>
			{/if}

		{:else if operationsWorkspaceView}
			{#if operationsWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label={copy.readOnlyStateAria}>
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{copy.ownershipKicker}</p>
							<h2 class="product-title">{operationsWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{operationsWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={operationsWorkspaceView.ownership.badgeStatus} label={copy.readOnly} />
					</div>
				</section>
			{/if}

			<section class="product-panel" aria-label={operationsWorkspaceView.studyBriefContext.title}>
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{copy.studyBriefKicker}</p>
						<h2 class="product-title">{operationsWorkspaceView.studyBriefContext.title}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{operationsWorkspaceView.studyBriefContext.summary}
						</p>
					</div>
					<StatusBadge
						status={operationsWorkspaceView.studyBriefContext.status}
						label={operationsWorkspaceView.studyBriefContext.badgeLabel}
					/>
				</div>
				<p class="text-sm text-[var(--color-text-muted)]">
					{operationsWorkspaceView.studyBriefContext.guidance}
				</p>
				<dl class="record-grid">
					{#each operationsWorkspaceView.studyBriefContext.rows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			</section>

			{#if operationsWorkspace}
				<SelectedSeriesOperationsWorkflow
					workspace={operationsWorkspace}
					{canManageSetup}
					onWorkspaceRefresh={() => refreshOperationsWorkspace()}
				/>
			{/if}

			<details class="product-panel reference-context" aria-label={copy.collectionDetails.summary}>
				<summary class="record-row__title">{copy.collectionDetails.summary}</summary>
				<div class="product-panel__header mt-4">
					<div>
						<p class="product-kicker">{copy.collectionDetails.kicker}</p>
						<h2 class="product-title">{copy.collectionDetails.title}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{copy.collectionDetails.description}
						</p>
					</div>
				</div>

				<dl class="metric-grid">
					{#each operationsWorkspaceView.summaryRows as row}
						<div class="metric-card">
							<dt class="metric-card__label">{row.label}</dt>
							<dd class="metric-card__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

				{#if operationsWorkspaceView.collectionMonitor}
					<div
						role="group"
						aria-label={copy.collectionDetails.monitorAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.collectionDetails.monitorKicker}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">{copy.collectionDetails.monitorTitle}</h3>
							<p class="text-sm text-[var(--color-text-muted)]">
								{operationsWorkspaceView.collectionMonitor.guidance}
							</p>
						</div>

						<dl class="record-grid">
							{#each operationsWorkspaceView.collectionMonitor.summaryRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>
					</div>
				{/if}

				{#if operationsWorkspaceView.scoreCoverageMonitor}
					<div
						role="group"
						aria-label={copy.collectionDetails.scoreCoverageAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.collectionDetails.scoreCoverageKicker}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{copy.collectionDetails.scoreCoverageTitle}
							</h3>
							<p class="text-sm text-[var(--color-text-muted)]">
								{operationsWorkspaceView.scoreCoverageMonitor.guidance}
							</p>
						</div>

						<dl class="record-grid">
							{#each operationsWorkspaceView.scoreCoverageMonitor.summaryRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>

						{#if canManageSetup}
							<div class="action-row">
								<button
									type="button"
									class="primary-button"
									disabled={!canRemediateScores}
									title={scoreRemediationDisabledReason ?? undefined}
									onclick={() => remediateScores()}
								>
									{#if scoreRemediationState === 'submitting'}
										<LoaderCircle size={17} aria-hidden="true" />
									{:else}
										<RefreshCw size={17} aria-hidden="true" />
									{/if}
									<span>
										{scoreRemediationState === 'submitting'
											? copy.collectionDetails.remediatingScores
											: copy.collectionDetails.remediateMissingScores}
									</span>
								</button>
								<p class="step-pill" data-state={scoreRemediationState}>
									{scoreRemediationStateLabel}
								</p>
							</div>

							{#if scoreRemediationError}
								<p class="error-line">{scoreRemediationError}</p>
							{/if}

							{#if scoreRemediationRefreshWarning}
								<p class="text-sm text-[var(--color-text-muted)]">
									{scoreRemediationRefreshWarning}
								</p>
							{/if}
						{:else}
							<p class="text-sm text-[var(--color-text-muted)]">
								{copy.collectionDetails.remediationRequiresAccess}
							</p>
						{/if}

						{#if scoreRemediationResult}
							<dl class="record-grid" aria-label={copy.collectionDetails.resultAria}>
								<div class="record-field">
									<dt class="record-field__label">{copy.collectionDetails.submittedResponses}</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.submittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{copy.collectionDetails.eligibleSubmitted}</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.eligibleSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{copy.collectionDetails.alreadyScored}</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.alreadyScoredSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{copy.collectionDetails.remediated}</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.remediatedSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{copy.collectionDetails.notConfigured}</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.skippedNotConfiguredSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">{copy.collectionDetails.latestScoringActivity}</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.latestScoringActivityAt ?? 'Not available'}
									</dd>
								</div>
							</dl>
						{/if}
					</div>
				{/if}

				{#if operationsWorkspaceView.missingPrerequisiteRows.length > 0}
					<div
						role="group"
						aria-label={copy.collectionDetails.prerequisitesAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.collectionDetails.prerequisitesKicker}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{copy.collectionDetails.prerequisitesTitle}
							</h3>
						</div>
						<div class="record-list">
							{#each operationsWorkspaceView.missingPrerequisiteRows as row}
								<article class="record-row" aria-label={row.label}>
									<div class="record-row__header">
										<div>
											<h4 class="record-row__title">{row.label}</h4>
											<p class="text-sm text-[var(--color-text-muted)]">{row.message}</p>
										</div>
										<StatusBadge status={row.status} label={row.severity} />
									</div>
								</article>
							{/each}
						</div>
					</div>
				{/if}
			</details>
		{:else if reportsWorkspaceView}
			{#if reportsWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label={copy.readOnlyStateAria}>
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{copy.ownershipKicker}</p>
							<h2 class="product-title">{reportsWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{reportsWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={reportsWorkspaceView.ownership.badgeStatus} label={copy.readOnly} />
					</div>
				</section>
			{/if}

			<section class="product-panel" aria-label={reportsWorkspaceView.studyBriefContext.title}>
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{copy.studyBriefKicker}</p>
						<h2 class="product-title">{reportsWorkspaceView.studyBriefContext.title}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{reportsWorkspaceView.studyBriefContext.summary}
						</p>
					</div>
					<StatusBadge
						status={reportsWorkspaceView.studyBriefContext.status}
						label={reportsWorkspaceView.studyBriefContext.badgeLabel}
					/>
				</div>
				<p class="text-sm text-[var(--color-text-muted)]">
					{reportsWorkspaceView.studyBriefContext.guidance}
				</p>
				<dl class="record-grid">
					{#each reportsWorkspaceView.studyBriefContext.rows as row}
						<div class="record-field">
							<dt class="record-field__label">{row.label}</dt>
							<dd class="record-field__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			</section>

			{#if reportsWorkspace}
				<SelectedSeriesReportsWorkflow
					workspace={reportsWorkspace}
					widgetManifest={reportsWidgetManifest}
					widgetWarning={reportsWidgetManifestWarning}
					{canManageSetup}
					onWorkspaceRefresh={() => refreshReportsWorkspace()}
				/>

				<details class="product-panel reference-context" aria-label={copy.resultsDetails.summary}>
					<summary class="record-row__title">{copy.resultsDetails.summary}</summary>
					<div class="product-panel__header mt-4">
						<div>
							<p class="product-kicker">{copy.resultsDetails.kicker}</p>
							<h2 class="product-title">{copy.resultsDetails.title}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{copy.resultsDetails.description}
							</p>
						</div>
					</div>

					<dl class="metric-grid">
						{#each reportsWorkspaceView.summaryRows as row}
							<div class="metric-card">
								<dt class="metric-card__label">{row.label}</dt>
								<dd class="metric-card__value">{row.value}</dd>
							</div>
						{/each}
					</dl>

					{#if reportsWorkspaceView.resultsOverview.length > 0}
						<div
							role="group"
							aria-label={copy.resultsDetails.readinessAria}
							class="grid gap-3 border-t border-[var(--color-border)] pt-4"
						>
							<div>
								<p class="product-kicker">{copy.resultsDetails.readinessKicker}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{copy.resultsDetails.readinessTitle}
								</h3>
							</div>
							<div class="record-list">
								{#each reportsWorkspaceView.resultsOverview as item (item.id)}
									<article class="record-row" aria-label={item.label}>
										<div class="record-row__header">
											<div>
												<h4 class="record-row__title">{item.label}</h4>
												<p class="text-sm text-[var(--color-text-muted)]">
													{item.summary}
												</p>
											</div>
											<StatusBadge status={item.status} label={item.badgeLabel} />
										</div>
										<p class="text-sm text-[var(--color-text-muted)]">{item.guidance}</p>
									</article>
								{/each}
							</div>
						</div>
					{/if}

				{#if reportsWorkspaceView.scoreCoverageSignal}
					<div
						role="group"
						aria-label={copy.resultsDetails.scoreCoverageAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div class="product-panel__header">
							<div>
								<p class="product-kicker">{copy.resultsDetails.scoreCoverageKicker}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">{copy.resultsDetails.scoreCoverageTitle}</h3>
								<p class="text-sm text-[var(--color-text-muted)]">
									{reportsWorkspaceView.scoreCoverageSignal.guidance}
								</p>
							</div>
						</div>

						<dl class="record-grid">
							{#each reportsWorkspaceView.scoreCoverageSignal.summaryRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>
					</div>
				{/if}

				<div
					role="group"
					aria-label={copy.resultsDetails.selectedCampaignAria}
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">{copy.resultsDetails.selectedWaveKicker}</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">{copy.resultsDetails.reportStateTitle}</h3>
					</div>

					{#if reportsWorkspaceView.selectedCampaignRows.some((row) => !row.mono)}
						<dl class="record-grid">
							{#each reportsWorkspaceView.selectedCampaignRows.filter((row) => !row.mono) as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>
					{:else if reportsWorkspaceView.emptyState}
						<p class="record-row text-sm text-[var(--color-text-muted)]">
							<strong class="record-row__title">{reportsWorkspaceView.emptyState.title}</strong>
							<span>{reportsWorkspaceView.emptyState.message}</span>
						</p>
					{/if}
				</div>

				{#if reportsWorkspaceView.provenanceRows.length > 0}
					<div
						role="group"
						aria-label={copy.resultsDetails.sourceAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.resultsDetails.basedOn}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{copy.resultsDetails.launchPolicyExport}
							</h3>
						</div>
						<dl class="record-grid">
							{#each reportsWorkspaceView.provenanceRows.filter((row) => !row.mono) as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>
					</div>
				{/if}

				{#if reportsWorkspaceView.missingPrerequisiteRows.length > 0}
					<div
						role="group"
						aria-label={copy.resultsDetails.prerequisitesAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.resultsDetails.prerequisitesKicker}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{copy.resultsDetails.prerequisitesTitle}
							</h3>
						</div>
						<div class="record-list">
							{#each reportsWorkspaceView.missingPrerequisiteRows as row}
								<article class="record-row" aria-label={row.label}>
									<div class="record-row__header">
										<div>
											<h4 class="record-row__title">{row.label}</h4>
											<p class="text-sm text-[var(--color-text-muted)]">{row.message}</p>
										</div>
										<StatusBadge status={row.status} label={row.severity} />
									</div>
								</article>
							{/each}
						</div>
					</div>
				{/if}

				<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
					<div>
						<p class="product-kicker">{copy.resultsDetails.waves}</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							{copy.resultsDetails.includedWaves}
						</h3>
					</div>

					{#if reportsWorkspaceView.emptyState}
						<p class="record-row text-sm text-[var(--color-text-muted)]">
							<strong class="record-row__title">{reportsWorkspaceView.emptyState.title}</strong>
							<span>{reportsWorkspaceView.emptyState.message}</span>
						</p>
					{:else}
						<div class="record-list">
							{#each reportsWorkspaceView.campaignRows as campaign (campaign.id)}
								<article aria-label={campaign.title} class="record-row">
									<div class="record-row__header">
										<h4 class="record-row__title">{campaign.title}</h4>
										<StatusBadge status={campaign.status} />
									</div>
									<dl class="record-grid">
										{#each campaign.rows.filter((row) => !row.mono) as row}
											<div class="record-field">
												<dt class="record-field__label">{row.label}</dt>
												<dd class="record-field__value">{row.value}</dd>
											</div>
										{/each}
									</dl>
								</article>
							{/each}
						</div>
					{/if}
				</div>
			</details>
			{/if}
		{:else if wavesWorkspaceView}
			{#if wavesWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label={copy.readOnlyStateAria}>
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{copy.ownershipKicker}</p>
							<h2 class="product-title">{wavesWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{wavesWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={wavesWorkspaceView.ownership.badgeStatus} label={copy.readOnly} />
					</div>
				</section>
			{/if}

			{#if wavesWorkspace}
				<SelectedSeriesWavesWorkflow
					workspace={wavesWorkspace}
					onWorkspaceRefresh={() => refreshWavesWorkspace()}
				/>

				<details class="product-panel reference-context" aria-label={copy.wavesDetails.summary}>
					<summary class="record-row__title">{copy.wavesDetails.summary}</summary>
					<div class="product-panel__header mt-4">
						<div>
							<p class="product-kicker">{copy.wavesDetails.kicker}</p>
							<h2 class="product-title">{copy.wavesDetails.title}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{copy.wavesDetails.description}
							</p>
						</div>
					</div>

					<dl class="metric-grid">
						{#each wavesWorkspaceView.summaryRows as row}
							<div class="metric-card">
								<dt class="metric-card__label">{row.label}</dt>
								<dd class="metric-card__value">{row.value}</dd>
							</div>
						{/each}
					</dl>

					<div
						role="group"
						aria-label={copy.wavesDetails.comparedWavesAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.wavesDetails.comparedWavesKicker}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{copy.wavesDetails.selectedComparison}
							</h3>
						</div>
						<dl class="record-grid">
							<div class="record-field">
								<dt class="record-field__label">{copy.wavesDetails.baselineWave}</dt>
								<dd class="record-field__value">
									{wavesWorkspace.selectedBaselineWave?.name ?? copy.wavesDetails.missing}
								</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{copy.wavesDetails.comparisonWave}</dt>
								<dd class="record-field__value">
									{wavesWorkspace.selectedComparisonWave?.name ?? copy.wavesDetails.missing}
								</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{copy.wavesDetails.comparisonStatus}</dt>
								<dd class="record-field__value">{wavesWorkspace.comparison.status}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{copy.wavesDetails.disclosure}</dt>
								<dd class="record-field__value">{wavesWorkspace.comparison.disclosureState}</dd>
							</div>
							<div class="record-field">
								<dt class="record-field__label">{copy.wavesDetails.compatibility}</dt>
								<dd class="record-field__value">{wavesWorkspace.comparison.compatibilityState}</dd>
							</div>
						</dl>
					</div>

					{#if wavesWorkspaceView.selectedWaveRows.length > 0}
						<div
							role="group"
							aria-label={copy.wavesDetails.readinessAria}
							class="grid gap-3 border-t border-[var(--color-border)] pt-4"
						>
							<div>
								<p class="product-kicker">{copy.wavesDetails.readinessKicker}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{copy.wavesDetails.availableTitle}
								</h3>
							</div>
							<dl class="record-grid">
								{#each wavesWorkspaceView.selectedWaveRows as row}
									<div class="record-field">
										<dt class="record-field__label">{row.label}</dt>
										<dd class="record-field__value">{row.value}</dd>
									</div>
								{/each}
							</dl>
						</div>
					{:else if wavesWorkspaceView.emptyState}
						<p class="record-row text-sm text-[var(--color-text-muted)]">
							<strong class="record-row__title">{wavesWorkspaceView.emptyState.title}</strong>
							<span>{wavesWorkspaceView.emptyState.message}</span>
						</p>
					{/if}

					{#if wavesWorkspaceView.provenanceRows.length > 0}
						<div
							role="group"
							aria-label={copy.wavesDetails.sourceAria}
							class="grid gap-3 border-t border-[var(--color-border)] pt-4"
						>
							<div>
								<p class="product-kicker">{copy.wavesDetails.basedOn}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{copy.wavesDetails.launchPolicy}
								</h3>
							</div>
							<dl class="record-grid">
								{#each wavesWorkspaceView.provenanceRows as row}
									<div class="record-field">
										<dt class="record-field__label">{row.label}</dt>
										<dd class="record-field__value">{row.value}</dd>
									</div>
								{/each}
							</dl>
						</div>
					{/if}

					{#if wavesWorkspaceView.missingPrerequisiteRows.length > 0}
						<div
							role="group"
							aria-label={copy.wavesDetails.prerequisitesAria}
							class="grid gap-3 border-t border-[var(--color-border)] pt-4"
						>
							<div>
								<p class="product-kicker">{copy.wavesDetails.prerequisitesKicker}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{copy.wavesDetails.prerequisitesTitle}
								</h3>
							</div>
							<div class="record-list">
								{#each wavesWorkspaceView.missingPrerequisiteRows as row}
									<article class="record-row" aria-label={row.label}>
										<div class="record-row__header">
											<div>
												<h4 class="record-row__title">{row.label}</h4>
												<p class="text-sm text-[var(--color-text-muted)]">{row.message}</p>
											</div>
											<StatusBadge status={row.status} label={row.severity} />
										</div>
									</article>
								{/each}
							</div>
						</div>
					{/if}

					<div
						role="group"
						aria-label={copy.wavesDetails.availableWavesAria}
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.wavesDetails.availableWavesKicker}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{copy.wavesDetails.waveHistory}
							</h3>
						</div>

						{#if wavesWorkspaceView.emptyState}
							<p class="record-row text-sm text-[var(--color-text-muted)]">
								<strong class="record-row__title">{wavesWorkspaceView.emptyState.title}</strong>
								<span>{wavesWorkspaceView.emptyState.message}</span>
							</p>
						{:else}
							<div class="record-list">
								{#each wavesWorkspaceView.campaignRows as campaign (campaign.id)}
									<article aria-label={campaign.title} class="record-row">
										<div class="record-row__header">
											<h4 class="record-row__title">{campaign.title}</h4>
											<StatusBadge status={campaign.status} />
										</div>
										<dl class="record-grid">
											{#each campaign.rows as row}
												<div class="record-field">
													<dt class="record-field__label">{row.label}</dt>
													<dd class="record-field__value">{row.value}</dd>
												</div>
											{/each}
										</dl>
									</article>
								{/each}
							</div>
						{/if}
					</div>
				</details>
			{/if}
		{:else if surfaceView}
			<section
				class="product-panel"
				aria-label={`${surfaceView.surfaceLabel} ${copy.fallback.selectedSeriesContext}`}
			>
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{surfaceView.surfaceEyebrow}</p>
						<h2 class="product-title">{surfaceView.title}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{surfaceView.surfaceLabel} for {surfaceView.subtitle}.
						</p>
					</div>
				</div>

<dl class="metric-grid">
					{#each surfaceView.summaryRows as row}
						<div class="metric-card">
							<dt class="metric-card__label">{row.label}</dt>
							<dd class="metric-card__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

				{#if surfaceView.governanceRows.length > 0}
					<div
						role="group"
						aria-label="Selected series governance"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">{copy.fallback.governance}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{copy.fallback.selectedSeriesReadiness}
							</h3>
						</div>
						<div class="record-grid">
							{#each surfaceView.governanceRows as row}
								<div class="record-row">
									<div class="record-row__header">
										<div>
											<p class="record-field__label">{row.label}</p>
											<p class="record-field__value">{row.value}</p>
										</div>
										<StatusBadge status={row.status} />
									</div>
								</div>
							{/each}
						</div>
					</div>
				{/if}

				<div
					role="group"
					aria-label={copy.fallback.campaignRowsAria}
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">{copy.fallback.campaignRows}</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							{copy.fallback.campaignContext}
						</h3>
					</div>

					{#if surfaceView.emptyState}
						<p class="record-row text-sm text-[var(--color-text-muted)]">
							<strong class="record-row__title">{surfaceView.emptyState.title}</strong>
							<span>{surfaceView.emptyState.message}</span>
						</p>
					{:else}
						<div class="record-list">
							{#each surfaceView.campaignRows as campaign (campaign.id)}
								<article aria-label={campaign.title} class="record-row">
									<div class="record-row__header">
										<h4 class="record-row__title">{campaign.title}</h4>
										<StatusBadge status={campaign.status} />
									</div>
									<dl class="record-grid">
										{#each campaign.rows as row}
											<div class="record-field">
												<dt class="record-field__label">{row.label}</dt>
												<dd class="record-field__value">{row.value}</dd>
											</div>
										{/each}
									</dl>
								</article>
							{/each}
						</div>
					{/if}
				</div>
			</section>

			{#if canManageSetup}
				<section class="proof-workbench" aria-label="Action workflow">
					<div class="proof-workbench__header">
						<div>
							<p class="product-kicker">{copy.fallback.productWorkflow}</p>
							<h3 class="product-title">{surfaceView.proofActionTitle}</h3>
							<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
								{surfaceView.proofActionDescription}
							</p>
						</div>
						<span class="status-badge" data-status="proof_only">{copy.fallback.previewWorkflow}</span>
					</div>
					<ProofWorkflowSurface
						{surface}
						authManagedByParent={true}
						showIntro={false}
						selectedSeries={{ id: surfaceView.id, name: surfaceView.title }}
						onWorkflowMutation={() => loadSelectedSeries()}
					/>
				</section>
			{:else}
				<section class="product-panel" aria-label="Read-only selected-series access">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{copy.fallback.productWorkflow}</p>
							<h3 class="product-title">{copy.fallback.readOnlyAccess}</h3>
						</div>
					</div>
					<p class="text-sm text-[var(--color-text-muted)]">
						{copy.fallback.workflowRequiresSetup}
					</p>
				</section>
			{/if}
		{/if}
	</LoadingBoundary>
</section>
