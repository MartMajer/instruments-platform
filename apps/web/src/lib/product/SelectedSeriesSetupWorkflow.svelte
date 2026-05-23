<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { ArrowDown, ArrowUp, Copy, LoaderCircle, Plus, RefreshCw, SearchCheck, Send, Trash2 } from 'lucide-svelte';
	import type {
		CampaignSeriesSetupWorkspaceResponse,
		RespondentRulePreviewResponse,
		SubjectDirectoryItemResponse,
		SubjectGroupResponse
	} from '$lib/api/product';
	import type {
		CampaignAssignmentListResponse,
		CampaignAssignmentResponse,
		CampaignDraftResponse,
		CampaignRespondentRuleListResponse,
		CampaignRespondentRuleResponse,
		CreateCampaignRequest,
		CreateCampaignTestRecipientsResponse,
		CreatePrivateInstrumentImportRequest,
		CreateScoringRuleRequest,
		CreateTemplateVersionRequest,
		InstrumentSummaryResponse,
		LaunchReadinessResponse,
		SetupIdResponse,
		TemplateVersionDetailResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { createProductApiFromEnv, createSetupApiFromEnv } from './route-state';
	import {
		defaultCampaignWaveName,
		selectSetupCampaignId,
		selectSetupTemplateVersionId,
		toSelectedSeriesSetupLaunchPlan,
		toSelectedSeriesSetupLaunchState,
		toSelectedSeriesSetupPath,
		toSelectedSeriesSetupPathStepDisplay,
		toSelectedSeriesSetupWaveContext,
		type SelectedSeriesSetupPathStep,
		type SelectedSeriesSetupWorkflowActionId
	} from './setup-workflow';
	import {
		appendRecipientImportEntry,
		keepValidRecipientImportRows,
		maxRecipientImportRecipients,
		readRecipientImportFile,
		reviewRecipientImport
	} from './recipient-import';
	import {
	appendScoreOutputRow,
	applyQuestionScalePreset,
	appendTemplateQuestionRow,
	buildScoreProduces,
	buildScoringDocument,
	createScoreOutputRowsForQuestionnairePalette,
	createDefaultTemplateQuestionRows,
	createTemplateQuestionRowsForQuestionnairePalette,
	describeQuestionResultUsage,
	describeQuestionScaleIntent,
	describeQuestionScoringDirection,
	describeScoreMissingDataStrategy,
	duplicateTemplateQuestionRow,
	isMeanScoreEligible,
	listQuestionnairePaletteOptions,
	moveTemplateQuestionRow,
	questionScalePresetOptions,
	removeScoreOutputRow,
	removeTemplateQuestionRow,
	summarizeAuthoringReadiness,
	summarizeCollectedContextQuestions,
	summarizeQuestionDimensions,
	summarizeQuestionAuthoringCards,
	summarizeQuestionnaireBlueprintReview,
	summarizeReverseScoringReview,
	summarizeResultsBlueprintReview,
	summarizeScorePlan,
	syncScoreOutputQuestionCodes,
	toDraftRespondentPreviewContract,
		toCreateQuestionScales,
		toCreateTemplateQuestions,
		validateScoreOutputRows,
		type DraftRespondentPreviewQuestion,
		type QuestionnairePaletteId,
		type QuestionRankingMode,
		type QuestionScalePreset,
		type ScoreCalculation,
		type ScoreMissingStrategy,
		type ScoreOutputAuthoringRow,
		validateTemplateQuestionRows,
		type TemplateQuestionAnswerType,
		type TemplateQuestionAuthoringRow
	} from './template-authoring';
	import { toCampaignSeriesSetupWorkspaceView, toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';
	type PreviewRuleKind =
		| 'self'
		| 'all_in_group'
		| 'manager_of_target'
		| 'reports_of_target'
		| 'external_emails';
	type PreviewRuleRow = RespondentRulePreviewResponse['rows'][number];

	let {
		workspace,
		canManageSetup = true,
		onWorkspaceRefresh
	}: {
		workspace: CampaignSeriesSetupWorkspaceResponse;
		canManageSetup?: boolean;
		onWorkspaceRefresh?: () => Promise<boolean>;
	} = $props();

	const productApi = createProductApiFromEnv(env);
	const setupApi = createSetupApiFromEnv(env);
	const appLocale = $derived(appLocaleFromPageData(page.data));
	const setupWorkflowCopy = $derived(routePageCopy(appLocale).selectedStudy.setupWorkflow);
	const setupBodyCopy = $derived(routePageCopy(appLocale).selectedStudy.setupBody);
	const initialSetupRunSuffix = generateSetupRunSuffix();
	const initialScoringRuleKey = 'custom.total_score';
	const initialTemplateQuestionRows = createDefaultTemplateQuestionRows();
	const initialScoreOutputs = createScoreOutputRowsForQuestionnairePalette('blank', initialTemplateQuestionRows);
	const questionnairePaletteOptionBase = listQuestionnairePaletteOptions();
	const questionnairePaletteOptions = $derived(
	questionnairePaletteOptionBase.map((option) => ({
		...option,
		...(setupBodyCopy.questionnaire.paletteOptions[
			option.id as keyof typeof setupBodyCopy.questionnaire.paletteOptions
		] ?? {})
	}))
);

	let instrumentResult = $state<InstrumentSummaryResponse | null>(null);
	let templateResult = $state<TemplateVersionDetailResponse | null>(null);
	let scoringResult = $state<SetupIdResponse | null>(null);
	let campaignResult = $state<CampaignDraftResponse | null>(null);
	let readinessResult = $state<LaunchReadinessResponse | null>(null);
	let selectedQuestionnairePalette = $state<QuestionnairePaletteId>('blank');
	let refreshWarning = $state<string | null>(null);
	let actionStates = $state<Record<SelectedSeriesSetupWorkflowActionId, StepState>>({
		instrument: 'idle',
		template: 'idle',
		scoring: 'idle',
		campaign: 'idle',
		readiness: 'idle'
	});
	let actionErrors = $state<Record<SelectedSeriesSetupWorkflowActionId, string | null>>({
		instrument: null,
		template: null,
		scoring: null,
		campaign: null,
		readiness: null
	});
	let instrumentForm = $state<CreatePrivateInstrumentImportRequest>({
		code: `custom-study-${initialSetupRunSuffix}`,
		version: '1.0.0',
		fullName: `Custom study ${initialSetupRunSuffix}`,
		domain: 'psychometric',
		provenanceNote: 'Tenant provided item text and attested rights for internal use.',
		rightsStatus: 'attested_by_tenant',
		validityLabel: 'tenant_provided',
		licenseType: 'unknown'
	});
	let templateName = $state('Study questionnaire');
	let questionnaireLocale = $state('en');
	let sectionTitle = $state('Questions');
	let templateQuestionRows = $state<TemplateQuestionAuthoringRow[]>(initialTemplateQuestionRows);
	let scoreOutputs = $state<ScoreOutputAuthoringRow[]>(initialScoreOutputs);

	function applyQuestionnairePalette(paletteId: QuestionnairePaletteId) {
		const nextRows = createTemplateQuestionRowsForQuestionnairePalette(paletteId);
		const nextOutputs = createScoreOutputRowsForQuestionnairePalette(paletteId, nextRows);

		selectedQuestionnairePalette = paletteId;
		templateQuestionRows = nextRows;
		scoreOutputs = nextOutputs;
		scoringForm.document = buildScoringDocument(scoringForm.ruleKey, nextRows, nextOutputs);
		scoringForm.produces = buildScoreProduces(nextOutputs);
		templateResult = null;
		scoringResult = null;
		campaignResult = null;
		readinessResult = null;
	}
	let scoringDocumentManuallyEdited = $state(false);
	let scoringForm = $state({
		ruleKey: initialScoringRuleKey,
		ruleVersion: '1.0.0',
		schemaVersion: 'scoring-rule/v1',
		engineMinVersion: 'engine/v1',
		document: buildScoringDocument(
			initialScoringRuleKey,
			initialTemplateQuestionRows,
			initialScoreOutputs
		),
		produces: buildScoreProduces(initialScoreOutputs),
		compatibility: '{}'
	});
	let campaignForm = $state({
		name: '',
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en'
	});
	let campaignNameInitialized = $state(false);
	let previewRuleKind = $state<PreviewRuleKind>('all_in_group');
	let previewTargetSubjectId = $state('');
	let previewGroupId = $state('');
	let previewExternalEmailText = $state('');
	let previewExternalEmailFileError = $state<string | null>(null);
	let previewManualRecipientName = $state('');
	let previewManualRecipientEmail = $state('');
	let previewManualRecipientError = $state<string | null>(null);
	let previewMaxRows = $state(25);
	let previewSubjects = $state<SubjectDirectoryItemResponse[]>([]);
	let previewGroups = $state<SubjectGroupResponse[]>([]);
	let previewOptionsLoadAttempted = $state(false);
	let previewOptionsLoading = $state(false);
	let previewOptionsError = $state<string | null>(null);
	let previewState = $state<StepState>('idle');
	let previewError = $state<string | null>(null);
	let previewResult = $state<RespondentRulePreviewResponse | null>(null);
	let respondentRuleLoadCampaignId = $state<string | null>(null);
	let savedRuleState = $state<StepState>('idle');
	let savedRuleError = $state<string | null>(null);
	let savedRuleResult = $state<CampaignRespondentRuleListResponse | null>(null);
	let assignmentState = $state<StepState>('idle');
	let assignmentError = $state<string | null>(null);
	let assignmentResult = $state<CampaignAssignmentListResponse | null>(null);
	let testRecipientCount = $state(50);
	let testRecipientGroupName = $state('Demo respondents');
	let testRecipientState = $state<StepState>('idle');
	let testRecipientError = $state<string | null>(null);
	let testRecipientResult = $state<CreateCampaignTestRecipientsResponse | null>(null);

	const workspaceView = $derived(toCampaignSeriesSetupWorkspaceView(workspace, appLocale));
	const localState = $derived({
		instrumentId: instrumentResult?.id ?? null,
		templateVersionId: templateResult?.templateVersionId ?? null,
		scoringRuleId: scoringResult?.id ?? null,
		campaignId: campaignResult?.id ?? null
	});
	const setupPath = $derived(toSelectedSeriesSetupPath(workspace, localState, setupWorkflowCopy));
	const workflowActions = $derived(setupPath.steps);
	const currentActionId = $derived(setupPath.currentActionId);
	let activeActionId = $state<SelectedSeriesSetupWorkflowActionId>('instrument');
	let activeActionInitialized = $state(false);
	const activeActionIdForView = $derived(activeActionInitialized ? activeActionId : currentActionId);
	const activeStep = $derived(
		setupPath.steps.find((step) => step.id === activeActionIdForView) ??
			setupPath.steps.find((step) => step.id === currentActionId) ??
			setupPath.steps[0]
	);
	const activeActionIndex = $derived(
		workflowActions.findIndex((action) => action.id === activeActionIdForView)
	);
	const previousAction = $derived(
		activeActionIndex > 0 ? workflowActions[activeActionIndex - 1] : null
	);
	const nextAction = $derived(
		activeActionIndex >= 0 && activeActionIndex < workflowActions.length - 1
			? workflowActions[activeActionIndex + 1]
			: null
	);
	const canGoPrevious = $derived(Boolean(previousAction && canSelectSetupAction(previousAction.id)));
	const canGoNext = $derived(Boolean(nextAction && canSelectSetupAction(nextAction.id)));
	const selectedTemplateVersionId = $derived(selectSetupTemplateVersionId(workspace, localState));
	const selectedCampaignId = $derived(selectSetupCampaignId(workspace, localState));
	const selectedCampaign = $derived(
		campaignResult?.id === selectedCampaignId
			? campaignResult
			: workspace.selectedCampaign?.id === selectedCampaignId
				? workspace.selectedCampaign
				: null
	);
	const selectedCampaignLabel = $derived(
		campaignResult?.id === selectedCampaignId
			? campaignResult.name
			: workspace.selectedCampaign?.id === selectedCampaignId
				? workspace.selectedCampaign.name
				: selectedCampaignId
					? 'Collection wave selected'
					: 'No editable collection wave selected'
	);
	const lockedSelectedCampaign = $derived(
		workspace.selectedCampaign && workspace.selectedCampaign.id !== selectedCampaignId
			? workspace.selectedCampaign
			: null
	);
	const questionnaireQuestionCount = $derived(
		templateResult?.questions.length ?? workspace.template?.questionCount ?? templateQuestionRows.length
	);
	const templateQuestionErrors = $derived(validateTemplateQuestionRows(templateQuestionRows));
	const questionDimensionSummaries = $derived(summarizeQuestionDimensions(templateQuestionRows));
	const questionAuthoringSummaries = $derived(
		summarizeQuestionAuthoringCards(templateQuestionRows, scoreOutputs)
	);
	const questionnaireBlueprintReview = $derived(
		summarizeQuestionnaireBlueprintReview(templateQuestionRows, scoreOutputs)
	);
	const scoreableQuestionRows = $derived(templateQuestionRows.filter(isMeanScoreEligible));
	const collectedContextSummaries = $derived(summarizeCollectedContextQuestions(templateQuestionRows));
	const scoreOutputErrors = $derived(validateScoreOutputRows(scoreOutputs, templateQuestionRows));
	const scorePlanSummaries = $derived(summarizeScorePlan(scoreOutputs, templateQuestionRows));
	const resultsBlueprintReview = $derived(
		summarizeResultsBlueprintReview(templateQuestionRows, scoreOutputs)
	);
	const reverseScoringReview = $derived(summarizeReverseScoringReview(templateQuestionRows, scoreOutputs));
	const respondentPreviewContract = $derived(
		toDraftRespondentPreviewContract(templateQuestionRows, scoreOutputs)
	);
	const authoringReadiness = $derived(summarizeAuthoringReadiness(templateQuestionRows, scoreOutputs));
	const selectedScoreQuestionRows = $derived(
		scoreableQuestionRows.filter((row) =>
			scoreOutputs.some((output) => output.includedQuestionCodes.includes(row.code))
		)
	);
	const launchPlan = $derived(
		toSelectedSeriesSetupLaunchPlan(workspace, localState, {
			responseIdentityMode: selectedCampaign?.responseIdentityMode ?? campaignForm.responseIdentityMode,
			savedRecipientSelectionCount: savedRuleResult?.rules.length ?? 0,
			savedRecipientPairCount: assignmentResult?.assignmentCount ?? 0,
			readinessPassed: readinessResult?.ready ?? workspace.readiness.ready,
			waveName: selectedCampaignId ? selectedCampaignLabel : campaignForm.name
		}, setupWorkflowCopy)
	);
	const waveContext = $derived(toSelectedSeriesSetupWaveContext(workspace, localState, setupWorkflowCopy));
	const previewRequiresTarget = $derived(
		previewRuleKind === 'manager_of_target' || previewRuleKind === 'reports_of_target'
	);
	const previewRequiresGroup = $derived(previewRuleKind === 'all_in_group');
	const previewUsesExternalEmails = $derived(previewRuleKind === 'external_emails');
	const previewExternalEmailReview = $derived(reviewRecipientImport(previewExternalEmailText));
	const canRunPreview = $derived(
		canManageSetup &&
			!!selectedCampaignId &&
			previewState !== 'submitting' &&
			!previewOptionsLoading &&
			(!previewRequiresTarget || !!previewTargetSubjectId) &&
			(!previewRequiresGroup || !!previewGroupId) &&
			(!previewUsesExternalEmails ||
				(previewExternalEmailReview.validRecipientCount > 0 &&
					!previewExternalEmailReview.hasBlockingIssues))
	);
	const canSaveCurrentRule = $derived(
		canManageSetup &&
			!!selectedCampaignId &&
			savedRuleState !== 'submitting' &&
			(!previewRequiresTarget || !!previewTargetSubjectId) &&
			(!previewRequiresGroup || !!previewGroupId) &&
			(!previewUsesExternalEmails ||
				(previewExternalEmailReview.validRecipientCount > 0 &&
					!previewExternalEmailReview.hasBlockingIssues))
	);

	$effect(() => {
		if (!activeActionInitialized && currentActionId) {
			activeActionId = currentActionId;
			activeActionInitialized = true;
		} else if (!canSelectSetupAction(activeActionId)) {
			activeActionId = currentActionId;
		}
	});

	$effect(() => {
		if (!campaignNameInitialized) {
			campaignForm.name = defaultCampaignWaveName(workspace, setupWorkflowCopy);
			campaignNameInitialized = true;
		}
	});

	$effect(() => {
		if (canManageSetup && !previewOptionsLoadAttempted) {
			void loadPreviewOptions();
		}
	});

	$effect(() => {
		const campaignId = selectedCampaignId;
		if (!canManageSetup || !campaignId) {
			respondentRuleLoadCampaignId = null;
			savedRuleResult = null;
			assignmentResult = null;
			savedRuleState = 'idle';
			assignmentState = 'idle';
			savedRuleError = null;
			assignmentError = null;
			return;
		}

		if (respondentRuleLoadCampaignId !== campaignId) {
			respondentRuleLoadCampaignId = campaignId;
			void loadRespondentRuleState(campaignId);
		}
	});

	async function createInstrumentImport() {
		const result = await runAction('instrument', () =>
			setupApi.createPrivateInstrumentImport(instrumentForm)
		);

		if (result) {
			instrumentResult = result;
			activeActionId = 'template';
		}
	}

	async function createTemplateVersion() {
		if (templateQuestionErrors.length > 0) {
			actionErrors = {
				...actionErrors,
				template: templateQuestionErrors[0]
			};
			return;
		}

		syncGeneratedScoringIfPristine(templateQuestionRows);
		const result = await runAction('template', () =>
			setupApi.createTemplateVersion(buildTemplateRequest())
		);

		if (result) {
			templateResult = result;
			scoringResult = null;
			campaignResult = null;
			readinessResult = null;
			activeActionId = 'scoring';
		}
	}

	async function createScoringRule() {
		const templateVersionId = selectedTemplateVersionId;
		if (!templateVersionId) {
			actionErrors = {
				...actionErrors,
				scoring: 'Save the questionnaire first.'
			};
			return;
		}

		if (scoreOutputErrors.length > 0) {
			actionErrors = {
				...actionErrors,
				scoring: scoreOutputErrors[0]
			};
			return;
		}

		syncGeneratedScoringIfPristine(templateQuestionRows);
		const scoringDocument = scoringDocumentManuallyEdited
			? scoringForm.document
			: buildDefaultScoringDocument(scoringForm.ruleKey, templateQuestionRows);
		const request: CreateScoringRuleRequest = {
			templateVersionId,
			ruleKey: scoringForm.ruleKey,
			ruleVersion: scoringForm.ruleVersion,
			schemaVersion: scoringForm.schemaVersion,
			engineMinVersion: scoringForm.engineMinVersion,
			document: scoringDocument,
			produces: scoringForm.produces,
			compatibility: scoringForm.compatibility
		};
		const result = await runAction('scoring', () => setupApi.createScoringRule(request));

		if (result) {
			scoringResult = result;
			activeActionId = 'campaign';
		}
	}

	async function createCampaignDraft() {
		const templateVersionId = selectedTemplateVersionId;
		if (!templateVersionId) {
			actionErrors = {
				...actionErrors,
				campaign: 'Save the questionnaire first.'
			};
			return;
		}

		const request: CreateCampaignRequest = {
			templateVersionId,
			name: campaignForm.name,
			responseIdentityMode: campaignForm.responseIdentityMode,
			campaignSeriesId: workspace.series.id,
			schedule: '{}',
			defaultLocale: campaignForm.defaultLocale
		};
		const result = await runAction('campaign', () => setupApi.createCampaign(request));

		if (result) {
			campaignResult = result;
			readinessResult = null;
			respondentRuleLoadCampaignId = null;
			activeActionId = 'readiness';
		}
	}

	async function checkLaunchReadiness() {
		const campaignId = selectedCampaignId;
		if (!campaignId) {
			actionErrors = {
				...actionErrors,
				readiness: 'Create the collection wave first.'
			};
			return;
		}

		const result = await runAction('readiness', () => setupApi.getLaunchReadiness(campaignId));

		if (result) {
			readinessResult = result;
		}
	}

	async function loadPreviewOptions() {
		if (!canManageSetup) {
			return;
		}

		previewOptionsLoadAttempted = true;
		previewOptionsLoading = true;
		previewOptionsError = null;

		try {
			const [directory, groupList] = await Promise.all([
				productApi.listSubjects(),
				productApi.listSubjectGroups()
			]);
			previewSubjects = directory.subjects;
			previewGroups = groupList.groups;
			syncPreviewSelections();
		} catch (error) {
			previewOptionsError = toProductApiErrorMessage(error, 'Recipient preview options failed to load.');
		} finally {
			previewOptionsLoading = false;
		}
	}

	async function previewRespondentRule() {
		const campaignId = selectedCampaignId;
		if (!canManageSetup) {
			return;
		}

		if (!campaignId) {
			previewError = 'Create the collection wave first.';
			return;
		}

		if (previewRequiresGroup && !previewGroupId) {
			previewError = 'Select a subject group.';
			return;
		}

		if (previewRequiresTarget && !previewTargetSubjectId) {
			previewError = 'Select a target subject.';
			return;
		}

		if (
			previewUsesExternalEmails &&
			(previewExternalEmailReview.validRecipientCount === 0 ||
				previewExternalEmailReview.hasBlockingIssues)
		) {
			previewError = 'Add at least one valid email and remove invalid or duplicate rows.';
			return;
		}

		previewState = 'submitting';
		previewError = null;
		previewResult = null;

		try {
			previewResult = await productApi.previewRespondentRule(workspace.series.id, campaignId, {
				rule: buildCurrentRespondentRuleJson(),
				targetSubjectId: previewRequiresTarget ? previewTargetSubjectId : null,
				groupId: previewRequiresGroup ? previewGroupId : null,
				maxRows: normalizePreviewMaxRows(previewMaxRows)
			});
			previewState = 'succeeded';
		} catch (error) {
			previewState = 'failed';
			previewError = toProductApiErrorMessage(error, 'Recipient preview failed.');
		}
	}

	async function loadRespondentRuleState(campaignId: string) {
		await Promise.all([loadSavedRespondentRules(campaignId), loadCampaignAssignments(campaignId)]);
	}

	async function loadSavedRespondentRules(campaignId: string) {
		savedRuleState = 'submitting';
		savedRuleError = null;

		try {
			const result = await setupApi.listCampaignRespondentRules(campaignId);
			if (campaignId === selectedCampaignId) {
				savedRuleResult = result;
				savedRuleState = 'succeeded';
			}
		} catch (error) {
			if (campaignId === selectedCampaignId) {
				savedRuleState = 'failed';
				savedRuleError = toProductApiErrorMessage(error, 'Saved recipient selections failed to load.');
			}
		}
	}

	async function loadCampaignAssignments(campaignId: string) {
		assignmentState = 'submitting';
		assignmentError = null;

		try {
			const result = await setupApi.listCampaignAssignments(campaignId);
			if (campaignId === selectedCampaignId) {
				assignmentResult = result;
				assignmentState = 'succeeded';
			}
		} catch (error) {
			if (campaignId === selectedCampaignId) {
				assignmentState = 'failed';
				assignmentError = toProductApiErrorMessage(error, 'Campaign assignments failed to load.');
			}
		}
	}

	async function saveCurrentRespondentRule() {
		const campaignId = selectedCampaignId;
		if (!campaignId) {
			savedRuleError = 'Create the collection wave first.';
			return;
		}

		if (previewRequiresGroup && !previewGroupId) {
			savedRuleError = 'Select a subject group.';
			return;
		}

		if (previewRequiresTarget && !previewTargetSubjectId) {
			savedRuleError = 'Select a target subject.';
			return;
		}

		if (
			previewUsesExternalEmails &&
			(previewExternalEmailReview.validRecipientCount === 0 ||
				previewExternalEmailReview.hasBlockingIssues)
		) {
			savedRuleError = 'Add at least one valid email and remove invalid or duplicate rows.';
			return;
		}

		savedRuleState = 'submitting';
		savedRuleError = null;

		try {
			savedRuleResult = await setupApi.updateCampaignRespondentRules(campaignId, {
				rules: [{ rule: buildCurrentRespondentRuleJson() }]
			});
			savedRuleState = 'succeeded';
			await loadCampaignAssignments(campaignId);
		} catch (error) {
			savedRuleState = 'failed';
			savedRuleError = toProductApiErrorMessage(error, 'Respondent rule save failed.');
		}
	}

	async function createTestRecipients() {
		const campaignId = selectedCampaignId;
		if (!campaignId) {
			testRecipientError = 'Create the collection wave first.';
			return;
		}

		testRecipientState = 'submitting';
		testRecipientError = null;

		try {
			const result = await setupApi.createCampaignTestRecipients(campaignId, {
				count: clampNumber(testRecipientCount, 1, 1000),
				groupName: testRecipientGroupName.trim() || 'Demo respondents',
				emailDomain: 'test.validatedscale.local',
				locale: campaignForm.defaultLocale || 'en'
			});
			testRecipientResult = result;
			previewRuleKind = 'all_in_group';
			previewGroupId = result.groupId;
			previewResult = null;
			previewGroups = [
				...previewGroups.filter((group) => group.id !== result.groupId),
				{
					id: result.groupId,
					type: 'cohort',
					name: result.groupName,
					parentGroupId: null,
					attributes: '{"simulated_test_data":true}',
					memberCount: result.createdSubjectCount
				}
			];
			savedRuleResult = await setupApi.listCampaignRespondentRules(campaignId);
			savedRuleState = 'succeeded';
			assignmentResult = null;
			testRecipientState = 'succeeded';
			const refreshed = await onWorkspaceRefresh?.();
			if (refreshed === false) {
				refreshWarning = 'Test recipients were saved, but this setup view could not refresh.';
			}
		} catch (error) {
			testRecipientState = 'failed';
			testRecipientError = toProductApiErrorMessage(error, 'Test recipients could not be created.');
		}
	}

	async function loadPreviewExternalEmailFile(file: File | null | undefined) {
		previewExternalEmailFileError = null;
		if (!file) {
			return;
		}

		try {
			previewExternalEmailText = await readRecipientImportFile(file);
		} catch (error) {
			previewExternalEmailFileError =
				error instanceof Error ? error.message : 'Recipient file could not be read.';
		}
	}

	function addPreviewManualRecipient() {
		previewManualRecipientError = null;
		const candidateReview = reviewRecipientImport(
			appendRecipientImportEntry('', {
				displayName: previewManualRecipientName,
				email: previewManualRecipientEmail
			})
		);
		const recipient = candidateReview.recipients[0];

		if (!recipient || candidateReview.hasBlockingIssues) {
			previewManualRecipientError = 'Enter one valid email address.';
			return;
		}

		if (previewExternalEmailReview.recipients.some((item) => item.email === recipient.email)) {
			previewManualRecipientError = 'This recipient is already in the wave list.';
			return;
		}

		previewExternalEmailText = appendRecipientImportEntry(previewExternalEmailText, {
			displayName: previewManualRecipientName,
			email: recipient.email
		});
		previewManualRecipientName = '';
		previewManualRecipientEmail = '';
	}

	function keepOnlyValidPreviewRecipients() {
		previewExternalEmailText = keepValidRecipientImportRows(previewExternalEmailText);
		previewExternalEmailFileError = null;
		previewManualRecipientError = null;
	}

	function clearPreviewRecipients() {
		previewExternalEmailText = '';
		previewExternalEmailFileError = null;
		previewManualRecipientError = null;
	}

	async function runAction<T>(
		actionId: SelectedSeriesSetupWorkflowActionId,
		action: () => Promise<T>
	) {
		actionStates = { ...actionStates, [actionId]: 'submitting' };
		actionErrors = { ...actionErrors, [actionId]: null };
		refreshWarning = null;

		try {
			const result = await action();
			actionStates = { ...actionStates, [actionId]: 'succeeded' };
			const refreshed = await onWorkspaceRefresh?.();
			if (refreshed === false) {
				refreshWarning = 'Setup action saved, but the setup workspace refresh failed.';
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, 'Setup action failed.')
			};
			return null;
		}
	}

	function buildTemplateRequest(): CreateTemplateVersionRequest {
		return {
			templateName,
			semver: '1.0.0',
			defaultLocale: questionnaireLocale,
			instrumentId: instrumentResult?.id ?? workspace.template?.instrumentId ?? null,
			sections: questionDimensionSummaries.map((dimension, index) => ({
				ordinal: index + 1,
				code: dimension.code,
				titleDefault: dimension.label || sectionTitle
			})),
			scales: toCreateQuestionScales(templateQuestionRows),
			questions: toCreateTemplateQuestions(templateQuestionRows)
		};
	}

	function workflowAction(id: SelectedSeriesSetupWorkflowActionId) {
		return workflowActions.find((action) => action.id === id) ?? workflowActions[0];
	}

	function canSelectSetupAction(id: SelectedSeriesSetupWorkflowActionId) {
		const step = setupPath.steps.find((candidate) => candidate.id === id);
		if (!step) {
			return false;
		}

		return step.available || step.pathState === 'done' || step.id === currentActionId;
	}

	function selectSetupAction(id: SelectedSeriesSetupWorkflowActionId) {
		if (canSelectSetupAction(id)) {
			activeActionId = id;
		}
	}

	function goToPreviousSetupAction() {
		if (previousAction && canSelectSetupAction(previousAction.id)) {
			activeActionId = previousAction.id;
		}
	}

	function goToNextSetupAction() {
		if (nextAction && canSelectSetupAction(nextAction.id)) {
			activeActionId = nextAction.id;
		}
	}

	function openLaunchSurface() {
		const campaignId = selectedCampaignId;
		const target = campaignId
			? `/app/campaign-series/${workspace.series.id}/operations?campaignId=${campaignId}`
			: `/app/campaign-series/${workspace.series.id}/operations`;
		window.location.href = target;
	}

	function syncPreviewSelections() {
		if (!previewSubjects.some((subject) => subject.id === previewTargetSubjectId)) {
			previewTargetSubjectId = previewSubjects[0]?.id ?? '';
		}

		if (!previewGroups.some((group) => group.id === previewGroupId)) {
			previewGroupId = previewGroups[0]?.id ?? '';
		}

		if (previewGroups.length === 0 && previewRuleKind === 'all_in_group') {
			previewRuleKind = 'external_emails';
		}
	}

	function isActionDisabled(id: SelectedSeriesSetupWorkflowActionId) {
		const action = workflowAction(id);
		return (
			!action.available ||
			actionStates[id] === 'submitting' ||
			(id === 'template' && templateQuestionErrors.length > 0) ||
			(id === 'scoring' && scoreOutputErrors.length > 0)
		);
	}

	function actionDisabledReason(id: SelectedSeriesSetupWorkflowActionId) {
		if (id === 'template' && templateQuestionErrors.length > 0) {
			return templateQuestionErrors[0];
		}

		if (id === 'scoring' && scoreOutputErrors.length > 0) {
			return scoreOutputErrors[0];
		}

		return workflowAction(id).disabledReason ?? undefined;
	}

	function updateTemplateQuestionRow(
		rowIndex: number,
		updates: Partial<Omit<TemplateQuestionAuthoringRow, 'ordinal'>>
	) {
		const nextRows = templateQuestionRows.map((row, index) =>
			index === rowIndex ? { ...row, ...updates } : row
		);
		templateQuestionRows = nextRows;
		syncIncludedScoreQuestions(nextRows);
		syncGeneratedScoringIfPristine(nextRows);
	}

	function updateTemplateQuestionType(rowIndex: number, type: TemplateQuestionAnswerType) {
		const current = templateQuestionRows[rowIndex];
		if (!current) {
			return;
		}

		const isScaleBacked = type === 'likert' || type === 'nps';
		updateTemplateQuestionRow(rowIndex, {
			type,
			reverseCoded: isScaleBacked ? current.reverseCoded : false,
			scaleMin: type === 'nps' ? 0 : current.scaleMin,
			scaleMax: type === 'nps' ? 10 : current.scaleMax,
			scaleLowLabel: type === 'nps' ? 'Not at all likely' : current.scaleLowLabel,
			scaleHighLabel: type === 'nps' ? 'Extremely likely' : current.scaleHighLabel,
			scalePreset: type === 'nps' ? 'custom' : current.scalePreset
		});
	}

	function updateTemplateQuestionScalePreset(rowIndex: number, preset: QuestionScalePreset) {
		const current = templateQuestionRows[rowIndex];
		if (!current) {
			return;
		}

		updateTemplateQuestionRow(rowIndex, applyQuestionScalePreset(current, preset));
	}

	function updateChoiceOptions(rowIndex: number, value: string) {
		updateTemplateQuestionRow(rowIndex, {
			choiceOptions: value
				.split('\n')
				.map((option) => option.trim())
				.filter(Boolean)
		});
	}

	function updateScaleNumber(
		rowIndex: number,
		field: 'scaleMin' | 'scaleMax',
		value: string,
		fallback: number
	) {
		const parsed = Number.parseInt(value, 10);
		updateTemplateQuestionRow(rowIndex, {
			[field]: Number.isFinite(parsed) ? parsed : fallback,
			scalePreset: 'custom'
		});
	}

	function updateMetadataNumber(
		rowIndex: number,
		field: 'numberMin' | 'numberMax' | 'textMaxLength' | 'rankingTopN',
		value: string
	) {
		const trimmed = value.trim();
		const parsed = Number.parseFloat(trimmed);
		updateTemplateQuestionRow(rowIndex, {
			[field]: trimmed && Number.isFinite(parsed) ? parsed : null
		});
	}

	function isScaleQuestion(question: TemplateQuestionAuthoringRow) {
		return question.type === 'likert' || question.type === 'nps';
	}

	function isChoiceQuestion(question: TemplateQuestionAuthoringRow) {
		return question.type === 'single' || question.type === 'multi';
	}

	function questionTypeLabel(type: TemplateQuestionAnswerType) {
		const labels: Record<TemplateQuestionAnswerType, string> = {
			likert: 'Rating scale',
			nps: '0-10 recommendation scale',
			single: 'Single choice',
			multi: 'Multiple choice',
			number: 'Number',
			text: 'Text',
			date: 'Date',
			ranking: 'Ranking'
		};
		return labels[type] ?? 'Question';
	}

	function questionPreviewDetail(question: TemplateQuestionAuthoringRow) {
		if (isScaleQuestion(question)) {
			return `${question.scaleMin} to ${question.scaleMax}: ${question.scaleLowLabel} -> ${question.scaleHighLabel}`;
		}

		if (question.type === 'single') {
			return `Choose one: ${question.choiceOptions.join(', ')}`;
		}

		if (question.type === 'multi') {
			return `Choose any: ${question.choiceOptions.join(', ')}`;
		}

		if (question.type === 'number') {
			return 'Number response';
		}

		if (question.type === 'date') {
			return 'Date response';
		}

		if (question.type === 'ranking') {
			return `Rank options: ${question.choiceOptions.join(', ')}`;
		}

		return 'Text response';
	}

	function questionScoringDetail(question: TemplateQuestionAuthoringRow) {
		return describeQuestionScoringDirection(question);
	}

	function questionScaleIntent(question: TemplateQuestionAuthoringRow) {
		return describeQuestionScaleIntent(question);
	}

	function questionResultUsage(question: TemplateQuestionAuthoringRow) {
		return describeQuestionResultUsage(question, scoreOutputs);
	}

	function runtimeRatingValues(question: DraftRespondentPreviewQuestion) {
		if (question.scaleMin === null || question.scaleMax === null || question.scaleMin > question.scaleMax) {
			return [];
		}

		return Array.from(
			{ length: question.scaleMax - question.scaleMin + 1 },
			(_, index) => (question.scaleMin ?? 0) + index
		);
	}

	function questionAuthoringSummary(code: string) {
		return questionAuthoringSummaries.find((summary) => summary.code === code);
	}

	function dimensionCoverageLabel(labels: string[]) {
		return labels.length ? labels.join(', ') : 'No questionnaire dimensions selected';
	}

	function reverseScoredCountLabel(count: number) {
		if (count === 0) {
			return 'No reverse-scored questions';
		}

		return `${count} reverse-scored ${count === 1 ? 'question' : 'questions'}`;
	}

	function scoreOutputMissingDataDetail(localId: string) {
		const output = scoreOutputs.find((candidate) => candidate.localId === localId);
		return output ? describeScoreMissingDataStrategy(output).detail : 'Missing-data rule not configured.';
	}

	function addTemplateQuestionRow() {
		const nextRows = appendTemplateQuestionRow(templateQuestionRows);
		templateQuestionRows = nextRows;
		syncIncludedScoreQuestions(nextRows);
		syncGeneratedScoringIfPristine(nextRows);
	}

	function deleteTemplateQuestionRow(code: string) {
		const nextRows = removeTemplateQuestionRow(templateQuestionRows, code);
		templateQuestionRows = nextRows;
		syncIncludedScoreQuestions(nextRows);
		syncGeneratedScoringIfPristine(nextRows);
	}

	function copyTemplateQuestionRow(code: string) {
		const nextRows = duplicateTemplateQuestionRow(templateQuestionRows, code);
		templateQuestionRows = nextRows;
		syncIncludedScoreQuestions(nextRows);
		syncGeneratedScoringIfPristine(nextRows);
	}

	function reorderTemplateQuestionRow(code: string, direction: 'up' | 'down') {
		const nextRows = moveTemplateQuestionRow(templateQuestionRows, code, direction);
		templateQuestionRows = nextRows;
		syncIncludedScoreQuestions(nextRows);
		syncGeneratedScoringIfPristine(nextRows);
	}

	function syncIncludedScoreQuestions(rows: TemplateQuestionAuthoringRow[]) {
		scoreOutputs = syncScoreOutputQuestionCodes(scoreOutputs, rows);
	}

	function addScoreOutput() {
		scoreOutputs = appendScoreOutputRow(scoreOutputs, templateQuestionRows);
		syncGeneratedScoringIfPristine(templateQuestionRows);
	}

	function deleteScoreOutput(localId: string) {
		scoreOutputs = removeScoreOutputRow(scoreOutputs, localId);
		syncGeneratedScoringIfPristine(templateQuestionRows);
	}

	function updateScoreOutput(localId: string, patch: Partial<ScoreOutputAuthoringRow>) {
		scoreOutputs = scoreOutputs.map((output) =>
			output.localId === localId ? { ...output, ...patch } : output
		);
		const firstOutput = scoreOutputs[0];
		const ruleKey = firstOutput ? `custom.${scoreCodeFromName(firstOutput.code || firstOutput.name)}` : 'custom.total';
		scoringForm = {
			...scoringForm,
			ruleKey,
			produces: buildDefaultProduces(scoreOutputs)
		};
		syncGeneratedScoringIfPristine(templateQuestionRows, ruleKey);
	}

	function toggleScoreQuestion(outputLocalId: string, code: string, checked: boolean) {
		scoreOutputs = scoreOutputs.map((output) =>
			output.localId === outputLocalId
				? {
						...output,
						includedQuestionCodes: checked
							? [...output.includedQuestionCodes.filter((candidate) => candidate !== code), code]
							: output.includedQuestionCodes.filter((candidate) => candidate !== code)
					}
				: output
		);
		syncGeneratedScoringIfPristine(templateQuestionRows);
	}

	function parseScoreMinValidCount(value: string) {
		const parsed = Number.parseInt(value, 10);
		return Number.isFinite(parsed) ? Math.max(1, parsed) : 1;
	}

	function updateScoringRuleKey(ruleKey: string) {
		scoringForm = {
			...scoringForm,
			ruleKey
		};
		syncGeneratedScoringIfPristine(templateQuestionRows, ruleKey);
	}

	function updateScoringDocument(document: string) {
		scoringDocumentManuallyEdited = true;
		scoringForm = {
			...scoringForm,
			document
		};
	}

	function regenerateScoringDocument() {
		scoringDocumentManuallyEdited = false;
		scoringForm = {
			...scoringForm,
			document: buildDefaultScoringDocument(scoringForm.ruleKey, templateQuestionRows),
			produces: buildDefaultProduces(scoreOutputs)
		};
	}

	function syncGeneratedScoringIfPristine(
		rows: TemplateQuestionAuthoringRow[],
		ruleKey = scoringForm.ruleKey
	) {
		if (scoringDocumentManuallyEdited) {
			return;
		}

		scoringForm = {
			...scoringForm,
			ruleKey,
			document: buildDefaultScoringDocument(ruleKey, rows),
			produces: buildDefaultProduces(scoreOutputs)
		};
	}

	function stepLabel(state: StepState): string {
		if (state === 'submitting') {
			return setupBodyCopy.status.working;
		}

		if (state === 'succeeded') {
			return setupBodyCopy.status.saved;
		}

		if (state === 'failed') {
			return setupBodyCopy.status.failed;
		}

		return setupBodyCopy.status.ready;
	}

	function setupPathStepDisplay(step: SelectedSeriesSetupPathStep) {
		return toSelectedSeriesSetupPathStepDisplay(
			step,
			currentActionId,
			activeActionIdForView,
			setupWorkflowCopy
		);
	}

	function activeSetupStepEyebrow(): string {
		return activeStep.id === currentActionId
			? setupBodyCopy.currentSetupStep
			: setupBodyCopy.selectedSetupStep;
	}

	function activeSetupStepLabel() {
		return setupPathStepDisplay(activeStep).label;
	}

	function defaultPreviewRole(kind: PreviewRuleKind) {
		if (kind === 'all_in_group') {
			return 'group_member';
		}

		if (kind === 'manager_of_target') {
			return 'manager';
		}

		if (kind === 'reports_of_target') {
			return 'direct_report';
		}

		if (kind === 'external_emails') {
			return 'email_recipient';
		}

		return 'self';
	}

	function buildCurrentRespondentRuleJson() {
		const rule: {
			kind: PreviewRuleKind;
			role: string;
			target_subject_id?: string;
			group_id?: string;
			emails?: string[];
		} = {
			kind: previewRuleKind,
			role: defaultPreviewRole(previewRuleKind)
		};

		if (previewRequiresTarget) {
			rule.target_subject_id = previewTargetSubjectId;
		}

		if (previewRequiresGroup) {
			rule.group_id = previewGroupId;
		}

		if (previewUsesExternalEmails) {
			rule.emails = previewExternalEmailReview.recipients.map((recipient) => recipient.email);
		}

		return JSON.stringify(rule);
	}

	function normalizePreviewMaxRows(value: number) {
		const normalized = Number.isFinite(value) ? Math.trunc(value) : 25;
		return Math.min(200, Math.max(1, normalized));
	}

	function previewSubjectLabel(subject: SubjectDirectoryItemResponse | null | undefined) {
		if (!subject) {
			return 'No subject selected';
		}

		return subject.displayName || subject.email || subject.externalId || subject.id;
	}

	function savedAudienceSummary() {
		return currentLaunchState().recipientSummary;
	}

	function savedRecipientSelectionCount() {
		return savedRuleResult?.rules.length ?? 0;
	}

	function savedRecipientPairCount() {
		return (savedRuleResult?.rules ?? []).reduce((sum, rule) => sum + rule.assignmentPairCount, 0);
	}

	function currentLaunchState() {
		return toSelectedSeriesSetupLaunchState(workspace, localState, {
			readinessPassed: readinessResult?.ready ?? workspace.readiness.ready,
			savedRecipientSelectionCount: savedRecipientSelectionCount(),
			savedRecipientPairCount: savedRecipientPairCount(),
			savedRecipientLoading: savedRuleState === 'submitting',
			responseIdentityMode: selectedCampaign?.responseIdentityMode ?? campaignForm.responseIdentityMode
		}, setupWorkflowCopy);
	}

	function deliveryRosterSummary() {
		if (assignmentState === 'submitting') {
			return 'Loading invitation roster...';
		}

		if (!assignmentResult || assignmentResult.assignmentCount === 0) {
			return 'No invitations prepared yet.';
		}

		return assignmentCountLabel(assignmentResult.assignmentCount);
	}

	function assignmentPairLabel(assignment: CampaignAssignmentResponse) {
		if (assignment.role === 'email_recipient') {
			return assignment.respondent?.label ?? 'Email recipient';
		}

		return `${assignment.target?.label ?? 'Study recipients'} to ${
			assignment.respondent?.label ?? 'No respondent'
		}`;
	}

	function previewPairLabel(row: PreviewRuleRow) {
		if (row.role === 'email_recipient') {
			return row.respondent?.label ?? 'Email recipient';
		}

		return `${row.target?.label ?? 'Study recipients'} to ${row.respondent?.label ?? 'No respondent'}`;
	}

	function savedRecipientSelectionLabel(rule: CampaignRespondentRuleResponse) {
		return audienceRuleLabel(normalizePreviewRuleKind(rule.ruleKind));
	}

	function savedRecipientSelectionDetail(rule: CampaignRespondentRuleResponse) {
		if (normalizePreviewRuleKind(rule.ruleKind) === 'external_emails') {
			return externalEmailRuleDetail(rule.rule);
		}

		const selector = rule.groupId
			? previewGroupLabelById(rule.groupId)
			: rule.targetSubjectId
				? previewSubjectLabelById(rule.targetSubjectId)
				: 'Study recipients';

		return selector;
	}

	function externalEmailRuleDetail(rule: string) {
		try {
			const parsed = JSON.parse(rule) as { emails?: string[] };
			const count = Array.isArray(parsed.emails) ? parsed.emails.length : 0;
			return `${count} ${count === 1 ? 'email recipient' : 'email recipients'}`;
		} catch {
			return 'Email recipient list';
		}
	}

	function previewSubjectLabelById(subjectId: string | null | undefined) {
		if (!subjectId) {
			return 'Study recipients';
		}

		return previewSubjectLabel(previewSubjects.find((subject) => subject.id === subjectId));
	}

	function previewGroupLabelById(groupId: string | null | undefined) {
		if (!groupId) {
			return 'Study recipients';
		}

		return previewGroups.find((group) => group.id === groupId)?.name ?? 'Selected group';
	}

	function normalizePreviewRuleKind(value: string): PreviewRuleKind {
		if (
			value === 'self' ||
			value === 'all_in_group' ||
			value === 'manager_of_target' ||
			value === 'reports_of_target' ||
			value === 'external_emails'
		) {
			return value;
		}

		return 'self';
	}

	function pairCountLabel(count: number) {
		return `${count} ${count === 1 ? 'invitation pair' : 'invitation pairs'}`;
	}

	function ruleCountLabel(count: number) {
		return `${count} ${count === 1 ? 'selection' : 'selections'}`;
	}

	function assignmentCountLabel(count: number) {
		return `${count} ${count === 1 ? 'invitation' : 'invitations'}`;
	}

	function formatCount(count: number): string {
		return new Intl.NumberFormat(appLocale).format(count);
	}

	function clampNumber(value: number, min: number, max: number) {
		if (!Number.isFinite(value)) {
			return min;
		}

		return Math.min(Math.max(Math.round(value), min), max);
	}

	function generateSetupRunSuffix() {
		if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
			return crypto.randomUUID().slice(0, 8).toLowerCase();
		}

		return Math.random().toString(36).slice(2, 10);
	}

	function buildDefaultScoringDocument(
		ruleId: string,
		rows: TemplateQuestionAuthoringRow[] = templateQuestionRows
	) {
		return buildScoringDocument(ruleId, rows, scoreOutputs);
	}

	function buildDefaultProduces(outputs: ScoreOutputAuthoringRow[] = scoreOutputs) {
		return buildScoreProduces(outputs);
	}

	function scoreCodeFromName(value: string) {
		const normalized = value
			.trim()
			.toLowerCase()
			.replace(/[^a-z0-9]+/g, '_')
			.replace(/^_+|_+$/g, '');
		return normalized || 'total';
	}

	function scoreCalculationLabel(value: ScoreCalculation) {
		return value === 'sum' ? 'sum' : 'average';
	}

	function missingPolicyLabel(output: ScoreOutputAuthoringRow) {
		if (output.missingStrategy === 'min_valid_count') {
			return `A score is allowed when at least ${output.minValidCount} selected ${
				output.minValidCount === 1 ? 'question is' : 'questions are'
			} answered.`;
		}

		return 'Every selected question must be answered.';
	}

	function responseModeLabel(mode: string): string {
		if (mode === 'anonymous_longitudinal') {
			return setupBodyCopy.wave.responseMode.anonymousLongitudinalLabel;
		}

		if (mode === 'identified') {
			return setupBodyCopy.wave.responseMode.identifiedLabel;
		}

		return setupBodyCopy.wave.responseMode.anonymousLabel;
	}

	function responseModeHelp(mode: string): string {
		if (mode === 'anonymous_longitudinal') {
			return setupBodyCopy.wave.responseMode.anonymousLongitudinalHelp;
		}

		if (mode === 'identified') {
			return setupBodyCopy.wave.responseMode.identifiedHelp;
		}

		return setupBodyCopy.wave.responseMode.anonymousHelp;
	}

	function audienceRuleLabel(rule: PreviewRuleKind): string {
		if (rule === 'manager_of_target' || rule === 'reports_of_target') {
			return setupBodyCopy.recipients.audienceRules.managerLabel;
		}

		if (rule === 'external_emails') {
			return setupBodyCopy.recipients.audienceRules.externalEmailsLabel;
		}

		return setupBodyCopy.recipients.audienceRules.selfLabel;
	}

	function audienceRuleHelp(rule: PreviewRuleKind): string {
		if (rule === 'manager_of_target' || rule === 'reports_of_target') {
			return setupBodyCopy.recipients.audienceRules.managerHelp;
		}

		if (rule === 'external_emails') {
			return setupBodyCopy.recipients.audienceRules.externalEmailsHelp;
		}

		return setupBodyCopy.recipients.audienceRules.selfHelp;
	}

	function recipientRoleLabel(role: string): string {
		if (role === 'manager') {
			return setupBodyCopy.recipients.roles.manager;
		}

		if (role === 'external' || role === 'email_recipient') {
			return setupBodyCopy.recipients.roles.external;
		}

		return setupBodyCopy.recipients.roles.respondent;
	}

	function audienceWarningLabel(warning: { code: string; message: string }): string {
		if (warning.code.includes('audience_missing')) {
			return setupBodyCopy.recipients.warnings.audienceMissing;
		}

		if (warning.code.includes('empty')) {
			return setupBodyCopy.recipients.warnings.empty;
		}

		if (warning.code.includes('truncated')) {
			return setupBodyCopy.recipients.warnings.truncated;
		}

		return warning.message;
	}

	function launchIssueLabel(issue: { code: string; message: string }) {
		if (issue.code.includes('campaign')) {
			return issue.message.replace('campaign', 'collection wave');
		}

		if (issue.code.includes('template')) {
			return issue.message.replace('Template', 'Questionnaire').replace('template', 'questionnaire');
		}

		if (issue.code.includes('scoring')) {
			return issue.message.replace('Scoring rule', 'Results setup').replace('scoring rule', 'results setup');
		}

		if (issue.code === 'respondent_rule.email_required') {
			return 'Every saved Directory recipient needs an email address before invite-only collection can start.';
		}

		if (issue.code === 'respondent_rule.no_recipients') {
			return 'Save at least one recipient selection before launch, and make sure it resolves to active people.';
		}

		if (issue.code === 'respondent_rule.identity_mode_not_supported') {
			return 'Specific email lists are available for anonymous invite-only or repeat-participation waves only.';
		}

		return issue.message;
	}

	function readinessLabel() {
		return currentLaunchState().statusLabel;
	}

	function canOpenLaunchSurface() {
		return currentLaunchState().collectionButtonAvailable;
	}

	function launchSurfaceButtonLabel() {
		return currentLaunchState().collectionButtonLabel;
	}
