<script lang="ts">
	import { env } from '$env/dynamic/public';
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
	import ProofWorkflowSurface from '$lib/product/ProofWorkflowSurface.svelte';
	import SelectedSeriesOperationsWorkflow from '$lib/product/SelectedSeriesOperationsWorkflow.svelte';
	import SelectedSeriesReportSnapshot from '$lib/product/SelectedSeriesReportSnapshot.svelte';
	import SelectedSeriesReportsWorkflow from '$lib/product/SelectedSeriesReportsWorkflow.svelte';
	import SelectedSeriesSetupWorkflow from '$lib/product/SelectedSeriesSetupWorkflow.svelte';
	import SelectedSeriesWaveComparisonSnapshot from '$lib/product/SelectedSeriesWaveComparisonSnapshot.svelte';
	import SelectedSeriesWavesWorkflow from '$lib/product/SelectedSeriesWavesWorkflow.svelte';
	import ReportWidgetsSection from '$lib/product/widgets/ReportWidgetsSection.svelte';
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
		campaignSeriesHub ? toSelectedSeriesSurfaceView(campaignSeriesHub, surface) : null
	);
	const setupWorkspaceView = $derived(
		setupWorkspace ? toCampaignSeriesSetupWorkspaceView(setupWorkspace) : null
	);
	const operationsWorkspaceView = $derived(
		operationsWorkspace ? toCampaignSeriesOperationsWorkspaceView(operationsWorkspace) : null
	);
	const reportsWorkspaceView = $derived(
		reportsWorkspace ? toCampaignSeriesReportsWorkspaceView(reportsWorkspace) : null
	);
	const wavesWorkspaceView = $derived(
		wavesWorkspace ? toCampaignSeriesWavesWorkspaceView(wavesWorkspace) : null
	);
	const setupRouteGuidance = $derived(
		setupWorkspaceView
			? toProductRouteGuidance('setup', {
					isSample: setupWorkspaceView.ownership.isSample,
					canManageSetup
				})
			: null
	);
	const operationsRouteGuidance = $derived(
		operationsWorkspaceView
			? toProductRouteGuidance('operations', {
					isSample: operationsWorkspaceView.ownership.isSample,
					canManageSetup
				})
			: null
	);
	const reportsRouteGuidance = $derived(
		reportsWorkspaceView
			? toProductRouteGuidance('reports', {
					isSample: reportsWorkspaceView.ownership.isSample,
					canManageSetup
				})
			: null
	);
	const wavesRouteGuidance = $derived(
		wavesWorkspaceView
			? toProductRouteGuidance('waves', {
					isSample: wavesWorkspaceView.ownership.isSample,
					canManageSetup
				})
			: null
	);
	const scoreRemediationDisabledReason = $derived(
		!operationsWorkspace?.scoreCoverage
			? 'Score coverage is not available.'
			: operationsWorkspace.scoreCoverage.unscoredSubmittedResponseCount <= 0
				? 'No missing submitted scores to remediate.'
				: null
	);
	const canRemediateScores = $derived(
		Boolean(
			operationsWorkspace?.series.id &&
			!scoreRemediationDisabledReason &&
			scoreRemediationState !== 'submitting'
		)
	);
	const scoreRemediationStateLabel = $derived(actionStateLabel(scoreRemediationState));

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
			errorMessage = 'Select a study before opening this surface.';
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
				'Campaign series surface could not be loaded.'
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
				'Report widgets could not be loaded.'
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

	function actionStateLabel(state: ActionState) {
		if (state === 'submitting') {
			return 'Running';
		}

		if (state === 'succeeded') {
			return 'Done';
		}

		if (state === 'failed') {
			return 'Failed';
		}

		return 'Ready';
	}
</script>

