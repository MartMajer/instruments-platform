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
		toSelectedSeriesSetupDesignMap,
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
	const setupDesignMap = $derived(
		toSelectedSeriesSetupDesignMap(workspace, localState, setupWorkflowCopy)
	);
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
					? appLocale === 'hr-HR'
						? 'Odabran val prikupljanja'
						: 'Collection wave selected'
					: appLocale === 'hr-HR'
						? 'Nije odabran uređivi val prikupljanja'
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
				scoring: setupUi('Save the questionnaire first.')
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
				campaign: setupUi('Save the questionnaire first.')
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
				readiness: setupUi('Create the collection wave first.')
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
			previewOptionsError = toProductApiErrorMessage(error, setupUi('Recipient preview options failed to load.'));
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
			previewError = setupUi('Create the collection wave first.');
			return;
		}

		if (previewRequiresGroup && !previewGroupId) {
			previewError = setupUi('Select a subject group.');
			return;
		}

		if (previewRequiresTarget && !previewTargetSubjectId) {
			previewError = setupUi('Select a target subject.');
			return;
		}

		if (
			previewUsesExternalEmails &&
			(previewExternalEmailReview.validRecipientCount === 0 ||
				previewExternalEmailReview.hasBlockingIssues)
		) {
			previewError = setupUi('Add at least one valid email and remove invalid or duplicate rows.');
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
			previewError = toProductApiErrorMessage(error, setupUi('Recipient preview failed.'));
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
				savedRuleError = toProductApiErrorMessage(error, setupUi('Saved recipient selections failed to load.'));
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
				assignmentError = toProductApiErrorMessage(error, setupUi('Campaign assignments failed to load.'));
			}
		}
	}

	async function saveCurrentRespondentRule() {
		const campaignId = selectedCampaignId;
		if (!campaignId) {
			savedRuleError = setupUi('Create the collection wave first.');
			return;
		}

		if (previewRequiresGroup && !previewGroupId) {
			savedRuleError = setupUi('Select a subject group.');
			return;
		}

		if (previewRequiresTarget && !previewTargetSubjectId) {
			savedRuleError = setupUi('Select a target subject.');
			return;
		}

		if (
			previewUsesExternalEmails &&
			(previewExternalEmailReview.validRecipientCount === 0 ||
				previewExternalEmailReview.hasBlockingIssues)
		) {
			savedRuleError = setupUi('Add at least one valid email and remove invalid or duplicate rows.');
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
			savedRuleError = toProductApiErrorMessage(error, setupUi('Respondent rule save failed.'));
		}
	}

	async function createTestRecipients() {
		const campaignId = selectedCampaignId;
		if (!campaignId) {
			testRecipientError = setupUi('Create the collection wave first.');
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
				refreshWarning = setupUi('Test recipients were saved, but this setup view could not refresh.');
			}
		} catch (error) {
			testRecipientState = 'failed';
			testRecipientError = toProductApiErrorMessage(error, setupUi('Test recipients could not be created.'));
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
				error instanceof Error ? error.message : setupUi('Recipient file could not be read.');
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
			previewManualRecipientError = setupUi('Enter one valid email address.');
			return;
		}

		if (previewExternalEmailReview.recipients.some((item) => item.email === recipient.email)) {
			previewManualRecipientError = setupUi('This recipient is already in the wave list.');
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
				refreshWarning = setupUi('Setup action saved, but the setup workspace refresh failed.');
			}
			return result;
		} catch (error) {
			actionStates = { ...actionStates, [actionId]: 'failed' };
			actionErrors = {
				...actionErrors,
				[actionId]: toProductApiErrorMessage(error, setupUi('Setup action failed.'))
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
			scaleLowLabel: type === 'nps' ? setupUi('Not at all likely') : current.scaleLowLabel,
			scaleHighLabel: type === 'nps' ? setupUi('Extremely likely') : current.scaleHighLabel,
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

	function updateMatrixRows(rowIndex: number, value: string) {
		updateTemplateQuestionRow(rowIndex, {
			matrixRows: value
				.split('\n')
				.map((row) => row.trim())
				.filter(Boolean)
		});
	}

	function updateMatrixColumns(rowIndex: number, value: string) {
		updateTemplateQuestionRow(rowIndex, {
			matrixColumns: value
				.split('\n')
				.map((column) => column.trim())
				.filter(Boolean)
		});
	}

	function displayLogicSourceQuestions(rowIndex: number) {
		return templateQuestionRows
			.slice(0, rowIndex)
			.filter((question) => question.type === 'single' && question.choiceOptions.length > 0);
	}

	function displayLogicSourceOptions(sourceQuestionCode: string) {
		const source = templateQuestionRows.find(
			(question) =>
				question.code.trim().toLowerCase() === sourceQuestionCode.trim().toLowerCase() &&
				question.type === 'single'
		);

		return (source?.choiceOptions ?? [])
			.map((label, optionIndex) => ({
				code: `o${String(optionIndex + 1).padStart(2, '0')}`,
				label
			}))
			.filter((option) => option.label.trim());
	}

	function updateDisplayLogicEnabled(rowIndex: number, enabled: boolean) {
		if (!enabled) {
			updateTemplateQuestionRow(rowIndex, {
				displayLogicEnabled: false,
				displayLogicSourceQuestionCode: '',
				displayLogicSourceOptionCode: ''
			});
			return;
		}

		const source = displayLogicSourceQuestions(rowIndex)[0];
		const optionCode = source ? displayLogicSourceOptions(source.code)[0]?.code ?? '' : '';
		updateTemplateQuestionRow(rowIndex, {
			displayLogicEnabled: true,
			displayLogicSourceQuestionCode: source?.code ?? '',
			displayLogicSourceOptionCode: optionCode
		});
	}

	function updateDisplayLogicSource(rowIndex: number, sourceQuestionCode: string) {
		updateTemplateQuestionRow(rowIndex, {
			displayLogicSourceQuestionCode: sourceQuestionCode,
			displayLogicSourceOptionCode: displayLogicSourceOptions(sourceQuestionCode)[0]?.code ?? ''
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
			ranking: 'Ranking',
			matrix: 'Matrix / grid'
		};
		return setupUi(labels[type] ?? 'Question');
	}

	function questionPreviewDetail(question: TemplateQuestionAuthoringRow) {
		if (isScaleQuestion(question)) {
			return setupUi(`${question.scaleMin} to ${question.scaleMax}: ${question.scaleLowLabel} -> ${question.scaleHighLabel}`);
		}

		if (question.type === 'single') {
			return setupUi(`Choose one: ${question.choiceOptions.join(', ')}`);
		}

		if (question.type === 'multi') {
			return setupUi(`Choose any: ${question.choiceOptions.join(', ')}`);
		}

		if (question.type === 'number') {
			return setupUi('Number response');
		}

		if (question.type === 'date') {
			return setupUi('Date response');
		}

		if (question.type === 'ranking') {
			return setupUi(`Rank options: ${question.choiceOptions.join(', ')}`);
		}

		if (question.type === 'matrix') {
			return setupUi(
				`Matrix rows: ${question.matrixRows.join(', ')}; columns: ${question.matrixColumns.join(', ')}`
			);
		}

		return setupUi('Text response');
	}

	function questionScoringDetail(question: TemplateQuestionAuthoringRow) {
		const detail = describeQuestionScoringDirection(question);
		return {
			...detail,
			label: setupUi(detail.label),
			detail: setupUi(detail.detail)
		};
	}

	function questionScaleIntent(question: TemplateQuestionAuthoringRow) {
		const detail = describeQuestionScaleIntent(question);
		return {
			...detail,
			label: setupUi(detail.label),
			detail: setupUi(detail.detail)
		};
	}

	function questionResultUsage(question: TemplateQuestionAuthoringRow) {
		return setupUi(describeQuestionResultUsage(question, scoreOutputs));
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
		return localizeQuestionAuthoringSummary(code);
	}

	function dimensionCoverageLabel(labels: string[]) {
		return labels.length ? labels.join(', ') : setupUi('No questionnaire dimensions selected');
	}

	function reverseScoredCountLabel(count: number) {
		if (count === 0) {
			return setupUi('No reverse-scored questions');
		}

		return appLocale === 'hr-HR'
			? `${formatCount(count)} ${count === 1 ? 'obrnuto bodovano pitanje' : 'obrnuto bodovana pitanja'}`
			: `${formatCount(count)} reverse-scored ${count === 1 ? 'question' : 'questions'}`;
	}

	function scoreOutputMissingDataDetail(localId: string) {
		const output = scoreOutputs.find((candidate) => candidate.localId === localId);
		return output
			? setupUi(describeScoreMissingDataStrategy(output).detail)
			: setupUi('Missing-data rule not configured.');
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

	function designMapStatusLabel(status: string) {
		if (status === 'ready') {
			return setupBodyCopy.status.ready;
		}

		if (status === 'blocked') {
			return setupUi('Blocked');
		}

		if (status === 'pending') {
			return setupUi('Needs attention');
		}

		return setupUi(status);
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
			return setupUi('No subject selected');
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
			return setupUi('Loading invitation roster...');
		}

		if (!assignmentResult || assignmentResult.assignmentCount === 0) {
			return setupUi('No invitations prepared yet.');
		}

		return assignmentCountLabel(assignmentResult.assignmentCount);
	}

	function assignmentPairLabel(assignment: CampaignAssignmentResponse) {
		if (assignment.role === 'email_recipient') {
			return assignment.respondent?.label ?? setupUi('Email recipient');
		}

		return `${assignment.target?.label ?? setupUi('Study recipients')} ${setupUi('to')} ${
			assignment.respondent?.label ?? setupUi('No respondent')
		}`;
	}

	function previewPairLabel(row: PreviewRuleRow) {
		if (row.role === 'email_recipient') {
			return row.respondent?.label ?? setupUi('Email recipient');
		}

		return `${row.target?.label ?? setupUi('Study recipients')} ${setupUi('to')} ${row.respondent?.label ?? setupUi('No respondent')}`;
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
				: setupUi('Study recipients');

		return selector;
	}

	function externalEmailRuleDetail(rule: string) {
		try {
			const parsed = JSON.parse(rule) as { emails?: string[] };
			const count = Array.isArray(parsed.emails) ? parsed.emails.length : 0;
			return `${formatCount(count)} ${count === 1 ? setupUi('email recipient') : setupUi('email recipients')}`;
		} catch {
			return setupUi('Email recipient list');
		}
	}

	function previewSubjectLabelById(subjectId: string | null | undefined) {
		if (!subjectId) {
			return setupUi('Study recipients');
		}

		return previewSubjectLabel(previewSubjects.find((subject) => subject.id === subjectId));
	}

	function previewGroupLabelById(groupId: string | null | undefined) {
		if (!groupId) {
			return setupUi('Study recipients');
		}

		return previewGroups.find((group) => group.id === groupId)?.name ?? setupUi('Selected group');
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
		return `${formatCount(count)} ${count === 1 ? setupUi('invitation pair') : setupUi('invitation pairs')}`;
	}

	function ruleCountLabel(count: number) {
		return `${formatCount(count)} ${count === 1 ? setupUi('selection') : setupUi('selections')}`;
	}

	function assignmentCountLabel(count: number) {
		return `${formatCount(count)} ${count === 1 ? setupUi('invitation') : setupUi('invitations')}`;
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
		return setupUi(value === 'sum' ? 'sum' : 'average');
	}

	function missingPolicyLabel(output: ScoreOutputAuthoringRow) {
		if (output.missingStrategy === 'min_valid_count') {
			return setupUi(`A score is allowed when at least ${output.minValidCount} selected ${
				output.minValidCount === 1 ? 'question is' : 'questions are'
			} answered.`);
		}

		return setupUi('Every selected question must be answered.');
	}

	function setupUi(value: string): string {
		if (appLocale !== 'hr-HR') {
			return value;
		}

		const exact = hrSetupUiStrings[value];
		if (exact) {
			return exact;
		}

		return value
			.replace(/^Question (\d+) of (\d+)$/u, 'Pitanje $1 od $2')
			.replace(/^Question (\d+)$/u, 'Pitanje $1')
			.replace(/^Result (\d+)$/u, 'Rezultat $1')
			.replace(/^(\d+) questions$/u, (_, count: string) => setupQuestionCount(Number(count)))
			.replace(/^(\d+) dimensions$/u, (_, count: string) => setupDimensionCount(Number(count)))
			.replace(/^(\d+) selected questions$/u, (_, count: string) =>
				setupSelectedQuestionCount(Number(count))
			)
			.replace(/^(\d+) result (output|outputs), (\d+) scored (question|questions), (\d+) reversed$/u, (_, outputs: string, _outputWord: string, scored: string, _questionWord: string, reversed: string) =>
				`${outputs} ${Number(outputs) === 1 ? 'izlaz rezultata' : 'izlaza rezultata'}, ${scored} ${
					Number(scored) === 1 ? 'bodovano pitanje' : 'bodovana pitanja'
				}, ${reversed} obrnuto`
			)
			.replace(/^(\d+) dimensions?, (\d+) scored questions?, (\d+) result outputs?$/u, (_, dimensions: string, scored: string, outputs: string) =>
				`${dimensions} ${Number(dimensions) === 1 ? 'dimenzija' : 'dimenzije'}, ${scored} ${
					Number(scored) === 1 ? 'bodovano pitanje' : 'bodovana pitanja'
				}, ${outputs} ${Number(outputs) === 1 ? 'izlaz rezultata' : 'izlaza rezultata'}`
			)
			.replace(/^(\d+) constructs?, (\d+) questions?, (\d+) required$/u, (_, constructs: string, questions: string, required: string) =>
				`${constructs} ${Number(constructs) === 1 ? 'konstrukt' : 'konstrukta'}, ${questions} ${
					Number(questions) === 1 ? 'pitanje' : 'pitanja'
				}, ${required} obavezno`
			)
			.replace(/^(\d+) result outputs? will be saved: (.+)\.$/u, (_, count: string, names: string) =>
				`${count} ${Number(count) === 1 ? 'izlaz rezultata bit će spremljen' : 'izlaza rezultata bit će spremljeno'}: ${names}.`
			)
			.replace(/^(\d+) of (\d+) scoreable questions? (is|are) included in at least one result output\.$/u, '$1 od $2 bodovanih pitanja uključeno je u barem jedan izlaz rezultata.')
			.replace(/^(\d+) scored question feeds (\d+) result output\.$/u, '$1 bodovano pitanje puni $2 izlaz rezultata.')
			.replace(/^(\d+) scored questions feed (\d+) result outputs\.$/u, '$1 bodovana pitanja pune $2 izlaza rezultata.')
			.replace(/^(\d+) required, (\d+) optional\.$/u, '$1 obavezno, $2 neobavezno.')
			.replace(/^Questions are grouped into (.+)\.$/u, 'Pitanja su grupirana u $1.')
			.replace(/^Respondents answer (\d+) questions? in order, from "(.+)" to "(.+)"$/u, 'Ispitanici odgovaraju na $1 pitanja redom, od "$2" do "$3"')
			.replace(/^Used in: (.+)\.$/u, 'Koristi se u: $1.')
			.replace(/^(\d+) \((.+)\) to (\d+) \((.+)\) is used as entered in every result output that includes this question\.$/u, '$1 ($2) do $3 ($4) koristi se kako je uneseno u svakom izlazu rezultata koji uključuje ovo pitanje.')
			.replace(/^(\d+) \((.+)\) is converted toward (\d+); (\d+) \((.+)\) is converted toward (\d+)\. Use this for protective wording when the result score should still point in one direction\.$/u, '$1 ($2) pretvara se prema $3; $4 ($5) pretvara se prema $6. Koristite za zaštitno formulirana pitanja kada rezultat i dalje treba pokazivati u jednom smjeru.')
			.replace(/^A respondent needs at least (\d+) selected questions answered for this result score\.$/u, 'Ispitanik treba barem $1 odabranih odgovorenih pitanja za ovaj rezultat.')
			.replace(/^Requires at least (\d+) selected questions$/u, 'Zahtijeva barem $1 odabranih pitanja')
			.replace(/^A score is allowed when at least (\d+) selected questions are answered\.$/u, 'Rezultat je dopušten kada je odgovoreno na barem $1 odabranih pitanja.')
			.replace(/^A score is allowed when at least (\d+) selected question is answered\.$/u, 'Rezultat je dopušten kada je odgovoreno na barem $1 odabrano pitanje.')
			.replace(/^Uses (.+) from$/u, 'Koristi $1 iz')
			.replace(/^Choose one: (.+)$/u, 'Odaberite jedno: $1')
			.replace(/^Choose any: (.+)$/u, 'Odaberite više: $1')
			.replace(/^Rank options: (.+)$/u, 'Poredajte opcije: $1')
			.replace(/^(\d+) to (\d+): (.+)$/u, '$1 do $2: $3');
	}

	function setupQuestionCount(count: number): string {
		return appLocale === 'hr-HR'
			? `${formatCount(count)} ${count === 1 ? 'pitanje' : 'pitanja'}`
			: `${formatCount(count)} ${count === 1 ? 'question' : 'questions'}`;
	}

	function setupDimensionCount(count: number): string {
		return appLocale === 'hr-HR'
			? `${formatCount(count)} ${count === 1 ? 'dimenzija' : 'dimenzije'}`
			: `${formatCount(count)} ${count === 1 ? 'dimension' : 'dimensions'}`;
	}

	function setupSelectedQuestionCount(count: number): string {
		return appLocale === 'hr-HR'
			? `${formatCount(count)} ${count === 1 ? 'odabrano pitanje' : 'odabrana pitanja'}`
			: `${formatCount(count)} selected ${count === 1 ? 'question' : 'questions'}`;
	}

	function setupContextQuestionSummary(count: number): string {
		if (appLocale !== 'hr-HR') {
			return `${formatCount(count)} ${count === 1 ? 'context question is' : 'context questions are'} collected but not scored.`;
		}

		return `${formatCount(count)} ${count === 1 ? 'kontekstualno pitanje prikuplja se' : 'kontekstualna pitanja prikupljaju se'} bez bodovanja.`;
	}

	function setupQuestionnaireSavedSummary(count: number): string {
		if (appLocale !== 'hr-HR') {
			return `${formatCount(count)} ${count === 1 ? 'question is' : 'questions are'} saved. Continue to scoring.`;
		}

		return `${formatCount(count)} ${count === 1 ? 'pitanje je spremljeno' : 'pitanja su spremljena'}. Nastavite na postavljanje rezultata.`;
	}

	function setupResultOutputsList(outputs: string[]) {
		return outputs.map(setupUi).join(', ');
	}

	function localizeQuestionAuthoringSummary(code: string) {
		const summary = questionAuthoringSummaries.find((candidate) => candidate.code === code);
		return summary
			? {
					...summary,
					scaleLabel: setupUi(summary.scaleLabel),
					requiredLabel: setupUi(summary.requiredLabel),
					resultUsageLabel: setupUi(summary.resultUsageLabel)
				}
			: null;
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
			return setupUi(issue.message.replace('campaign', 'collection wave'));
		}

		if (issue.code.includes('template')) {
			return setupUi(issue.message.replace('Template', 'Questionnaire').replace('template', 'questionnaire'));
		}

		if (issue.code.includes('scoring')) {
			return setupUi(issue.message.replace('Scoring rule', 'Results setup').replace('scoring rule', 'results setup'));
		}

		if (issue.code === 'respondent_rule.email_required') {
			return setupUi('Every saved Directory recipient needs an email address before invite-only collection can start.');
		}

		if (issue.code === 'respondent_rule.no_recipients') {
			return setupUi('Save at least one recipient selection before launch, and make sure it resolves to active people.');
		}

		if (issue.code === 'respondent_rule.identity_mode_not_supported') {
			return setupUi('Specific email lists are available for anonymous invite-only or repeat-participation waves only.');
		}

		return setupUi(issue.message);
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

	const hrSetupUiStrings: Record<string, string> = {
		'Read-only access': 'Pristup samo za čitanje',
		'Setup workflow actions require setup management access.':
			'Radnje postavljanja zahtijevaju pristup za upravljanje postavljanjem.',
		Done: 'Dovršeno',
		Editable: 'Uredivo',
		'Study source ready': 'Izvor studije spreman',
		'The study source is saved. Continue to the questionnaire.':
			'Izvor studije je spremljen. Nastavite na upitnik.',
		'Study source name': 'Naziv izvora studije',
		'Save study source': 'Spremi izvor studije',
		'Questionnaire ready': 'Upitnik spreman',
		'Questionnaire name': 'Naziv upitnika',
		Language: 'Jezik',
		English: 'Engleski',
		Croatian: 'Hrvatski',
		'Questionnaire palette': 'Paleta upitnika',
		'Choose an editable question set': 'Odaberite uređivi skup pitanja',
		'Start blank, or load original editable starter items that match the study you are building. These are not marketed as validated named instruments; review and edit before launch.':
			'Krenite od praznog upitnika ili učitajte izvorne uređive početne stavke koje odgovaraju studiji koju gradite. Ne predstavljaju se kao validirani imenovani instrumenti; pregledajte ih i uredite prije pokretanja.',
		'Suggested results': 'Predloženi rezultati',
		'Custom result': 'Prilagođeni rezultat',
		'Workload strain': 'Radno opterećenje',
		'Recovery capacity': 'Kapacitet oporavka',
		'Work climate context': 'Kontekst radne klime',
		'Posture and repetition': 'Držanje i ponavljanje',
		'Discomfort severity': 'Jačina nelagode',
		'Recovery and control': 'Oporavak i kontrola',
		'Workstation strain': 'Opterećenje radnog mjesta',
		'Focus conditions': 'Uvjeti fokusa',
		'Academic workload strain': 'Akademsko opterećenje',
		'Support and clarity': 'Podrška i jasnoća',
		'Team climate': 'Timska klima',
		'Workload fairness': 'Pravednost opterećenja',
		'Shift strain': 'Opterećenje smjene',
		'Operational support': 'Operativna podrška',
		'Study model': 'Model studije',
		'Authoring summary': 'Sažetak izrade',
		'Questionnaire design review': 'Pregled dizajna upitnika',
		'Design review': 'Pregled dizajna',
		'Study dimensions': 'Dimenzije studije',
		'What this questionnaire measures': 'Što ovaj upitnik mjeri',
		Question: 'Pitanje',
		'Untitled question': 'Pitanje bez naslova',
		'Question text': 'Tekst pitanja',
		'Dimension / construct': 'Dimenzija / konstrukt',
		'Group questions by what they measure, for example workload, recovery, or autonomy.':
			'Grupirajte pitanja prema onome što mjere, primjerice opterećenje, oporavak ili autonomiju.',
		'Answer format': 'Format odgovora',
		'Rating scale': 'Ljestvica procjene',
		'0-10 recommendation scale': 'Ljestvica preporuke 0-10',
		'Single choice': 'Jedan odabir',
		'Multiple choice': 'Višestruki odabir',
		Number: 'Broj',
		Text: 'Tekst',
		Date: 'Datum',
		Ranking: 'Rangiranje',
		'Matrix / grid': 'Matrica / mreza',
		'Matrix rows and columns': 'Redci i stupci matrice',
		Rows: 'Redci',
		Columns: 'Stupci',
		Row: 'Redak',
		'Enter one row per line. Each row becomes a separate prompt in the grid.':
			'Unesite jedan redak po liniji. Svaki redak postaje zaseban upit u matrici.',
		'Enter one answer column per line. Respondents choose one column for each row.':
			'Unesite jedan odgovor po liniji. Ispitanici biraju jedan stupac za svaki redak.',
		'Display rule': 'Pravilo prikaza',
		'Show this question only after a specific answer':
			'Prikaži ovo pitanje samo nakon određenog odgovora',
		'Add an earlier single-choice question before creating a follow-up rule.':
			'Dodajte ranije pitanje s jednim odabirom prije izrade pravila za dodatno pitanje.',
		'Source question': 'Izvorno pitanje',
		'Source answer': 'Izvorni odgovor',
		'Hidden follow-up questions are saved as skipped answers and are required only when visible.':
			'Skrivena dodatna pitanja spremaju se kao preskočena i obavezna su samo kada su vidljiva.',
		'Scale values and labels': 'Vrijednosti i oznake ljestvice',
		'Scale preset': 'Predložak ljestvice',
		'Keep the current scale values and labels.': 'Zadrži trenutačne vrijednosti i oznake ljestvice.',
		'Lowest value': 'Najniža vrijednost',
		'Highest value': 'Najviša vrijednost',
		'Low label': 'Oznaka niskog kraja',
		'High label': 'Oznaka visokog kraja',
		'Not at all likely': 'Nimalo vjerojatno',
		'Extremely likely': 'Izrazito vjerojatno',
		'hours/week, kg, minutes...': 'sati/tjedan, kg, minute...',
		'Number rules': 'Pravila broja',
		Minimum: 'Minimum',
		Maximum: 'Maksimum',
		'Unit label': 'Oznaka jedinice',
		'Whole numbers only': 'Samo cijeli brojevi',
		'Text response rules': 'Pravila tekstualnog odgovora',
		'Long text answer': 'Dugi tekstualni odgovor',
		'Max characters': 'Najviše znakova',
		'Date rules': 'Pravila datuma',
		'Earliest date': 'Najraniji datum',
		'Latest date': 'Najkasniji datum',
		'Answer options': 'Opcije odgovora',
		Options: 'Opcije',
		'Enter one option per line.': 'Unesite jednu opciju po retku.',
		'Add an Other write-in option': 'Dodaj opciju Ostalo s upisom',
		'Other label': 'Oznaka opcije Ostalo',
		'Exclusive option': 'Isključiva opcija',
		'Example: None of these': 'Primjer: Ništa od navedenog',
		'Ranking rule': 'Pravilo rangiranja',
		Rank: 'Rang',
		'Rank all options': 'Rangiraj sve opcije',
		'Rank only top N': 'Rangiraj samo prvih N',
		'Top N': 'Prvih N',
		'Question order below is the order respondents will see. Scoring stays attached to question meaning, not the visual position.':
			'Redoslijed pitanja ispod je redoslijed koji će ispitanici vidjeti. Bodovanje ostaje vezano uz značenje pitanja, ne uz vizualnu poziciju.',
		Required: 'Obavezno',
		Optional: 'Neobavezno',
		'Reverse score this question': 'Obrnuto boduj ovo pitanje',
		'Move question earlier': 'Pomakni pitanje ranije',
		'Move earlier': 'Pomakni ranije',
		'Move question later': 'Pomakni pitanje kasnije',
		'Move later': 'Pomakni kasnije',
		'Duplicate question': 'Dupliciraj pitanje',
		Duplicate: 'Dupliciraj',
		'Remove question': 'Ukloni pitanje',
		Remove: 'Ukloni',
		'Answer scale': 'Ljestvica odgovora',
		'Scoring meaning': 'Značenje bodovanja',
		'Respondent preview': 'Pregled za ispitanika',
		'Runtime control': 'Kontrola za ispitanika',
		'Runtime notes': 'Napomene prikaza',
		'Limitations to review before launch.': 'Ograničenja koja treba pregledati prije pokretanja.',
		'This preview uses the same control families as the respondent SurveyJS runtime: rating, radio group, checkbox group, ranking, number, date, and text.':
			'Ovaj pregled koristi iste vrste kontrola kao ispitanikov SurveyJS prikaz: procjena, radio grupa, checkbox grupa, rangiranje, broj, datum i tekst.',
		'Number response': 'Brojčani odgovor',
		'Date response': 'Datumski odgovor',
		'Text response': 'Tekstualni odgovor',
		'Long text response': 'Dugi tekstualni odgovor',
		'This question cannot be rendered by the current respondent runtime.':
			'Ovo pitanje trenutačni prikaz za ispitanike ne može prikazati.',
		'Result outputs': 'Izlazi rezultata',
		Result: 'Rezultat',
		'Results setup ready': 'Postavljanje rezultata spremno',
		Outputs: 'Izlazi',
		'Unique scored questions': 'Jedinstvena bodovana pitanja',
		'Create one total score or several dimensions/subscales. Each output chooses its own questions, calculation, and missing-answer rule.':
			'Izradite jedan ukupni rezultat ili više dimenzija/podljestvica. Svaki izlaz bira vlastita pitanja, izračun i pravilo za nedostajuće odgovore.',
		'Results plan': 'Plan rezultata',
		'Remove result': 'Ukloni rezultat',
		'Result name': 'Naziv rezultata',
		'Result code': 'Kod rezultata',
		'Used as the report/export dimension code.': 'Koristi se kao kod dimenzije u izvještaju/izvozu.',
		Calculation: 'Izračun',
		'Average selected answers': 'Prosjek odabranih odgovora',
		'Sum selected answers': 'Zbroj odabranih odgovora',
		'Missing answers': 'Nedostajući odgovori',
		'Require every selected answer': 'Zahtijevaj svaki odabrani odgovor',
		'Allow a score after enough answers': 'Dopusti rezultat nakon dovoljno odgovora',
		'Minimum answered': 'Najmanje odgovoreno',
		'Questions in this result': 'Pitanja u ovom rezultatu',
		'Add a rating scale, recommendation scale, or number question before saving results.':
			'Dodajte ljestvicu procjene, ljestvicu preporuke ili brojčano pitanje prije spremanja rezultata.',
		'Add result output': 'Dodaj izlaz rezultata',
		'Collected but not scored': 'Prikupljeno bez bodovanja',
		'Scoring plan preview': 'Pregled plana bodovanja',
		'Reverse-scoring review': 'Pregled obrnutog bodovanja',
		'question is': 'pitanje je',
		'questions are': 'pitanja su',
		'reversed before scoring': 'obrnuto bodovana prije izračuna',
		Uses: 'Koristi',
		from: 'iz',
		Affects: 'Utječe na',
		'no result outputs yet': 'još nema izlaza rezultata',
		'Save questionnaire': 'Spremi upitnik',
		'Save results setup': 'Spremi postavljanje rezultata',
		'Save collection wave': 'Spremi val prikupljanja',
		'Run launch check': 'Pokreni provjeru za pokretanje',
		Ready: 'Spremno',
		Yes: 'Da',
		No: 'Ne',
		'Save questionnaire first': 'Najprije spremite upitnik',
		'Save results setup first': 'Najprije spremite postavljanje rezultata',
		'Collection wave ready': 'Val prikupljanja spreman',
		'Wave context': 'Kontekst vala',
		'Wave setup guidance': 'Upute za postavljanje vala',
		Wave: 'Val',
		'Response mode': 'Način odgovaranja',
		'Launch plan': 'Plan pokretanja',
		'Wave name': 'Naziv vala',
		'Respondent language': 'Jezik ispitanika',
		'Launch checklist': 'Kontrolna lista pokretanja',
		Questionnaire: 'Upitnik',
		'Results setup': 'Postavljanje rezultata',
		'Collection wave': 'Val prikupljanja',
		'Create collection wave first': 'Najprije izradite val prikupljanja',
		'Create the collection wave first.': 'Najprije izradite val prikupljanja.',
		'Save the questionnaire first.': 'Najprije spremite upitnik.',
		'Select a subject group.': 'Odaberite grupu osoba.',
		'Select a target subject.': 'Odaberite ciljnu osobu.',
		'Add at least one valid email and remove invalid or duplicate rows.':
			'Dodajte barem jednu valjanu email adresu i uklonite nevaljane ili duplicirane retke.',
		'Saved recipient selections failed to load.': 'Spremljeni odabiri primatelja nisu se mogli učitati.',
		'Recipient preview options failed to load.': 'Opcije pregleda primatelja nisu se mogle učitati.',
		'Recipient preview failed.': 'Pregled primatelja nije uspio.',
		'Campaign assignments failed to load.': 'Dodjele pozivnica nisu se mogle učitati.',
		'Respondent rule save failed.': 'Spremanje pravila primatelja nije uspjelo.',
		'Test recipients could not be created.': 'Testni primatelji nisu se mogli izraditi.',
		'Setup action failed.': 'Radnja postavljanja nije uspjela.',
		'Test recipients were saved, but this setup view could not refresh.':
			'Testni primatelji su spremljeni, ali se ovaj prikaz postavljanja nije mogao osvježiti.',
		'Recipient file could not be read.': 'Datoteka primatelja nije se mogla pročitati.',
		'Enter one valid email address.': 'Unesite jednu valjanu email adresu.',
		'This recipient is already in the wave list.': 'Ovaj primatelj već je na popisu vala.',
		'Setup action saved, but the setup workspace refresh failed.':
			'Radnja postavljanja je spremljena, ali osvježavanje radnog prostora nije uspjelo.',
		Status: 'Status',
		'Recipient selection': 'Odabir primatelja',
		'Launch checklist issues': 'Problemi kontrolne liste pokretanja',
		'Every saved Directory recipient needs an email address before invite-only collection can start.':
			'Svaki spremljeni primatelj iz Imenika mora imati email adresu prije nego što može početi prikupljanje samo preko pozivnica.',
		'Save at least one recipient selection before launch, and make sure it resolves to active people.':
			'Spremite barem jedan odabir primatelja prije pokretanja i provjerite da odabir pronalazi aktivne osobe.',
		'Specific email lists are available for anonymous invite-only or repeat-participation waves only.':
			'Posebni popisi email adresa dostupni su samo za anonimne valove s pozivnicom ili ponovljenim sudjelovanjem.',
		'Next action': 'Sljedeći korak',
		'Next step': 'Sljedeći korak',
		'Previous wave': 'Prethodni val',
		'Recipient selection is locked': 'Odabir primatelja je zaključan',
		'Preview recipients, then save the selection': 'Pregledajte primatelje, zatim spremite odabir',
		'Use Directory groups for recurring populations. Use all active Directory people only when the wave is truly for everyone. Use one-off email list for ad hoc recipients you do not want to manage in Directory. Save recipients before launch so Collection can create private respondent links and send email.':
			'Koristite grupe iz imenika za ponavljajuće populacije. Sve aktivne osobe koristite samo kada je val stvarno za sve. Jednokratni popis email adresa koristite za ad hoc primatelje koje ne želite voditi u imeniku. Spremite primatelje prije pokretanja kako bi Prikupljanje moglo izraditi privatne poveznice i poslati email.',
		'No recipient selection is saved yet. Preview recipients first, then save the previewed selection before launch.':
			'Odabir primatelja još nije spremljen. Najprije pregledajte primatelje, zatim spremite pregledani odabir prije pokretanja.',
		'Saved for launch': 'Spremljeno za pokretanje',
		'Reusable population': 'Ponovno upotrebljiva populacija',
		'Directory groups': 'Grupe iz imenika',
		'One-off population': 'Jednokratna populacija',
		'Email import': 'Uvoz email adresa',
		'Open participation': 'Otvoreno sudjelovanje',
		'Open link in Collection': 'Otvorena poveznica u Prikupljanju',
		'Demo/test data': 'Demo/testni podaci',
		'Create test recipients for this wave': 'Izradi testne primatelje za ovaj val',
		"Use this in staging or demos when you need realistic recipients without importing a real directory. It creates a marked test cohort and saves it as this wave's recipient selection.":
			'Koristite ovo u stagingu ili demo prikazima kada trebate realistične primatelje bez uvoza stvarnog imenika. Izrađuje označenu testnu skupinu i sprema je kao odabir primatelja za ovaj val.',
		'Group name': 'Naziv grupe',
		People: 'Osobe',
		'Create test recipients': 'Izradi testne primatelje',
		'Test cohort saved': 'Testna skupina spremljena',
		'Send invitations to': 'Pošalji pozivnice za',
		'Campaign-local recipients': 'Primatelji samo za ovaj val',
		'Build a one-off recipient list': 'Izradi jednokratni popis primatelja',
		ready: 'spremno',
		invalid: 'nevaljano',
		duplicate: 'duplikat',
		recipients: 'primatelja',
		'Name for review': 'Ime za pregled',
		Email: 'Email',
		'Add person': 'Dodaj osobu',
		'Import recipients': 'Uvezi primatelje',
		'Review or paste source list': 'Pregledajte ili zalijepite izvorni popis',
		'Recipient source': 'Izvor primatelja',
		'Use this when this wave has a one-time recipient list. For repeated waves or reusable cohorts, import people and groups in Directory instead. Limit:':
			'Koristite ovo kada ovaj val ima jednokratni popis primatelja. Za ponavljajuće valove ili ponovno upotrebljive skupine radije uvezite osobe i grupe u Imenik. Ograničenje:',
		'recipients per wave update.': 'primatelja po ažuriranju vala.',
		'Keep valid only': 'Zadrži samo valjane',
		'Clear list': 'Očisti popis',
		'Directory group': 'Grupa iz imenika',
		'No groups available': 'Nema dostupnih grupa',
		'Create a reusable cohort, department, class, or location in Directory, or switch to one-off email import for this wave only.':
			'Izradite ponovno upotrebljivu skupinu, odjel, razred ili lokaciju u Imeniku ili prijeđite na jednokratni uvoz email adresa samo za ovaj val.',
		'Focus person': 'Fokus osoba',
		'Directory people': 'Osobe iz imenika',
		'active people loaded': 'aktivnih osoba učitano',
		'No active people loaded yet': 'Još nema učitanih aktivnih osoba',
		'This selection is broad. Use a Directory group when the wave should only reach a department, cohort, class, or location.':
			'Ovaj odabir je širok. Koristite grupu iz imenika kada val treba dosegnuti samo odjel, skupinu, razred ili lokaciju.',
		'Preview rows': 'Redci pregleda',
		'Preview recipients': 'Pregledaj primatelje',
		'Save previewed recipients': 'Spremi pregledane primatelje',
		'Refresh directory': 'Osvježi imenik',
		'Previewed selection': 'Pregledani odabir',
		'recipient found': 'primatelj pronađen',
		'recipients found': 'primatelja pronađeno',
		'Recipients found': 'Pronađeni primatelji',
		'Invitation rows': 'Redci pozivnica',
		'Preview capped': 'Pregled ograničen',
		'Recipient preview warnings': 'Upozorenja pregleda primatelja',
		'No people to show yet.': 'Još nema osoba za prikaz.',
		'Saved recipients': 'Spremljeni primatelji',
		'Saved recipient selection': 'Spremljeni odabir primatelja',
		'Save a recipient selection after the preview looks right.':
			'Spremite odabir primatelja nakon što pregled izgleda ispravno.',
		Selection: 'Odabir',
		'Saved recipient selection issues': 'Problemi spremljenog odabira primatelja',
		'Invitation roster': 'Popis pozivnica',
		'Prepared invitation roster': 'Pripremljeni popis pozivnica',
		'Loading invitation roster...': 'Učitavanje popisa pozivnica...',
		'No invitations prepared yet.': 'Pozivnice još nisu pripremljene.',
		'Email recipient': 'Email primatelj',
		'Study recipients': 'Primatelji studije',
		'No respondent': 'Nema ispitanika',
		'No subject selected': 'Nije odabrana osoba',
		'No contact': 'Nema kontakta',
		to: 'prema',
		'email recipient': 'email primatelj',
		'email recipients': 'email primatelja',
		'Email recipient list': 'Popis email primatelja',
		'Selected group': 'Odabrana grupa',
		'invitation pair': 'par pozivnice',
		'invitation pairs': 'parova pozivnica',
		selection: 'odabir',
		selections: 'odabira',
		invitation: 'pozivnica',
		invitations: 'pozivnica',
		Locked: 'Zaključano',
		'This wave is already locked. Recipient selection can only be changed before launch. Save the next collection wave first, then choose recipients for that draft.':
			'Ovaj val je već zaključan. Odabir primatelja može se mijenjati samo prije pokretanja. Najprije spremite sljedeći val prikupljanja, zatim odaberite primatelje za taj nacrt.',
		'Open Directory': 'Otvori imenik',
		'Setup path': 'Put postavljanja',
		'Number entry': 'Brojčani unos',
		'Written response': 'Tekstualni odgovor',
		'Recommendation scale': 'Ljestvica preporuke',
		'Frequency scale': 'Ljestvica učestalosti',
		'Agreement scale': 'Ljestvica slaganja',
		'Intensity scale': 'Ljestvica intenziteta',
		'Higher numbers increase included result scores': 'Viši brojevi povećavaju uključene rezultate',
		'Number answers are used as entered in every result output that includes this question.':
			'Brojčani odgovori koriste se kako su uneseni u svakom izlazu rezultata koji uključuje ovo pitanje.',
		'This answer format is collected for context, but it is not used in numeric result scores.':
			'Ovaj format odgovora prikuplja se za kontekst, ali se ne koristi u brojčanim rezultatima.',
		'Higher answers are reversed before scoring': 'Viši odgovori se obrću prije bodovanja',
		'Higher answers increase included result scores': 'Viši odgovori povećavaju uključene rezultate',
		'Not available for numeric result outputs.': 'Nije dostupno za brojčane izlaze rezultata.',
		'Not included in any result output yet.': 'Još nije uključeno ni u jedan izlaz rezultata.',
		'Respondents enter a numeric value. Use this for counts, minutes, ratings with known units, or direct measurements.':
			'Ispitanici unose brojčanu vrijednost. Koristite za brojeve, minute, procjene s poznatim jedinicama ili izravna mjerenja.',
		'Respondents write free text. This is collected for review but is not included in numeric scoring.':
			'Ispitanici pišu slobodan tekst. Prikuplja se za pregled, ali nije uključen u brojčano bodovanje.',
		'Respondents provide a date. This is collected for context and is not included in numeric scoring.':
			'Ispitanici unose datum. Prikuplja se za kontekst i nije uključen u brojčano bodovanje.',
		'Respondents order choices. Ranking can support descriptive review but is not included in current numeric result outputs.':
			'Ispitanici poredaju opcije. Rangiranje može pomoći opisnom pregledu, ali nije uključeno u trenutačne brojčane rezultate.',
		'Respondents choose from defined options. This is collected for grouping or context, not current numeric result outputs.':
			'Ispitanici biraju iz definiranih opcija. Prikuplja se za grupiranje ili kontekst, ne za trenutačne brojčane rezultate.',
		'Respondents answer on a recommendation-style numeric scale. Higher values increase included result scores unless reversed.':
			'Ispitanici odgovaraju na brojčanoj ljestvici preporuke. Više vrijednosti povećavaju uključene rezultate osim ako su obrnuto bodovane.',
		'Respondents report how often something happens. Higher answers increase included result scores unless reversed.':
			'Ispitanici navode koliko se često nešto događa. Viši odgovori povećavaju uključene rezultate osim ako su obrnuto bodovani.',
		'Respondents report how much they agree with a statement. Higher answers increase included result scores unless reversed.':
			'Ispitanici navode koliko se slažu s tvrdnjom. Viši odgovori povećavaju uključene rezultate osim ako su obrnuto bodovani.',
		'Respondents report strength or intensity. Higher answers increase included result scores unless reversed.':
			'Ispitanici navode jačinu ili intenzitet. Viši odgovori povećavaju uključene rezultate osim ako su obrnuto bodovani.',
		'Respondents answer on a numeric rating scale. Higher answers increase included result scores unless reversed.':
			'Ispitanici odgovaraju na brojčanoj ljestvici procjene. Viši odgovori povećavaju uključene rezultate osim ako su obrnuto bodovani.',
		'Minimum answered rule': 'Pravilo minimalnog broja odgovora',
		'Strict missing-data rule': 'Strogo pravilo nedostajućih odgovora',
		'A respondent needs every selected question answered for this result score.':
			'Ispitanik mora odgovoriti na svako odabrano pitanje za ovaj rezultat.',
		'Missing-data rule not configured.': 'Pravilo nedostajućih odgovora nije postavljeno.',
		'Mean score': 'Prosječni rezultat',
		'Sum score': 'Zbrojni rezultat',
		'Requires every selected question': 'Zahtijeva svako odabrano pitanje',
		'Construct plan': 'Plan konstrukata',
		'Answer formats': 'Formati odgovora',
		'Respondent order': 'Redoslijed za ispitanika',
		'Required answers': 'Obavezni odgovori',
		'Results coverage': 'Pokrivenost rezultata',
		'Add at least one construct or dimension label before saving.':
			'Dodajte barem jednu oznaku konstrukta ili dimenzije prije spremanja.',
		'Review whether each format matches the evidence you need.':
			'Provjerite odgovara li svaki format dokazima koji vam trebaju.',
		'Add questions before reviewing respondent order.': 'Dodajte pitanja prije pregleda redoslijeda za ispitanika.',
		'Add at least one rating, recommendation, or number question for numeric results.':
			'Dodajte barem jedno pitanje s procjenom, preporukom ili brojem za brojčane rezultate.',
		'Question coverage': 'Pokrivenost pitanja',
		'Scale compatibility': 'Kompatibilnost ljestvice',
		'Score direction': 'Smjer rezultata',
		'Interpretation boundary': 'Granica tumačenja',
		'Export schema': 'Shema izvoza',
		'Add at least one result output.': 'Dodajte barem jedan izlaz rezultata.',
		'Add rating, recommendation, or number questions before saving numeric results.':
			'Dodajte pitanja s procjenom, preporukom ili brojem prije spremanja brojčanih rezultata.',
		'All outputs require every selected question.': 'Svi izlazi zahtijevaju svako odabrano pitanje.',
		'At least one output uses a minimum-answered rule; review whether partial answers should still produce a result.':
			'Barem jedan izlaz koristi pravilo minimalnog broja odgovora; provjerite trebaju li djelomični odgovori ipak proizvesti rezultat.',
		'Each result output uses one compatible answer-scale family.':
			'Svaki izlaz rezultata koristi jednu kompatibilnu skupinu ljestvica odgovora.',
		'Add result outputs before reviewing scale compatibility.':
			'Dodajte izlaze rezultata prije pregleda kompatibilnosti ljestvice.',
		'No questions are reversed before scoring.': 'Nijedno pitanje se ne obrće prije bodovanja.',
		'These are custom study result outputs. They describe calculation, not official norms, benchmarks, or validated thresholds.':
			'Ovo su prilagođeni izlazi rezultata studije. Opisuju izračun, ne službene norme, referentne vrijednosti ili validirane pragove.',
		'CSV/report exports should preserve question codes, answer formats, score outputs, missing-answer rules, and visibility guardrails.':
			'CSV/izvještaj izvozi trebaju zadržati kodove pitanja, formate odgovora, izlaze rezultata, pravila nedostajućih odgovora i pravila vidljivosti.',
		'No questionnaire dimensions selected': 'Nema odabranih dimenzija upitnika',
		sum: 'zbroj',
		average: 'prosjek'
	};
</script>

<section class="product-panel" role="group" aria-label={setupBodyCopy.progressAriaLabel}>
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">{setupBodyCopy.progressKicker}</p>
			<h3 class="product-title">{setupBodyCopy.progressTitle}</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				{setupBodyCopy.progressBody}
			</p>
		</div>
	</div>

	{#if !canManageSetup}
		<p class="record-row text-sm text-[var(--color-text-muted)]">
			<strong class="record-row__title">{setupBodyCopy.readOnlyTitle}</strong>
			<span>{setupBodyCopy.readOnlyBody}</span>
		</p>
			{@render SetupPath()}
	{:else}
		<div class="grid gap-3">
					<p class="record-field__label">
				{setupBodyCopy.requiredStepsComplete(setupPath.completedCount, setupPath.totalCount)}
			</p>
			{@render SetupPath()}
		</div>

		<section class="record-row" aria-labelledby="study-design-map-heading">
			<div class="record-row__header">
				<div>
					<p class="record-field__label">{setupUi('Study model')}</p>
					<h4 id="study-design-map-heading" class="record-row__title">{setupDesignMap.title}</h4>
					<p class="text-sm text-[var(--color-text-muted)]">{setupDesignMap.summary}</p>
				</div>
			</div>
			<div class="record-grid">
				{#each setupDesignMap.items as item (item.id)}
					<div class="record-field">
						<div class="record-row__header">
							<p class="record-field__label">{item.label}</p>
							<StatusBadge status={item.status} label={designMapStatusLabel(item.status)} />
						</div>
						<p class="text-sm text-[var(--color-text-muted)]">{item.detail}</p>
					</div>
				{/each}
			</div>
		</section>

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
					label={activeStep.pathState === 'done' ? setupBodyCopy.status.done : undefined}
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
									<h5 class="record-row__title">{setupUi('Study source ready')}</h5>
									<p class="text-sm text-[var(--color-text-muted)]">
										{setupUi('The study source is saved. Continue to the questionnaire.')}
									</p>
								</div>
								<StatusBadge status="ready" label={setupBodyCopy.status.done} />
							</div>
						</div>
					{:else}
						<div class="grid gap-4">
							<label class="field lg:col-span-2">
								<span>{setupUi('Study source name')}</span>
								<input bind:value={instrumentForm.fullName} />
							</label>
						</div>
						{@render ActionFooter({
							id: 'instrument',
							label: setupUi('Save study source'),
							icon: 'plus',
							onclick: createInstrumentImport
						})}
					{/if}
				{:else if activeActionIdForView === 'template'}
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<div class="record-row__header">
								<div>
									<h5 class="record-row__title">{setupUi('Questionnaire ready')}</h5>
									<p class="text-sm text-[var(--color-text-muted)]">
										{setupQuestionnaireSavedSummary(questionnaireQuestionCount)}
									</p>
								</div>
								<StatusBadge status="ready" label={setupBodyCopy.status.done} />
							</div>
						</div>
					{:else}
						<div class="grid gap-4 lg:grid-cols-2">
							<label class="field">
								<span>{setupUi('Questionnaire name')}</span>
								<input bind:value={templateName} />
							</label>
							<label class="field">
								<span>{setupUi('Language')}</span>
								<select bind:value={questionnaireLocale}>
									<option value="en">{setupUi('English')}</option>
									<option value="hr">{setupUi('Croatian')}</option>
								</select>
							</label>
						</div>
						<div class="mt-4 record-row">
							<div class="record-row__header">
								<div>
									<p class="record-field__label">{setupUi('Questionnaire palette')}</p>
									<h5 class="record-row__title">{setupBodyCopy.questionnaire.paletteTitle}</h5>
									<p class="text-sm text-[var(--color-text-muted)]">
										{setupUi('Start blank, or load original editable starter items that match the study you are building. These are not marketed as validated named instruments; review and edit before launch.')}
									</p>
								</div>
								<StatusBadge status="neutral" label={setupUi('Editable')} />
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
											{preset.category} - {setupQuestionCount(preset.questionCount)}
										</p>
										<p class="record-field__value">{preset.label}</p>
										<p class="mt-1 text-sm text-[var(--color-text-muted)]">{preset.summary}</p>
										<p class="mt-2 text-xs text-[var(--color-text-muted)]">{preset.detail}</p>
										<p class="mt-2 text-xs text-[var(--color-text-muted)]">
											{setupUi('Suggested results')}: {setupResultOutputsList(preset.resultOutputs)}
										</p>
									</button>
								{/each}
							</div>
						</div>
						<div class="mt-4 grid gap-4">
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">{setupUi('Authoring summary')}</p>
										<h5 class="record-row__title">{setupUi(authoringReadiness.label)}</h5>
									</div>
									<StatusBadge status="neutral" label={setupQuestionCount(authoringReadiness.questionCount)} />
								</div>
								<p class="text-sm text-[var(--color-text-muted)]">
									{setupContextQuestionSummary(authoringReadiness.contextQuestionCount)}
									{reverseScoredCountLabel(authoringReadiness.reverseScoredQuestionCount)}.
								</p>
							</div>
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">{setupBodyCopy.questionnaire.blueprintTitle}</p>
										<h5 class="record-row__title">{setupUi(questionnaireBlueprintReview.label)}</h5>
									</div>
									<StatusBadge status="neutral" label={setupUi('Design review')} />
								</div>
								<div class="questionnaire-blueprint-review">
									{#each questionnaireBlueprintReview.items as item (item.id)}
										<div class="questionnaire-blueprint-review__item" data-state={item.status}>
											<p class="record-field__label">{setupUi(item.label)}</p>
											<p class="record-field__value">{setupUi(item.detail)}</p>
										</div>
									{/each}
								</div>
							</div>
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">{setupBodyCopy.questionnaire.studyDimensions}</p>
										<h5 class="record-row__title">{setupUi('What this questionnaire measures')}</h5>
									</div>
									<StatusBadge status="neutral" label={setupDimensionCount(questionDimensionSummaries.length)} />
								</div>
								<div class="record-grid">
									{#each questionDimensionSummaries as dimension (dimension.code)}
										<div class="record-field">
											<p class="record-field__label">{dimension.code}</p>
											<p class="record-field__value">{dimension.label}</p>
											<p class="text-sm text-[var(--color-text-muted)]">
												{setupQuestionCount(dimension.questionCount)}
											</p>
										</div>
									{/each}
								</div>
							</div>
							{#each templateQuestionRows as question, index (question.ordinal)}
								<div class="question-row">
									<div class="record-row__header">
										<div>
											<p class="record-field__label">{setupUi(`Question ${index + 1}`)}</p>
											<h5 class="record-row__title">
												{question.textDefault.trim() || setupUi('Untitled question')}
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
												? `${question.scaleMin}-${question.scaleMax} ${setupUi('Rating scale').toLowerCase()}`
												: questionTypeLabel(question.type)}
										/>
									</div>
									<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(10rem,14rem)]">
										<label class="field">
											<span>{setupBodyCopy.questionnaire.questionText}</span>
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
											<span>{setupUi('Dimension / construct')}</span>
											<input
												value={question.dimensionLabel}
												oninput={(event) =>
													updateTemplateQuestionRow(index, {
														dimensionLabel: event.currentTarget.value
													})}
											/>
											<span class="text-xs leading-5 text-[var(--color-text-muted)]">
												{setupUi('Group questions by what they measure, for example workload, recovery, or autonomy.')}
											</span>
										</label>
										<label class="field">
											<span>{setupBodyCopy.questionnaire.answerFormat}</span>
											<select
												value={question.type}
												onchange={(event) =>
													updateTemplateQuestionType(
														index,
														event.currentTarget.value as TemplateQuestionAnswerType
													)}
											>
												<option value="likert">{setupUi('Rating scale')}</option>
												<option value="nps">{setupUi('0-10 recommendation scale')}</option>
												<option value="single">{setupUi('Single choice')}</option>
												<option value="multi">{setupUi('Multiple choice')}</option>
												<option value="number">{setupUi('Number')}</option>
												<option value="text">{setupUi('Text')}</option>
												<option value="date">{setupUi('Date')}</option>
												<option value="ranking">{setupUi('Ranking')}</option>
												<option value="matrix">{setupUi('Matrix / grid')}</option>
											</select>
											<span class="text-xs leading-5 text-[var(--color-text-muted)]">
												{questionScaleIntent(question).label}. {questionScaleIntent(question).detail}
											</span>
										</label>
									</div>
									{#if isScaleQuestion(question)}
										<details class="record-row">
											<summary class="record-row__title">{setupUi('Scale values and labels')}</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-5">
												<label class="field">
													<span>{setupUi('Scale preset')}</span>
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
															?.detail ?? setupUi('Keep the current scale values and labels.')}
													</span>
												</label>
												<label class="field">
													<span>{setupUi('Lowest value')}</span>
													<input
														type="number"
														value={question.scaleMin}
														oninput={(event) =>
															updateScaleNumber(index, 'scaleMin', event.currentTarget.value, 1)}
													/>
												</label>
												<label class="field">
													<span>{setupUi('Highest value')}</span>
													<input
														type="number"
														value={question.scaleMax}
														oninput={(event) =>
															updateScaleNumber(index, 'scaleMax', event.currentTarget.value, 5)}
													/>
												</label>
												<label class="field">
													<span>{setupUi('Low label')}</span>
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
													<span>{setupUi('High label')}</span>
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
											<summary class="record-row__title">{setupUi('Number rules')}</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-4">
												<label class="field">
													<span>{setupUi('Minimum')}</span>
													<input
														type="number"
														value={question.numberMin ?? ''}
														oninput={(event) =>
															updateMetadataNumber(index, 'numberMin', event.currentTarget.value)}
													/>
												</label>
												<label class="field">
													<span>{setupUi('Maximum')}</span>
													<input
														type="number"
														value={question.numberMax ?? ''}
														oninput={(event) =>
															updateMetadataNumber(index, 'numberMax', event.currentTarget.value)}
													/>
												</label>
												<label class="field">
													<span>{setupUi('Unit label')}</span>
													<input
														placeholder={setupUi('hours/week, kg, minutes...')}
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
													<span>{setupUi('Whole numbers only')}</span>
												</label>
											</div>
										</details>
									{/if}
									{#if question.type === 'text'}
										<details class="record-row">
											<summary class="record-row__title">{setupUi('Text response rules')}</summary>
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
													<span>{setupUi('Long text answer')}</span>
												</label>
												<label class="field">
													<span>{setupUi('Max characters')}</span>
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
											<summary class="record-row__title">{setupUi('Date rules')}</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-2">
												<label class="field">
													<span>{setupUi('Earliest date')}</span>
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
													<span>{setupUi('Latest date')}</span>
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
											<summary class="record-row__title">{setupUi('Answer options')}</summary>
											<label class="field mt-3">
												<span>{setupUi('Options')}</span>
												<textarea
													rows="3"
													value={question.choiceOptions.join('\n')}
													oninput={(event) => updateChoiceOptions(index, event.currentTarget.value)}
												></textarea>
												<span class="text-sm text-[var(--color-text-muted)]">
													{setupUi('Enter one option per line.')}
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
														<span>{setupUi('Add an Other write-in option')}</span>
													</label>
													<label class="field">
														<span>{setupUi('Other label')}</span>
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
														<span>{setupUi('Exclusive option')}</span>
														<input
															placeholder={setupUi('Example: None of these')}
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
														<span>{setupUi('Ranking rule')}</span>
														<select
															value={question.rankingMode}
															onchange={(event) =>
																updateTemplateQuestionRow(index, {
																	rankingMode: event.currentTarget.value as QuestionRankingMode
															})}
														>
															<option value="rank_all">{setupUi('Rank all options')}</option>
															<option value="top_n">{setupUi('Rank only top N')}</option>
														</select>
													</label>
													<label class="field">
														<span>{setupUi('Top N')}</span>
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
									{#if question.type === 'matrix'}
										<details class="record-row" open>
											<summary class="record-row__title">{setupUi('Matrix rows and columns')}</summary>
											<div class="mt-3 grid gap-3 lg:grid-cols-2">
												<label class="field">
													<span>{setupUi('Rows')}</span>
													<textarea
														rows="4"
														value={question.matrixRows.join('\n')}
														oninput={(event) => updateMatrixRows(index, event.currentTarget.value)}
													></textarea>
													<span class="text-sm text-[var(--color-text-muted)]">
														{setupUi('Enter one row per line. Each row becomes a separate prompt in the grid.')}
													</span>
												</label>
												<label class="field">
													<span>{setupUi('Columns')}</span>
													<textarea
														rows="4"
														value={question.matrixColumns.join('\n')}
														oninput={(event) => updateMatrixColumns(index, event.currentTarget.value)}
													></textarea>
													<span class="text-sm text-[var(--color-text-muted)]">
														{setupUi('Enter one answer column per line. Respondents choose one column for each row.')}
													</span>
												</label>
											</div>
										</details>
									{/if}
									{#if true}
										{@const displaySources = displayLogicSourceQuestions(index)}
										{@const displayOptions = displayLogicSourceOptions(
											question.displayLogicSourceQuestionCode
										)}
										<details class="record-row">
											<summary class="record-row__title">{setupUi('Display rule')}</summary>
											<div class="mt-3 grid gap-3">
												<label class="checkbox-field">
													<input
														type="checkbox"
														checked={question.displayLogicEnabled}
														disabled={displaySources.length === 0}
														onchange={(event) =>
															updateDisplayLogicEnabled(index, event.currentTarget.checked)}
													/>
													<span>{setupUi('Show this question only after a specific answer')}</span>
												</label>
												{#if displaySources.length === 0}
													<p class="text-sm text-[var(--color-text-muted)]">
														{setupUi(
															'Add an earlier single-choice question before creating a follow-up rule.'
														)}
													</p>
												{:else if question.displayLogicEnabled}
													<div class="grid gap-3 lg:grid-cols-2">
														<label class="field">
															<span>{setupUi('Source question')}</span>
															<select
																value={question.displayLogicSourceQuestionCode}
																onchange={(event) =>
																	updateDisplayLogicSource(index, event.currentTarget.value)}
															>
																{#each displaySources as source (source.code)}
																	<option value={source.code}>
																		{source.textDefault.trim() || source.code}
																	</option>
																{/each}
															</select>
														</label>
														<label class="field">
															<span>{setupUi('Source answer')}</span>
															<select
																value={question.displayLogicSourceOptionCode}
																onchange={(event) =>
																	updateTemplateQuestionRow(index, {
																		displayLogicSourceOptionCode: event.currentTarget.value
																	})}
															>
																{#each displayOptions as option (option.code)}
																	<option value={option.code}>{option.label}</option>
																{/each}
															</select>
														</label>
													</div>
													<p class="text-sm text-[var(--color-text-muted)]">
														{setupUi(
															'Hidden follow-up questions are saved as skipped answers and are required only when visible.'
														)}
													</p>
												{/if}
											</div>
										</details>
									{/if}
									<div class="action-row">
										<p class="basis-full text-sm text-[var(--color-text-muted)]">
											{setupUi('Question order below is the order respondents will see. Scoring stays attached to question meaning, not the visual position.')}
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
											<span>{setupUi('Required')}</span>
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
												<span>{setupUi('Reverse score this question')}</span>
											</label>
										{/if}
										<button
											type="button"
											class="secondary-button"
											disabled={index === 0}
											title={setupUi('Move question earlier')}
											onclick={() => reorderTemplateQuestionRow(question.code, 'up')}
										>
											<ArrowUp size={16} aria-hidden="true" />
											<span>{setupUi('Move earlier')}</span>
										</button>
										<button
											type="button"
											class="secondary-button"
											disabled={index === templateQuestionRows.length - 1}
											title={setupUi('Move question later')}
											onclick={() => reorderTemplateQuestionRow(question.code, 'down')}
										>
											<ArrowDown size={16} aria-hidden="true" />
											<span>{setupUi('Move later')}</span>
										</button>
										<button
											type="button"
											class="secondary-button"
											title={setupUi('Duplicate question')}
											onclick={() => copyTemplateQuestionRow(question.code)}
										>
											<Copy size={16} aria-hidden="true" />
											<span>{setupUi('Duplicate')}</span>
										</button>
										<button
											type="button"
											class="secondary-button"
											disabled={templateQuestionRows.length <= 1}
											title={setupUi('Remove question')}
											onclick={() => deleteTemplateQuestionRow(question.code)}
										>
											<Trash2 size={16} aria-hidden="true" />
											<span>{setupUi('Remove')}</span>
										</button>
									</div>
									<div class="record-row">
										<div class="record-row__header">
											<div>
												<p class="record-field__label">{setupUi('Answer scale')}</p>
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
												<p class="record-field__label">{setupUi('Scoring meaning')}</p>
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
									<p class="record-field__label">{setupUi('Respondent preview')}</p>
									<h5 class="record-row__title">{setupUi(respondentPreviewContract.label)}</h5>
								</div>
								<StatusBadge
									status={respondentPreviewContract.unsupportedCount > 0 ? 'blocked' : 'neutral'}
									label={setupQuestionCount(respondentPreviewContract.questionCount)}
								/>
							</div>
							<p class="text-sm text-[var(--color-text-muted)]">
								{respondentPreviewContract.detail}
							</p>
							<div class="record-grid">
								{#each respondentPreviewContract.controls as control (control.label)}
									<div class="record-field">
										<p class="record-field__label">{setupUi('Runtime control')}</p>
										<p class="record-field__value">{setupUi(control.label)}</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{setupQuestionCount(control.count)}
										</p>
									</div>
								{/each}
								<div class="record-field">
									<p class="record-field__label">{setupUi('Runtime notes')}</p>
									<p class="record-field__value">{respondentPreviewContract.warningCount}</p>
									<p class="text-sm text-[var(--color-text-muted)]">
										Limitations to review before launch.
									</p>
								</div>
							</div>
							<p class="text-sm text-[var(--color-text-muted)]">
								{setupUi(
									'This preview uses the same control families as the respondent SurveyJS runtime: rating, radio group, checkbox group, ranking, matrix, number, date, and text.'
								)}
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
												label={setupUi(question.requiredLabel)}
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
															<p class="record-field__label">{setupUi('Rank')} {choiceIndex + 1}</p>
															<p class="record-field__value">{choice.text}</p>
														</li>
													{/each}
												</ol>
											{:else if question.controlType === 'matrix'}
												<div class="overflow-x-auto" aria-label={`${question.positionLabel} matrix preview`}>
													<table class="min-w-full text-left text-sm">
														<thead>
															<tr>
																<th class="py-2 pr-3">{setupUi('Row')}</th>
																{#each question.matrixColumns as column (column.value)}
																	<th class="py-2 pr-3">{column.text}</th>
																{/each}
															</tr>
														</thead>
														<tbody>
															{#each question.matrixRows as row (row.value)}
																<tr>
																	<th class="py-2 pr-3 font-medium text-[var(--color-text)]">{row.text}</th>
																	{#each question.matrixColumns as column (column.value)}
																		<td class="py-2 pr-3">
																			<input type="radio" disabled aria-label={`${row.text} ${column.text}`} />
																		</td>
																	{/each}
																</tr>
															{/each}
														</tbody>
													</table>
												</div>
											{:else if question.controlType === 'number'}
												<input type="number" disabled placeholder={setupUi('Number response')} />
											{:else if question.controlType === 'date'}
												<input type="date" disabled />
											{:else if question.controlType === 'text'}
												{#if question.runtimeElementType === 'comment'}
													<textarea rows="3" disabled placeholder={setupUi('Long text response')}></textarea>
												{:else}
													<input type="text" disabled placeholder={setupUi('Text response')} />
												{/if}
											{:else}
												<p class="error-line">
													{setupUi('This question cannot be rendered by the current respondent runtime.')}
												</p>
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
							label: setupUi('Save questionnaire'),
							icon: 'send',
							onclick: createTemplateVersion
						})}
					{/if}
				{:else if activeActionIdForView === 'scoring'}
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<h5 class="record-row__title">{setupUi('Results setup ready')}</h5>
							<div class="record-grid">
								<div class="record-field">
									<p class="record-field__label">{setupUi('Result outputs')}</p>
									<p class="record-field__value">{scoreOutputs.length}</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">{setupUi('Outputs')}</p>
									<p class="record-field__value">
										{scoreOutputs.map((output) => output.name.trim() || output.code).join(', ')}
									</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">{setupUi('Unique scored questions')}</p>
									<p class="record-field__value">{selectedScoreQuestionRows.length}</p>
								</div>
							</div>
						</div>
					{:else}
						<div class="record-row">
							<h5 class="record-row__title">{setupUi('Result outputs')}</h5>
							<p class="text-sm text-[var(--color-text-muted)]">
								{setupUi(
									'Create one total score or several dimensions/subscales. Each output chooses its own questions, calculation, and missing-answer rule.'
								)}
							</p>
						</div>

						<div class="grid gap-4">
							<div class="record-row">
								<div class="record-row__header">
									<div>
										<p class="record-field__label">{setupUi('Results plan')}</p>
										<h5 class="record-row__title">{setupUi(resultsBlueprintReview.label)}</h5>
									</div>
									<StatusBadge status="neutral" label="Results plan" />
								</div>
								<div class="questionnaire-blueprint-review">
									{#each resultsBlueprintReview.items as item (item.id)}
										<div class="questionnaire-blueprint-review__item" data-state={item.status}>
											<p class="record-field__label">{setupUi(item.label)}</p>
											<p class="record-field__value">{setupUi(item.detail)}</p>
										</div>
									{/each}
								</div>
							</div>
							{#each scoreOutputs as output, outputIndex (output.localId)}
								<div class="record-row">
									<div class="setup-current-task__header">
										<div>
											<p class="record-field__label">{setupUi('Result')} {outputIndex + 1}</p>
											<h5 class="record-row__title">
												{output.name.trim() || `${setupUi('Result')} ${outputIndex + 1}`}
											</h5>
											<p class="setup-current-task__title">
												{output.includedQuestionCodes.length}
												{setupSelectedQuestionCount(output.includedQuestionCodes.length)}
											</p>
										</div>
										{#if scoreOutputs.length > 1}
											<button
												type="button"
												class="secondary-button"
												onclick={() => deleteScoreOutput(output.localId)}
											>
												<Trash2 size={16} aria-hidden="true" />
												<span>{setupUi('Remove result')}</span>
											</button>
										{/if}
									</div>

									<div class="grid gap-4 lg:grid-cols-2">
										<label class="field">
											<span>{setupUi('Result name')}</span>
											<input
												value={output.name}
												oninput={(event) =>
													updateScoreOutput(output.localId, { name: event.currentTarget.value })}
											/>
										</label>
										<label class="field">
											<span>{setupUi('Result code')}</span>
											<input
												value={output.code}
												oninput={(event) =>
													updateScoreOutput(output.localId, { code: event.currentTarget.value })}
											/>
											<span class="text-xs leading-5 text-[var(--color-text-muted)]">
												{setupUi('Used as the report/export dimension code.')}
											</span>
										</label>
										<label class="field">
											<span>{setupUi('Calculation')}</span>
											<select
												value={output.calculation}
												onchange={(event) =>
													updateScoreOutput(output.localId, {
														calculation: event.currentTarget.value as ScoreCalculation
													})}
											>
												<option value="mean">{setupUi('Average selected answers')}</option>
												<option value="sum">{setupUi('Sum selected answers')}</option>
											</select>
										</label>
										<label class="field">
											<span>{setupUi('Missing answers')}</span>
											<select
												value={output.missingStrategy}
												onchange={(event) =>
													updateScoreOutput(output.localId, {
														missingStrategy: event.currentTarget.value as ScoreMissingStrategy
													})}
											>
												<option value="require_all">{setupUi('Require every selected answer')}</option>
												<option value="min_valid_count">{setupUi('Allow a score after enough answers')}</option>
											</select>
										</label>
										{#if output.missingStrategy === 'min_valid_count'}
											<label class="field">
												<span>{setupUi('Minimum answered')}</span>
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
										<h6 class="record-row__title">{setupUi('Questions in this result')}</h6>
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
															{setupUi(questionScoringDetail(question).label)}.
															{setupUi(questionScoringDetail(question).detail)}
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
								<span>{setupUi('Add result output')}</span>
							</button>
						</div>

						{#if collectedContextSummaries.length}
							<div class="record-row">
								<h5 class="record-row__title">{setupUi('Collected but not scored')}</h5>
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
							<h5 class="record-row__title">{setupUi('Scoring plan preview')}</h5>
							<div class="grid gap-2">
								{#each scorePlanSummaries as summary (summary.localId)}
									<div class="record-field">
										<p class="record-field__label">{summary.code}</p>
										<p class="record-field__value">{summary.name}</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{setupUi('Uses')} {dimensionCoverageLabel(summary.dimensionLabels)}
											{setupUi('from')} {setupSelectedQuestionCount(summary.includedQuestionCount)}.
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{setupUi(summary.calculationLabel)}. {setupUi(summary.missingPolicyLabel)}.
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
								<h5 class="record-row__title">{setupUi('Reverse-scoring review')}</h5>
								<p class="text-sm text-[var(--color-text-muted)]">
									{reverseScoringReview.reverseScoredQuestionCount}
									{reverseScoringReview.reverseScoredQuestionCount === 1
										? setupUi('question is')
										: setupUi('questions are')}
									{setupUi('reversed before scoring')}:
									{reverseScoringReview.reverseScoredQuestionLabels.join(', ')}.
								</p>
								<p class="text-sm text-[var(--color-text-muted)]">
									{setupUi('Affects')}: {reverseScoringReview.affectedResultLabels.join(', ') ||
										setupUi('no result outputs yet')}.
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
							label: setupUi('Save results setup'),
							icon: 'send',
							onclick: createScoringRule
						})}
					{/if}
				{:else if activeActionIdForView === 'campaign'}
					<div class="record-row">
						<div class="record-row__header">
							<div>
								<p class="record-field__label">{setupUi('Wave context')}</p>
								<h5 class="record-row__title">{setupUi(waveContext.title)}</h5>
								<p class="text-sm text-[var(--color-text-muted)]">{waveContext.summary}</p>
							</div>
							<StatusBadge status={waveContext.status} label={waveContext.label} />
						</div>
						<ul class="grid gap-2" aria-label={setupUi('Wave setup guidance')}>
							{#each waveContext.guidance as guidance}
								<li class="text-sm text-[var(--color-text-muted)]">{guidance}</li>
							{/each}
						</ul>
					</div>
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<h5 class="record-row__title">{setupUi('Collection wave ready')}</h5>
							<div class="record-grid">
								<div class="record-field">
									<p class="record-field__label">{setupUi('Wave')}</p>
									<p class="record-field__value">{selectedCampaignLabel}</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">{setupUi('Response mode')}</p>
									<p class="record-field__value">
										{responseModeLabel(campaignForm.responseIdentityMode)}
									</p>
								</div>
								<div class="record-field">
									<p class="record-field__label">{setupUi('Language')}</p>
									<p class="record-field__value">{campaignForm.defaultLocale}</p>
								</div>
							</div>
						</div>
					{:else}
						<div class="record-row">
							<div class="record-row__header">
								<div>
									<p class="record-field__label">{setupUi('Launch plan')}</p>
									<h5 class="record-row__title">{setupUi(launchPlan.label)}</h5>
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
								<span>{setupUi('Wave name')}</span>
								<input bind:value={campaignForm.name} />
							</label>
							<label class="field">
								<span>{setupUi('Response mode')}</span>
								<select bind:value={campaignForm.responseIdentityMode}>
									<option value="anonymous">{responseModeLabel('anonymous')}</option>
									<option value="anonymous_longitudinal">{responseModeLabel('anonymous_longitudinal')}</option>
									<option value="identified">{responseModeLabel('identified')}</option>
								</select>
								<span class="text-xs leading-5 text-[var(--color-text-muted)]">
									{responseModeHelp(campaignForm.responseIdentityMode)}
								</span>
							</label>
							<label class="field">
								<span>{setupUi('Respondent language')}</span>
								<input bind:value={campaignForm.defaultLocale} />
							</label>
						</div>
						{@render ActionFooter({
							id: 'campaign',
							label: setupUi('Save collection wave'),
							icon: 'send',
							onclick: createCampaignDraft
						})}
					{/if}
				{:else if activeActionIdForView === 'readiness'}
					<div class="record-row">
						<h5 class="record-row__title">{setupUi('Launch checklist')}</h5>
						<div class="record-grid">
							<div class="record-field">
								<p class="record-field__label">{setupUi('Questionnaire')}</p>
								<p class="record-field__value">
									{selectedTemplateVersionId
										? setupUi('Ready')
										: setupUi('Save questionnaire first')}
								</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">{setupUi('Results setup')}</p>
								<p class="record-field__value">
									{localState.scoringRuleId ?? workspace.scoring?.id
										? setupUi('Ready')
										: setupUi('Save results setup first')}
								</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">{setupUi('Collection wave')}</p>
								<p class="record-field__value">
									{selectedCampaignId ? selectedCampaignLabel : setupUi('Create collection wave first')}
								</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">{setupUi('Status')}</p>
								<p class="record-field__value">{readinessLabel()}</p>
							</div>
							<div class="record-field">
								<p class="record-field__label">{setupUi('Recipient selection')}</p>
								<p class="record-field__value">{currentLaunchState().recipientSummary}</p>
							</div>
						</div>
					</div>
					{#if readinessResult?.issues.length}
						<ul class="grid gap-2" aria-label={setupUi('Launch checklist issues')}>
							{#each readinessResult.issues as issue}
								<li class="text-sm text-[var(--color-text-muted)]">
									{launchIssueLabel(issue)}
								</li>
							{/each}
						</ul>
					{/if}
					<p class="result-line">
						<span>{setupUi('Next action')}</span>
						<span>{currentLaunchState().nextActionLabel}</span>
					</p>
					{@render ActionFooter({
						id: 'readiness',
						label: setupUi('Run launch check'),
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
					{activeActionIdForView === 'readiness' ? launchSurfaceButtonLabel() : setupUi('Next step')}
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
					<p class="record-field__label">{setupUi('Previous wave')}</p>
					<h4 id="locked-wave-heading" class="record-row__title">{setupUi('Recipient selection is locked')}</h4>
					<p class="setup-current-task__title">{lockedSelectedCampaign.name}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						{setupUi(
							'This wave is already locked. Recipient selection can only be changed before launch. Save the next collection wave first, then choose recipients for that draft.'
						)}
					</p>
				</div>
				<p class="step-pill" data-state="idle">{setupUi('Locked')}</p>
			</div>
		</section>
		{/if}

		{#if selectedCampaignId && (activeActionIdForView === 'campaign' || activeActionIdForView === 'readiness')}
		<section class="record-row setup-current-task" aria-labelledby="audience-preview-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">{setupUi('Recipient selection')}</p>
					<h4 id="audience-preview-heading" class="record-row__title">
						{setupUi('Preview recipients, then save the selection')}
					</h4>
					<p class="setup-current-task__title">{selectedCampaignLabel}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						{setupUi(
							'Use Directory groups for recurring populations. Use all active Directory people only when the wave is truly for everyone. Use one-off email list for ad hoc recipients you do not want to manage in Directory. Save recipients before launch so Collection can create private respondent links and send email.'
						)}
					</p>
				</div>
				<p class="step-pill" data-state={previewState}>{stepLabel(previewState)}</p>
			</div>

			{#if savedRuleResult?.rules.length}
				<p class="result-line">
					<span>{setupUi('Saved for launch')}</span>
					<span>{savedAudienceSummary()}</span>
				</p>
			{:else}
				<p class="error-line" role="status">
					{setupUi(
						'No recipient selection is saved yet. Preview recipients first, then save the previewed selection before launch.'
					)}
				</p>
			{/if}

			<div class="record-grid">
				<div class="record-field">
					<p class="record-field__label">{setupUi('Reusable population')}</p>
					<p class="record-field__value">{setupUi('Directory groups')}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Best for departments, cohorts, classes, and repeated waves.
					</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">{setupUi('One-off population')}</p>
					<p class="record-field__value">{setupUi('Email import')}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Best for a temporary list copied from a collaborator or spreadsheet export.
					</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">{setupUi('Open participation')}</p>
					<p class="record-field__value">{setupUi('Open link in Collection')}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Best when anyone with the link may answer and no invite-only list is needed.
					</p>
				</div>
			</div>

			<div class="record-row">
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{setupUi('Demo/test data')}</p>
						<h5 class="record-row__title">{setupUi('Create test recipients for this wave')}</h5>
						<p class="text-sm text-[var(--color-text-muted)]">
							{setupUi(
								"Use this in staging or demos when you need realistic recipients without importing a real directory. It creates a marked test cohort and saves it as this wave's recipient selection."
							)}
						</p>
					</div>
					<p class="step-pill" data-state={testRecipientState}>{stepLabel(testRecipientState)}</p>
				</div>
				<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(8rem,12rem)_auto]">
					<label class="field">
						<span>{setupUi('Group name')}</span>
						<input
							value={testRecipientGroupName}
							disabled={testRecipientState === 'submitting'}
							oninput={(event) => (testRecipientGroupName = event.currentTarget.value)}
						/>
					</label>
					<label class="field">
						<span>{setupUi('People')}</span>
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
						<span>{setupUi('Create test recipients')}</span>
					</button>
				</div>
				{#if testRecipientError}
					<p class="error-line" role="alert">{testRecipientError}</p>
				{/if}
				{#if testRecipientResult}
					<p class="result-line">
						<span>{setupUi('Test cohort saved')}</span>
						<span>
							{testRecipientResult.groupName} -
							{formatCount(testRecipientResult.createdSubjectCount)} {setupUi('recipients')}
						</span>
					</p>
				{/if}
			</div>

			<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(8rem,12rem)]">
				<label class="field">
					<span>{setupUi('Send invitations to')}</span>
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
								<p class="record-field__label">{setupUi('Campaign-local recipients')}</p>
								<h5 class="record-row__title">{setupUi('Build a one-off recipient list')}</h5>
							</div>
							<span
								class="step-pill"
								data-state={previewExternalEmailReview.hasBlockingIssues ? 'failed' : 'idle'}
							>
								{formatCount(previewExternalEmailReview.validRecipientCount)} {setupUi('ready')}
							</span>
						</div>
						<div class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto]">
							<label class="field">
								<span>{setupUi('Name for review')}</span>
								<input
									value={previewManualRecipientName}
									placeholder="Bo Horvat"
									disabled={previewState === 'submitting'}
									oninput={(event) => (previewManualRecipientName = event.currentTarget.value)}
								/>
							</label>
							<label class="field">
								<span>{setupUi('Email')}</span>
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
								<span>{setupUi('Add person')}</span>
							</button>
						</div>
						{#if previewManualRecipientError}
							<p class="error-line" role="alert">{previewManualRecipientError}</p>
						{/if}
						<label class="field">
							<span>{setupUi('Import recipients')}</span>
							<input
								type="file"
								accept=".csv,.txt,text/csv,text/plain"
								disabled={previewState === 'submitting'}
								onchange={(event) => loadPreviewExternalEmailFile(event.currentTarget.files?.[0])}
							/>
							<span class="text-xs leading-5 text-[var(--color-text-muted)]">
								Use a class list, cohort list, HR export, or spreadsheet with an email column
								{setupUi(
									'Use this when this wave has a one-time recipient list. For repeated waves or reusable cohorts, import people and groups in Directory instead. Limit:'
								)}
								{formatCount(maxRecipientImportRecipients)} {setupUi('recipients per wave update.')}
							</span>
						</label>
						<details>
							<summary class="record-row__title">{setupUi('Review or paste source list')}</summary>
							<label class="field mt-3">
								<span>{setupUi('Recipient source')}</span>
								<textarea
									rows="5"
									value={previewExternalEmailText}
									placeholder={'ada@example.com\nBo Horvat <bo@example.com>\ncarla@example.com; diego@example.com'}
									disabled={previewState === 'submitting'}
									oninput={(event) => (previewExternalEmailText = event.currentTarget.value)}
								></textarea>
								<span class="text-xs leading-5 text-[var(--color-text-muted)]">
									{formatCount(previewExternalEmailReview.validRecipientCount)} {setupUi('ready')},
									{formatCount(previewExternalEmailReview.invalidCount)} {setupUi('invalid')},
									{formatCount(previewExternalEmailReview.duplicateCount)} {setupUi('duplicate')}.
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
								<span>{setupUi('Keep valid only')}</span>
							</button>
							<button
								type="button"
								class="secondary-button"
								disabled={previewExternalEmailReview.rows.length === 0 || previewState === 'submitting'}
								onclick={clearPreviewRecipients}
							>
								<Trash2 size={16} aria-hidden="true" />
								<span>{setupUi('Clear list')}</span>
							</button>
						</div>
						{#if previewExternalEmailFileError}
							<p class="error-line" role="alert">{previewExternalEmailFileError}</p>
						{/if}
					</div>
				{:else if previewRequiresGroup}
					{#if previewGroups.length === 0}
						<div class="record-field">
							<p class="record-field__label">{setupUi('Directory group')}</p>
							<p class="record-field__value">{setupUi('No groups available')}</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{setupUi(
									'Create a reusable cohort, department, class, or location in Directory, or switch to one-off email import for this wave only.'
								)}
							</p>
							<a class="secondary-button mt-3" href="/app/directory">{setupUi('Open Directory')}</a>
						</div>
					{:else}
						<label class="field">
							<span>{setupUi('Directory group')}</span>
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
						<span>{setupUi('Focus person')}</span>
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
						<p class="record-field__label">{setupUi('Directory people')}</p>
						<p class="record-field__value">
							{previewSubjects.length
								? `${formatCount(previewSubjects.length)} ${setupUi('active people loaded')}`
								: setupUi('No active people loaded yet')}
						</p>
						<p class="text-sm text-[var(--color-text-muted)]">
							{setupUi(
								'This selection is broad. Use a Directory group when the wave should only reach a department, cohort, class, or location.'
							)}
						</p>
					</div>
				{/if}

				<label class="field">
					<span>{setupUi('Preview rows')}</span>
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
					<span>{setupUi('Preview recipients')}</span>
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
					<span>{setupUi('Save previewed recipients')}</span>
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
					<span>{setupUi('Refresh directory')}</span>
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
					<span>{setupUi('Previewed selection')}</span>
					<span>
						{audienceRuleLabel(previewRuleKind)} - {previewResult.summary.respondentCount}
						{setupUi(previewResult.summary.respondentCount === 1 ? 'recipient found' : 'recipients found')}
					</span>
				</p>
				<div class="record-grid">
					<div class="record-field">
						<p class="record-field__label">{setupUi('Recipients found')}</p>
						<p class="record-field__value">{previewResult.summary.respondentCount}</p>
					</div>
					<div class="record-field">
						<p class="record-field__label">{setupUi('Invitation rows')}</p>
						<p class="record-field__value">{previewResult.summary.assignmentPairCount}</p>
					</div>
					<div class="record-field">
						<p class="record-field__label">{setupUi('Preview capped')}</p>
						<p class="record-field__value">
							{previewResult.summary.truncated ? setupUi('Yes') : setupUi('No')}
						</p>
					</div>
				</div>

				{#if previewResult.warnings.length}
					<ul class="grid gap-2" aria-label={setupUi('Recipient preview warnings')}>
						{#each previewResult.warnings as warning}
							<li class="text-sm text-[var(--color-text-muted)]">
								{audienceWarningLabel(warning)}
							</li>
						{/each}
					</ul>
				{/if}

				<div class="grid gap-2">
					{#if previewResult.rows.length === 0}
					<p class="text-sm text-[var(--color-text-muted)]">{setupUi('No people to show yet.')}</p>
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
					<p class="record-field__label">{setupUi('Saved recipients')}</p>
					<h4 id="saved-recipient-selection-heading" class="record-row__title">
						{setupUi('Saved recipient selection')}
					</h4>
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
							<p class="record-field__label">{setupUi('Selection')} #{rule.ordinal}</p>
							<p class="record-field__value">{savedRecipientSelectionLabel(rule)}</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{savedRecipientSelectionDetail(rule)}
							</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{pairCountLabel(rule.assignmentPairCount)}
							</p>
							{#if rule.issues.length}
								<ul class="grid gap-1" aria-label={setupUi('Saved recipient selection issues')}>
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
					{setupUi('Save a recipient selection after the preview looks right.')}
				</p>
			{/if}
		</section>
		{/if}

		{#if assignmentResult?.assignmentCount || assignmentError}
		<section class="record-row setup-current-task" aria-labelledby="prepared-invitation-roster-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">{setupUi('Invitation roster')}</p>
					<h4 id="prepared-invitation-roster-heading" class="record-row__title">
						{setupUi('Prepared invitation roster')}
					</h4>
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
								{assignment.respondent?.email ?? assignment.respondent?.externalId ?? setupUi('No contact')}
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
	<div class="setup-path" aria-label={setupUi('Setup path')}>
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