</script>

<section class="product-panel" role="group" aria-label={setupBodyCopy.progressAriaLabel}>
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">{setupBodyCopy.progressKicker}</p>
			<h3 class="product-title">{setupBodyCopy.progressTitle}</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Build the study in order: confirm the instrument, write the questionnaire, decide how
				results are calculated, then prepare the campaign for launch.
			</p>
		</div>
	</div>

	{#if !canManageSetup}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">Read-only access</strong>
			<span>Setup workflow actions require setup management access.</span>
		</p>
			{@render SetupPath()}
	{:else}
		<div class="grid gap-3">
					<p class="record-field__label">
				{setupPath.completedCount} of {setupPath.totalCount} required steps complete
			</p>
			{@render SetupPath()}
		</div>

		<section class="record-row setup-current-task" aria-labelledby="current-setup-task-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">{activeSetupStepEyebrow()}</p>
					<h4 id="current-setup-task-heading" class="record-row__title">{activeStep.title}</h4>
					<p class="setup-current-task__title">{activeSetupStepLabel()}</p>
					<p class="text-sm text-[var(--color-text-muted)]">{activeStep.description}</p>
				</div>
				<StatusBadge
					status={activeStep.status}
					label={activeStep.pathState === 'done' ? 'Done' : undefined}
				/>
			</div>
			{#if activeStep.disabledReason}
				<p class="text-sm text-[var(--color-text-muted)]">{activeStep.disabledReason}</p>
			{/if}

			<div class="setup-current-task__body">
				{#if activeActionIdForView === 'instrument'}
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<div class="record-row__header">
								<div>
									<h5 class="record-row__title">Study source ready</h5>
									<p class="text-sm text-[var(--color-text-muted)]">
										The study source is saved. Continue to the questionnaire.
									</p>
								</div>
								<StatusBadge status="ready" label="Done" />
							</div>
						</div>
					{:else}
						<div class="grid gap-4">
							<label class="field lg:col-span-2">
								<span>Study source name</span>
								<input bind:value={instrumentForm.fullName} />
							</label>
						</div>
						{@render ActionFooter({
							id: 'instrument',
							label: 'Save study source',
							icon: 'plus',
							onclick: createInstrumentImport
						})}
					{/if}
				{:else if activeActionIdForView === 'template'}
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<div class="record-row__header">
								<div>
									<h5 class="record-row__title">Questionnaire ready</h5>
									<p class="text-sm text-[var(--color-text-muted)]">
										{questionnaireQuestionCount}
										{questionnaireQuestionCount === 1 ? 'question is' : 'questions are'} saved.
										Continue to scoring.
									</p>
								</div>
								<StatusBadge status="ready" label="Done" />
							</div>
						</div>
					{:else}
						<div class="grid gap-4 lg:grid-cols-2">
							<label class="field">
								<span>Questionnaire name</span>
								<input bind:value={templateName} />
							</label>
							<label class="field">
								<span>Language</span>
								<select bind:value={questionnaireLocale}>
									<option value="en">English</option>
									<option value="hr">Croatian</option>
								</select>
							</label>
						</div>
						<div class="mt-4 record-row">
							<div class="record-row__header">
								<div>
									<p class="record-field__label">Questionnaire palette</p>
									<h5 class="record-row__title">Choose an editable question set</h5>
									<p class="text-sm text-[var(--color-text-muted)]">
										Start blank, or load original editable starter items that match the study you are building.
										These are not marketed as validated named instruments; review and edit before launch.
									</p>
								</div>
								<StatusBadge status="neutral" label="Editable" />
							</div>
							<div class="record-grid">
								{#each questionnairePaletteOptions as preset (preset.id)}
									<button
										type="button"
										class="record-field text-left"
										aria-pressed={selectedQuestionnairePalette === preset.id}
										onclick={() => applyQuestionnairePalette(preset.id)}
									>
										<p class="record-field__label">
											{preset.category} - {preset.questionCount} questions
										</p>
										<p class="record-field__value">{preset.label}</p>
										<p class="mt-1 text-sm text-[var(--color-text-muted)]">{preset.summary}</p>
										<p class="mt-2 text-xs text-[var(--color-text-muted)]">{preset.detail}</p>
										<p class="mt-2 text-xs text-[var(--color-text-muted)]">
											Suggested results: {preset.resultOutputs.join(', ')}
										</p>
									</button>
								{/each}
							</div>
						</div>
						<div class="mt-4 grid gap-4">
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">Authoring summary</p>
										<h5 class="record-row__title">{authoringReadiness.label}</h5>
									</div>
									<StatusBadge status="neutral" label={`${authoringReadiness.questionCount} questions`} />
								</div>
								<p class="text-sm text-[var(--color-text-muted)]">
									{authoringReadiness.contextQuestionCount}
									{authoringReadiness.contextQuestionCount === 1 ? 'context question is' : 'context questions are'}
									collected but not scored.
									{reverseScoredCountLabel(authoringReadiness.reverseScoredQuestionCount)}.
								</p>
							</div>
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">Questionnaire blueprint</p>
										<h5 class="record-row__title">{questionnaireBlueprintReview.label}</h5>
									</div>
									<StatusBadge status="neutral" label="Design review" />
								</div>
								<div class="questionnaire-blueprint-review">
									{#each questionnaireBlueprintReview.items as item (item.id)}
										<div class="questionnaire-blueprint-review__item" data-state={item.status}>
											<p class="record-field__label">{item.label}</p>
											<p class="record-field__value">{item.detail}</p>
										</div>
									{/each}
								</div>
							</div>
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">Study dimensions</p>
										<h5 class="record-row__title">What this questionnaire measures</h5>
									</div>
									<StatusBadge status="neutral" label={`${questionDimensionSummaries.length} dimensions`} />
								</div>
								<div class="record-grid">
									{#each questionDimensionSummaries as dimension (dimension.code)}
										<div class="record-field">
											<p class="record-field__label">{dimension.code}</p>
											<p class="record-field__value">{dimension.label}</p>
											<p class="text-sm text-[var(--color-text-muted)]">
												{dimension.questionCount}
												{dimension.questionCount === 1 ? 'question' : 'questions'}
											</p>
										</div>
									{/each}
								</div>
							</div>
							{#each templateQuestionRows as question, index (question.ordinal)}
								<div class="question-row">
									<div class="record-row__header">
										<div>
											<p class="record-field__label">Question {index + 1}</p>
											<h5 class="record-row__title">
												{question.textDefault.trim() || 'Untitled question'}
											</h5>
											<p class="text-sm text-[var(--color-text-muted)]">
												{questionAuthoringSummary(question.code)?.dimensionLabel ?? question.dimensionLabel}
												-
												{questionAuthoringSummary(question.code)?.scaleLabel ?? questionTypeLabel(question.type)}
												-
												{questionAuthoringSummary(question.code)?.resultUsageLabel ?? questionResultUsage(question)}
											</p>
										</div>
										<StatusBadge
											status="neutral"
											label={isScaleQuestion(question)
												? `${question.scaleMin}-${question.scaleMax} rating`
												: questionTypeLabel(question.type)}
										/>
									</div>
									<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(10rem,14rem)]">
										<label class="field">
											<span>Question text</span>
											<textarea
												rows="2"
												value={question.textDefault}
												oninput={(event) =>
													updateTemplateQuestionRow(index, {
														textDefault: event.currentTarget.value
													})}
											></textarea>
										</label>
										<label class="field">
											<span>Dimension / construct</span>
											<input
												value={question.dimensionLabel}
												oninput={(event) =>
													updateTemplateQuestionRow(index, {
														dimensionLabel: event.currentTarget.value
													})}
											/>
											<span class="text-xs leading-5 text-[var(--color-text-muted)]">
												Group questions by what they measure, for example workload, recovery, or autonomy.
											</span>
										</label>
										<label class="field">
											<span>Answer format</span>
											<select
												value={question.type}
												onchange={(event) =>
													updateTemplateQuestionType(
														index,
														event.currentTarget.value as TemplateQuestionAnswerType
													)}
											>
												<option value="likert">Rating scale</option>
												<option value="nps">0-10 recommendation scale</option>
												<option value="single">Single choice</option>
												<option value="multi">Multiple choice</option>
												<option value="number">Number</option>
												<option value="text">Text</option>
												<option value="date">Date</option>
												<option value="ranking">Ranking</option>
											</select>
											<span class="text-xs leading-5 text-[var(--color-text-muted)]">
												{questionScaleIntent(question).label}. {questionScaleIntent(question).detail}
											</span>
										</label>
									</div>
									{#if isScaleQuestion(question)}
										<details class="record-row">
											<summary class="record-row__title">Scale values and labels</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-5">
												<label class="field">
													<span>Scale preset</span>
													<select
														value={question.scalePreset}
														onchange={(event) =>
															updateTemplateQuestionScalePreset(
																index,
																event.currentTarget.value as QuestionScalePreset
															)}
													>
														{#each questionScalePresetOptions as option (option.value)}
															<option value={option.value}>{option.label}</option>
														{/each}
													</select>
													<span class="text-xs leading-5 text-[var(--color-text-muted)]">
														{questionScalePresetOptions.find((option) => option.value === question.scalePreset)
															?.detail ?? 'Keep the current scale values and labels.'}
													</span>
												</label>
												<label class="field">
													<span>Lowest value</span>
													<input
														type="number"
														value={question.scaleMin}
														oninput={(event) =>
															updateScaleNumber(index, 'scaleMin', event.currentTarget.value, 1)}
													/>
												</label>
												<label class="field">
													<span>Highest value</span>
													<input
														type="number"
														value={question.scaleMax}
														oninput={(event) =>
															updateScaleNumber(index, 'scaleMax', event.currentTarget.value, 5)}
													/>
												</label>
												<label class="field">
													<span>Low label</span>
													<input
														value={question.scaleLowLabel}
														oninput={(event) =>
															updateTemplateQuestionRow(index, {
																scaleLowLabel: event.currentTarget.value,
																scalePreset: 'custom'
															})}
													/>
												</label>
												<label class="field">
													<span>High label</span>
													<input
														value={question.scaleHighLabel}
														oninput={(event) =>
															updateTemplateQuestionRow(index, {
																scaleHighLabel: event.currentTarget.value,
																scalePreset: 'custom'
															})}
													/>
												</label>
											</div>
										</details>
									{/if}
									{#if question.type === 'number'}
										<details class="record-row">
											<summary class="record-row__title">Number rules</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-4">
												<label class="field">
													<span>Minimum</span>
													<input
														type="number"
														value={question.numberMin ?? ''}
														oninput={(event) =>
															updateMetadataNumber(index, 'numberMin', event.currentTarget.value)}
													/>
												</label>
												<label class="field">
													<span>Maximum</span>
													<input
														type="number"
														value={question.numberMax ?? ''}
														oninput={(event) =>
															updateMetadataNumber(index, 'numberMax', event.currentTarget.value)}
													/>
												</label>
												<label class="field">
													<span>Unit label</span>
													<input
														placeholder="hours/week, kg, minutes..."
														value={question.numberUnit}
														oninput={(event) =>
															updateTemplateQuestionRow(index, { numberUnit: event.currentTarget.value })}
													/>
												</label>
												<label class="checkbox-field self-end">
													<input
														type="checkbox"
														checked={question.numberIntegerOnly}
														onchange={(event) =>
															updateTemplateQuestionRow(index, {
																numberIntegerOnly: event.currentTarget.checked
															})}
													/>
													<span>Whole numbers only</span>
												</label>
											</div>
										</details>
									{/if}
									{#if question.type === 'text'}
										<details class="record-row">
											<summary class="record-row__title">Text response rules</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-3">
												<label class="checkbox-field self-end">
													<input
														type="checkbox"
														checked={question.textMultiline}
														onchange={(event) =>
															updateTemplateQuestionRow(index, {
																textMultiline: event.currentTarget.checked
															})}
													/>
													<span>Long text answer</span>
												</label>
												<label class="field">
													<span>Max characters</span>
													<input
														type="number"
														min="1"
														value={question.textMaxLength ?? ''}
														oninput={(event) =>
															updateMetadataNumber(index, 'textMaxLength', event.currentTarget.value)}
													/>
												</label>
											</div>
										</details>
									{/if}
									{#if question.type === 'date'}
										<details class="record-row">
											<summary class="record-row__title">Date rules</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-2">
												<label class="field">
													<span>Earliest date</span>
													<input
														type="date"
														value={question.dateEarliest}
														oninput={(event) =>
															updateTemplateQuestionRow(index, {
																dateEarliest: event.currentTarget.value
															})}
													/>
												</label>
												<label class="field">
													<span>Latest date</span>
													<input
														type="date"
														value={question.dateLatest}
														oninput={(event) =>
															updateTemplateQuestionRow(index, { dateLatest: event.currentTarget.value })}
													/>
												</label>
											</div>
										</details>
									{/if}
									{#if isChoiceQuestion(question) || question.type === 'ranking'}
										<details class="record-row">
											<summary class="record-row__title">Answer options</summary>
											<label class="field mt-3">
												<span>Options</span>
												<textarea
													rows="3"
													value={question.choiceOptions.join('\n')}
													oninput={(event) => updateChoiceOptions(index, event.currentTarget.value)}
												></textarea>
												<span class="text-sm text-[var(--color-text-muted)]">
													Enter one option per line.
												</span>
											</label>
											{#if isChoiceQuestion(question)}
												<div class="mt-3 grid gap-3 lg:grid-cols-3">
													<label class="checkbox-field self-end">
														<input
															type="checkbox"
															checked={question.choiceAllowOther}
															onchange={(event) =>
																updateTemplateQuestionRow(index, {
																	choiceAllowOther: event.currentTarget.checked
																})}
														/>
														<span>Add an Other write-in option</span>
													</label>
													<label class="field">
														<span>Other label</span>
														<input
															disabled={!question.choiceAllowOther}
															value={question.choiceOtherLabel}
															oninput={(event) =>
																updateTemplateQuestionRow(index, {
																	choiceOtherLabel: event.currentTarget.value
																})}
														/>
													</label>
													<label class="field">
														<span>Exclusive option</span>
														<input
															placeholder="Example: None of these"
															value={question.choiceExclusiveOptionLabel}
															oninput={(event) =>
																updateTemplateQuestionRow(index, {
																	choiceExclusiveOptionLabel: event.currentTarget.value
																})}
														/>
													</label>
												</div>
											{/if}
											{#if question.type === 'ranking'}
												<div class="mt-3 grid gap-3 lg:grid-cols-3">
													<label class="field">
														<span>Ranking rule</span>
														<select
															value={question.rankingMode}
															onchange={(event) =>
																updateTemplateQuestionRow(index, {
																	rankingMode: event.currentTarget.value as QuestionRankingMode
																})}
														>
															<option value="rank_all">Rank all options</option>
															<option value="top_n">Rank only top N</option>
														</select>
													</label>
													<label class="field">
														<span>Top N</span>
														<input
															type="number"
															min="1"
															disabled={question.rankingMode !== 'top_n'}
															value={question.rankingTopN ?? ''}
															oninput={(event) =>
																updateMetadataNumber(index, 'rankingTopN', event.currentTarget.value)}
														/>
													</label>
												</div>
											{/if}
										</details>
									{/if}
									<div class="action-row">
										<p class="basis-full text-sm text-[var(--color-text-muted)]">
											Question order below is the order respondents will see. Scoring stays attached to
											question meaning, not the visual position.
										</p>
										<label class="checkbox-field">
											<input
												type="checkbox"
												checked={question.required}
												onchange={(event) =>
													updateTemplateQuestionRow(index, {
														required: event.currentTarget.checked
													})}
											/>
											<span>Required</span>
										</label>
										{#if isScaleQuestion(question)}
											<label class="checkbox-field">
												<input
													type="checkbox"
													checked={question.reverseCoded}
													onchange={(event) =>
														updateTemplateQuestionRow(index, {
															reverseCoded: event.currentTarget.checked
														})}
												/>
												<span>Reverse score this question</span>
											</label>
										{/if}
										<button
											type="button"
											class="secondary-button"
											disabled={index === 0}
											title="Move question earlier"
											onclick={() => reorderTemplateQuestionRow(question.code, 'up')}
										>
											<ArrowUp size={16} aria-hidden="true" />
											<span>Move earlier</span>
										</button>
										<button
											type="button"
											class="secondary-button"
											disabled={index === templateQuestionRows.length - 1}
											title="Move question later"
											onclick={() => reorderTemplateQuestionRow(question.code, 'down')}
										>
											<ArrowDown size={16} aria-hidden="true" />
											<span>Move later</span>
										</button>
										<button
											type="button"
											class="secondary-button"
											title="Duplicate question"
											onclick={() => copyTemplateQuestionRow(question.code)}
										>
											<Copy size={16} aria-hidden="true" />
											<span>Duplicate</span>
										</button>
										<button
											type="button"
											class="secondary-button"
											disabled={templateQuestionRows.length <= 1}
											title="Remove question"
											onclick={() => deleteTemplateQuestionRow(question.code)}
										>
											<Trash2 size={16} aria-hidden="true" />
											<span>Remove</span>
										</button>
									</div>
									<div class="record-row">
										<div class="record-row__header">
											<div>
												<p class="record-field__label">Answer scale</p>
												<h6 class="record-row__title">{questionScaleIntent(question).label}</h6>
											</div>
											<StatusBadge status="neutral" label={questionTypeLabel(question.type)} />
										</div>
										<p class="text-sm text-[var(--color-text-muted)]">
											{questionScaleIntent(question).detail}
										</p>
									</div>
									<div class="record-row">
										<div class="record-row__header">
											<div>
												<p class="record-field__label">Scoring meaning</p>
												<h6 class="record-row__title">{questionScoringDetail(question).label}</h6>
											</div>
											<StatusBadge status={isMeanScoreEligible(question) ? 'ready' : 'neutral'} />
										</div>
										<p class="text-sm text-[var(--color-text-muted)]">
											{questionScoringDetail(question).detail}
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{questionResultUsage(question)}
										</p>
									</div>
								</div>
							{/each}
						</div>
						<div class="action-row">
							<button type="button" class="secondary-button" onclick={addTemplateQuestionRow}>
								<Plus size={16} aria-hidden="true" />
								<span>{setupBodyCopy.questionnaire.addQuestion}</span>
							</button>
						</div>
						<div class="record-row">
							<div class="record-row__header">
								<div>
									<p class="record-field__label">Respondent preview</p>
									<h5 class="record-row__title">{respondentPreviewContract.label}</h5>
								</div>
								<StatusBadge
									status={respondentPreviewContract.unsupportedCount > 0 ? 'blocked' : 'neutral'}
									label={`${respondentPreviewContract.questionCount} questions`}
								/>
							</div>
							<p class="text-sm text-[var(--color-text-muted)]">
								{respondentPreviewContract.detail}
							</p>
							<div class="record-grid">
								{#each respondentPreviewContract.controls as control (control.label)}
									<div class="record-field">
										<p class="record-field__label">Runtime control</p>
										<p class="record-field__value">{control.label}</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{control.count} {control.count === 1 ? 'question' : 'questions'}
										</p>
									</div>
								{/each}
								<div class="record-field">
									<p class="record-field__label">Runtime notes</p>
									<p class="record-field__value">{respondentPreviewContract.warningCount}</p>
									<p class="text-sm text-[var(--color-text-muted)]">
										Limitations to review before launch.
									</p>
								</div>
							</div>
							<p class="text-sm text-[var(--color-text-muted)]">
								This preview uses the same control families as the respondent SurveyJS runtime: rating,
								radio group, checkbox group, ranking, number, date, and text.
							</p>
							<div class="grid gap-3">
								{#each respondentPreviewContract.questions as question (question.code)}
									<div class="record-row">
										<div class="record-row__header">
											<div>
												<p class="record-field__label">
													{question.positionLabel} - {question.dimensionLabel}
												</p>
												<p class="record-field__value">{question.text}</p>
											</div>
											<StatusBadge
												status={question.requiredLabel === 'Required' ? 'ready' : 'neutral'}
												label={question.requiredLabel}
											/>
										</div>
										<div class="record-field">
											<p class="record-field__label">
												{question.answerFormatLabel} - {question.responsePreviewLabel}
											</p>
											<p class="record-field__value">{question.answerFormatDetail}</p>
											<p class="text-sm text-[var(--color-text-muted)]">
												{question.scoreEligibilityLabel}. {question.scoreDirectionLabel}.
												{question.resultUsageLabel}
											</p>
										</div>
										<div class="record-field">
											{#if question.controlType === 'rating'}
												<div class="action-row" aria-label={`${question.positionLabel} rating preview`}>
													{#each runtimeRatingValues(question) as value}
														<label class="checkbox-field">
															<input type="radio" disabled />
															<span>{value}</span>
														</label>
													{/each}
												</div>
												<p class="text-sm text-[var(--color-text-muted)]">
													{question.scaleLowLabel} -> {question.scaleHighLabel}
												</p>
											{:else if question.controlType === 'radio'}
												<div class="grid gap-2" aria-label={`${question.positionLabel} single choice preview`}>
													{#each question.choices as choice (choice.value)}
														<label class="checkbox-field">
															<input type="radio" disabled />
															<span>{choice.text}</span>
														</label>
													{/each}
												</div>
											{:else if question.controlType === 'checkbox'}
												<div class="grid gap-2" aria-label={`${question.positionLabel} multiple choice preview`}>
													{#each question.choices as choice (choice.value)}
														<label class="checkbox-field">
															<input type="checkbox" disabled />
															<span>{choice.text}</span>
														</label>
													{/each}
												</div>
											{:else if question.controlType === 'ranking'}
												<ol class="grid gap-2" aria-label={`${question.positionLabel} ranking preview`}>
													{#each question.choices as choice, choiceIndex (choice.value)}
														<li class="record-field">
															<p class="record-field__label">Rank {choiceIndex + 1}</p>
															<p class="record-field__value">{choice.text}</p>
														</li>
													{/each}
												</ol>
											{:else if question.controlType === 'number'}
												<input type="number" disabled placeholder="Number response" />
											{:else if question.controlType === 'date'}
												<input type="date" disabled />
											{:else if question.controlType === 'text'}
												{#if question.runtimeElementType === 'comment'}
													<textarea rows="3" disabled placeholder="Long text response"></textarea>
												{:else}
													<input type="text" disabled placeholder="Text response" />
												{/if}
											{:else}
												<p class="error-line">This question cannot be rendered by the current respondent runtime.</p>
											{/if}
										</div>
										{#if question.warnings.length > 0}
											<ul class="grid gap-1" aria-label={`${question.positionLabel} runtime notes`}>
												{#each question.warnings as warning}
													<li class="text-sm text-[var(--color-text-muted)]">{warning}</li>
												{/each}
											</ul>
										{/if}
									</div>
								{/each}
							</div>
						</div>
						{#if templateQuestionErrors.length > 0}
							<ul class="grid gap-1" aria-label={setupBodyCopy.questionnaire.errorsLabel}>
								{#each templateQuestionErrors as error}
									<li class="error-line">{error}</li>
								{/each}
							</ul>
						{/if}
						{@render ActionFooter({
							id: 'template',
							label: 'Save questionnaire',
							icon: 'send',
							onclick: createTemplateVersion
						})}
					{/if}
				{:else if activeActionIdForView === 'scoring'}
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<h5 class="record-row__title">Results setup ready</h5>
							<div class="record-grid">
								<div class="record-field">
									<p class="record-field__label">Result outputs</p>
									<p class="record-field__value">{scoreOutputs.length}</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">Outputs</p>
									<p class="record-field__value">
										{scoreOutputs.map((output) => output.name.trim() || output.code).join(', ')}
									</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">Unique scored questions</p>
									<p class="record-field__value">{selectedScoreQuestionRows.length}</p>
								</div>
							</div>
						</div>
					{:else}
						<div class="record-row">
							<h5 class="record-row__title">Result outputs</h5>
							<p class="text-sm text-[var(--color-text-muted)]">
								Create one total score or several dimensions/subscales. Each output chooses its own
								questions, calculation, and missing-answer rule.
							</p>
						</div>

						<div class="grid gap-4">
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">Results blueprint</p>
										<h5 class="record-row__title">{resultsBlueprintReview.label}</h5>
									</div>
									<StatusBadge status="neutral" label="Results plan" />
								</div>
								<div class="questionnaire-blueprint-review">
									{#each resultsBlueprintReview.items as item (item.id)}
										<div class="questionnaire-blueprint-review__item" data-state={item.status}>
											<p class="record-field__label">{item.label}</p>
											<p class="record-field__value">{item.detail}</p>
										</div>
									{/each}
								</div>
							</div>
							{#each scoreOutputs as output, outputIndex (output.localId)}
								<div class="record-row">
									<div class="setup-current-task__header">
										<div>
											<p class="record-field__label">Result {outputIndex + 1}</p>
											<h5 class="record-row__title">{output.name.trim() || `Result ${outputIndex + 1}`}</h5>
											<p class="setup-current-task__title">
												{output.includedQuestionCodes.length}
												selected {output.includedQuestionCodes.length === 1 ? 'question' : 'questions'}
											</p>
										</div>
										{#if scoreOutputs.length > 1}
											<button
												type="button"
												class="secondary-button"
												onclick={() => deleteScoreOutput(output.localId)}
											>
												<Trash2 size={16} aria-hidden="true" />
												<span>Remove result</span>
											</button>
										{/if}
									</div>

									<div class="grid gap-4 lg:grid-cols-2">
										<label class="field">
											<span>Result name</span>
											<input
												value={output.name}
												oninput={(event) =>
													updateScoreOutput(output.localId, { name: event.currentTarget.value })}
											/>
										</label>
										<label class="field">
											<span>Result code</span>
											<input
												value={output.code}
												oninput={(event) =>
													updateScoreOutput(output.localId, { code: event.currentTarget.value })}
											/>
											<span class="text-xs leading-5 text-[var(--color-text-muted)]">
												Used as the report/export dimension code.
											</span>
										</label>
										<label class="field">
											<span>Calculation</span>
											<select
												value={output.calculation}
												onchange={(event) =>
													updateScoreOutput(output.localId, {
														calculation: event.currentTarget.value as ScoreCalculation
													})}
											>
												<option value="mean">Average selected answers</option>
												<option value="sum">Sum selected answers</option>
											</select>
										</label>
										<label class="field">
											<span>Missing answers</span>
											<select
												value={output.missingStrategy}
												onchange={(event) =>
													updateScoreOutput(output.localId, {
														missingStrategy: event.currentTarget.value as ScoreMissingStrategy
													})}
											>
												<option value="require_all">Require every selected answer</option>
												<option value="min_valid_count">Allow a score after enough answers</option>
											</select>
										</label>
										{#if output.missingStrategy === 'min_valid_count'}
											<label class="field">
												<span>Minimum answered</span>
												<input
													type="number"
													min="1"
													max={Math.max(1, output.includedQuestionCodes.length)}
													value={output.minValidCount}
													oninput={(event) =>
														updateScoreOutput(output.localId, {
															minValidCount: parseScoreMinValidCount(event.currentTarget.value)
														})}
												/>
											</label>
										{/if}
									</div>

									<div class="record-row">
										<h6 class="record-row__title">Questions in this result</h6>
										{#if scoreableQuestionRows.length}
											<div class="grid gap-2">
												{#each scoreableQuestionRows as question (question.code)}
													<div class="record-field">
														<label class="checkbox-field">
															<input
																type="checkbox"
																checked={output.includedQuestionCodes.includes(question.code)}
																onchange={(event) =>
																	toggleScoreQuestion(
																		output.localId,
																		question.code,
																		event.currentTarget.checked
																	)}
															/>
															<span>{question.textDefault.trim() || question.code}</span>
														</label>
														<p class="text-sm text-[var(--color-text-muted)]">
															{questionPreviewDetail(question)}
														</p>
														<p class="text-sm text-[var(--color-text-muted)]">
															{questionScoringDetail(question).label}. {questionScoringDetail(question).detail}
														</p>
													</div>
												{/each}
											</div>
										{:else}
											<p class="text-sm text-[var(--color-text-muted)]">
												Add a rating scale, recommendation scale, or number question before saving results.
											</p>
										{/if}
									</div>
								</div>
							{/each}
						</div>

						<div class="action-row">
							<button type="button" class="secondary-button" onclick={addScoreOutput}>
								<Plus size={16} aria-hidden="true" />
								<span>Add result output</span>
							</button>
						</div>

						{#if collectedContextSummaries.length}
							<div class="record-row">
								<h5 class="record-row__title">Collected but not scored</h5>
								<div class="grid gap-2">
									{#each collectedContextSummaries as question (question.code)}
										<div class="record-field">
											<p class="record-field__label">{question.dimensionLabel} - {question.typeLabel}</p>
											<p class="record-field__value">
												{question.text}
											</p>
										</div>
									{/each}
								</div>
							</div>
						{/if}

						<div class="record-row">
							<h5 class="record-row__title">Scoring plan preview</h5>
							<div class="grid gap-2">
								{#each scorePlanSummaries as summary (summary.localId)}
									<div class="record-field">
										<p class="record-field__label">{summary.code}</p>
										<p class="record-field__value">{summary.name}</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											Uses {dimensionCoverageLabel(summary.dimensionLabels)} from
											{summary.includedQuestionCount}
											selected {summary.includedQuestionCount === 1 ? 'question' : 'questions'}.
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{summary.calculationLabel}. {summary.missingPolicyLabel}.
											{reverseScoredCountLabel(summary.reverseScoredQuestionCount)}.
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{scoreOutputMissingDataDetail(summary.localId)}
										</p>
									</div>
								{/each}
							</div>
						</div>

						{#if reverseScoringReview.reverseScoredQuestionCount > 0}
							<div class="record-row">
								<h5 class="record-row__title">Reverse-scoring review</h5>
								<p class="text-sm text-[var(--color-text-muted)]">
									{reverseScoringReview.reverseScoredQuestionCount}
									{reverseScoringReview.reverseScoredQuestionCount === 1 ? 'question is' : 'questions are'}
									reversed before scoring:
									{reverseScoringReview.reverseScoredQuestionLabels.join(', ')}.
								</p>
								<p class="text-sm text-[var(--color-text-muted)]">
									Affects: {reverseScoringReview.affectedResultLabels.join(', ') || 'no result outputs yet'}.
								</p>
							</div>
						{/if}

						{#if scoreOutputErrors.length > 0}
							<ul class="grid gap-1" aria-label={setupBodyCopy.scoring.errorsLabel}>
								{#each scoreOutputErrors as error}
									<li class="error-line">{error}</li>
								{/each}
							</ul>
						{/if}

						{@render ActionFooter({
							id: 'scoring',
							label: 'Save results setup',
							icon: 'send',
							onclick: createScoringRule
						})}
					{/if}
				{:else if activeActionIdForView === 'campaign'}
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">Wave context</p>
								<h5 class="record-row__title">{waveContext.title}</h5>
								<p class="text-sm text-[var(--color-text-muted)]">{waveContext.summary}</p>
							</div>
							<StatusBadge status={waveContext.status} label={waveContext.label} />
						</div>
						<ul class="grid gap-2" aria-label="Wave setup guidance">
							{#each waveContext.guidance as guidance}
								<li class="text-sm text-[var(--color-text-muted)]">{guidance}</li>
							{/each}
						</ul>
					</div>
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<h5 class="record-row__title">Collection wave ready</h5>
							<div class="record-grid">
								<div class="record-field">
									<p class="record-field__label">Wave</p>
									<p class="record-field__value">{selectedCampaignLabel}</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">Response mode</p>
									<p class="record-field__value">
										{responseModeLabel(campaignForm.responseIdentityMode)}
									</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">Language</p>
									<p class="record-field__value">{campaignForm.defaultLocale}</p>
								</div>
							</div>
						</div>
					{:else}
						<div class="record-row">
							<div class="record-row__header">
								<div>
									<p class="record-field__label">Launch plan</p>
									<h5 class="record-row__title">{launchPlan.label}</h5>
									<p class="text-sm text-[var(--color-text-muted)]">{launchPlan.summary}</p>
								</div>
								<StatusBadge status="neutral" label="Wave plan" />
							</div>
							<div class="questionnaire-blueprint-review">
								{#each launchPlan.items as item (item.id)}
									<div class="questionnaire-blueprint-review__item" data-state={item.status}>
										<p class="record-field__label">{item.label}</p>
										<p class="record-field__value">{item.detail}</p>
									</div>
								{/each}
							</div>
						</div>
						<div class="grid gap-4 lg:grid-cols-2">
							<label class="field">
								<span>Wave name</span>
								<input bind:value={campaignForm.name} />
							</label>
							<label class="field">
								<span>Response mode</span>
								<select bind:value={campaignForm.responseIdentityMode}>
									<option value="anonymous">Anonymous</option>
									<option value="anonymous_longitudinal">Anonymous with repeat participation</option>
									<option value="identified">Identified invite-only</option>
								</select>
								<span class="text-xs leading-5 text-[var(--color-text-muted)]">
									{responseModeHelp(campaignForm.responseIdentityMode)}
								</span>
							</label>
							<label class="field">
								<span>Respondent language</span>
								<input bind:value={campaignForm.defaultLocale} />
							</label>
						</div>
						{@render ActionFooter({
							id: 'campaign',
							label: 'Save collection wave',
							icon: 'send',
							onclick: createCampaignDraft
						})}
					{/if}
				{:else if activeActionIdForView === 'readiness'}
					<div class="record-row">
						<h5 class="record-row__title">Launch checklist</h5>
						<div class="record-grid">
							<div class="record-field">
								<p class="record-field__label">Questionnaire</p>
								<p class="record-field__value">
									{selectedTemplateVersionId ? 'Ready' : 'Save questionnaire first'}
								</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">Results setup</p>
								<p class="record-field__value">
									{localState.scoringRuleId ?? workspace.scoring?.id
										? 'Ready'
										: 'Save results setup first'}
								</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">Collection wave</p>
								<p class="record-field__value">
									{selectedCampaignId ? selectedCampaignLabel : 'Create collection wave first'}
								</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">Status</p>
								<p class="record-field__value">{readinessLabel()}</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">Recipient selection</p>
								<p class="record-field__value">{currentLaunchState().recipientSummary}</p>
							</div>
						</div>
					</div>
					{#if readinessResult?.issues.length}
						<ul class="grid gap-2" aria-label="Launch checklist issues">
							{#each readinessResult.issues as issue}
								<li class="text-sm text-[var(--color-text-muted)]">
									{launchIssueLabel(issue)}
								</li>
							{/each}
						</ul>
					{/if}
					<p class="result-line">
						<span>Next action</span>
						<span>{currentLaunchState().nextActionLabel}</span>
					</p>
					{@render ActionFooter({
						id: 'readiness',
						label: 'Run launch check',
						icon: 'search',
						onclick: checkLaunchReadiness
					})}
				{/if}
			</div>
			<div class="action-row">
				<button
					type="button"
					class="secondary-button"
					disabled={!canGoPrevious}
					onclick={goToPreviousSetupAction}
				>
					Previous step
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={activeActionIdForView === 'readiness' ? !canOpenLaunchSurface() : !canGoNext}
					onclick={activeActionIdForView === 'readiness' ? openLaunchSurface : goToNextSetupAction}
				>
					{activeActionIdForView === 'readiness' ? launchSurfaceButtonLabel() : 'Next step'}
				</button>
			</div>
		</section>

		{#if refreshWarning}
			<p class="error-line">{refreshWarning}</p>
		{/if}

		{#if lockedSelectedCampaign && activeActionIdForView === 'campaign'}
		<section class="record-row setup-current-task" aria-labelledby="locked-wave-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Previous wave</p>
					<h4 id="locked-wave-heading" class="record-row__title">Recipient selection is locked</h4>
					<p class="setup-current-task__title">{lockedSelectedCampaign.name}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						This wave is already {lockedSelectedCampaign.status}. Recipient selection can only be
						changed before launch. Save the next collection wave first, then choose recipients for
						that draft.
					</p>
				</div>
				<p class="step-pill" data-state="idle">Locked</p>
			</div>
		</section>
		{/if}

		{#if selectedCampaignId && (activeActionIdForView === 'campaign' || activeActionIdForView === 'readiness')}
		<section class="record-row setup-current-task" aria-labelledby="audience-preview-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Recipient selection</p>
					<h4 id="audience-preview-heading" class="record-row__title">Preview recipients, then save the selection</h4>
					<p class="setup-current-task__title">{selectedCampaignLabel}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Use Directory groups for recurring populations. Use all active Directory people only
						when the wave is truly for everyone. Use one-off email list for ad hoc recipients you
						do not want to manage in Directory. Save recipients before launch so Collection can
						create private respondent links and send email.
					</p>
				</div>
				<p class="step-pill" data-state={previewState}>{stepLabel(previewState)}</p>
			</div>

			{#if savedRuleResult?.rules.length}
				<p class="result-line">
					<span>Saved for launch</span>
					<span>{savedAudienceSummary()}</span>
				</p>
			{:else}
				<p class="error-line" role="status">
					No recipient selection is saved yet. Preview recipients first, then save the previewed
					selection before launch.
				</p>
			{/if}

			<div class="record-grid">
				<div class="record-field">
					<p class="record-field__label">Reusable population</p>
					<p class="record-field__value">Directory groups</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Best for departments, cohorts, classes, and repeated waves.
					</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">One-off population</p>
					<p class="record-field__value">Email import</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Best for a temporary list copied from a collaborator or spreadsheet export.
					</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">Open participation</p>
					<p class="record-field__value">Open link in Collection</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Best when anyone with the link may answer and no invite-only list is needed.
					</p>
				</div>
			</div>

			<div class="record-row">
				<div class="record-row__header">
					<div>
						<p class="record-field__label">Demo/test data</p>
						<h5 class="record-row__title">Create test recipients for this wave</h5>
						<p class="text-sm text-[var(--color-text-muted)]">
							Use this in staging or demos when you need realistic recipients without importing a
							real directory. It creates a marked test cohort and saves it as this wave's recipient
							selection.
						</p>
					</div>
					<p class="step-pill" data-state={testRecipientState}>{stepLabel(testRecipientState)}</p>
				</div>
				<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(8rem,12rem)_auto]">
					<label class="field">
						<span>Group name</span>
						<input
							value={testRecipientGroupName}
							disabled={testRecipientState === 'submitting'}
							oninput={(event) => (testRecipientGroupName = event.currentTarget.value)}
						/>
					</label>
					<label class="field">
						<span>People</span>
						<input
							type="number"
							min="1"
							max="1000"
							bind:value={testRecipientCount}
							disabled={testRecipientState === 'submitting'}
						/>
					</label>
					<button
						type="button"
						class="secondary-button self-end"
						disabled={!canManageSetup || testRecipientState === 'submitting'}
						onclick={createTestRecipients}
					>
						{#if testRecipientState === 'submitting'}
							<LoaderCircle size={16} aria-hidden="true" />
						{:else}
							<Plus size={16} aria-hidden="true" />
						{/if}
						<span>Create test recipients</span>
					</button>
				</div>
				{#if testRecipientError}
					<p class="error-line" role="alert">{testRecipientError}</p>
				{/if}
				{#if testRecipientResult}
					<p class="result-line">
						<span>Test cohort saved</span>
						<span>
							{testRecipientResult.groupName} -
							{formatCount(testRecipientResult.createdSubjectCount)} recipients
						</span>
					</p>
				{/if}
			</div>

			<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(8rem,12rem)]">
				<label class="field">
					<span>Send invitations to</span>
					<select bind:value={previewRuleKind} disabled={previewState === 'submitting'}>
						<option value="all_in_group">{audienceRuleLabel('all_in_group')}</option>
						<option value="self">{audienceRuleLabel('self')}</option>
						<option value="manager_of_target">{audienceRuleLabel('manager_of_target')}</option>
						<option value="reports_of_target">{audienceRuleLabel('reports_of_target')}</option>
						<option value="external_emails">{audienceRuleLabel('external_emails')}</option>
					</select>
					<span class="text-xs leading-5 text-[var(--color-text-muted)]">
						{audienceRuleHelp(previewRuleKind)}
					</span>
				</label>

				{#if previewUsesExternalEmails}
					<div class="record-row lg:col-span-2">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">Campaign-local recipients</p>
								<h5 class="record-row__title">Build a one-off recipient list</h5>
							</div>
							<span
								class="step-pill"
								data-state={previewExternalEmailReview.hasBlockingIssues ? 'failed' : 'idle'}
							>
								{formatCount(previewExternalEmailReview.validRecipientCount)} ready
							</span>
						</div>
						<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto]">
							<label class="field">
								<span>Name for review</span>
								<input
									value={previewManualRecipientName}
									placeholder="Bo Horvat"
									disabled={previewState === 'submitting'}
									oninput={(event) => (previewManualRecipientName = event.currentTarget.value)}
								/>
							</label>
							<label class="field">
								<span>Email</span>
								<input
									type="email"
									value={previewManualRecipientEmail}
									placeholder="bo@example.com"
									disabled={previewState === 'submitting'}
									oninput={(event) => (previewManualRecipientEmail = event.currentTarget.value)}
								/>
							</label>
							<button
								type="button"
								class="secondary-button self-end"
								disabled={previewState === 'submitting'}
								onclick={addPreviewManualRecipient}
							>
								<Plus size={16} aria-hidden="true" />
								<span>Add person</span>
							</button>
						</div>
						{#if previewManualRecipientError}
							<p class="error-line" role="alert">{previewManualRecipientError}</p>
						{/if}
						<label class="field">
							<span>Import recipients</span>
							<input
								type="file"
								accept=".csv,.txt,text/csv,text/plain"
								disabled={previewState === 'submitting'}
								onchange={(event) => loadPreviewExternalEmailFile(event.currentTarget.files?.[0])}
							/>
							<span class="text-xs leading-5 text-[var(--color-text-muted)]">
								Use a class list, cohort list, HR export, or spreadsheet with an email column
								when this wave has a one-time recipient list. For repeated waves or reusable cohorts,
								import people and groups in Directory instead. Limit:
								{formatCount(maxRecipientImportRecipients)} recipients per wave update.
							</span>
						</label>
						<details>
							<summary class="record-row__title">Review or paste source list</summary>
							<label class="field mt-3">
								<span>Recipient source</span>
								<textarea
									rows="5"
									value={previewExternalEmailText}
									placeholder={'ada@example.com\nBo Horvat <bo@example.com>\ncarla@example.com; diego@example.com'}
									disabled={previewState === 'submitting'}
									oninput={(event) => (previewExternalEmailText = event.currentTarget.value)}
								></textarea>
								<span class="text-xs leading-5 text-[var(--color-text-muted)]">
									{formatCount(previewExternalEmailReview.validRecipientCount)} ready,
									{formatCount(previewExternalEmailReview.invalidCount)} invalid,
									{formatCount(previewExternalEmailReview.duplicateCount)} duplicate.
								</span>
							</label>
						</details>
						<div class="action-row">
							<button
								type="button"
								class="secondary-button"
								disabled={!previewExternalEmailReview.hasBlockingIssues || previewState === 'submitting'}
								onclick={keepOnlyValidPreviewRecipients}
							>
								<RefreshCw size={16} aria-hidden="true" />
								<span>Keep valid only</span>
							</button>
							<button
								type="button"
								class="secondary-button"
								disabled={previewExternalEmailReview.rows.length === 0 || previewState === 'submitting'}
								onclick={clearPreviewRecipients}
							>
								<Trash2 size={16} aria-hidden="true" />
								<span>Clear list</span>
							</button>
						</div>
						{#if previewExternalEmailFileError}
							<p class="error-line" role="alert">{previewExternalEmailFileError}</p>
						{/if}
					</div>
				{:else if previewRequiresGroup}
					{#if previewGroups.length === 0}
						<div class="record-field">
							<p class="record-field__label">Directory group</p>
							<p class="record-field__value">No groups available</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								Create a reusable cohort, department, class, or location in Directory, or switch to
								one-off email import for this wave only.
							</p>
							<a class="secondary-button mt-3" href="/app/directory">Open Directory</a>
						</div>
					{:else}
						<label class="field">
							<span>Directory group</span>
							<select
								bind:value={previewGroupId}
								disabled={previewGroups.length === 0 || previewState === 'submitting'}
							>
								{#each previewGroups as group (group.id)}
									<option value={group.id}>{group.name}</option>
								{/each}
							</select>
						</label>
					{/if}
				{:else if previewRequiresTarget}
					<label class="field">
						<span>Focus person</span>
						<select
							bind:value={previewTargetSubjectId}
							disabled={previewSubjects.length === 0 || previewState === 'submitting'}
						>
							{#each previewSubjects as subject (subject.id)}
								<option value={subject.id}>{previewSubjectLabel(subject)}</option>
							{/each}
						</select>
					</label>
				{:else}
					<div class="record-field">
						<p class="record-field__label">Directory people</p>
						<p class="record-field__value">
							{previewSubjects.length
								? `${previewSubjects.length} active people loaded`
								: 'No active people loaded yet'}
						</p>
						<p class="text-sm text-[var(--color-text-muted)]">
							This selection is broad. Use a Directory group when the wave should only reach a
							department, cohort, class, or location.
						</p>
					</div>
				{/if}

				<label class="field">
					<span>Preview rows</span>
					<input
						type="number"
						min="1"
						max="200"
						bind:value={previewMaxRows}
						disabled={previewState === 'submitting'}
					/>
				</label>
			</div>

			<div class="action-row">
				<button type="button" class="primary-button" disabled={!canRunPreview} onclick={previewRespondentRule}>
					{#if previewState === 'submitting'}
						<LoaderCircle size={17} aria-hidden="true" />
					{:else}
						<SearchCheck size={17} aria-hidden="true" />
					{/if}
					<span>Preview recipients</span>
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={!canSaveCurrentRule}
					onclick={saveCurrentRespondentRule}
				>
					{#if savedRuleState === 'submitting'}
						<LoaderCircle size={16} aria-hidden="true" />
					{:else}
						<Send size={16} aria-hidden="true" />
					{/if}
					<span>Save previewed recipients</span>
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={previewOptionsLoading || previewState === 'submitting'}
					onclick={() => {
						previewOptionsLoadAttempted = false;
						void loadPreviewOptions();
					}}
				>
					{#if previewOptionsLoading}
						<LoaderCircle size={16} aria-hidden="true" />
					{:else}
						<RefreshCw size={16} aria-hidden="true" />
					{/if}
					<span>Refresh directory</span>
				</button>
			</div>

			{#if previewOptionsError}
				<p class="error-line" role="alert">{previewOptionsError}</p>
			{/if}
			{#if previewError}
				<p class="error-line" role="alert">{previewError}</p>
			{/if}

			{#if previewResult}
				<p class="result-line">
					<span>Previewed selection</span>
					<span>
						{audienceRuleLabel(previewRuleKind)} - {previewResult.summary.respondentCount}
						{previewResult.summary.respondentCount === 1 ? 'recipient' : 'recipients'} found
					</span>
				</p>
				<div class="record-grid">
					<div class="record-field">
						<p class="record-field__label">Recipients found</p>
						<p class="record-field__value">{previewResult.summary.respondentCount}</p>
					</div>
					<div class="record-field">
						<p class="record-field__label">Invitation rows</p>
						<p class="record-field__value">{previewResult.summary.assignmentPairCount}</p>
					</div>
					<div class="record-field">
						<p class="record-field__label">Preview capped</p>
						<p class="record-field__value">{previewResult.summary.truncated ? 'Yes' : 'No'}</p>
					</div>
				</div>

				{#if previewResult.warnings.length}
					<ul class="grid gap-2" aria-label="Recipient preview warnings">
						{#each previewResult.warnings as warning}
							<li class="text-sm text-[var(--color-text-muted)]">
								{audienceWarningLabel(warning)}
							</li>
						{/each}
					</ul>
				{/if}

				<div class="grid gap-2">
					{#if previewResult.rows.length === 0}
						<p class="text-sm text-[var(--color-text-muted)]">No people to show yet.</p>
					{:else}
						{#each previewResult.rows as row (row.ordinal)}
							<div class="record-field">
								<p class="record-field__label">#{row.ordinal} {recipientRoleLabel(row.role)}</p>
								<p class="record-field__value">
									{previewPairLabel(row)}
								</p>
								{#if row.respondent?.email || row.respondent?.externalId}
									<p class="text-sm text-[var(--color-text-muted)]">
										{row.respondent.email ?? row.respondent.externalId}
									</p>
								{/if}
							</div>
						{/each}
					{/if}
				</div>
			{/if}
		</section>

		{#if savedRuleResult?.rules.length || savedRuleError}
		<section class="record-row setup-current-task" aria-labelledby="saved-recipient-selection-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Saved recipients</p>
					<h4 id="saved-recipient-selection-heading" class="record-row__title">Saved recipient selection</h4>
					<p class="setup-current-task__title">{savedAudienceSummary()}</p>
				</div>
				<p class="step-pill" data-state={savedRuleState}>{stepLabel(savedRuleState)}</p>
			</div>

			{#if savedRuleError}
				<p class="error-line" role="alert">{savedRuleError}</p>
			{:else if savedRuleResult?.rules.length}
				<div class="grid gap-2">
					{#each savedRuleResult.rules as rule (rule.id)}
						<div class="record-field">
							<p class="record-field__label">Selection #{rule.ordinal}</p>
							<p class="record-field__value">{savedRecipientSelectionLabel(rule)}</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{savedRecipientSelectionDetail(rule)}
							</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{pairCountLabel(rule.assignmentPairCount)}
							</p>
							{#if rule.issues.length}
								<ul class="grid gap-1" aria-label="Saved recipient selection issues">
									{#each rule.issues as issue}
										<li class="error-line">{launchIssueLabel(issue)}</li>
									{/each}
								</ul>
							{/if}
						</div>
					{/each}
				</div>
			{:else}
				<p class="text-sm text-[var(--color-text-muted)]">
					Save a recipient selection after the preview looks right.
				</p>
			{/if}
		</section>
		{/if}

		{#if assignmentResult?.assignmentCount || assignmentError}
		<section class="record-row setup-current-task" aria-labelledby="prepared-invitation-roster-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Invitation roster</p>
					<h4 id="prepared-invitation-roster-heading" class="record-row__title">Prepared invitation roster</h4>
					<p class="setup-current-task__title">{deliveryRosterSummary()}</p>
				</div>
				<p class="step-pill" data-state={assignmentState}>{stepLabel(assignmentState)}</p>
			</div>

			{#if assignmentError}
				<p class="error-line" role="alert">{assignmentError}</p>
			{:else if assignmentResult?.assignments.length}
				<div class="grid gap-2">
					{#each assignmentResult.assignments as assignment (assignment.id)}
						<div class="record-field">
							<p class="record-field__label">{recipientRoleLabel(assignment.role)}</p>
							<p class="record-field__value">{assignmentPairLabel(assignment)}</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{assignment.respondent?.email ?? assignment.respondent?.externalId ?? 'No contact'}
							</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{assignment.status}
							</p>
						</div>
					{/each}
				</div>
			{:else}
				<p class="text-sm text-[var(--color-text-muted)]">
					Invitations are prepared after the saved selection resolves to active people.
				</p>
			{/if}
		</section>
		{/if}
		{/if}
	{/if}
</section>

{#snippet SetupPath()}
	<div class="setup-path" aria-label="Setup path">
		{#each setupPath.steps as step}
			{@const display = setupPathStepDisplay(step)}
			<button
				type="button"
				class="setup-path__item"
				data-state={display.state}
				disabled={!canSelectSetupAction(step.id)}
				aria-current={step.id === activeActionIdForView ? 'step' : undefined}
				onclick={() => selectSetupAction(step.id)}
			>
				<span class="setup-path__marker" aria-hidden="true">{step.step.replace('Step ', '')}</span>
				<span class="setup-path__content">
					<span class="setup-path__title">{step.title}</span>
					<span class="setup-path__description">{step.description}</span>
				</span>
				<span class="setup-path__state">{display.label}</span>
			</button>
		{/each}
	</div>
{/snippet}


{#snippet ActionFooter({
	id,
	label,
	icon,
	onclick
}: {
	id: SelectedSeriesSetupWorkflowActionId;
	label: string;
	icon: 'plus' | 'send' | 'search';
	onclick: () => void | Promise<void>;
})}
	<div class="action-row">
		<button
			type="button"
			class="primary-button"
			disabled={isActionDisabled(id)}
			title={actionDisabledReason(id)}
			{onclick}
		>
			{#if actionStates[id] === 'submitting'}
				<LoaderCircle size={17} aria-hidden="true" />
			{:else if icon === 'plus'}
				<Plus size={17} aria-hidden="true" />
			{:else if icon === 'search'}
				<SearchCheck size={17} aria-hidden="true" />
			{:else}
				<Send size={17} aria-hidden="true" />
			{/if}
			<span>{label}</span>
		</button>
		<p class="step-pill" data-state={actionStates[id]}>{stepLabel(actionStates[id])}</p>
	</div>
	{#if actionErrors[id]}
		<p class="error-line">{actionErrors[id]}</p>
	{/if}
{/snippet}