<section class="product-stack" aria-label={ariaLabel}>
	<LoadingBoundary loading={loadState === 'loading'} label={`Loading ${surface} context`}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title="Campaign series unavailable"
				message={errorMessage}
				retryLabel="Retry surface"
				onRetry={() => loadSelectedSeries()}
			/>
		{:else if setupWorkspaceView}
			{#if setupWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label="Sample study read-only state">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Study ownership</p>
							<h2 class="product-title">{setupWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{setupWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={setupWorkspaceView.ownership.badgeStatus} label="Read-only" />
					</div>
				</section>
			{/if}

			{#if setupWorkspace}
				<SelectedSeriesSetupWorkflow
					workspace={setupWorkspace}
					{canManageSetup}
					onWorkspaceRefresh={() => refreshSetupWorkspace()}
				/>
			{/if}

		{:else if operationsWorkspaceView}
			{#if operationsWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label="Sample study read-only state">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Study ownership</p>
							<h2 class="product-title">{operationsWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{operationsWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={operationsWorkspaceView.ownership.badgeStatus} label="Read-only" />
					</div>
				</section>
			{/if}

			{#if operationsWorkspace}
				<SelectedSeriesOperationsWorkflow
					workspace={operationsWorkspace}
					{canManageSetup}
					onWorkspaceRefresh={() => refreshOperationsWorkspace()}
				/>
			{/if}

			{#if operationsWorkspaceView.collectionOverview.length > 0}
				<details class="product-panel reference-context" aria-label="Collection progress">
					<summary class="record-row__title">Collection status details</summary>
					<div class="product-panel__header mt-4">
						<div>
							<p class="product-kicker">{operationsWorkspaceView.surfaceEyebrow}</p>
							<h2 class="product-title">Collection status</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{operationsWorkspaceView.surfaceDescription}
							</p>
						</div>
					</div>

					<div class="record-list">
						{#each operationsWorkspaceView.collectionOverview as item (item.id)}
							<article class="record-row" aria-label={item.label}>
								<div class="record-row__header">
									<div>
										<h3 class="record-row__title">{item.label}</h3>
										<p class="mt-1 text-sm font-semibold text-[var(--color-text)]">
											{item.summary}
										</p>
										<p class="mt-1 text-sm text-[var(--color-text-muted)]">{item.guidance}</p>
									</div>
									<StatusBadge status={item.status} label={item.badgeLabel} />
								</div>

								<dl class="record-grid">
									{#each item.detailRows as row}
										<div class="record-field">
											<dt class="record-field__label">{row.label}</dt>
											<dd class="record-field__value">
												{#if row.mono}
													<code>{row.value}</code>
												{:else}
													{row.value}
												{/if}
											</dd>
										</div>
									{/each}
								</dl>
							</article>
						{/each}
					</div>
				</details>
			{/if}

			{#if operationsRouteGuidance}
				<details class="product-panel reference-context" aria-label="Collection guidance">
					<summary class="record-row__title">Collection guidance</summary>
					<div class="mt-4">
						<RouteGuidancePanel guidance={operationsRouteGuidance} />
					</div>
				</details>
			{/if}

			<details
				class="product-panel reference-context"
				aria-label={operationsWorkspaceView.referenceTitle}
			>
				<summary class="record-row__title">Technical collection reference</summary>
				<div class="product-panel__header mt-4">
					<div>
						<p class="product-kicker">Collection reference</p>
						<h2 class="product-title">{operationsWorkspaceView.referenceTitle}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{operationsWorkspaceView.referenceDescription}
						</p>
						<p class="mt-2 text-sm font-semibold text-[var(--color-text)]">
							{operationsWorkspaceView.title} / {operationsWorkspaceView.subtitle}
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
						aria-label="Collection monitor"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Collection monitor</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">Response collection</h3>
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
						aria-label="Score coverage"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Score coverage</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Submitted scoring coverage
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
											? 'Remediating scores'
											: 'Remediate missing scores'}
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
								Score remediation requires setup management access.
							</p>
						{/if}

						{#if scoreRemediationResult}
							<dl class="record-grid" aria-label="Score remediation result">
								<div class="record-field">
									<dt class="record-field__label">Submitted responses</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.submittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Eligible submitted</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.eligibleSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Already scored</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.alreadyScoredSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Remediated</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.remediatedSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Not configured</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.skippedNotConfiguredSubmittedResponseCount}
									</dd>
								</div>
								<div class="record-field">
									<dt class="record-field__label">Latest scoring activity</dt>
									<dd class="record-field__value">
										{scoreRemediationResult.latestScoringActivityAt ?? 'Not available'}
									</dd>
								</div>
							</dl>
						{/if}
					</div>
				{/if}

				<div
					role="group"
					aria-label="Collection selected campaign"
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">Selected campaign</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Collection state detail
						</h3>
					</div>

					{#if operationsWorkspaceView.selectedCampaignRows.length > 0}
						<dl class="record-grid">
							{#each operationsWorkspaceView.selectedCampaignRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>
					{:else if operationsWorkspaceView.emptyState}
						<p class="record-row text-sm text-[var(--color-text-muted)]">
							<strong class="record-row__title">{operationsWorkspaceView.emptyState.title}</strong>
							<span>{operationsWorkspaceView.emptyState.message}</span>
						</p>
					{/if}
				</div>

				{#if operationsWorkspaceView.launchSnapshotRows.length > 0}
					<div
						role="group"
						aria-label="Launch snapshot review"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Launch snapshot</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Frozen launch configuration
							</h3>
						</div>

						<dl class="record-grid">
							{#each operationsWorkspaceView.launchSnapshotRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">
										{#if row.mono}
											<code>{row.value}</code>
										{:else}
											{row.value}
										{/if}
									</dd>
								</div>
							{/each}
						</dl>
					</div>
				{/if}

				{#if operationsWorkspaceView.missingPrerequisiteRows.length > 0}
					<div
						role="group"
						aria-label="Missing collection prerequisites"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Prerequisites</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Missing collection requirements
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
									<p class="result-line">
										<span>Code</span>
										<code>{row.code}</code>
									</p>
								</article>
							{/each}
						</div>
					</div>
				{/if}

				<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
					<div>
						<p class="product-kicker">Campaign rows</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Collection campaign context
						</h3>
					</div>

					{#if operationsWorkspaceView.emptyState}
						<p class="record-row text-sm text-[var(--color-text-muted)]">
							<strong class="record-row__title">{operationsWorkspaceView.emptyState.title}</strong>
							<span>{operationsWorkspaceView.emptyState.message}</span>
						</p>
					{:else}
						<div class="record-list">
							{#each operationsWorkspaceView.campaignRows as campaign (campaign.id)}
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
		{:else if reportsWorkspaceView}
			{#if reportsWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label="Sample study read-only state">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Study ownership</p>
							<h2 class="product-title">{reportsWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{reportsWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={reportsWorkspaceView.ownership.badgeStatus} label="Read-only" />
					</div>
				</section>
			{/if}

			{#if reportsWorkspace}
				<SelectedSeriesReportsWorkflow
					workspace={reportsWorkspace}
					{canManageSetup}
					onWorkspaceRefresh={() => refreshReportsWorkspace()}
				/>

				<details class="product-panel reference-context" aria-label="Results overview">
					<summary class="record-row__title">Results status details</summary>
					<div class="product-panel__header mt-4">
						<div>
							<p class="product-kicker">{reportsWorkspaceView.surfaceEyebrow}</p>
							<h2 class="product-title">Results status</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{reportsWorkspaceView.surfaceDescription}
							</p>
						</div>
						<StatusBadge
							status={reportsWorkspaceView.resultsOverview[0]?.status ?? 'not_available'}
							label={reportsWorkspaceView.resultsOverview[0]?.badgeLabel ?? 'Unavailable'}
						/>
					</div>

					<div class="record-list">
						{#each reportsWorkspaceView.resultsOverview as item (item.id)}
							<article class="record-row" aria-label={item.label}>
								<div class="record-row__header">
									<div>
										<h3 class="record-row__title">{item.label}</h3>
										<p class="text-sm text-[var(--color-text-muted)]">{item.summary}</p>
									</div>
									<StatusBadge status={item.status} label={item.badgeLabel} />
								</div>
								<p class="text-sm text-[var(--color-text-muted)]">{item.guidance}</p>
								{#if item.detailRows.length > 0}
									<dl class="record-grid">
										{#each item.detailRows as row}
											<div class="record-field">
												<dt class="record-field__label">{row.label}</dt>
												<dd class="record-field__value">
													{#if row.mono}
														<code>{row.value}</code>
													{:else}
														{row.value}
													{/if}
												</dd>
											</div>
										{/each}
									</dl>
								{/if}
							</article>
						{/each}
					</div>
				</details>

				<details class="product-panel reference-context" aria-label="Report widgets">
					<summary class="record-row__title">Configured report widgets</summary>
					<div class="mt-4">
						<ReportWidgetsSection
							manifest={reportsWidgetManifest}
							warning={reportsWidgetManifestWarning}
						/>
					</div>
				</details>

				<details class="product-panel reference-context" aria-label="Report dashboard">
					<summary class="record-row__title">Report dashboard and snapshot</summary>
					<div class="mt-4">
						<SelectedSeriesReportSnapshot workspace={reportsWorkspace} />
					</div>
				</details>
			{/if}

			{#if reportsRouteGuidance}
				<details class="product-panel reference-context" aria-label="Results guidance">
					<summary class="record-row__title">Results guidance</summary>
					<div class="mt-4">
						<RouteGuidancePanel guidance={reportsRouteGuidance} />
					</div>
				</details>
			{/if}

			<details
				class="product-panel reference-context"
				aria-label={reportsWorkspaceView.referenceTitle}
			>
				<summary class="record-row__title">Technical results reference</summary>
				<div class="product-panel__header mt-4">
					<div>
						<p class="product-kicker">Results reference</p>
						<h2 class="product-title">{reportsWorkspaceView.referenceTitle}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{reportsWorkspaceView.referenceDescription}
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

				{#if reportsWorkspaceView.scoreCoverageSignal}
					<div
						role="group"
						aria-label="Score coverage"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div class="product-panel__header">
							<div>
								<p class="product-kicker">Score coverage</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">Report readiness</h3>
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
					aria-label="Results selected campaign"
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">Selected campaign</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">Report state</h3>
					</div>

					{#if reportsWorkspaceView.selectedCampaignRows.length > 0}
						<dl class="record-grid">
							{#each reportsWorkspaceView.selectedCampaignRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">
										{#if row.mono}
											<code>{row.value}</code>
										{:else}
											{row.value}
										{/if}
									</dd>
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
						aria-label="Results provenance"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Provenance</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Launch, policy, and export state
							</h3>
						</div>
						<dl class="record-grid">
							{#each reportsWorkspaceView.provenanceRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">
										{#if row.mono}
											<code>{row.value}</code>
										{:else}
											{row.value}
										{/if}
									</dd>
								</div>
							{/each}
						</dl>
					</div>
				{/if}

				{#if reportsWorkspaceView.missingPrerequisiteRows.length > 0}
					<div
						role="group"
						aria-label="Missing results prerequisites"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Prerequisites</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Missing result requirements
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
									<p class="result-line">
										<span>Code</span>
										<code>{row.code}</code>
									</p>
								</article>
							{/each}
						</div>
					</div>
				{/if}

				<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
					<div>
						<p class="product-kicker">Campaign rows</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Results campaign context
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
										{#each campaign.rows as row}
											<div class="record-field">
												<dt class="record-field__label">{row.label}</dt>
												<dd class="record-field__value">
													{#if row.mono}
														<code>{row.value}</code>
													{:else}
														{row.value}
													{/if}
												</dd>
											</div>
										{/each}
									</dl>
								</article>
							{/each}
						</div>
					{/if}
				</div>
			</details>
		{:else if wavesWorkspaceView}
			{#if wavesWorkspaceView.readOnlyMessage}
				<section class="product-panel" aria-label="Sample study read-only state">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Study ownership</p>
							<h2 class="product-title">{wavesWorkspaceView.ownership.label}</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								{wavesWorkspaceView.readOnlyMessage}
							</p>
						</div>
						<StatusBadge status={wavesWorkspaceView.ownership.badgeStatus} label="Read-only" />
					</div>
				</section>
			{/if}

			{#if wavesRouteGuidance}
				<RouteGuidancePanel guidance={wavesRouteGuidance} />
			{/if}

			{#if wavesWorkspace}
				<section class="product-panel" role="group" aria-label="Longitudinal analysis overview">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Advanced study lifecycle</p>
							<h2 class="product-title">Longitudinal analysis overview</h2>
							<p class="mt-1 text-sm text-[var(--color-text-muted)]">
								Waves compares repeated waves in the same study when change over time
								matters.
							</p>
						</div>
					</div>

					<dl class="metric-grid">
						<div class="metric-card">
							<dt class="metric-card__label">Longitudinal waves</dt>
							<dd class="metric-card__value">
								{wavesWorkspace.summary.longitudinalWaveCount}
							</dd>
						</div>
						<div class="metric-card">
							<dt class="metric-card__label">Complete trajectories</dt>
							<dd class="metric-card__value">
								{wavesWorkspace.summary.completeTrajectoryCount}
							</dd>
						</div>
						<div class="metric-card">
							<dt class="metric-card__label">Visible comparisons</dt>
							<dd class="metric-card__value">
								{wavesWorkspace.summary.visibleComparisonCount}
							</dd>
						</div>
						<div class="metric-card">
							<dt class="metric-card__label">Suppressed comparisons</dt>
							<dd class="metric-card__value">
								{wavesWorkspace.summary.suppressedComparisonCount}
							</dd>
						</div>
						<div class="metric-card">
							<dt class="metric-card__label">Blocked comparisons</dt>
							<dd class="metric-card__value">
								{wavesWorkspace.summary.blockedComparisonCount}
							</dd>
						</div>
					</dl>

					<dl class="record-grid">
						<div class="record-field">
							<dt class="record-field__label">Baseline wave</dt>
							<dd class="record-field__value">
								{wavesWorkspace.selectedBaselineWave?.name ?? 'Missing'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Comparison wave</dt>
							<dd class="record-field__value">
								{wavesWorkspace.selectedComparisonWave?.name ?? 'Missing'}
							</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Comparison availability</dt>
							<dd class="record-field__value">{wavesWorkspace.comparison.status}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Disclosure</dt>
							<dd class="record-field__value">{wavesWorkspace.comparison.disclosureState}</dd>
						</div>
						<div class="record-field">
							<dt class="record-field__label">Compatibility</dt>
							<dd class="record-field__value">
								{wavesWorkspace.comparison.compatibilityState}
							</dd>
						</div>
					</dl>

					<div class="record-list">
						<article class="record-row" aria-label="Repeated-wave studies">
							<h3 class="record-row__title">Repeated-wave studies</h3>
							<p class="text-sm text-[var(--color-text-muted)]">
								Use Waves when the same study is launched in repeated waves and the
								review depends on linked trajectories or change over time.
							</p>
						</article>
						<article class="record-row" aria-label="Single-wave studies">
							<h3 class="record-row__title">Single-wave studies</h3>
							<p class="text-sm text-[var(--color-text-muted)]">
								Use Review results or Use exports when the study has only one wave and
								does not need longitudinal comparison.
							</p>
						</article>
					</div>
				</section>

				<SelectedSeriesWaveComparisonSnapshot workspace={wavesWorkspace} />
				<SelectedSeriesWavesWorkflow
					workspace={wavesWorkspace}
					onWorkspaceRefresh={() => refreshWavesWorkspace()}
				/>
			{/if}

			<section
				class="product-panel reference-context"
				aria-label={`${wavesWorkspaceView.surfaceLabel} selected-series context`}
			>
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{wavesWorkspaceView.surfaceEyebrow}</p>
						<h2 class="product-title">{wavesWorkspaceView.title}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							Waves for {wavesWorkspaceView.subtitle}.
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
					aria-label="Waves selected waves"
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">Selected waves</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">Comparison state</h3>
					</div>

					{#if wavesWorkspaceView.selectedWaveRows.length > 0}
						<dl class="record-grid">
							{#each wavesWorkspaceView.selectedWaveRows as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>
					{:else if wavesWorkspaceView.emptyState}
						<p class="record-row text-sm text-[var(--color-text-muted)]">
							<strong class="record-row__title">{wavesWorkspaceView.emptyState.title}</strong>
							<span>{wavesWorkspaceView.emptyState.message}</span>
						</p>
					{/if}
				</div>

				{#if wavesWorkspaceView.provenanceRows.length > 0}
					<div
						role="group"
						aria-label="Waves provenance"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Provenance</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Launch and policy state
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
						aria-label="Missing waves prerequisites"
						class="grid gap-3 border-t border-[var(--color-border)] pt-4"
					>
						<div>
							<p class="product-kicker">Prerequisites</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Missing wave requirements
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
									<p class="result-line">
										<span>Code</span>
										<code>{row.code}</code>
									</p>
								</article>
							{/each}
						</div>
					</div>
				{/if}

				<div
					role="group"
					aria-label="Waves campaign rows"
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">Wave rows</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Longitudinal campaign context
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
			</section>
		{:else if surfaceView}
			<section
				class="product-panel"
				aria-label={`${surfaceView.surfaceLabel} selected-series context`}
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
							<p class="product-kicker">Governance</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Selected-series readiness
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
					aria-label="Selected series campaign rows"
					class="grid gap-3 border-t border-[var(--color-border)] pt-4"
				>
					<div>
						<p class="product-kicker">Campaign rows</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">
							Selected-series campaign context
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
							<p class="product-kicker">Product workflow</p>
							<h3 class="product-title">{surfaceView.proofActionTitle}</h3>
							<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
								{surfaceView.proofActionDescription}
							</p>
						</div>
						<span class="status-badge" data-status="proof_only">Proof-only</span>
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
							<p class="product-kicker">Product workflow</p>
							<h3 class="product-title">Read-only access</h3>
						</div>
					</div>
					<p class="text-sm text-[var(--color-text-muted)]">
						Workflow actions require setup management access.
					</p>
				</section>
			{/if}
		{/if}
	</LoadingBoundary>
</section>
