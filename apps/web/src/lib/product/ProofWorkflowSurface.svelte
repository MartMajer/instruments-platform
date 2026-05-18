<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { onMount } from 'svelte';
	import {
		AlertTriangle,
		Check,
		Download,
		Link,
		LogIn,
		LogOut,
		LoaderCircle,
		MailCheck,
		Plus,
		RefreshCw,
		SearchCheck,
		Send
	} from 'lucide-svelte';
	import { ApiError, createApiClient } from '$lib/api/client';
	import {
		createLoginUrlFromEnv,
		createSessionHeadersFromEnv,
		defaultDevTenantId,
		defaultDevUserId
	} from '$lib/api/session-headers';
	import {
		createSetupApi,
		type AuthSessionResponse,
		type CampaignSeriesWaveComparisonProofResponse,
		type CampaignSeriesTwoWaveProofResponse,
		type CampaignReportProofResponse,
		type CampaignDraftResponse,
		type CampaignInvitationBatchResponse,
		type CampaignOpenLinkResponse,
		type ComputeScoresResponse,
		type CreateCampaignRequest,
		type CreatePrivateInstrumentImportRequest,
		type CreateScoringRuleRequest,
		type CreateTemplateQuestionRequest,
		type CreateTemplateVersionRequest,
		type ExportArtifactDownloadResponse,
		type InstrumentSummaryResponse,
		type LabAssignmentResponse,
		type LaunchCampaignResponse,
		type LaunchReadinessResponse,
		type ProcessCampaignEmailDeliveriesResponse,
		type ReportProofExportArtifactResponse,
		type RespondentCampaignResponse,
		type ResponseSessionResponse,
		type SaveAnswersResponse,
		type SetupIdResponse,
		type SubmitResponseSessionResponse,
		type TemplateVersionDetailResponse,
		type TwoWaveProofWaveResponse
	} from '$lib/api/setup';
	import {
		resolveProofWorkflowSeries,
		type ProofWorkflowSelectedCampaign,
		type ProofWorkflowSelectedSeries
	} from '$lib/product/proof-workflow';
	import { formatScoreOutputMetadata } from './view-models';

	type StepKey =
		| 'instrument'
		| 'template'
		| 'scoring'
		| 'campaign'
		| 'readiness'
		| 'response'
		| 'twoWave';
	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';
	type AuthState = 'checking' | 'authenticated' | 'unauthenticated' | 'forbidden' | 'failed';
	type ProofWorkflowSurface = 'all' | 'setup' | 'operations' | 'reports' | 'waves';
	type TwoWaveProofState = {
		series: SetupIdResponse | null;
		waves: Array<{
			draft: CampaignDraftResponse;
			readiness: LaunchReadinessResponse;
			launch: LaunchCampaignResponse;
			openLink: CampaignOpenLinkResponse;
		}>;
		proof: (CampaignSeriesTwoWaveProofResponse & { waves: TwoWaveProofWaveResponse[] }) | null;
	};

	const loginUrl = createLoginUrlFromEnv(env);
	const logoutUrl = env.PUBLIC_AUTH_LOGOUT_URL || '/auth/logout';

	let {
		surface = 'all',
		authManagedByParent = false,
		showIntro = true,
		selectedSeries = null,
		selectedCampaign = null,
		onWorkflowMutation
	}: {
		surface?: ProofWorkflowSurface;
		authManagedByParent?: boolean;
		showIntro?: boolean;
		selectedSeries?: ProofWorkflowSelectedSeries | null;
		selectedCampaign?: ProofWorkflowSelectedCampaign | null;
		onWorkflowMutation?: () => void | Promise<void>;
	} = $props();

	const setupApi = createSetupApi(
		createApiClient({
			defaultHeaders: createSessionHeadersFromEnv(env),
			csrf: true
		})
	);

	const managedAuthSession: AuthSessionResponse = {
		userId: defaultDevUserId,
		tenantId: defaultDevTenantId,
		permissions: ['setup.manage']
	};
	const showSetupSections = $derived(surface === 'all' || surface === 'setup');
	const showOperationsSections = $derived(
		surface === 'all' || surface === 'setup' || surface === 'operations'
	);
	const showReportsSections = $derived(surface === 'all' || surface === 'reports');
	const showWavesSections = $derived(surface === 'all' || surface === 'waves');

	let authState = $state<AuthState>('checking');
	let authSession = $state<AuthSessionResponse | null>(null);
	let authMessage = $state<string | null>(null);

	let stepStates = $state<Record<StepKey, StepState>>({
		instrument: 'idle',
		template: 'idle',
		scoring: 'idle',
		campaign: 'idle',
		readiness: 'idle',
		response: 'idle',
		twoWave: 'idle'
	});
	let errors = $state<Record<StepKey, string | null>>({
		instrument: null,
		template: null,
		scoring: null,
		campaign: null,
		readiness: null,
		response: null,
		twoWave: null
	});

	let instrumentForm = $state<CreatePrivateInstrumentImportRequest>({
		code: 'tenant-burnout-pulse',
		version: '1.0.0',
		fullName: 'Tenant burnout pulse',
		domain: 'psychometric',
		provenanceNote: 'Tenant provided item text and attested rights for internal use.',
		rightsStatus: 'attested_by_tenant',
		validityLabel: 'tenant_provided',
		licenseType: 'unknown'
	});
	let instruments = $state<InstrumentSummaryResponse[]>([]);
	let instrumentResult = $state<InstrumentSummaryResponse | null>(null);

	let templateName = $state('Tenant burnout pulse template');
	let sectionTitle = $state('Core');
	let questions = $state<CreateTemplateQuestionRequest[]>([
		{
			ordinal: 1,
			code: 'q01',
			type: 'likert',
			textDefault: 'After work, I need time to recover mentally.',
			sectionCode: 'core',
			scaleCode: 'agreement',
			required: true,
			reverseCoded: false,
			measurementLevel: 'ordinal',
			payload: '{}',
			missingCodes: '[]'
		},
		{
			ordinal: 2,
			code: 'q02',
			type: 'likert',
			textDefault: 'During demanding weeks, small interruptions feel harder to handle.',
			sectionCode: 'core',
			scaleCode: 'agreement',
			required: true,
			reverseCoded: false,
			measurementLevel: 'ordinal',
			payload: '{}',
			missingCodes: '[]'
		},
		{
			ordinal: 3,
			code: 'q03',
			type: 'likert',
			textDefault: 'I can usually regain focus after a short break.',
			sectionCode: 'core',
			scaleCode: 'agreement',
			required: true,
			reverseCoded: true,
			measurementLevel: 'ordinal',
			payload: '{}',
			missingCodes: '[]'
		}
	]);
	let templateResult = $state<TemplateVersionDetailResponse | null>(null);

	let scoringForm = $state({
		ruleKey: 'tenant-burnout.total',
		ruleVersion: '1.0.0',
		schemaVersion: 'scoring-rule/v1',
		engineMinVersion: 'engine/v1',
		document: buildDefaultScoringDocument('tenant-burnout.total'),
		produces: buildDefaultProduces(),
		compatibility: '{}'
	});
	let scoringResult = $state<SetupIdResponse | null>(null);

	let campaignForm = $state({
		seriesName: 'Tenant quarterly pulse',
		name: 'Wave 1',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en'
	});
	let campaignSeriesResult = $state<SetupIdResponse | null>(null);
	let campaignResult = $state<CampaignDraftResponse | null>(null);
	let readinessResult = $state<LaunchReadinessResponse | null>(null);
	let launchResult = $state<LaunchCampaignResponse | null>(null);
	let openLinkResult = $state<CampaignOpenLinkResponse | null>(null);
	let invitationBatchResult = $state<CampaignInvitationBatchResponse | null>(null);
	let deliveryResult = $state<ProcessCampaignEmailDeliveriesResponse | null>(null);
	let respondentCampaign = $state<RespondentCampaignResponse | null>(null);
	let labAssignmentResult = $state<LabAssignmentResponse | null>(null);
	let responseSessionResult = $state<ResponseSessionResponse | null>(null);
	let savedAnswersResult = $state<SaveAnswersResponse | null>(null);
	let submittedResponseResult = $state<SubmitResponseSessionResponse | null>(null);
	let scoreResult = $state<ComputeScoresResponse | null>(null);
	let reportProofResult = $state<CampaignReportProofResponse | null>(null);
	let reportExportResult = $state<ReportProofExportArtifactResponse | null>(null);
	let storedReportExportResult = $state<ReportProofExportArtifactResponse | null>(null);
	let reportExportDownloadResult = $state<ExportArtifactDownloadResponse | null>(null);
	let responseAnswers = $state<Record<string, string>>({});
	let twoWaveProof = $state<TwoWaveProofState>({
		series: null,
		waves: [],
		proof: null
	});
	let waveComparisonProofResult = $state<CampaignSeriesWaveComparisonProofResponse | null>(null);
	let setupRunSuffix = $state('');

	const activeCampaignSeries = $derived(
		resolveProofWorkflowSeries(selectedSeries, campaignSeriesResult)
	);

	$effect(() => {
		applySelectedCampaignContext();
	});
	const activeTwoWaveSeries = $derived(
		resolveProofWorkflowSeries(selectedSeries, twoWaveProof.series)
	);

	onMount(() => {
		void initializeSetupWorkspace();
	});

	async function initializeSetupWorkspace() {
		resetSetupRun();
		if (authManagedByParent) {
			authState = 'authenticated';
			authSession = managedAuthSession;
			return;
		}

		await loadCurrentSession();
	}

	async function loadCurrentSession() {
		authState = 'checking';
		authMessage = null;

		try {
			authSession = await setupApi.getCurrentSession();
			authState = 'authenticated';
		} catch (error) {
			authSession = null;

			if (error instanceof ApiError && error.status === 401) {
				authState = 'unauthenticated';
				authMessage = 'Sign in to manage tenant setup.';
				return;
			}

			if (error instanceof ApiError && error.status === 403) {
				authState = 'forbidden';
				authMessage = 'This account does not have access to this tenant setup workspace.';
				return;
			}

			authState = 'failed';
			authMessage = formatError(error);
		}
	}

	async function createInstrumentImport() {
		const result = await runStep('instrument', async () => {
			const created = await setupApi.createPrivateInstrumentImport(instrumentForm);
			instruments = await setupApi.listInstruments();
			return created;
		});

		if (result) {
			instrumentResult = result;
		}
	}

	async function createTemplateVersion() {
		const result = await runStep('template', () =>
			setupApi.createTemplateVersion(buildTemplateRequest())
		);

		if (result) {
			templateResult = result;
			scoringResult = null;
			campaignSeriesResult = null;
			campaignResult = null;
			readinessResult = null;
			launchResult = null;
			openLinkResult = null;
			invitationBatchResult = null;
			deliveryResult = null;
			clearResponseLab();
			clearTwoWaveProof({ resetStatus: true });
		}
	}

	async function createScoringRule() {
		if (!templateResult) {
			errors = { ...errors, scoring: 'Create a template version first.' };
			return;
		}

		const request: CreateScoringRuleRequest = {
			templateVersionId: templateResult.templateVersionId,
			ruleKey: scoringForm.ruleKey,
			ruleVersion: scoringForm.ruleVersion,
			schemaVersion: scoringForm.schemaVersion,
			engineMinVersion: scoringForm.engineMinVersion,
			document: scoringForm.document,
			produces: scoringForm.produces,
			compatibility: scoringForm.compatibility
		};
		const result = await runStep('scoring', () => setupApi.createScoringRule(request));

		if (result) {
			scoringResult = result;
			clearTwoWaveProof({ resetStatus: true });
		}
	}

	async function ensureCampaignSeriesForProof(defaultName: string): Promise<SetupIdResponse> {
		if (activeCampaignSeries) {
			return { id: activeCampaignSeries.id };
		}

		const series = await setupApi.createCampaignSeries({ name: defaultName });
		campaignSeriesResult = series;
		return series;
	}

	async function createCampaignDraft() {
		const currentTemplate = templateResult;
		if (!currentTemplate) {
			errors = { ...errors, campaign: 'Create a template version first.' };
			return;
		}

		const result = await runStep('campaign', async () => {
			const series = await ensureCampaignSeriesForProof(campaignForm.seriesName);
			const campaignRequest: CreateCampaignRequest = {
				templateVersionId: currentTemplate.templateVersionId,
				name: campaignForm.name,
				responseIdentityMode: campaignForm.responseIdentityMode,
				campaignSeriesId: series.id,
				schedule: '{}',
				defaultLocale: campaignForm.defaultLocale
			};

			return setupApi.createCampaign(campaignRequest);
		});

		if (result) {
			campaignResult = result;
			readinessResult = null;
			launchResult = null;
			openLinkResult = null;
			invitationBatchResult = null;
			deliveryResult = null;
			clearResponseLab();
			void onWorkflowMutation?.();
		}
	}

	async function checkLaunchReadiness() {
		const currentCampaign = campaignResult;
		if (!currentCampaign) {
			errors = { ...errors, readiness: 'Create a campaign draft first.' };
			return;
		}

		const result = await runStep('readiness', () =>
			setupApi.getLaunchReadiness(currentCampaign.id)
		);

		if (result) {
			readinessResult = result;
			launchResult = null;
			openLinkResult = null;
			invitationBatchResult = null;
			deliveryResult = null;
		}
	}

	async function launchCampaign() {
		const currentCampaign = campaignResult;
		if (!currentCampaign) {
			errors = { ...errors, readiness: 'Create a campaign draft first.' };
			return;
		}

		if (!readinessResult?.ready) {
			errors = { ...errors, readiness: 'Check readiness and resolve blockers before launch.' };
			return;
		}

		const result = await runStep('readiness', () => setupApi.launchCampaign(currentCampaign.id));

		if (result) {
			launchResult = result;
			campaignResult = { ...currentCampaign, status: result.status };
			openLinkResult = null;
			invitationBatchResult = null;
			deliveryResult = null;
			clearResponseLab();
			void onWorkflowMutation?.();
		}
	}

	async function createOpenLink() {
		const currentCampaign = campaignResult;
		if (!currentCampaign || !launchResult) {
			errors = { ...errors, readiness: 'Launch a campaign before creating an open link.' };
			return;
		}

		const result = await runStep('readiness', () =>
			setupApi.createCampaignOpenLink(currentCampaign.id)
		);

		if (result) {
			openLinkResult = result;
			void onWorkflowMutation?.();
		}
	}

	async function createTwoWaveProof() {
		const currentTemplate = templateResult;
		if (!currentTemplate || !scoringResult) {
			errors = { ...errors, twoWave: 'Create a template version and scoring rule first.' };
			return;
		}

		clearTwoWaveProof();
		const result = await runStep('twoWave', async () => {
			const series = await ensureCampaignSeriesForProof(
				`Tenant longitudinal proof ${setupRunSuffix}`
			);
			const waves: TwoWaveProofState['waves'] = [];

			for (const waveName of ['Wave 1', 'Wave 2']) {
				const draft = await setupApi.createCampaign({
					templateVersionId: currentTemplate.templateVersionId,
					name: `${waveName} ${setupRunSuffix}`,
					responseIdentityMode: 'anonymous_longitudinal',
					campaignSeriesId: series.id,
					schedule: '{}',
					defaultLocale: campaignForm.defaultLocale
				});
				const readiness = await setupApi.getLaunchReadiness(draft.id);
				if (!readiness.ready) {
					throw new Error(`${waveName} launch readiness failed.`);
				}
				const launch = await setupApi.launchCampaign(draft.id);
				const openLink = await setupApi.createCampaignOpenLink(draft.id);
				waves.push({ draft, readiness, launch, openLink });
			}

			return { series, waves };
		});

		if (result) {
			twoWaveProof = { series: result.series, waves: result.waves, proof: null };
			void onWorkflowMutation?.();
		}
	}

	async function refreshTwoWaveProof() {
		const series = activeTwoWaveSeries;
		if (!series) {
			errors = { ...errors, twoWave: 'Create a two-wave proof first.' };
			return;
		}

		const proof = await runStep('twoWave', () =>
			setupApi.getCampaignSeriesTwoWaveProof(series.id)
		);

		if (proof) {
			twoWaveProof = { ...twoWaveProof, series: { id: series.id }, proof };
			waveComparisonProofResult = null;
			void onWorkflowMutation?.();
		}
	}

	async function viewWaveComparisonProof() {
		const series = activeTwoWaveSeries;
		if (!series) {
			errors = { ...errors, twoWave: 'Create a two-wave proof first.' };
			return;
		}

		const proof = await runStep('twoWave', () =>
			setupApi.getCampaignSeriesWaveComparisonProof(series.id)
		);

		if (proof) {
			twoWaveProof = { ...twoWaveProof, series: { id: series.id } };
			waveComparisonProofResult = proof;
			void onWorkflowMutation?.();
		}
	}

	async function queueEmailInvitations() {
		const currentCampaign = campaignResult;
		if (!currentCampaign || !launchResult) {
			errors = { ...errors, readiness: 'Launch a campaign before queuing invitations.' };
			return;
		}

		const result = await runStep('readiness', () =>
			setupApi.createCampaignInvitationBatch(currentCampaign.id, {
				recipients: [
					{ email: `ada.${setupRunSuffix}@example.com` },
					{ email: `bo.${setupRunSuffix}@example.com` }
				]
			})
		);

		if (result) {
			invitationBatchResult = result;
			deliveryResult = null;
			void onWorkflowMutation?.();
		}
	}

	async function processLocalDelivery() {
		const currentCampaign = campaignResult;
		if (!currentCampaign || !invitationBatchResult) {
			errors = { ...errors, readiness: 'Queue invitations before processing local delivery.' };
			return;
		}

		const result = await runStep('readiness', () =>
			setupApi.processCampaignEmailDeliveries(currentCampaign.id, { batchSize: 25 })
		);

		if (result) {
			deliveryResult = result;
			void onWorkflowMutation?.();
		}
	}

	async function loadResponseLab() {
		const currentCampaign = campaignResult;
		if (!currentCampaign) {
			errors = { ...errors, response: 'Create a campaign draft first.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.getRespondentCampaign(currentCampaign.id)
		);

		if (result) {
			respondentCampaign = result;
			responseAnswers = Object.fromEntries(
				result.questions.map((question) => [question.id, defaultAnswerFor(question)])
			);
			labAssignmentResult = null;
			responseSessionResult = null;
			savedAnswersResult = null;
			submittedResponseResult = null;
			scoreResult = null;
		}
	}

	async function createLabAssignment() {
		const currentCampaign = campaignResult;
		if (!currentCampaign) {
			errors = { ...errors, response: 'Create a campaign draft first.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.createLabAssignment(currentCampaign.id)
		);

		if (result) {
			labAssignmentResult = result;
			responseSessionResult = null;
			savedAnswersResult = null;
			submittedResponseResult = null;
			scoreResult = null;
			reportProofResult = null;
		}
	}

	async function startResponseSession() {
		const assignment = labAssignmentResult;
		if (!assignment) {
			errors = { ...errors, response: 'Create a lab assignment first.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.createResponseSession({
				assignmentId: assignment.assignmentId,
				locale: respondentCampaign?.defaultLocale ?? campaignForm.defaultLocale
			})
		);

		if (result) {
			responseSessionResult = result;
			savedAnswersResult = null;
			submittedResponseResult = null;
			scoreResult = null;
			reportProofResult = null;
		}
	}

	async function saveResponseAnswers() {
		const session = responseSessionResult;
		const campaign = respondentCampaign;
		if (!session || !campaign) {
			errors = { ...errors, response: 'Start a response session first.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.saveAnswers(session.id, {
				answers: campaign.questions.map((question) => ({
					questionId: question.id,
					value: normalizeAnswerValue(responseAnswers[question.id])
				}))
			})
		);

		if (result) {
			savedAnswersResult = result;
			submittedResponseResult = null;
			scoreResult = null;
			reportProofResult = null;
			reportExportResult = null;
			storedReportExportResult = null;
			reportExportDownloadResult = null;
		}
	}

	async function submitResponse() {
		const session = responseSessionResult;
		if (!session) {
			errors = { ...errors, response: 'Start a response session first.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.submitResponseSession(session.id, { timeTakenMs: null })
		);

		if (result) {
			submittedResponseResult = result;
			scoreResult = null;
			reportProofResult = null;
			reportExportResult = null;
			storedReportExportResult = null;
			reportExportDownloadResult = null;
		}
	}

	async function computeResponseScores() {
		const session = responseSessionResult;
		if (!session) {
			errors = { ...errors, response: 'Start a response session first.' };
			return;
		}

		const result = await runStep('response', () => setupApi.computeResponseScores(session.id));

		if (result) {
			scoreResult = result;
			reportProofResult = null;
			reportExportResult = null;
			storedReportExportResult = null;
			reportExportDownloadResult = null;
		}
	}

	async function viewReportProof() {
		const currentCampaign = campaignResult;
		if (!currentCampaign) {
			errors = { ...errors, response: 'Create a campaign before viewing report proof.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.getCampaignReportProof(currentCampaign.id)
		);

		if (result) {
			reportProofResult = result;
			reportExportResult = null;
			storedReportExportResult = null;
			reportExportDownloadResult = null;
		}
	}

	async function createReportProofExport() {
		const currentCampaign = campaignResult;
		if (!currentCampaign) {
			errors = { ...errors, response: 'Create a campaign before creating export proof.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.createCampaignReportProofExport(currentCampaign.id)
		);

		if (result) {
			reportExportResult = result;
			storedReportExportResult = null;
			reportExportDownloadResult = null;
			void onWorkflowMutation?.();
		}
	}

	async function fetchStoredReportProofExport() {
		const currentExport = reportExportResult;
		if (!currentExport) {
			errors = { ...errors, response: 'Create an export proof before fetching it.' };
			return;
		}

		const result = await runStep('response', () => setupApi.getExportArtifact(currentExport.id));

		if (result) {
			storedReportExportResult = result;
			reportExportDownloadResult = null;
		}
	}

	async function downloadReportProofExport() {
		const currentExport = storedReportExportResult ?? reportExportResult;
		if (!currentExport) {
			errors = { ...errors, response: 'Create or fetch an export proof before downloading it.' };
			return;
		}

		const result = await runStep('response', () =>
			setupApi.downloadExportArtifactCsv(currentExport.id)
		);

		if (result) {
			reportExportDownloadResult = result;
		}
	}

	function buildTemplateRequest(): CreateTemplateVersionRequest {
		return {
			templateName,
			semver: '1.0.0',
			defaultLocale: 'en',
			instrumentId: instrumentResult?.id ?? null,
			sections: [{ ordinal: 1, code: 'core', titleDefault: sectionTitle }],
			scales: [
				{
					code: 'agreement',
					type: 'likert',
					minValue: 1,
					maxValue: 5,
					step: 1,
					naAllowed: false,
					anchors: JSON.stringify([
						{ value: 1, label: 'Strongly disagree' },
						{ value: 5, label: 'Strongly agree' }
					])
				}
			],
			questions
		};
	}

	async function runStep<T>(step: StepKey, action: () => Promise<T>) {
		stepStates = { ...stepStates, [step]: 'submitting' };
		errors = { ...errors, [step]: null };

		try {
			const result = await action();
			stepStates = { ...stepStates, [step]: 'succeeded' };
			return result;
		} catch (error) {
			stepStates = { ...stepStates, [step]: 'failed' };
			errors = { ...errors, [step]: formatError(error) };
			return null;
		}
	}

	function resetSetupRun() {
		applySetupRun(generateSetupRunSuffix());
	}

	function applySetupRun(suffix: string) {
		const ruleKey = `tenant-burnout.${suffix}.total`;

		setupRunSuffix = suffix;
		instrumentForm = {
			...instrumentForm,
			code: `tenant-burnout-pulse-${suffix}`,
			fullName: `Tenant burnout pulse ${suffix}`
		};
		templateName = `Tenant burnout pulse template ${suffix}`;
		scoringForm = {
			...scoringForm,
			ruleKey,
			document: buildDefaultScoringDocument(ruleKey),
			produces: buildDefaultProduces()
		};
		campaignForm = {
			...campaignForm,
			seriesName: `Tenant quarterly pulse ${suffix}`
		};
		stepStates = {
			instrument: 'idle',
			template: 'idle',
			scoring: 'idle',
			campaign: 'idle',
			readiness: 'idle',
			response: 'idle',
			twoWave: 'idle'
		};
		errors = {
			instrument: null,
			template: null,
			scoring: null,
			campaign: null,
			readiness: null,
			response: null,
			twoWave: null
		};
		instruments = [];
		instrumentResult = null;
		templateResult = null;
		scoringResult = null;
		campaignSeriesResult = null;
		campaignResult = null;
		readinessResult = null;
		launchResult = null;
		openLinkResult = null;
		invitationBatchResult = null;
		deliveryResult = null;
		clearTwoWaveProof({ resetStatus: true });
		clearResponseLab();
		applySelectedCampaignContext();
	}

	function applySelectedCampaignContext() {
		if (!(surface === 'operations' || surface === 'reports') || !selectedCampaign) {
			return;
		}

		campaignResult = {
			id: selectedCampaign.id,
			campaignSeriesId: selectedSeries?.id ?? null,
			templateVersionId: '',
			name: selectedCampaign.name,
			status: selectedCampaign.status,
			responseIdentityMode: selectedCampaign.responseIdentityMode
		};
		readinessResult = { campaignId: selectedCampaign.id, ready: true, issues: [] };
		launchResult =
			selectedCampaign.status === 'live' && selectedCampaign.latestLaunchSnapshotId
				? {
						campaignId: selectedCampaign.id,
						status: selectedCampaign.status,
						launchSnapshotId: selectedCampaign.latestLaunchSnapshotId,
						templateVersionId: '',
						scoringRuleId: '',
						retentionPolicyId: '',
						disclosurePolicyId: '',
						responseIdentityMode: selectedCampaign.responseIdentityMode,
						defaultLocale: '',
						launchedAt: selectedCampaign.latestLaunchAt ?? ''
					}
				: null;
		openLinkResult = null;
		invitationBatchResult = null;
		deliveryResult = null;
		clearResponseLab();
	}

	function clearTwoWaveProof({ resetStatus = false }: { resetStatus?: boolean } = {}) {
		twoWaveProof = {
			series: null,
			waves: [],
			proof: null
		};
		waveComparisonProofResult = null;

		if (resetStatus) {
			stepStates = { ...stepStates, twoWave: 'idle' };
			errors = { ...errors, twoWave: null };
		}
	}

	function clearResponseLab() {
		respondentCampaign = null;
		labAssignmentResult = null;
		responseSessionResult = null;
		savedAnswersResult = null;
		submittedResponseResult = null;
		scoreResult = null;
		reportProofResult = null;
		reportExportResult = null;
		storedReportExportResult = null;
		reportExportDownloadResult = null;
		responseAnswers = {};
	}

	function generateSetupRunSuffix() {
		return crypto.randomUUID().slice(0, 8).toLowerCase();
	}

	function visibleInstruments() {
		const currentInstrument = instrumentResult;
		if (!currentInstrument) {
			return instruments.slice(0, 6);
		}

		const latest = instruments.find((instrument) => instrument.id === currentInstrument.id);
		const others = instruments.filter((instrument) => instrument.id !== currentInstrument.id);

		return latest ? [latest, ...others].slice(0, 6) : instruments.slice(0, 6);
	}

	function formatError(error: unknown) {
		if (error instanceof ApiError) {
			return `${error.message}: ${formatBody(error.body)}`;
		}

		return error instanceof Error ? error.message : 'Unknown error.';
	}

	function setResponseAnswer(questionId: string, value: string) {
		responseAnswers = { ...responseAnswers, [questionId]: value };
	}

	function defaultAnswerFor(question: {
		scaleMinValue?: number | null;
		scaleMaxValue?: number | null;
	}) {
		if (typeof question.scaleMinValue === 'number' && typeof question.scaleMaxValue === 'number') {
			return String(Math.round((question.scaleMinValue + question.scaleMaxValue) / 2));
		}

		return '4';
	}

	function normalizeAnswerValue(value: string | undefined) {
		const trimmed = value?.trim() ?? '';

		return trimmed.length > 0 ? trimmed : null;
	}

	function formatScoreValue(value: number) {
		return Number.isFinite(value) ? value.toFixed(2) : String(value);
	}

	function reportScoreDisplay(score: { disclosure: string; mean: number | null }) {
		if (score.disclosure === 'visible' && score.mean !== null) {
			return formatScoreValue(score.mean);
		}

		return 'insufficient responses';
	}

	function formatNullableScoreValue(value: number | null) {
		return value === null ? 'not shown' : formatScoreValue(value);
	}

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

	function csvPreview(content: string) {
		return content.split(/\r?\n/).slice(0, 4).join('\n');
	}

	function buildDefaultScoringDocument(ruleId: string) {
		return JSON.stringify(
			{
				rule_id: ruleId,
				rule_version: '1.0.0',
				schema_version: '1.0.0',
				engine_min_version: '1.0.0',
				scale_defaults: {
					agreement: { min: 1, max: 5 }
				},
				inputs: [{ id: 'core_items', kind: 'answers', items: ['q01', 'q02', 'q03'] }],
				nodes: [
					{ id: 'core_answers', op: 'select_answers', input: 'core_items' },
					{
						id: 'scored_answers',
						op: 'reverse_code',
						input: 'core_answers',
						scale: 'agreement',
						reverse_flag_source: 'explicit_list',
						explicit_reverse_items: ['q03']
					},
					{ id: 'total', op: 'mean', input: 'scored_answers' }
				],
				outputs: [{ code: 'total', node: 'total' }],
				missing_data: {
					defaults: { strategy: 'require_all' }
				}
			},
			null,
			2
		);
	}

	function buildDefaultProduces() {
		return JSON.stringify(
			{
				scores: ['total'],
				interpretation: {
					status: 'tenant_attested',
					source: 'tenant_defined',
					provenance:
						'Tenant-defined score bands for this setup; not validated; not official.',
					scores: {
						total: [
							{ code: 'lower', label: 'Tenant lower range', min: 1, max: 2.49 },
							{ code: 'middle', label: 'Tenant middle range', min: 2.5, max: 3.49 },
							{ code: 'higher', label: 'Tenant higher range', min: 3.5, max: 5 }
						]
					}
				}
			},
			null,
			2
		);
	}

	function formatBody(body: unknown) {
		const maxLength = 420;
		const truncate = (value: string) =>
			value.length > maxLength ? `${value.slice(0, maxLength)}... [truncated]` : value;

		if (body === null || body === undefined) {
			return 'No response body.';
		}

		if (typeof body === 'string') {
			return truncate(body);
		}

		if (isProblemDetails(body)) {
			return truncate([body.title, body.detail].filter(Boolean).join(': '));
		}

		return truncate(JSON.stringify(body));
	}

	function isProblemDetails(body: unknown): body is { title?: string; detail?: string } {
		return (
			typeof body === 'object' &&
			body !== null &&
			('title' in body || 'detail' in body) &&
			(!('title' in body) || typeof body.title === 'string') &&
			(!('detail' in body) || typeof body.detail === 'string')
		);
	}

	function stepLabel(state: StepState) {
		if (state === 'submitting') {
			return 'Working';
		}

		if (state === 'succeeded') {
			return 'Saved';
		}

		if (state === 'failed') {
			return 'Failed';
		}

		return 'Ready';
	}
</script>

<svelte:head>
	<title>Tenant setup workspace</title>
	<meta
		name="description"
		content="Tenant-provided instrument setup workspace for the Instruments Platform."
	/>
</svelte:head>

<div class="grid gap-6">
	{#if showIntro}
	<section class="grid gap-5 border-b border-[var(--color-border)] pb-6">
		<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem] lg:items-end">
			<div>
				<p class="text-sm font-semibold text-[var(--color-accent)]">Tenant-provided workflow</p>
				<h1 class="serif-heading mt-2 text-3xl text-[var(--color-text)] sm:text-4xl">
					Tenant setup workspace
				</h1>
				<p class="mt-3 max-w-3xl text-base leading-7 text-[var(--color-text-muted)]">
					Configure rights-attested content, bind it to a template version, draft scoring, and
					prepare a campaign without platform-shipped instrument presets.
				</p>
			</div>

			<div
				class="setup-callout"
				role="region"
				aria-label={authState === 'authenticated'
					? 'Authenticated tenant session'
					: 'Setup auth state'}
			>
				{#if authState === 'authenticated' && authSession}
					<p class="setup-callout__key">Tenant session</p>
					<p class="setup-callout__value">Signed in</p>
					<p class="setup-callout__note">Tenant {authSession.tenantId}</p>
					<a class="secondary-button" href={logoutUrl}>
						<LogOut size={14} aria-hidden="true" />
						<span>Sign out</span>
					</a>
				{:else if authState === 'checking'}
					<p class="setup-callout__key">Tenant session</p>
					<p class="setup-callout__value">Checking</p>
					<p class="setup-callout__note">Loading setup access.</p>
				{:else}
					<p class="setup-callout__key">Tenant session</p>
					<p class="setup-callout__value">Required</p>
					<p class="setup-callout__note">Setup controls are hidden until access is confirmed.</p>
				{/if}
			</div>
		</div>

		<div class="provenance-strip" aria-label="Setup provenance">
			<div class="provenance-item">
				<span class="provenance-item__key">Content rights</span>
				<span class="provenance-item__value">Attested by tenant</span>
			</div>
			<div class="provenance-item">
				<span class="provenance-item__key">Identity mode</span>
				<span class="provenance-item__value">Anonymous</span>
			</div>
			<div class="provenance-item">
				<span class="provenance-item__key">Backend contract</span>
				<span class="provenance-item__value provenance-item__value--mono">GF05 setup endpoints</span
				>
			</div>
			<div class="provenance-item">
				<span class="provenance-item__key">Scope</span>
				<span class="provenance-item__value">Tenant-provided workflow</span>
			</div>
		</div>

		{#if authState === 'authenticated'}
			<div class="setup-run" role="region" aria-label="Current setup run">
				<div>
					<p class="setup-run__key">Current setup run</p>
					<p class="setup-run__value">
						sample <span class="setup-run__suffix">{setupRunSuffix || 'generating'}</span>
					</p>
					<p class="setup-run__note">
						Sample values are generated locally so repeated setup runs do not collide.
					</p>
				</div>
				<button type="button" class="secondary-button" onclick={resetSetupRun}>
					Generate new sample values
				</button>
			</div>
		{/if}
	</section>
	{/if}

	{#if authState === 'authenticated'}
	{#if showSetupSections}
	<section id="instrument" class="setup-panel" aria-labelledby="instrument-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Step 1</p>
				<h2 id="instrument-title" class="setup-panel__title">Instrument import</h2>
			</div>
			{@render StepPill({ state: stepStates.instrument })}
		</div>

		<div class="grid gap-4 lg:grid-cols-2">
			<label class="field">
				<span>Code</span>
				<input bind:value={instrumentForm.code} />
			</label>
			<label class="field">
				<span>Version</span>
				<input bind:value={instrumentForm.version} />
			</label>
			<label class="field lg:col-span-2">
				<span>Full name</span>
				<input bind:value={instrumentForm.fullName} />
			</label>
			<label class="field">
				<span>Domain</span>
				<input bind:value={instrumentForm.domain} />
			</label>
			<label class="field">
				<span>License type</span>
				<input bind:value={instrumentForm.licenseType} />
			</label>
			<label class="field">
				<span>Rights status</span>
				<input bind:value={instrumentForm.rightsStatus} />
			</label>
			<label class="field">
				<span>Validity label</span>
				<input bind:value={instrumentForm.validityLabel} />
			</label>
			<label class="field lg:col-span-2">
				<span>Provenance note</span>
				<textarea rows="3" bind:value={instrumentForm.provenanceNote}></textarea>
			</label>
		</div>

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={stepStates.instrument === 'submitting'}
				onclick={createInstrumentImport}
			>
				{#if stepStates.instrument === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<Plus size={17} aria-hidden="true" />
				{/if}
				<span>Create instrument import</span>
			</button>
			{@render ResultLine({ value: instrumentResult?.id })}
		</div>

		{@render ErrorLine({ message: errors.instrument })}

		{#if instruments.length > 0}
			<section class="existing-instruments" aria-label="Existing tenant instruments">
				<div class="existing-instruments__header">
					<div>
						<p class="setup-panel__eyebrow">Existing tenant instruments</p>
						<p class="existing-instruments__count">
							{instruments.length}
							persisted tenant-private import{instruments.length === 1 ? '' : 's'}
						</p>
					</div>
				</div>
				<div class="existing-instruments__list">
					{#each visibleInstruments() as instrument (instrument.id)}
						<div
							class="existing-instruments__row"
							data-latest={instrument.id === instrumentResult?.id}
						>
							<div>
								<p class="text-sm font-semibold">{instrument.fullName}</p>
								<p class="text-xs text-[var(--color-text-muted)]">
									{instrument.code}
									{instrument.version} · {instrument.rightsStatus}
								</p>
							</div>
							{#if instrument.id === instrumentResult?.id}
								<span class="latest-badge">Latest</span>
							{/if}
						</div>
					{/each}
				</div>
			</section>
		{/if}
	</section>

	<section id="template" class="setup-panel" aria-labelledby="template-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Step 2</p>
				<h2 id="template-title" class="setup-panel__title">Template version</h2>
			</div>
			{@render StepPill({ state: stepStates.template })}
		</div>

		<div class="grid gap-4 lg:grid-cols-2">
			<label class="field">
				<span>Template name</span>
				<input bind:value={templateName} />
			</label>
			<label class="field">
				<span>Section title</span>
				<input bind:value={sectionTitle} />
			</label>
		</div>

		<div class="mt-4 grid gap-3">
			{#each questions as question (question.code)}
				<div class="question-row">
					<label class="field">
						<span>{question.code}</span>
						<textarea rows="2" bind:value={question.textDefault}></textarea>
					</label>
					<label class="checkbox-field">
						<input type="checkbox" bind:checked={question.reverseCoded} />
						<span>Reverse coded</span>
					</label>
				</div>
			{/each}
		</div>

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={stepStates.template === 'submitting'}
				onclick={createTemplateVersion}
			>
				{#if stepStates.template === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<Send size={17} aria-hidden="true" />
				{/if}
				<span>Create template version</span>
			</button>
			{@render ResultLine({ value: templateResult?.templateVersionId })}
		</div>

		{@render ErrorLine({ message: errors.template })}
	</section>

	<section id="scoring" class="setup-panel" aria-labelledby="scoring-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Step 3</p>
				<h2 id="scoring-title" class="setup-panel__title">Scoring rule</h2>
			</div>
			{@render StepPill({ state: stepStates.scoring })}
		</div>

		<div class="grid gap-4 lg:grid-cols-2">
			<label class="field">
				<span>Rule key</span>
				<input bind:value={scoringForm.ruleKey} />
			</label>
			<label class="field">
				<span>Rule version</span>
				<input bind:value={scoringForm.ruleVersion} />
			</label>
			<label class="field lg:col-span-2">
				<span>Document</span>
				<textarea rows="8" bind:value={scoringForm.document}></textarea>
			</label>
			<label class="field lg:col-span-2">
				<span>Produces</span>
				<textarea rows="3" bind:value={scoringForm.produces}></textarea>
			</label>
		</div>

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={!templateResult || stepStates.scoring === 'submitting'}
				onclick={createScoringRule}
			>
				{#if stepStates.scoring === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<Send size={17} aria-hidden="true" />
				{/if}
				<span>Create scoring rule</span>
			</button>
			{@render ResultLine({ value: scoringResult?.id })}
		</div>

		{@render ErrorLine({ message: errors.scoring })}
	</section>

	<section id="campaign" class="setup-panel" aria-labelledby="campaign-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Step 4</p>
				<h2 id="campaign-title" class="setup-panel__title">Campaign draft</h2>
			</div>
			{@render StepPill({ state: stepStates.campaign })}
		</div>

		<div class="grid gap-4 lg:grid-cols-2">
			{#if selectedSeries}
				<label class="field">
					<span>Series name</span>
					<input value={selectedSeries.name} disabled />
				</label>
			{:else}
				<label class="field">
					<span>Series name</span>
					<input bind:value={campaignForm.seriesName} />
				</label>
			{/if}
			<label class="field">
				<span>Campaign name</span>
				<input bind:value={campaignForm.name} />
			</label>
			<label class="field">
				<span>Identity mode</span>
				<select bind:value={campaignForm.responseIdentityMode}>
					<option value="anonymous_longitudinal">anonymous_longitudinal</option>
					<option value="anonymous">anonymous</option>
					<option value="identified">identified</option>
				</select>
			</label>
			<label class="field">
				<span>Locale</span>
				<input bind:value={campaignForm.defaultLocale} />
			</label>
		</div>

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={!templateResult || stepStates.campaign === 'submitting'}
				onclick={createCampaignDraft}
			>
				{#if stepStates.campaign === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<Send size={17} aria-hidden="true" />
				{/if}
				<span>Create campaign draft</span>
			</button>
			<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
				{@render ResultLine({ label: 'Series', value: activeCampaignSeries?.id })}
				{@render ResultLine({ label: 'Campaign', value: campaignResult?.id })}
			</div>
		</div>

		{@render ErrorLine({ message: errors.campaign })}
	</section>
	{/if}

	{#if showOperationsSections}
	<section id="launch-readiness" class="setup-panel" aria-labelledby="readiness-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Step 5</p>
				<h2 id="readiness-title" class="setup-panel__title">Launch readiness</h2>
			</div>
			{@render StepPill({ state: stepStates.readiness })}
		</div>

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={!campaignResult || stepStates.readiness === 'submitting'}
				onclick={checkLaunchReadiness}
			>
				{#if stepStates.readiness === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<SearchCheck size={17} aria-hidden="true" />
				{/if}
				<span>Check launch readiness</span>
			</button>
			{#if readinessResult}
				<p class="text-sm font-semibold">
					Ready:
					<span
						class={readinessResult.ready
							? 'text-[var(--color-success)]'
							: 'text-[var(--color-warning)]'}
					>
						{readinessResult.ready ? 'yes' : 'no'}
					</span>
				</p>
			{/if}
		</div>

		<div class="action-row">
			<button
				type="button"
				class="secondary-button"
				disabled={!campaignResult ||
					!readinessResult?.ready ||
					stepStates.readiness === 'submitting'}
				onclick={launchCampaign}
			>
				{#if stepStates.readiness === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<Send size={17} aria-hidden="true" />
				{/if}
				<span>Launch campaign</span>
			</button>
			<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
				{@render ResultLine({ label: 'Status', value: launchResult?.status })}
				{@render ResultLine({ label: 'Launch snapshot', value: launchResult?.launchSnapshotId })}
				{@render ResultLine({ label: 'Retention policy', value: launchResult?.retentionPolicyId })}
				{@render ResultLine({ label: 'Disclosure policy', value: launchResult?.disclosurePolicyId })}
			</div>
		</div>

		<div class="action-row">
			<button
				type="button"
				class="secondary-button"
				disabled={!launchResult || stepStates.readiness === 'submitting'}
				onclick={createOpenLink}
			>
				{#if stepStates.readiness === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<Link size={17} aria-hidden="true" />
				{/if}
				<span>Create open link</span>
			</button>
			<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
				{@render ResultLine({ label: 'Respondent path', value: openLinkResult?.respondentPath })}
				{@render ResultLine({ label: 'Open assignment', value: openLinkResult?.assignmentId })}
			</div>
		</div>

		<div class="action-row">
			<button
				type="button"
				class="secondary-button"
				disabled={!launchResult || stepStates.readiness === 'submitting'}
				onclick={queueEmailInvitations}
			>
				{#if stepStates.readiness === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<Send size={17} aria-hidden="true" />
				{/if}
				<span>Queue email invitations</span>
			</button>
			{#if invitationBatchResult}
				<p class="result-line">
					<span>Queued</span>
					<code>{invitationBatchResult.createdInvitationCount}</code>
				</p>
			{/if}
			<button
				type="button"
				class="secondary-button"
				disabled={!invitationBatchResult || stepStates.readiness === 'submitting'}
				onclick={processLocalDelivery}
			>
				{#if stepStates.readiness === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<MailCheck size={17} aria-hidden="true" />
				{/if}
				<span>Process local delivery</span>
			</button>
			{#if deliveryResult}
				<p class="result-line">
					<span>Sent</span>
					<code>{deliveryResult.sentCount}</code>
				</p>
			{/if}
		</div>

		{#if invitationBatchResult}
			<section
				class="grid gap-2 rounded-md border border-[var(--color-border)] p-3"
				aria-label="Queued invitation intents"
			>
				{#each invitationBatchResult.invitations as invitation (invitation.notificationId)}
					<div class="grid gap-2 text-sm md:grid-cols-[minmax(0,1fr)_auto] md:items-center">
						<div class="min-w-0">
							<p class="font-semibold text-[var(--color-text)]">{invitation.recipient}</p>
							<code class="block break-all text-xs text-[var(--color-text-muted)]"
								>{invitation.respondentPath}</code
							>
						</div>
						<span class="step-pill" data-state="succeeded">{invitation.status}</span>
					</div>
				{/each}
			</section>
		{/if}

		{#if deliveryResult}
			<section
				class="grid gap-2 rounded-md border border-[var(--color-border)] p-3"
				aria-label="Local delivery proof"
			>
				{#each deliveryResult.deliveries as delivery (delivery.notificationId)}
					<div class="grid gap-2 text-sm md:grid-cols-[minmax(0,1fr)_auto] md:items-center">
						<div class="min-w-0">
							<p class="font-semibold text-[var(--color-text)]">{delivery.recipient}</p>
							<code class="block break-all text-xs text-[var(--color-text-muted)]"
								>{delivery.respondentPath ?? delivery.providerMessageId}</code
							>
							<p class="text-xs text-[var(--color-text-muted)]">{delivery.provider}</p>
							{#if delivery.error}
								<p class="text-xs text-[var(--color-danger)]">{delivery.error}</p>
							{/if}
						</div>
						<span
							class="step-pill"
							data-state={delivery.status === 'sent' ? 'succeeded' : 'failed'}
							>{delivery.status}</span
						>
					</div>
				{/each}
			</section>
		{/if}

		{@render ErrorLine({ message: errors.readiness })}

		{#if readinessResult?.issues.length}
			<ul class="grid gap-2" aria-label="Launch readiness issues">
				{#each readinessResult.issues as issue (issue.code)}
					<li class="issue-row" data-severity={issue.severity}>
						<AlertTriangle size={17} aria-hidden="true" />
						<div>
							<p class="text-sm font-semibold">{issue.code}</p>
							<p class="text-sm text-[var(--color-text-muted)]">{issue.message}</p>
						</div>
					</li>
				{/each}
			</ul>
		{/if}
	</section>
	{/if}

	{#if showWavesSections}
	<section id="two-wave-proof" class="setup-panel" aria-label="Two-wave proof">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Longitudinal proof</p>
				<h2 class="setup-panel__title">Two-wave proof</h2>
			</div>
			{@render StepPill({ state: stepStates.twoWave })}
		</div>

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={!templateResult || !scoringResult || stepStates.twoWave === 'submitting'}
				onclick={createTwoWaveProof}
			>
				Create two-wave proof
			</button>
			<button
				type="button"
				class="secondary-button"
				disabled={!activeTwoWaveSeries || stepStates.twoWave === 'submitting'}
				onclick={refreshTwoWaveProof}
			>
				Refresh two-wave proof
			</button>
			<button
				type="button"
				class="secondary-button"
				disabled={!activeTwoWaveSeries || stepStates.twoWave === 'submitting'}
				onclick={viewWaveComparisonProof}
			>
				View wave comparison proof
			</button>
		</div>

		{@render ErrorLine({ message: errors.twoWave })}

		{#if activeTwoWaveSeries}
			<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
				{@render ResultLine({ label: 'Series', value: activeTwoWaveSeries?.id })}
			</div>
		{/if}

		{#if twoWaveProof.waves.length > 0}
			<div class="grid gap-3 lg:grid-cols-2">
				{#each twoWaveProof.waves as wave (wave.draft.id)}
					<article class="score-card" aria-label={wave.draft.name}>
						<div>
							<p class="score-card__label">{wave.draft.name}</p>
							<p class="score-card__value">{wave.launch.status}</p>
						</div>
						<p class="score-card__meta">{wave.openLink.respondentPath}</p>
						<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
							{@render ResultLine({ label: 'Campaign', value: wave.draft.id })}
							{@render ResultLine({ label: 'Open assignment', value: wave.openLink.assignmentId })}
						</div>
					</article>
				{/each}
			</div>
		{/if}

		{#if twoWaveProof.proof}
			<div class="response-lab__meta">
				<span>{twoWaveProof.proof.proofStatus}</span>
				<span>launched waves {twoWaveProof.proof.launchedWaveCount}</span>
				<span>submitted waves {twoWaveProof.proof.submittedWaveCount}</span>
				<span>linked trajectories {twoWaveProof.proof.linkedTrajectoryCount}</span>
				<span>complete trajectories {twoWaveProof.proof.completeTrajectoryCount}</span>
			</div>
		{/if}

		{#if waveComparisonProofResult}
			<section class="score-result-panel report-proof-panel" aria-label="Wave comparison proof">
				<div class="score-result-panel__header">
					<div>
						<p class="setup-panel__eyebrow">Wave comparison proof</p>
						<h3 class="score-result-panel__title">Aggregate wave deltas</h3>
					</div>
					<span class="status-badge" data-status="blocked">Proof only</span>
				</div>

				<div class="response-lab__meta">
					<span>{waveComparisonProofResult.proofStatus}</span>
					<span>Not validated interpretation</span>
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
								<p class="score-card__value">{score.compatibilityStatus}</p>
							</div>
							<p class="score-card__meta">{score.disclosure}</p>
							<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
								<p>baseline mean {formatNullableScoreValue(score.baselineMean)}</p>
								<p>comparison mean {formatNullableScoreValue(score.comparisonMean)}</p>
								<p>aggregate delta {formatNullableScoreValue(score.aggregateDelta)}</p>
								<p>paired delta {formatNullableScoreValue(score.pairedDeltaMean)}</p>
								<p>linked pairs {score.linkedPairCount}</p>
								{#if baselineScoreMetadata}
									<p>baseline {baselineScoreMetadata}</p>
								{/if}
								{#if comparisonScoreMetadata}
									<p>comparison {comparisonScoreMetadata}</p>
								{/if}
								{#if score.baselineInterpretation}
									<p>baseline band {score.baselineInterpretation.label}</p>
								{/if}
								{#if score.comparisonInterpretation}
									<p>comparison band {score.comparisonInterpretation.label}</p>
								{/if}
							</div>
							{#if score.baselineInterpretation || score.comparisonInterpretation}
								<p class="score-card__interpretation">
									{scoreInterpretationMeta(
										score.baselineInterpretation ?? score.comparisonInterpretation
									)}
								</p>
							{/if}
							{#if score.suppressionReason}
								<p class="score-card__interpretation">
									{score.suppressionReason.replaceAll('_', ' ')}
								</p>
							{/if}
							{#if score.compatibilityReason}
								<p class="score-card__interpretation">{score.compatibilityReason}</p>
							{/if}
						</article>
					{/each}
				</div>
			</section>
		{/if}
	</section>
	{/if}

	{#if showReportsSections}
	<section id="response-lab" class="setup-panel" aria-labelledby="response-lab-title">
		<div class="setup-panel__header">
			<div>
				<p class="setup-panel__eyebrow">Step 6</p>
				<h2 id="response-lab-title" class="setup-panel__title">Response lab</h2>
			</div>
			{@render StepPill({ state: stepStates.response })}
		</div>

		<div class="action-row">
			<button
				type="button"
				class="primary-button"
				disabled={!campaignResult || stepStates.response === 'submitting'}
				onclick={loadResponseLab}
			>
				{#if stepStates.response === 'submitting'}
					<LoaderCircle size={17} aria-hidden="true" />
				{:else}
					<SearchCheck size={17} aria-hidden="true" />
				{/if}
				<span>Load response lab</span>
			</button>
			{@render ResultLine({ label: 'Campaign', value: respondentCampaign?.campaignId })}
		</div>

		{#if surface === 'reports' && campaignResult && !respondentCampaign}
			<div class="response-lab__actions">
				<button
					type="button"
					class="secondary-button"
					disabled={stepStates.response === 'submitting'}
					onclick={viewReportProof}
				>
					View report proof
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!reportProofResult || stepStates.response === 'submitting'}
					onclick={createReportProofExport}
				>
					Create export proof
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!reportExportResult || stepStates.response === 'submitting'}
					onclick={fetchStoredReportProofExport}
				>
					Fetch stored artifact
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!reportExportResult || stepStates.response === 'submitting'}
					onclick={downloadReportProofExport}
				>
					<Download size={14} aria-hidden="true" />
					Download CSV
				</button>
			</div>

			<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
				{@render ResultLine({ label: 'Campaign', value: campaignResult.id })}
				{#if reportProofResult}
					<section class="score-result-panel report-proof-panel" aria-label="Report proof">
						<div class="score-result-panel__header">
							<div>
								<p class="setup-panel__eyebrow">Report/export proof</p>
								<h3 class="score-result-panel__title">Aggregate score projection</h3>
							</div>
							<span class="status-badge" data-status="blocked">Proof only</span>
						</div>

						<div class="response-lab__meta">
							<span>Not validated interpretation</span>
							<span>{reportProofResult.launchSnapshot.responseIdentityMode}</span>
							<span>Disclosure k={reportProofResult.disclosurePolicy.kMin}</span>
						</div>

						<div class="score-card-list" aria-label="Report proof scores">
							{#each reportProofResult.scores as score (score.dimensionCode)}
								{@const scoreMetadata =
									score.disclosure === 'visible'
										? formatScoreOutputMetadata(
												score.nValidTotal,
												score.nExpectedTotal,
												score.missingPolicyStatusSummary
											)
										: null}
								<article class="score-card" aria-label={`Report score ${score.dimensionCode}`}>
									<div>
										<p class="score-card__label">{score.dimensionCode}</p>
										<p
											class={score.disclosure === 'visible'
												? 'score-card__value'
												: 'score-card__interpretation'}
										>
											{reportScoreDisplay(score)}
										</p>
									</div>
									<p class="score-card__meta">{score.disclosure}</p>
									{#if score.disclosure === 'visible' && score.scoreCount !== null}
										<p class="score-card__interpretation">scores={score.scoreCount}</p>
									{/if}
									{#if scoreMetadata}
										<p class="score-card__interpretation">{scoreMetadata}</p>
									{/if}
									{#if score.interpretation}
										<p class="score-card__interpretation">{score.interpretation.label}</p>
										<p class="score-card__interpretation">
											{scoreInterpretationMeta(score.interpretation)}
										</p>
									{/if}
								</article>
							{/each}
						</div>

						<div class="score-result-panel__footer">
							{@render ResultLine({
								label: 'Launch snapshot',
								value: reportProofResult.launchSnapshot.id
							})}
							{@render ResultLine({
								label: 'Scoring rule',
								value: reportProofResult.launchSnapshot.scoringRuleId
							})}
							{@render ResultLine({
								label: 'Disclosure policy',
								value: reportProofResult.disclosurePolicy.id
							})}
						</div>
					</section>
				{/if}
				{#if reportExportResult}
					<section class="score-result-panel report-proof-panel" aria-label="Export artifact proof">
						<div class="score-result-panel__header">
							<div>
								<p class="setup-panel__eyebrow">Export artifact proof</p>
								<h3 class="score-result-panel__title">CSV and codebook artifact</h3>
							</div>
							<span class="status-badge" data-status="blocked">Proof only</span>
						</div>

						<div class="response-lab__meta">
							<span>{reportExportResult.artifactType}</span>
							<span>{reportExportResult.status}</span>
							<span>rows {reportExportResult.rowCount}</span>
							{#if storedReportExportResult}
								<span>Stored artifact fetched</span>
							{/if}
							{#if reportExportDownloadResult}
								<span>Downloaded CSV</span>
								<span>{reportExportDownloadResult.contentType}</span>
							{/if}
						</div>

						<div class="score-result-panel__footer">
							{@render ResultLine({ label: 'Artifact', value: reportExportResult.id })}
							{@render ResultLine({ label: 'File', value: reportExportResult.fileName })}
							{@render ResultLine({
								label: 'Stored artifact',
								value: storedReportExportResult?.id
							})}
							{@render ResultLine({
								label: 'Downloaded file',
								value: reportExportDownloadResult?.fileName
							})}
							{@render ResultLine({
								label: 'Checksum',
								value: reportExportResult.checksumSha256
							})}
							{@render ResultLine({
								label: 'Disclosure policy',
								value: reportProofResult?.disclosurePolicy.id
							})}
						</div>

						<pre class="csv-preview">{csvPreview(reportExportResult.csvContent)}</pre>
					</section>
				{/if}
			</div>
		{/if}

		{#if respondentCampaign}
			<div class="response-lab__meta">
				<span>{respondentCampaign.responseIdentityMode}</span>
				<span
					>{respondentCampaign.questions.length} question{respondentCampaign.questions.length === 1
						? ''
						: 's'}</span
				>
				<span>{respondentCampaign.defaultLocale}</span>
			</div>

			<div class="grid gap-3">
				{#each respondentCampaign.questions as question (question.id)}
					<div class="question-row">
						<p class="response-lab__question">{question.textDefault}</p>
						<label class="field">
							<span>{question.code} answer</span>
							<input
								aria-label={`${question.code} answer`}
								inputmode="numeric"
								value={responseAnswers[question.id] ?? ''}
								oninput={(event) => setResponseAnswer(question.id, event.currentTarget.value)}
							/>
						</label>
					</div>
				{/each}
			</div>

			<div class="response-lab__actions">
				<button
					type="button"
					class="secondary-button"
					disabled={stepStates.response === 'submitting'}
					onclick={createLabAssignment}
				>
					Create lab assignment
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!labAssignmentResult || stepStates.response === 'submitting'}
					onclick={startResponseSession}
				>
					Start response session
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!responseSessionResult || stepStates.response === 'submitting'}
					onclick={saveResponseAnswers}
				>
					Save response answers
				</button>
				<button
					type="button"
					class="primary-button"
					disabled={!savedAnswersResult || stepStates.response === 'submitting'}
					onclick={submitResponse}
				>
					Submit response
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!submittedResponseResult || stepStates.response === 'submitting'}
					onclick={computeResponseScores}
				>
					Compute score
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={(!scoreResult && !(surface === 'reports' && campaignResult)) ||
						stepStates.response === 'submitting'}
					onclick={viewReportProof}
				>
					View report proof
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!reportProofResult || stepStates.response === 'submitting'}
					onclick={createReportProofExport}
				>
					Create export proof
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!reportExportResult || stepStates.response === 'submitting'}
					onclick={fetchStoredReportProofExport}
				>
					Fetch stored artifact
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!reportExportResult || stepStates.response === 'submitting'}
					onclick={downloadReportProofExport}
				>
					<Download size={14} aria-hidden="true" />
					Download CSV
				</button>
			</div>

			<div class="grid gap-1 text-sm text-[var(--color-text-muted)]">
				{@render ResultLine({ label: 'Assignment', value: labAssignmentResult?.assignmentId })}
				{@render ResultLine({ label: 'Session', value: responseSessionResult?.id })}
				{#if savedAnswersResult}
					<p class="result-line">
						<span>Saved answers</span>
						<code>{savedAnswersResult.savedAnswerCount}</code>
					</p>
				{/if}
				{#if submittedResponseResult}
					<p class="result-line">
						<span>Submitted</span>
						<code>{submittedResponseResult.submittedAt}</code>
					</p>
				{/if}
				{#if scoreResult}
					<section class="score-result-panel" aria-label="Setup score result">
						<div class="score-result-panel__header">
							<div>
								<p class="setup-panel__eyebrow">Setup lab result</p>
								<h3 class="score-result-panel__title">Computed score</h3>
							</div>
							<span class="status-badge" data-status="blocked">Not a production report</span>
						</div>

						<div class="score-card-list" aria-label="Computed scores">
							{#each scoreResult.scores as score (score.dimensionCode)}
								<article class="score-card" aria-label={`Score ${score.dimensionCode}`}>
									<div>
										<p class="score-card__label">{score.dimensionCode}</p>
										<p class="score-card__value">{formatScoreValue(score.value)}</p>
									</div>
									<p class="score-card__meta">
										n={score.nValid}/{score.nExpected}; {score.missingPolicyStatus}
									</p>
									<p class="score-card__interpretation">Interpretation pending</p>
								</article>
							{/each}
						</div>

						<div class="score-result-panel__footer">
							{@render ResultLine({ label: 'Score run', value: scoreResult.scoreRunId })}
						</div>
					</section>
				{/if}
				{#if reportProofResult}
					<section class="score-result-panel report-proof-panel" aria-label="Report proof">
						<div class="score-result-panel__header">
							<div>
								<p class="setup-panel__eyebrow">Report/export proof</p>
								<h3 class="score-result-panel__title">Aggregate score projection</h3>
							</div>
							<span class="status-badge" data-status="blocked">Proof only</span>
						</div>

						<div class="response-lab__meta">
							<span>Not validated interpretation</span>
							<span>{reportProofResult.launchSnapshot.responseIdentityMode}</span>
							<span>Disclosure k={reportProofResult.disclosurePolicy.kMin}</span>
						</div>

						<div class="score-card-list" aria-label="Report proof scores">
							{#each reportProofResult.scores as score (score.dimensionCode)}
								{@const scoreMetadata =
									score.disclosure === 'visible'
										? formatScoreOutputMetadata(
												score.nValidTotal,
												score.nExpectedTotal,
												score.missingPolicyStatusSummary
											)
										: null}
								<article class="score-card" aria-label={`Report score ${score.dimensionCode}`}>
									<div>
										<p class="score-card__label">{score.dimensionCode}</p>
										<p
											class={score.disclosure === 'visible'
												? 'score-card__value'
												: 'score-card__interpretation'}
										>
											{reportScoreDisplay(score)}
										</p>
									</div>
									<p class="score-card__meta">{score.disclosure}</p>
									{#if score.disclosure === 'visible' && score.scoreCount !== null}
										<p class="score-card__interpretation">scores={score.scoreCount}</p>
									{/if}
									{#if scoreMetadata}
										<p class="score-card__interpretation">{scoreMetadata}</p>
									{/if}
									{#if score.interpretation}
										<p class="score-card__interpretation">{score.interpretation.label}</p>
										<p class="score-card__interpretation">
											{scoreInterpretationMeta(score.interpretation)}
										</p>
									{/if}
								</article>
							{/each}
						</div>

						<div class="score-result-panel__footer">
							{@render ResultLine({
								label: 'Launch snapshot',
								value: reportProofResult.launchSnapshot.id
							})}
							{@render ResultLine({
								label: 'Scoring rule',
								value: reportProofResult.launchSnapshot.scoringRuleId
							})}
							{@render ResultLine({
								label: 'Disclosure policy',
								value: reportProofResult.disclosurePolicy.id
							})}
						</div>
					</section>
				{/if}
				{#if reportExportResult}
					<section class="score-result-panel report-proof-panel" aria-label="Export artifact proof">
						<div class="score-result-panel__header">
							<div>
								<p class="setup-panel__eyebrow">Export artifact proof</p>
								<h3 class="score-result-panel__title">CSV and codebook artifact</h3>
							</div>
							<span class="status-badge" data-status="blocked">Proof only</span>
						</div>

						<div class="response-lab__meta">
							<span>{reportExportResult.artifactType}</span>
							<span>{reportExportResult.status}</span>
							<span>rows {reportExportResult.rowCount}</span>
							{#if storedReportExportResult}
								<span>Stored artifact fetched</span>
							{/if}
							{#if reportExportDownloadResult}
								<span>Downloaded CSV</span>
								<span>{reportExportDownloadResult.contentType}</span>
							{/if}
						</div>

						<div class="score-result-panel__footer">
							{@render ResultLine({ label: 'Artifact', value: reportExportResult.id })}
							{@render ResultLine({ label: 'File', value: reportExportResult.fileName })}
							{@render ResultLine({
								label: 'Stored artifact',
								value: storedReportExportResult?.id
							})}
							{@render ResultLine({
								label: 'Downloaded file',
								value: reportExportDownloadResult?.fileName
							})}
							{@render ResultLine({
								label: 'Checksum',
								value: reportExportResult.checksumSha256
							})}
							{@render ResultLine({
								label: 'Disclosure policy',
								value: reportProofResult?.disclosurePolicy.id
							})}
						</div>

						<pre class="csv-preview">{csvPreview(reportExportResult.csvContent)}</pre>
					</section>
				{/if}
			</div>
		{/if}

		{@render ErrorLine({ message: errors.response })}
	</section>
	{/if}
	{:else}
		{@render AuthBoundary()}
	{/if}
</div>

{#snippet AuthBoundary()}
	<section class="setup-panel" aria-labelledby="auth-boundary-title">
		{#if authState === 'checking'}
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Tenant session</p>
					<h2 id="auth-boundary-title" class="setup-panel__title">Checking setup access</h2>
				</div>
				<span class="step-pill" data-state="submitting">
					<LoaderCircle size={14} aria-hidden="true" />
					Working
				</span>
			</div>
			<p class="text-sm leading-6 text-[var(--color-text-muted)]">
				Loading the current tenant session before setup controls are shown.
			</p>
		{:else if authState === 'unauthenticated'}
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Tenant session</p>
					<h2 id="auth-boundary-title" class="setup-panel__title">Sign in required</h2>
				</div>
				<span class="step-pill" data-state="failed">
					<AlertTriangle size={14} aria-hidden="true" />
					Required
				</span>
			</div>
			<p class="text-sm leading-6 text-[var(--color-text-muted)]">{authMessage}</p>
			<div class="action-row">
				<a class="primary-button" href={loginUrl}>
					<LogIn size={17} aria-hidden="true" />
					<span>Sign in</span>
				</a>
				<button type="button" class="secondary-button" onclick={loadCurrentSession}>
					<RefreshCw size={14} aria-hidden="true" />
					Retry session check
				</button>
			</div>
		{:else if authState === 'forbidden'}
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Tenant session</p>
					<h2 id="auth-boundary-title" class="setup-panel__title">Tenant access unavailable</h2>
				</div>
				<span class="step-pill" data-state="failed">
					<AlertTriangle size={14} aria-hidden="true" />
					Denied
				</span>
			</div>
			<p class="text-sm leading-6 text-[var(--color-text-muted)]">{authMessage}</p>
			<button type="button" class="secondary-button" onclick={loadCurrentSession}>
				<RefreshCw size={14} aria-hidden="true" />
				Retry session check
			</button>
		{:else}
			<div class="setup-panel__header">
				<div>
					<p class="setup-panel__eyebrow">Tenant session</p>
					<h2 id="auth-boundary-title" class="setup-panel__title">Session check failed</h2>
				</div>
				<span class="step-pill" data-state="failed">
					<AlertTriangle size={14} aria-hidden="true" />
					Failed
				</span>
			</div>
			{@render ErrorLine({ message: authMessage })}
			<button type="button" class="secondary-button" onclick={loadCurrentSession}>
				<RefreshCw size={14} aria-hidden="true" />
				Retry session check
			</button>
		{/if}
	</section>
{/snippet}

{#snippet StepPill({ state }: { state: StepState })}
	<span class="step-pill" data-state={state}>
		{#if state === 'succeeded'}
			<Check size={14} aria-hidden="true" />
		{/if}
		{stepLabel(state)}
	</span>
{/snippet}

{#snippet ResultLine({ value, label = 'ID' }: { value?: string | null; label?: string })}
	{#if value}
		<p class="result-line">
			<span>{label}</span>
			<code>{value}</code>
		</p>
	{/if}
{/snippet}

{#snippet ErrorLine({ message }: { message: string | null })}
	{#if message}
		<p class="error-line">{message}</p>
	{/if}
{/snippet}
