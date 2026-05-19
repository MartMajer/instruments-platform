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
		selectSetupCampaignId,
		selectSetupTemplateVersionId,
		toSelectedSeriesSetupPath,
		type SelectedSeriesSetupWorkflowActionId
	} from './setup-workflow';
	import {
		appendTemplateQuestionRow,
		buildMeanScoringDocument,
		createDefaultTemplateQuestionRows,
		moveTemplateQuestionRow,
		removeTemplateQuestionRow,
		toCreateTemplateQuestions,
		validateTemplateQuestionRows,
		type TemplateQuestionAuthoringRow
	} from './template-authoring';
	import { toCampaignSeriesSetupWorkspaceView, toProductApiErrorMessage } from './view-models';

	type StepState = 'idle' | 'submitting' | 'succeeded' | 'failed';
	type PreviewRuleKind = 'self' | 'all_in_group' | 'manager_of_target' | 'reports_of_target';

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
	const initialScoringRuleKey = `tenant-burnout.${initialSetupRunSuffix}.total`;
	const initialTemplateQuestionRows = createDefaultTemplateQuestionRows();

	let setupRunSuffix = $state(initialSetupRunSuffix);
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
	let templateName = $state(`Tenant burnout pulse template ${initialSetupRunSuffix}`);
	let sectionTitle = $state('Core');
	let templateQuestionRows = $state<TemplateQuestionAuthoringRow[]>(initialTemplateQuestionRows);
	let scoringDocumentManuallyEdited = $state(false);
	let scoringForm = $state({
		ruleKey: initialScoringRuleKey,
		ruleVersion: '1.0.0',
		schemaVersion: 'scoring-rule/v1',
		engineMinVersion: 'engine/v1',
		document: buildDefaultScoringDocument(initialScoringRuleKey, initialTemplateQuestionRows),
		produces: buildDefaultProduces(),
		compatibility: '{}'
	});
	let campaignForm = $state({
		name: `Wave 1 ${initialSetupRunSuffix}`,
		responseIdentityMode: 'anonymous',
		defaultLocale: 'en'
	});
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
	const templateQuestionErrors = $derived(validateTemplateQuestionRows(templateQuestionRows));
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
				scoring: 'Create or select an instrument template first.'
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
				campaign: 'Create or select an instrument template first.'
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
				readiness: 'Create a campaign draft first.'
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
			previewError = 'Create or select a campaign draft first.';
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
			savedRuleError = 'Create or select a campaign draft first.';
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
			defaultLocale: 'en',
			instrumentId: instrumentResult?.id ?? workspace.template?.instrumentId ?? null,
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
			questions: toCreateTemplateQuestions(templateQuestionRows)
		};
	}

	function resetSetupRun() {
		setupRunSuffix = generateSetupRunSuffix();
		const ruleKey = `tenant-burnout.${setupRunSuffix}.total`;
		const nextRows = createDefaultTemplateQuestionRows();

		instrumentForm = {
			...instrumentForm,
			code: `tenant-burnout-pulse-${setupRunSuffix}`,
			fullName: `Tenant burnout pulse ${setupRunSuffix}`
		};
		templateName = `Tenant burnout pulse template ${setupRunSuffix}`;
		templateQuestionRows = nextRows;
		scoringDocumentManuallyEdited = false;
		scoringForm = {
			...scoringForm,
			ruleKey,
			document: buildDefaultScoringDocument(ruleKey, nextRows),
			produces: buildDefaultProduces()
		};
		campaignForm = {
			...campaignForm,
			name: `Wave 1 ${setupRunSuffix}`
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
			(id === 'template' && templateQuestionErrors.length > 0)
		);
	}

	function actionDisabledReason(id: SelectedSeriesSetupWorkflowActionId) {
		if (id === 'template' && templateQuestionErrors.length > 0) {
			return templateQuestionErrors[0];
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
		syncGeneratedScoringIfPristine(nextRows);
	}

	function addTemplateQuestionRow() {
		const nextRows = appendTemplateQuestionRow(templateQuestionRows);
		templateQuestionRows = nextRows;
		syncGeneratedScoringIfPristine(nextRows);
	}

	function deleteTemplateQuestionRow(code: string) {
		const nextRows = removeTemplateQuestionRow(templateQuestionRows, code);
		templateQuestionRows = nextRows;
		syncGeneratedScoringIfPristine(nextRows);
	}

	function reorderTemplateQuestionRow(code: string, direction: 'up' | 'down') {
		const nextRows = moveTemplateQuestionRow(templateQuestionRows, code, direction);
		templateQuestionRows = nextRows;
		syncGeneratedScoringIfPristine(nextRows);
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
			document: buildDefaultScoringDocument(scoringForm.ruleKey, templateQuestionRows)
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
			document: buildDefaultScoringDocument(ruleKey, rows)
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

	function savedRuleSelectorLabel(rule: CampaignRespondentRuleResponse) {
		if (rule.targetSubjectId) {
			return rule.targetSubjectId;
		}

		if (rule.groupId) {
			return rule.groupId;
		}

		return 'campaign audience';
	}

	function assignmentPairLabel(assignment: CampaignAssignmentResponse) {
		return `${assignment.target?.label ?? 'Any target'} -> ${
			assignment.respondent?.label ?? 'No respondent'
		}`;
	}

	function pairCountLabel(count: number) {
		return `${count} ${count === 1 ? 'pair' : 'pairs'}`;
	}

	function ruleCountLabel(count: number) {
		return `${count} ${count === 1 ? 'rule' : 'rules'}`;
	}

	function assignmentCountLabel(count: number) {
		return `${count} ${count === 1 ? 'assignment' : 'assignments'}`;
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
		return buildMeanScoringDocument(ruleId, rows);
	}

	function buildDefaultProduces() {
		return JSON.stringify(
			{
				scores: ['total'],
				interpretation: {
					status: 'tenant_attested',
					source: 'tenant_defined',
					provenance: 'Tenant-defined score bands for this setup; not validated; not official.',
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
</script>

<section class="product-panel" role="group" aria-label="Study setup progress">
	<div class="product-panel__header">
		<div>
			<p class="product-kicker">Study setup</p>
			<h3 class="product-title">Study setup progress</h3>
			<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
				Instrument templates define what respondents answer. Campaigns use a selected template,
				and you can add more templates or versions later for other studies, waves, or variants.
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
				<StatusBadge status={activeStep.status} />
			</div>
			{#if activeStep.disabledReason}
				<p class="text-sm text-[var(--color-text-muted)]">{activeStep.disabledReason}</p>
			{/if}

			<div class="setup-current-task__body">
				{#if activeActionId === 'instrument'}
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
					{@render ActionFooter({
						id: 'instrument',
						label: 'Create instrument import',
						icon: 'plus',
						onclick: createInstrumentImport
					})}
				{:else if activeActionId === 'template'}
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
						{#each templateQuestionRows as question, index (question.ordinal)}
							<div class="question-row">
								<div class="grid gap-3 lg:grid-cols-[minmax(6rem,8rem)_minmax(7rem,9rem)_minmax(0,1fr)]">
									<label class="field">
										<span>Code</span>
										<input
											value={question.code}
											oninput={(event) =>
												updateTemplateQuestionRow(index, {
													code: event.currentTarget.value
												})}
										/>
									</label>
									<label class="field">
										<span>Type</span>
										<select
											value={question.type}
											onchange={(event) =>
												updateTemplateQuestionRow(index, {
													type: event.currentTarget.value
												})}
										>
											<option value="likert">Likert</option>
											<option value="text">Text</option>
											<option value="number">Number</option>
										</select>
									</label>
									<label class="field">
										<span>Question {question.ordinal}</span>
										<textarea
											rows="2"
											value={question.textDefault}
											oninput={(event) =>
												updateTemplateQuestionRow(index, {
													textDefault: event.currentTarget.value
												})}
										></textarea>
									</label>
								</div>
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
									<label class="checkbox-field">
										<input
											type="checkbox"
											checked={question.reverseCoded}
											disabled={question.type !== 'likert'}
											onchange={(event) =>
												updateTemplateQuestionRow(index, {
													reverseCoded: event.currentTarget.checked
												})}
										/>
										<span>Reverse coded</span>
									</label>
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
					{#if templateQuestionErrors.length > 0}
						<ul class="grid gap-1" aria-label="Template question errors">
							{#each templateQuestionErrors as error}
								<li class="error-line">{error}</li>
							{/each}
						</ul>
					{/if}
					{@render ActionFooter({
						id: 'template',
						label: 'Create instrument template',
						icon: 'send',
						onclick: createTemplateVersion
					})}
				{:else if activeActionId === 'scoring'}
					<div class="grid gap-4 lg:grid-cols-2">
						<label class="field">
							<span>Rule key</span>
							<input
								value={scoringForm.ruleKey}
								oninput={(event) => updateScoringRuleKey(event.currentTarget.value)}
							/>
						</label>
						<label class="field">
							<span>Rule version</span>
							<input bind:value={scoringForm.ruleVersion} />
						</label>
						<label class="field lg:col-span-2">
							<span>Document</span>
							<textarea
								rows="8"
								value={scoringForm.document}
								oninput={(event) => updateScoringDocument(event.currentTarget.value)}
							></textarea>
						</label>
						<label class="field lg:col-span-2">
							<span>Produces</span>
							<textarea rows="3" bind:value={scoringForm.produces}></textarea>
						</label>
					</div>
					<div class="action-row">
						<button type="button" class="secondary-button" onclick={regenerateScoringDocument}>
							<RefreshCw size={16} aria-hidden="true" />
							<span>Regenerate from template questions</span>
						</button>
						{#if scoringDocumentManuallyEdited}
							<p class="text-sm text-[var(--color-text-muted)]">
								Scoring JSON has manual edits. Regenerate only when you want to replace it.
							</p>
						{:else}
							<p class="text-sm text-[var(--color-text-muted)]">
								Default scoring is generated from current Likert question codes.
							</p>
						{/if}
					</div>
					{@render ActionFooter({
						id: 'scoring',
						label: 'Create scoring rule',
						icon: 'send',
						onclick: createScoringRule
					})}
				{:else if activeActionId === 'campaign'}
					<div class="grid gap-4 lg:grid-cols-2">
						<label class="field">
							<span>Series name</span>
							<input value={workspaceView.title} disabled />
						</label>
						<label class="field">
							<span>Campaign name</span>
							<input bind:value={campaignForm.name} />
						</label>
						<label class="field">
							<span>Identity mode</span>
							<select bind:value={campaignForm.responseIdentityMode}>
								<option value="anonymous">anonymous</option>
								<option value="anonymous_longitudinal">anonymous_longitudinal</option>
								<option value="identified">identified</option>
							</select>
						</label>
						<label class="field">
							<span>Locale</span>
							<input bind:value={campaignForm.defaultLocale} />
						</label>
					</div>
					{@render ActionFooter({
						id: 'campaign',
						label: 'Create campaign draft',
						icon: 'send',
						onclick: createCampaignDraft
					})}
				{:else if activeActionId === 'readiness'}
					<div class="record-grid">
						<div class="record-field">
							<p class="record-field__label">Selected campaign</p>
							<p class="record-field__value">{selectedCampaignLabel}</p>
						</div>
						<div class="record-field">
							<p class="record-field__label">Readiness</p>
							<p class="record-field__value">
								{readinessResult
									? readinessResult.ready
										? 'ready'
										: 'blocked'
									: workspace.readiness.status}
							</p>
						</div>
					</div>
					{#if readinessResult?.issues.length}
						<ul class="grid gap-2" aria-label="Launch readiness issues">
							{#each readinessResult.issues as issue}
								<li class="text-sm text-[var(--color-text-muted)]">
									<strong>{issue.code}</strong>: {issue.message}
								</li>
							{/each}
						</ul>
					{/if}
					{@render ActionFooter({
						id: 'readiness',
						label: 'Check launch readiness',
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
					disabled={!canGoNext}
					onclick={goToNextSetupAction}
				>
					Next step
				</button>
			</div>
		</section>

		<details class="setup-run">
			<summary>
				<span class="setup-run__label">Generated setup defaults</span>
				<span class="setup-run__note">Suffix {setupRunSuffix}</span>
			</summary>
			<div class="setup-run__body">
				<p class="setup-run__note">
					Default generated values stay editable for local setup runs, but they are secondary to the
					current setup task.
				</p>
				<button type="button" class="secondary-button" onclick={resetSetupRun}>
					<RefreshCw size={16} aria-hidden="true" />
					<span>Generate new sample values</span>
				</button>
			</div>
		</details>

		{#if refreshWarning}
			<p class="error-line">{refreshWarning}</p>
		{/if}

		{#if activeActionId === 'campaign' || activeActionId === 'readiness'}
		<section class="record-row setup-current-task" aria-labelledby="audience-preview-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Selected campaign</p>
					<h4 id="audience-preview-heading" class="record-row__title">Audience preview</h4>
					<p class="setup-current-task__title">{selectedCampaignLabel}</p>
				</div>
				<p class="step-pill" data-state={previewState}>{stepLabel(previewState)}</p>
			</div>

			<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(8rem,12rem)]">
				<label class="field">
					<span>Rule</span>
					<select bind:value={previewRuleKind} disabled={previewState === 'submitting'}>
						<option value="self">self</option>
						<option value="all_in_group">all_in_group</option>
						<option value="manager_of_target">manager_of_target</option>
						<option value="reports_of_target">reports_of_target</option>
					</select>
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
						<span>Target subject</span>
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
						<p class="record-field__label">Scope</p>
						<p class="record-field__value">{previewSubjects.length} subjects loaded</p>
					</div>
				{/if}

				<label class="field">
					<span>Rows</span>
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
					<span>Preview audience</span>
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
					<span>Save rule</span>
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
					<span>Refresh options</span>
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
						<p class="record-field__label">Targets</p>
						<p class="record-field__value">{previewResult.summary.targetCount}</p>
					</div>
					<div class="record-field">
						<p class="record-field__label">Respondents</p>
						<p class="record-field__value">{previewResult.summary.respondentCount}</p>
					</div>
					<div class="record-field">
						<p class="record-field__label">Pairs</p>
						<p class="record-field__value">{previewResult.summary.assignmentPairCount}</p>
					</div>
					<div class="record-field">
						<p class="record-field__label">Truncated</p>
						<p class="record-field__value">{previewResult.summary.truncated ? 'Yes' : 'No'}</p>
					</div>
				</div>

				{#if previewResult.warnings.length}
					<ul class="grid gap-2" aria-label="Audience preview warnings">
						{#each previewResult.warnings as warning}
							<li class="text-sm text-[var(--color-text-muted)]">
								<strong>{warning.code}</strong>: {warning.message}
							</li>
						{/each}
					</ul>
				{/if}

				<div class="grid gap-2">
					{#if previewResult.rows.length === 0}
						<p class="text-sm text-[var(--color-text-muted)]">No preview rows.</p>
					{:else}
						{#each previewResult.rows as row (row.ordinal)}
							<div class="record-field">
								<p class="record-field__label">#{row.ordinal} {row.role}</p>
								<p class="record-field__value">
									{row.target?.label ?? 'Any target'} -> {row.respondent?.label ?? 'No respondent'}
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

		<section class="record-row setup-current-task" aria-labelledby="saved-respondent-rules-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Campaign configuration</p>
					<h4 id="saved-respondent-rules-heading" class="record-row__title">
						Saved respondent rules
					</h4>
					<p class="setup-current-task__title">
						{savedRuleResult ? ruleCountLabel(savedRuleResult.rules.length) : 'Not loaded'}
					</p>
				</div>
				<p class="step-pill" data-state={savedRuleState}>{stepLabel(savedRuleState)}</p>
			</div>

			{#if savedRuleError}
				<p class="error-line" role="alert">{savedRuleError}</p>
			{/if}

			{#if savedRuleResult?.rules.length}
				<div class="grid gap-2">
					{#each savedRuleResult.rules as rule (rule.id)}
						<div class="record-field">
							<p class="record-field__label">#{rule.ordinal}</p>
							<p class="record-field__value">{rule.ruleKind}</p>
							<p class="text-sm text-[var(--color-text-muted)]">{rule.role}</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{pairCountLabel(rule.assignmentPairCount)}
							</p>
							<p class="text-sm text-[var(--color-text-muted)]">{savedRuleSelectorLabel(rule)}</p>
							{#if rule.issues.length}
								<p class="text-sm text-[var(--color-text-muted)]">
									{rule.issues[0].code}: {rule.issues[0].message}
								</p>
							{/if}
						</div>
					{/each}
				</div>
			{:else}
				<p class="text-sm text-[var(--color-text-muted)]">No saved respondent rules.</p>
			{/if}
		</section>

		<section class="record-row setup-current-task" aria-labelledby="campaign-assignments-heading">
			<div class="setup-current-task__header">
				<div>
					<p class="record-field__label">Assignment roster</p>
					<h4 id="campaign-assignments-heading" class="record-row__title">Campaign assignments</h4>
					<p class="setup-current-task__title">
						{assignmentResult
							? assignmentCountLabel(assignmentResult.assignmentCount)
							: 'Not loaded'}
					</p>
				</div>
				<p class="step-pill" data-state={assignmentState}>{stepLabel(assignmentState)}</p>
			</div>

			{#if assignmentError}
				<p class="error-line" role="alert">{assignmentError}</p>
			{/if}

			{#if assignmentResult?.assignments.length}
				<div class="grid gap-2">
					{#each assignmentResult.assignments as assignment (assignment.id)}
						<div class="record-field">
							<p class="record-field__label">{assignment.role}</p>
							<p class="record-field__value">{assignmentPairLabel(assignment)}</p>
							<p class="text-sm text-[var(--color-text-muted)]">{assignment.status}</p>
							<p class="text-sm text-[var(--color-text-muted)]">
								{assignment.anonymous ? 'anonymous' : 'identified'}
							</p>
						</div>
					{/each}
				</div>
			{:else}
				<p class="text-sm text-[var(--color-text-muted)]">No assignments.</p>
			{/if}
		</section>
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
