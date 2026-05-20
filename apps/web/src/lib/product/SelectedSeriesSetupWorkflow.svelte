<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { ArrowDown, ArrowUp, LoaderCircle, Plus, RefreshCw, SearchCheck, Send, Trash2 } from 'lucide-svelte';
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
		CreatePrivateInstrumentImportRequest,
		CreateScoringRuleRequest,
		CreateTemplateVersionRequest,
		InstrumentSummaryResponse,
		LaunchReadinessResponse,
		SetupIdResponse,
		TemplateVersionDetailResponse
	} from '$lib/api/setup';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { createProductApiFromEnv, createSetupApiFromEnv } from './route-state';
	import {
		defaultCampaignWaveName,
		selectSetupCampaignId,
		selectSetupTemplateVersionId,
		toSelectedSeriesSetupPath,
		type SelectedSeriesSetupWorkflowActionId
	} from './setup-workflow';
	import {
		appendScoreOutputRow,
		appendTemplateQuestionRow,
		buildScoreProduces,
		buildScoringDocument,
		createDefaultScoreOutputRows,
		createDefaultTemplateQuestionRows,
		isMeanScoreEligible,
		moveTemplateQuestionRow,
		removeScoreOutputRow,
		removeTemplateQuestionRow,
		syncScoreOutputQuestionCodes,
		toCreateQuestionScales,
		toCreateTemplateQuestions,
		validateScoreOutputRows,
		type ScoreCalculation,
		type ScoreMissingStrategy,
		type ScoreOutputAuthoringRow,
		validateTemplateQuestionRows,
		type TemplateQuestionAnswerType,
		type TemplateQuestionAuthoringRow
	} from './template-authoring';
	import { toCampaignSeriesSetupWorkspaceView, toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';
	type PreviewRuleKind = 'self' | 'all_in_group' | 'manager_of_target' | 'reports_of_target';
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
	const initialSetupRunSuffix = generateSetupRunSuffix();
	const initialScoringRuleKey = 'custom.total_score';
	const initialTemplateQuestionRows = createDefaultTemplateQuestionRows();
	const initialScoreOutputs = createDefaultScoreOutputRows(initialTemplateQuestionRows);

	let instrumentResult = $state<InstrumentSummaryResponse | null>(null);
	let templateResult = $state<TemplateVersionDetailResponse | null>(null);
	let scoringResult = $state<SetupIdResponse | null>(null);
	let campaignResult = $state<CampaignDraftResponse | null>(null);
	let readinessResult = $state<LaunchReadinessResponse | null>(null);
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
		code: `tenant-burnout-pulse-${initialSetupRunSuffix}`,
		version: '1.0.0',
		fullName: `Tenant burnout pulse ${initialSetupRunSuffix}`,
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
	let previewRuleKind = $state<PreviewRuleKind>('self');
	let previewTargetSubjectId = $state('');
	let previewGroupId = $state('');
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

	const workspaceView = $derived(toCampaignSeriesSetupWorkspaceView(workspace));
	const localState = $derived({
		instrumentId: instrumentResult?.id ?? null,
		templateVersionId: templateResult?.templateVersionId ?? null,
		scoringRuleId: scoringResult?.id ?? null,
		campaignId: campaignResult?.id ?? null
	});
	const setupPath = $derived(toSelectedSeriesSetupPath(workspace, localState));
	const workflowActions = $derived(setupPath.steps);
	const currentActionId = $derived(setupPath.currentActionId);
	let activeActionId = $state<SelectedSeriesSetupWorkflowActionId>('instrument');
	let activeActionInitialized = $state(false);
	const activeStep = $derived(
		setupPath.steps.find((step) => step.id === activeActionId) ??
			setupPath.steps.find((step) => step.id === currentActionId) ??
			setupPath.steps[0]
	);
	const activeActionIndex = $derived(
		workflowActions.findIndex((action) => action.id === activeActionId)
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
	const selectedCampaignLabel = $derived(
		workspace.selectedCampaign?.name?.trim() ||
			(selectedCampaignId ? 'Draft campaign selected' : 'No campaign selected')
	);
	const questionnaireQuestionCount = $derived(
		templateResult?.questions.length ?? workspace.template?.questionCount ?? templateQuestionRows.length
	);
	const templateQuestionErrors = $derived(validateTemplateQuestionRows(templateQuestionRows));
	const scoreableQuestionRows = $derived(templateQuestionRows.filter(isMeanScoreEligible));
	const nonScoreableQuestionRows = $derived(
		templateQuestionRows.filter((row) => !isMeanScoreEligible(row))
	);
	const scoreOutputErrors = $derived(validateScoreOutputRows(scoreOutputs, templateQuestionRows));
	const selectedScoreQuestionRows = $derived(
		scoreableQuestionRows.filter((row) =>
			scoreOutputs.some((output) => output.includedQuestionCodes.includes(row.code))
		)
	);
	const previewRequiresTarget = $derived(
		previewRuleKind === 'manager_of_target' || previewRuleKind === 'reports_of_target'
	);
	const previewRequiresGroup = $derived(previewRuleKind === 'all_in_group');
	const canRunPreview = $derived(
		canManageSetup &&
			!!selectedCampaignId &&
			previewState !== 'submitting' &&
			!previewOptionsLoading &&
			(!previewRequiresTarget || !!previewTargetSubjectId) &&
			(!previewRequiresGroup || !!previewGroupId)
	);
	const canSaveCurrentRule = $derived(
		canManageSetup &&
			!!selectedCampaignId &&
			savedRuleState !== 'submitting' &&
			(!previewRequiresTarget || !!previewTargetSubjectId) &&
			(!previewRequiresGroup || !!previewGroupId)
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
			campaignForm.name = defaultCampaignWaveName(workspace);
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
			previewOptionsError = toProductApiErrorMessage(error, 'Audience preview options failed to load.');
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

		previewState = 'submitting';
		previewError = null;
		previewResult = null;

		try {
			previewResult = await productApi.previewRespondentRule(workspace.series.id, campaignId, {
				rule: JSON.stringify({
					kind: previewRuleKind,
					role: defaultPreviewRole(previewRuleKind)
				}),
				targetSubjectId: previewRequiresTarget ? previewTargetSubjectId : null,
				groupId: previewRequiresGroup ? previewGroupId : null,
				maxRows: normalizePreviewMaxRows(previewMaxRows)
			});
			previewState = 'succeeded';
		} catch (error) {
			previewState = 'failed';
			previewError = toProductApiErrorMessage(error, 'Audience preview failed.');
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
				savedRuleError = toProductApiErrorMessage(error, 'Saved respondent rules failed to load.');
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
			sections: [{ ordinal: 1, code: 'core', titleDefault: sectionTitle }],
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
			scaleHighLabel: type === 'nps' ? 'Extremely likely' : current.scaleHighLabel
		});
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
			[field]: Number.isFinite(parsed) ? parsed : fallback
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

	function pathStateLabel(state: 'done' | 'current' | 'blocked') {
		if (state === 'done') {
			return 'Done';
		}

		if (state === 'current') {
			return 'Current';
		}

		return 'Blocked';
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

		return 'self';
	}

	function buildCurrentRespondentRuleJson() {
		const rule: {
			kind: PreviewRuleKind;
			role: string;
			target_subject_id?: string;
			group_id?: string;
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
		const rules = savedRuleResult?.rules ?? [];
		if (savedRuleState === 'submitting') {
			return 'Loading saved recipient selection...';
		}

		if (!rules.length) {
			return 'No recipient selection saved yet.';
		}

		const totalPairs = rules.reduce((sum, rule) => sum + rule.assignmentPairCount, 0);
		return `${ruleCountLabel(rules.length)} saved, ${pairCountLabel(totalPairs)} ready.`;
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
		return `${assignment.target?.label ?? 'Study audience'} to ${
			assignment.respondent?.label ?? 'No respondent'
		}`;
	}

	function previewPairLabel(row: PreviewRuleRow) {
		return `${row.target?.label ?? 'Study audience'} to ${row.respondent?.label ?? 'No respondent'}`;
	}

	function savedRecipientSelectionLabel(rule: CampaignRespondentRuleResponse) {
		return audienceRuleLabel(normalizePreviewRuleKind(rule.ruleKind));
	}

	function savedRecipientSelectionDetail(rule: CampaignRespondentRuleResponse) {
		const selector = rule.groupId
			? previewGroupLabelById(rule.groupId)
			: rule.targetSubjectId
				? previewSubjectLabelById(rule.targetSubjectId)
				: 'Study audience';

		return selector;
	}

	function previewSubjectLabelById(subjectId: string | null | undefined) {
		if (!subjectId) {
			return 'Study audience';
		}

		return previewSubjectLabel(previewSubjects.find((subject) => subject.id === subjectId));
	}

	function previewGroupLabelById(groupId: string | null | undefined) {
		if (!groupId) {
			return 'Study audience';
		}

		return previewGroups.find((group) => group.id === groupId)?.name ?? 'Selected group';
	}

	function normalizePreviewRuleKind(value: string): PreviewRuleKind {
		if (
			value === 'self' ||
			value === 'all_in_group' ||
			value === 'manager_of_target' ||
			value === 'reports_of_target'
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

	function responseModeLabel(value: string) {
		if (value === 'anonymous_longitudinal') {
			return 'Anonymous with repeat participation';
		}

		if (value === 'identified') {
			return 'Identified invite-only';
		}

		return 'Anonymous';
	}

	function responseModeHelp(value: string) {
		if (value === 'identified') {
			return 'Use this when answers must remain connected to named respondents.';
		}

		if (value === 'anonymous_longitudinal') {
			return 'Use this when respondents enter their own repeat-participation code. Saved audience invitations are not supported yet.';
		}

		return 'Use a public link, or save an audience below to send invite-only access while keeping answers anonymous in reports.';
	}

	function audienceRuleLabel(value: PreviewRuleKind) {
		if (value === 'all_in_group') {
			return 'Everyone in a selected group';
		}

		if (value === 'manager_of_target') {
			return "One person's manager";
		}

		if (value === 'reports_of_target') {
			return "One person's direct reports";
		}

		return 'Everyone in the study audience';
	}

	function audienceRuleHelp(value: PreviewRuleKind) {
		if (value === 'all_in_group') {
			return 'Use this when the Directory has a department, cohort, location, or other group for this wave.';
		}

		if (value === 'manager_of_target') {
			return 'Use this for manager feedback about one selected person.';
		}

		if (value === 'reports_of_target') {
			return 'Use this when direct reports should answer about one selected person.';
		}

		return 'Use this for a normal invite-only wave across the active study audience.';
	}

	function recipientRoleLabel(value: string) {
		if (value === 'group_member') {
			return 'Group invitation';
		}

		if (value === 'manager') {
			return 'Manager invitation';
		}

		if (value === 'direct_report') {
			return 'Direct-report invitation';
		}

		return 'Audience invitation';
	}

	function audienceWarningLabel(warning: { code: string; message: string }) {
		if (warning.code === 'respondent_rule_preview.audience_missing') {
			return 'No active study audience is selected yet. Add active people in Directory before launch.';
		}

		if (warning.code === 'respondent_rule_preview.empty') {
			return 'No matching recipients were found for this selection.';
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
			return 'Every saved audience recipient needs an email address before anonymous invite-only collection can start.';
		}

		if (issue.code === 'respondent_rule.no_recipients') {
			return 'Saved recipient selections must find at least one active person before launch.';
		}

		if (issue.code === 'respondent_rule.identity_mode_not_supported') {
			return 'Saved recipient selections are not available for repeat-participation waves yet. Use Anonymous or Identified collection, or remove the saved selection.';
		}

		return issue.message;
	}

	function readinessLabel() {
		if (readinessResult) {
			return readinessResult.ready ? 'Ready to launch' : 'Needs attention';
		}

		if (workspace.readiness.ready) {
			return 'Ready to launch';
		}

		return 'Not checked yet';
	}
</script>

<section class="product-panel" role="group" aria-label="Study setup progress">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Study setup</p>
			<h3 class="product-title">Study setup progress</h3>
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
					<p class="record-field__label">Current setup step</p>
					<h4 id="current-setup-task-heading" class="record-row__title">{activeStep.title}</h4>
					<p class="setup-current-task__title">
						{activeActionId === currentActionId ? 'Next step' : pathStateLabel(activeStep.pathState)}
					</p>
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
				{#if activeActionId === 'instrument'}
					{#if activeStep.pathState === 'done'}
						<div class="record-row">
							<div class="record-row__header">
								<div>
									<h5 class="record-row__title">Instrument ready</h5>
									<p class="text-sm text-[var(--color-text-muted)]">
										This study has an instrument foundation. Continue to the questionnaire template.
									</p>
								</div>
								<StatusBadge status="ready" label="Done" />
							</div>
						</div>
					{:else}
						<div class="grid gap-4 lg:grid-cols-2">
							<label class="field lg:col-span-2">
								<span>Instrument name</span>
								<input bind:value={instrumentForm.fullName} />
							</label>
							<label class="field">
								<span>Version</span>
								<input bind:value={instrumentForm.version} />
							</label>
						</div>
						{@render ActionFooter({
							id: 'instrument',
							label: 'Save instrument',
							icon: 'plus',
							onclick: createInstrumentImport
						})}
					{/if}
				{:else if activeActionId === 'template'}
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
						<div class="mt-4 grid gap-4">
							{#each templateQuestionRows as question, index (question.ordinal)}
								<div class="question-row">
									<div class="record-row__header">
										<div>
											<p class="record-field__label">Question {index + 1}</p>
											<h5 class="record-row__title">
												{question.textDefault.trim() || 'Untitled question'}
											</h5>
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
										</label>
									</div>
									{#if isScaleQuestion(question)}
										<div class="grid gap-3 lg:grid-cols-4">
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
															scaleLowLabel: event.currentTarget.value
														})}
												/>
											</label>
											<label class="field">
												<span>High label</span>
												<input
													value={question.scaleHighLabel}
													oninput={(event) =>
														updateTemplateQuestionRow(index, {
															scaleHighLabel: event.currentTarget.value
														})}
												/>
											</label>
										</div>
									{/if}
									{#if isChoiceQuestion(question) || question.type === 'ranking'}
										<label class="field">
											<span>Answer options</span>
											<textarea
												rows="3"
												value={question.choiceOptions.join('\n')}
												oninput={(event) => updateChoiceOptions(index, event.currentTarget.value)}
											></textarea>
											<span class="text-sm text-[var(--color-text-muted)]">
												Enter one option per line.
											</span>
										</label>
									{/if}
									<div class="action-row">
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
												<span>Reverse scored</span>
											</label>
										{/if}
										<button
											type="button"
											class="secondary-button"
											disabled={index === 0}
											title="Move question up"
											onclick={() => reorderTemplateQuestionRow(question.code, 'up')}
										>
											<ArrowUp size={16} aria-hidden="true" />
											<span>Move up</span>
										</button>
										<button
											type="button"
											class="secondary-button"
											disabled={index === templateQuestionRows.length - 1}
											title="Move question down"
											onclick={() => reorderTemplateQuestionRow(question.code, 'down')}
										>
											<ArrowDown size={16} aria-hidden="true" />
											<span>Move down</span>
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
								</div>
							{/each}
						</div>
						<div class="action-row">
							<button type="button" class="secondary-button" onclick={addTemplateQuestionRow}>
								<Plus size={16} aria-hidden="true" />
								<span>Add question</span>
							</button>
						</div>
						<div class="record-row">
							<h5 class="record-row__title">Respondent preview</h5>
							<div class="grid gap-3">
								{#each templateQuestionRows as question, index (question.ordinal)}
									<div class="record-field">
										<p class="record-field__label">Question {index + 1}</p>
										<p class="record-field__value">
											{question.textDefault.trim() || 'Question text'}
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{questionPreviewDetail(question)}
										</p>
									</div>
								{/each}
							</div>
						</div>
						{#if templateQuestionErrors.length > 0}
							<ul class="grid gap-1" aria-label="Questionnaire errors">
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
				{:else if activeActionId === 'scoring'}
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
															{question.reverseCoded ? ' Reverse scored.' : ''}
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

						{#if nonScoreableQuestionRows.length}
							<div class="record-row">
								<h5 class="record-row__title">Collected but not scored</h5>
								<div class="grid gap-2">
									{#each nonScoreableQuestionRows as question (question.code)}
										<div class="record-field">
											<p class="record-field__label">{questionTypeLabel(question.type)}</p>
											<p class="record-field__value">
												{question.textDefault.trim() || question.code}
											</p>
										</div>
									{/each}
								</div>
							</div>
						{/if}

						<div class="record-row">
							<h5 class="record-row__title">Result preview</h5>
							<div class="grid gap-2">
								{#each scoreOutputs as output (output.localId)}
									<div class="record-field">
										<p class="record-field__label">{output.code || scoreCodeFromName(output.name)}</p>
										<p class="record-field__value">
											{output.name || 'Result'} uses the {scoreCalculationLabel(output.calculation)} of
											{output.includedQuestionCodes.length}
											selected {output.includedQuestionCodes.length === 1 ? 'question' : 'questions'}.
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">{missingPolicyLabel(output)}</p>
									</div>
								{/each}
							</div>
						</div>

						{#if scoreOutputErrors.length > 0}
							<ul class="grid gap-1" aria-label="Results setup errors">
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
				{:else if activeActionId === 'campaign'}
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
				{:else if activeActionId === 'readiness'}
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
					disabled={!canGoNext && activeActionId !== 'readiness'}
					onclick={activeActionId === 'readiness' ? openLaunchSurface : goToNextSetupAction}
				>
					{activeActionId === 'readiness' ? 'Go to launch' : 'Next step'}
				</button>
			</div>
		</section>

		{#if refreshWarning}
			<p class="error-line">{refreshWarning}</p>
		{/if}

		{#if activeActionId === 'campaign' || activeActionId === 'readiness'}
		<section class="record-row setup-current-task" aria-labelledby="audience-preview-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Collection audience</p>
					<h4 id="audience-preview-heading" class="record-row__title">Choose recipients for this wave</h4>
					<p class="setup-current-task__title">{selectedCampaignLabel}</p>
					<p class="text-sm text-[var(--color-text-muted)]">
						Select who gets invited, preview the list, then save it for launch. Anonymous waves still
						report answers anonymously.
					</p>
				</div>
				<p class="step-pill" data-state={previewState}>{stepLabel(previewState)}</p>
			</div>

			<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(8rem,12rem)]">
				<label class="field">
					<span>Send invitations to</span>
					<select bind:value={previewRuleKind} disabled={previewState === 'submitting'}>
						<option value="self">{audienceRuleLabel('self')}</option>
						<option value="all_in_group">{audienceRuleLabel('all_in_group')}</option>
						<option value="manager_of_target">{audienceRuleLabel('manager_of_target')}</option>
						<option value="reports_of_target">{audienceRuleLabel('reports_of_target')}</option>
					</select>
					<span class="text-xs leading-5 text-[var(--color-text-muted)]">
						{audienceRuleHelp(previewRuleKind)}
					</span>
				</label>

				{#if previewRequiresGroup}
					<label class="field">
						<span>Group</span>
						<select
							bind:value={previewGroupId}
							disabled={previewGroups.length === 0 || previewState === 'submitting'}
						>
							{#each previewGroups as group (group.id)}
								<option value={group.id}>{group.name}</option>
							{/each}
						</select>
					</label>
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
						<p class="record-field__label">Study audience</p>
						<p class="record-field__value">
							{previewSubjects.length
								? `${previewSubjects.length} active people loaded`
								: 'No active people loaded yet'}
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
					<span>Save recipient selection</span>
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
					<ul class="grid gap-2" aria-label="Audience preview warnings">
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
					<p class="record-field__label">Saved audience</p>
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
			<button
				type="button"
				class="setup-path__item"
				data-state={step.id === activeActionId ? 'current' : step.pathState}
				disabled={!canSelectSetupAction(step.id)}
				aria-current={step.id === activeActionId ? 'step' : undefined}
				onclick={() => selectSetupAction(step.id)}
			>
				<span class="setup-path__marker" aria-hidden="true">{step.step.replace('Step ', '')}</span>
				<span class="setup-path__content">
					<span class="setup-path__title">{step.title}</span>
					<span class="setup-path__description">{step.description}</span>
				</span>
				<span class="setup-path__state">{pathStateLabel(step.pathState)}</span>
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
