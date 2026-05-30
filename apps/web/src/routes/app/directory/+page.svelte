<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { onDestroy } from 'svelte';
	import { Link2, LoaderCircle, Plus, RefreshCcw, Save, Upload, UserRound } from 'lucide-svelte';
	import type {
		DirectoryImportApplyResponse,
		DirectoryImportPreviewResponse,
		DirectoryImportRuleResponse,
		DirectoryImportWorkspaceResponse,
		SubjectDirectoryCsvImportResponse,
		SubjectDirectoryItemResponse,
		SubjectDirectoryResponse,
		SubjectGroupMembershipResponse,
		SubjectGroupListResponse,
		SubjectGroupResponse
	} from '$lib/api/product';
	import type { AuthSessionResponse } from '$lib/api/setup';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import InlineAlert from '$lib/components/InlineAlert.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import {
		getProductAuthContext,
		hasProductPermission,
		setupManagePermission
	} from '$lib/product/auth-context';
	import { toProductApiErrorMessage } from '$lib/product/view-models';

	type LoadState = 'idle' | 'loading' | 'ready' | 'error';
	type DirectoryPersonStatus = 'active' | 'excluded' | 'deactivated';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();
	const authContext = getProductAuthContext();
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const countFormatter = new Intl.NumberFormat('en-US');

	let authSession = $state<AuthSessionResponse | null>(null);
	let loadState = $state<LoadState>('idle');
	let directory = $state<SubjectDirectoryResponse | null>(null);
	let groupList = $state<SubjectGroupListResponse | null>(null);
	let errorMessage = $state<string | null>(null);
	let newSubjectDisplayName = $state('');
	let newSubjectEmail = $state('');
	let newSubjectExternalId = $state('');
	let newSubjectLocale = $state('en');
	let newSubjectAttributes = $state('{}');
	let newSubjectGroupId = $state('');
	let newSubjectRoleInGroup = $state('');
	let creatingSubject = $state(false);
	let subjectMutationError = $state<string | null>(null);
	let editSubjectSourceId = $state('');
	let editSubjectDisplayName = $state('');
	let editSubjectEmail = $state('');
	let editSubjectExternalId = $state('');
	let editSubjectLocale = $state('en');
	let editSubjectAttributes = $state('{}');
	let savingSubject = $state(false);
	let editSubjectError = $state<string | null>(null);
	let newGroupType = $state('department');
	let newGroupName = $state('');
	let newGroupParentId = $state('');
	let newGroupAttributes = $state('{}');
	let creatingGroup = $state(false);
	let groupMutationError = $state<string | null>(null);
	let selectedSubjectId = $state('');
	let selectedGroupId = $state('');
	let membershipRole = $state('');
	let addingMembership = $state(false);
	let membershipError = $state<string | null>(null);
	let pendingMembershipRemovalKey = $state('');
	let removingMembershipKey = $state('');
	let membershipRemovalError = $state<string | null>(null);
	let managerSubjectId = $state('');
	let managerValidFrom = $state('');
	let savingManager = $state(false);
	let managerError = $state<string | null>(null);
	let importCsvContent = $state('');
	let previewingCsv = $state(false);
	let applyingCsv = $state(false);
	let importResult = $state<SubjectDirectoryCsvImportResponse | null>(null);
	let importError = $state<string | null>(null);
	let graphWorkspace = $state<DirectoryImportWorkspaceResponse | null>(null);
	let graphWorkspaceState = $state<LoadState>('idle');
	let graphWorkspaceError = $state<string | null>(null);
	let selectedDirectoryConnectionId = $state('');
	let selectedDirectoryImportRuleId = $state('');
	let startingGraphAdminConsent = $state(false);
	let graphAdminConsentError = $state<string | null>(null);
	let graphConnectionDisplayName = $state('');
	let graphConnectionTenantId = $state('');
	let graphConnectionPrimaryDomain = $state('');
	let graphConnectionScopes = $state('User.Read.All, Group.Read.All, GroupMember.Read.All');
	let creatingGraphConnection = $state(false);
	let graphConnectionError = $state<string | null>(null);
	let graphRuleName = $state('');
	let graphRuleDepartments = $state('');
	let graphRuleGroupIds = $state('');
	let graphRuleJobTitleContains = $state('');
	let graphRuleIncludeManagers = $state(false);
	let graphRuleMirrorMode = $state(false);
	let graphRuleMirrorConfirmation = $state('');
	let savingGraphRule = $state(false);
	let graphRuleError = $state<string | null>(null);
	let previewingGraphImport = $state(false);
	let applyingGraphImport = $state(false);
	let graphPreview = $state<DirectoryImportPreviewResponse | null>(null);
	let graphApplyResult = $state<DirectoryImportApplyResponse | null>(null);
	let graphImportError = $state<string | null>(null);
	const directoryPageSize = 25;
	let directorySearch = $state('');
	let activeDirectorySearch = $state('');
	let directorySort = $state('name_asc');
	let activeDirectorySort = $state('name_asc');
	let directorySource = $state('all');
	let activeDirectorySource = $state('all');
	let directoryStatus = $state('active');
	let activeDirectoryStatus = $state('active');
	let directoryGroupId = $state('all');
	let activeDirectoryGroupId = $state('all');
	let directoryManager = $state('any');
	let activeDirectoryManager = $state('any');
	let directoryContact = $state('any');
	let activeDirectoryContact = $state('any');
	let directoryPageOffset = $state(0);
	let detailsSubjectId = $state('');
	let personDetailsDialog = $state<HTMLDialogElement | null>(null);
	let statusMutationReason = $state('');
	let statusMutatingSubject = $state(false);
	let statusMutationError = $state<string | null>(null);

	const unsubscribeAuth = authContext.session.subscribe((value) => {
		authSession = value;
	});
	onDestroy(unsubscribeAuth);

	const canManageSetup = $derived(hasProductPermission(authSession, setupManagePermission));
	const subjects = $derived(directory?.subjects ?? []);
	const groups = $derived(groupList?.groups ?? []);
	const visibleGroups = $derived(groups.slice(0, 50));
	const directorySummary = $derived(directory?.summary ?? null);
	const directoryReturnedCount = $derived(directorySummary?.returnedSubjectCount ?? subjects.length);
	const directoryFilteredCount = $derived(directorySummary?.filteredSubjectCount ?? subjects.length);
	const directoryTotalCount = $derived(directorySummary?.subjectCount ?? subjects.length);
	const directoryPageStart = $derived(
		directoryReturnedCount === 0 ? 0 : (directorySummary?.pageOffset ?? 0) + 1
	);
	const directoryPageEnd = $derived((directorySummary?.pageOffset ?? 0) + directoryReturnedCount);
	const directoryCanPageBack = $derived((directorySummary?.pageOffset ?? 0) > 0);
	const directoryCanPageForward = $derived(directorySummary?.hasMore ?? false);
	const membershipCount = $derived(
		subjects.reduce((total, subject) => total + subject.groups.length, 0)
	);
	const selectedSubject = $derived(
		subjects.find((subject) => subject.id === selectedSubjectId) ?? null
	);
	const detailsSubject = $derived(
		subjects.find((subject) => subject.id === detailsSubjectId) ?? null
	);
	const managerOptions = $derived(subjects.filter((subject) => subject.id !== selectedSubjectId));
	const importHasFailures = $derived(
		importResult?.rows.some((row) => row.status === 'failed') ?? false
	);
	const importBusy = $derived(previewingCsv || applyingCsv);
	const graphConnections = $derived(graphWorkspace?.connections ?? []);
	const graphRules = $derived(graphWorkspace?.rules ?? []);
	const graphRecentRuns = $derived(graphWorkspace?.recentRuns ?? []);
	const selectedDirectoryConnection = $derived(
		graphConnections.find((connection) => connection.id === selectedDirectoryConnectionId) ??
			graphConnections[0] ??
			null
	);
	const selectedDirectoryImportRule = $derived(
		graphRules.find((rule) => rule.id === selectedDirectoryImportRuleId) ?? graphRules[0] ?? null
	);
	const graphConnectionReturnStatus = $derived(page.url.searchParams.get('directoryConnection'));
	const graphImportBusy = $derived(previewingGraphImport || applyingGraphImport || savingGraphRule);
	const graphMirrorConfirmationText = 'MIRROR MICROSOFT DIRECTORY';
	const graphSaveRuleDisabled = $derived(
		graphImportBusy ||
			!selectedDirectoryConnection ||
			!graphRuleName.trim() ||
			(graphRuleMirrorMode && graphRuleMirrorConfirmation.trim() !== graphMirrorConfirmationText)
	);
	const csvTemplateHref = $derived(
		`data:text/csv;charset=utf-8,${encodeURIComponent('external_id,email,display_name,locale,group_type,group_name,role_in_group\nemp-001,ana@example.test,Ana Analyst,en,department,Research,member\nemp-002,bo@example.test,Bo Builder,en,department,Operations,member')}`
	);

	$effect(() => {
		if (canManageSetup && loadState === 'idle') {
			void loadDirectory();
		}
	});

	$effect(() => {
		if (canManageSetup && graphWorkspaceState === 'idle') {
			void loadGraphImportWorkspace();
		}
	});

	$effect(() => {
		if ((selectedSubject?.id ?? '') !== editSubjectSourceId) {
			syncSelectedSubjectFields(subjects);
		}
	});

	async function loadDirectory(
		search = activeDirectorySearch,
		skip = directoryPageOffset,
		sort = activeDirectorySort,
		source = activeDirectorySource,
		status = activeDirectoryStatus,
		groupId = activeDirectoryGroupId,
		manager = activeDirectoryManager,
		contact = activeDirectoryContact
	) {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;
		const normalizedSearch = search.trim();
		const normalizedSkip = Math.max(0, skip);
		const normalizedSort = normalizeSelect(sort, 'name_asc');
		const normalizedSource = normalizeSelect(source, 'all');
		const normalizedStatus = normalizeSelect(status, 'active');
		const normalizedGroupId = normalizeSelect(groupId, 'all');
		const normalizedManager = normalizeSelect(manager, 'any');
		const normalizedContact = normalizeSelect(contact, 'any');

		try {
			const [nextDirectory, nextGroupList] = await Promise.all([
				productApi.listSubjects({
					search: normalizedSearch,
					skip: normalizedSkip,
					take: directoryPageSize,
					sort: normalizedSort,
					source: normalizedSource === 'all' ? null : normalizedSource,
					status: normalizedStatus,
					groupId: normalizedGroupId === 'all' ? null : normalizedGroupId,
					manager: normalizedManager === 'any' ? null : normalizedManager,
					contact: normalizedContact === 'any' ? null : normalizedContact
				}),
				productApi.listSubjectGroups()
			]);

			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			directory = nextDirectory;
			groupList = nextGroupList;
			activeDirectorySearch = normalizedSearch;
			directorySearch = normalizedSearch;
			activeDirectorySort = normalizedSort;
			directorySort = normalizedSort;
			activeDirectorySource = normalizedSource;
			directorySource = normalizedSource;
			activeDirectoryStatus = normalizedStatus;
			directoryStatus = normalizedStatus;
			activeDirectoryGroupId = normalizedGroupId;
			directoryGroupId = normalizedGroupId;
			activeDirectoryManager = normalizedManager;
			directoryManager = normalizedManager;
			activeDirectoryContact = normalizedContact;
			directoryContact = normalizedContact;
			directoryPageOffset = nextDirectory.summary.pageOffset;
			syncSelections(nextDirectory.subjects, nextGroupList.groups);
			syncSelectedSubjectFields(nextDirectory.subjects);
			if (detailsSubjectId && !nextDirectory.subjects.some((subject) => subject.id === detailsSubjectId)) {
				detailsSubjectId = '';
				statusMutationReason = '';
				statusMutationError = null;
			}
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			directory = null;
			groupList = null;
			errorMessage = toProductApiErrorMessage(error, text.directory.loadFailed);
			loadState = 'error';
		}
	}

	async function loadGraphImportWorkspace() {
		graphWorkspaceState = 'loading';
		graphWorkspaceError = null;

		try {
			const workspace = await productApi.getDirectoryImportWorkspace();
			graphWorkspace = workspace;
			syncGraphImportSelections(workspace);
			graphWorkspaceState = 'ready';
		} catch (error) {
			graphWorkspace = null;
			graphWorkspaceError = toProductApiErrorMessage(
				error,
				'Microsoft Graph directory import workspace could not be loaded.'
			);
			graphWorkspaceState = 'error';
		}
	}

	function submitDirectoryFilters() {
		void loadDirectory(
			directorySearch,
			0,
			directorySort,
			directorySource,
			directoryStatus,
			directoryGroupId,
			directoryManager,
			directoryContact
		);
	}

	function clearDirectoryFilters() {
		directorySearch = '';
		directorySort = 'name_asc';
		directorySource = 'all';
		directoryStatus = 'active';
		directoryGroupId = 'all';
		directoryManager = 'any';
		directoryContact = 'any';
		void loadDirectory('', 0, 'name_asc', 'all', 'active', 'all', 'any', 'any');
	}

	function loadPreviousDirectoryPage() {
		void loadDirectory(activeDirectorySearch, Math.max(0, directoryPageOffset - directoryPageSize));
	}

	function loadNextDirectoryPage() {
		void loadDirectory(activeDirectorySearch, directoryPageOffset + directoryPageSize);
	}

	async function startGraphAdminConsent() {
		if (!canManageSetup) {
			return;
		}

		startingGraphAdminConsent = true;
		graphAdminConsentError = null;

		try {
			const response = await productApi.startMicrosoftGraphAdminConsent();
			window.location.assign(response.authorizationUrl);
		} catch (error) {
			graphAdminConsentError = toProductApiErrorMessage(
				error,
				'Microsoft admin consent could not be started.'
			);
		} finally {
			startingGraphAdminConsent = false;
		}
	}

	async function createGraphConnection() {
		if (!canManageSetup) {
			return;
		}

		if (
			!graphConnectionDisplayName.trim() ||
			!graphConnectionTenantId.trim() ||
			!graphConnectionPrimaryDomain.trim()
		) {
			graphConnectionError = 'Connection name, tenant ID, and primary domain are required.';
			return;
		}

		creatingGraphConnection = true;
		graphConnectionError = null;

		try {
			const connection = await productApi.createDirectoryConnection({
				displayName: graphConnectionDisplayName.trim(),
				externalTenantId: graphConnectionTenantId.trim(),
				primaryDomain: graphConnectionPrimaryDomain.trim(),
				grantedScopes: splitList(graphConnectionScopes)
			});
			graphWorkspace = mergeGraphWorkspace({
				connections: [connection, ...graphConnections.filter((item) => item.id !== connection.id)]
			});
			selectedDirectoryConnectionId = connection.id;
			graphConnectionDisplayName = '';
			graphConnectionTenantId = '';
			graphConnectionPrimaryDomain = '';
			graphConnectionScopes = 'User.Read.All, Group.Read.All, GroupMember.Read.All';
		} catch (error) {
			graphConnectionError = toProductApiErrorMessage(
				error,
				'Microsoft Graph connection could not be saved.'
			);
		} finally {
			creatingGraphConnection = false;
		}
	}

	async function saveGraphImportRule() {
		if (!canManageSetup || !selectedDirectoryConnection) {
			graphRuleError = 'Select a Microsoft Graph connection first.';
			return;
		}

		if (graphSaveRuleDisabled) {
			graphRuleError = graphRuleMirrorMode
				? `Type ${graphMirrorConfirmationText} before saving a mirror rule.`
				: 'Rule name is required.';
			return;
		}

		savingGraphRule = true;
		graphRuleError = null;
		graphImportError = null;
		graphPreview = null;
		graphApplyResult = null;

		try {
			const rule = await productApi.createDirectoryImportRule({
				connectionId: selectedDirectoryConnection.id,
				name: graphRuleName.trim(),
				criteria: buildGraphImportCriteria(),
				fieldSelection: buildGraphImportFieldSelection(),
				mirrorMode: graphRuleMirrorMode,
				mirrorConfirmation: graphRuleMirrorMode ? graphRuleMirrorConfirmation.trim() : null
			});
			graphWorkspace = mergeGraphWorkspace({
				rules: [rule, ...graphRules.filter((item) => item.id !== rule.id)]
			});
			selectedDirectoryImportRuleId = rule.id;
		} catch (error) {
			graphRuleError = toProductApiErrorMessage(error, 'Directory import rule could not be saved.');
		} finally {
			savingGraphRule = false;
		}
	}

	async function previewGraphImportRule() {
		const rule = selectedDirectoryImportRule;
		if (!canManageSetup || !rule) {
			graphImportError = 'Save or select a directory import rule first.';
			return;
		}

		previewingGraphImport = true;
		graphImportError = null;
		graphPreview = null;
		graphApplyResult = null;

		try {
			graphPreview = await productApi.previewDirectoryImportRule(rule.id);
		} catch (error) {
			graphImportError = toProductApiErrorMessage(error, 'Directory import preview failed.');
		} finally {
			previewingGraphImport = false;
		}
	}

	async function applyGraphImportRun() {
		if (!canManageSetup || !graphPreview || graphPreview.status !== 'previewed') {
			return;
		}

		applyingGraphImport = true;
		graphImportError = null;

		try {
			graphApplyResult = await productApi.applyDirectoryImportRun(graphPreview.runId);
			await Promise.all([loadDirectory(), loadGraphImportWorkspace()]);
		} catch (error) {
			graphImportError = toProductApiErrorMessage(error, 'Directory import apply failed.');
		} finally {
			applyingGraphImport = false;
		}
	}

	async function createSubject() {
		if (!canManageSetup) {
			return;
		}

		if (!newSubjectDisplayName.trim() && !newSubjectEmail.trim() && !newSubjectExternalId.trim()) {
			subjectMutationError = text.directory.enterPersonIdentity;
			return;
		}

		creatingSubject = true;
		subjectMutationError = null;

		try {
			const created = await productApi.createSubject({
				displayName: optionalText(newSubjectDisplayName),
				email: optionalText(newSubjectEmail),
				externalId: optionalText(newSubjectExternalId),
				locale: newSubjectLocale.trim() || 'en',
				attributes: newSubjectAttributes.trim() || '{}'
			});
			const requestedGroupId = optionalText(newSubjectGroupId);
			if (requestedGroupId) {
				await productApi.addSubjectGroupMember(requestedGroupId, {
					subjectId: created.id,
					roleInGroup: optionalText(newSubjectRoleInGroup)
				});
			}

			newSubjectDisplayName = '';
			newSubjectEmail = '';
			newSubjectExternalId = '';
			newSubjectLocale = 'en';
			newSubjectAttributes = '{}';
			newSubjectGroupId = '';
			newSubjectRoleInGroup = '';
			await loadDirectory();
			selectedSubjectId = created.id;
			if (requestedGroupId) {
				selectedGroupId = requestedGroupId;
			}
			syncSelectedSubjectFields(directory?.subjects ?? []);
		} catch (error) {
			subjectMutationError = toProductApiErrorMessage(error, text.directory.personCreateFailed);
		} finally {
			creatingSubject = false;
		}
	}

	async function previewSubjectDirectoryCsv() {
		await importSubjectDirectoryCsv(true);
	}

	async function applySubjectDirectoryCsv() {
		await importSubjectDirectoryCsv(false);
	}

	async function importSubjectDirectoryCsv(dryRun: boolean) {
		if (!canManageSetup) {
			return;
		}

		if (!importCsvContent.trim()) {
			importError = text.directory.csvRequired;
			return;
		}

		if (dryRun) {
			previewingCsv = true;
		} else {
			applyingCsv = true;
		}

		importError = null;
		if (dryRun) {
			importResult = null;
		}

		try {
			importResult = await productApi.importSubjectDirectoryCsv({
				csvContent: importCsvContent,
				dryRun
			});
			if (!dryRun) {
				await loadDirectory();
			}
		} catch (error) {
			importError = toProductApiErrorMessage(error, text.directory.csvImportFailed);
		} finally {
			previewingCsv = false;
			applyingCsv = false;
		}
	}

	async function loadCsvFile(event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		if (!file) {
			return;
		}

		importCsvContent = await file.text();
		importResult = null;
		importError = null;
	}

	async function saveSelectedSubject() {
		if (!canManageSetup || !selectedSubjectId) {
			editSubjectError = text.directory.selectPerson;
			return;
		}

		if (
			!editSubjectDisplayName.trim() &&
			!editSubjectEmail.trim() &&
			!editSubjectExternalId.trim()
		) {
			editSubjectError = text.directory.enterPersonIdentity;
			return;
		}

		savingSubject = true;
		editSubjectError = null;

		try {
			await productApi.updateSubject(selectedSubjectId, {
				displayName: optionalText(editSubjectDisplayName),
				email: optionalText(editSubjectEmail),
				externalId: optionalText(editSubjectExternalId),
				locale: editSubjectLocale.trim() || 'en',
				attributes: editSubjectAttributes.trim() || '{}'
			});
			await loadDirectory();
		} catch (error) {
			editSubjectError = toProductApiErrorMessage(error, text.directory.personUpdateFailed);
		} finally {
			savingSubject = false;
		}
	}

	async function setDetailsSubjectStatus(status: DirectoryPersonStatus) {
		if (!canManageSetup || !detailsSubject) {
			return;
		}

		statusMutatingSubject = true;
		statusMutationError = null;

		try {
			await productApi.setSubjectStatus(detailsSubject.id, {
				status,
				reason: optionalText(statusMutationReason)
			});
			statusMutationReason = '';
			directoryStatus = status;
			activeDirectoryStatus = status;
			directoryPageOffset = 0;
			await loadDirectory(activeDirectorySearch, 0);
		} catch (error) {
			statusMutationError = toProductApiErrorMessage(error, 'Person status could not be updated.');
		} finally {
			statusMutatingSubject = false;
		}
	}

	async function createSubjectGroup() {
		if (!canManageSetup) {
			return;
		}

		if (!newGroupType.trim() || !newGroupName.trim()) {
			groupMutationError = text.directory.enterGroupIdentity;
			return;
		}

		creatingGroup = true;
		groupMutationError = null;

		try {
			const created = await productApi.createSubjectGroup({
				type: newGroupType.trim(),
				name: newGroupName.trim(),
				parentGroupId: optionalText(newGroupParentId),
				attributes: newGroupAttributes.trim() || '{}'
			});
			newGroupName = '';
			newGroupParentId = '';
			newGroupAttributes = '{}';
			await loadDirectory();
			selectedGroupId = created.id;
			newSubjectGroupId = created.id;
		} catch (error) {
			groupMutationError = toProductApiErrorMessage(error, text.directory.groupCreateFailed);
		} finally {
			creatingGroup = false;
		}
	}

	async function addSubjectGroupMember() {
		if (!canManageSetup || !selectedSubjectId || !selectedGroupId) {
			membershipError = text.directory.selectPersonAndGroup;
			return;
		}

		addingMembership = true;
		membershipError = null;

		try {
			await productApi.addSubjectGroupMember(selectedGroupId, {
				subjectId: selectedSubjectId,
				roleInGroup: optionalText(membershipRole)
			});
			membershipRole = '';
			await loadDirectory();
		} catch (error) {
			membershipError = toProductApiErrorMessage(error, text.directory.membershipSaveFailed);
		} finally {
			addingMembership = false;
		}
	}

	function membershipRemovalKey(subjectId: string, groupId: string) {
		return `${subjectId}:${groupId}`;
	}

	function beginMembershipRemoval(subjectId: string, membership: SubjectGroupMembershipResponse) {
		pendingMembershipRemovalKey = membershipRemovalKey(subjectId, membership.groupId);
		membershipRemovalError = null;
	}

	function cancelMembershipRemoval() {
		pendingMembershipRemovalKey = '';
		membershipRemovalError = null;
	}

	async function removeSubjectGroupMember(
		subjectId: string,
		membership: SubjectGroupMembershipResponse
	) {
		if (!canManageSetup) {
			return;
		}

		const removalKey = membershipRemovalKey(subjectId, membership.groupId);
		removingMembershipKey = removalKey;
		membershipRemovalError = null;

		try {
			await productApi.removeSubjectGroupMember(membership.groupId, subjectId);
			pendingMembershipRemovalKey = '';
			await loadDirectory();
			selectedSubjectId = subjectId;
			detailsSubjectId = detailsSubjectId || subjectId;
			syncSelectedSubjectFields(directory?.subjects ?? []);
		} catch (error) {
			membershipRemovalError = toProductApiErrorMessage(
				error,
				'Group membership could not be removed.'
			);
		} finally {
			removingMembershipKey = '';
		}
	}

	async function setSubjectManager() {
		if (!canManageSetup || !selectedSubjectId) {
			managerError = text.directory.selectPerson;
			return;
		}

		savingManager = true;
		managerError = null;

		try {
			await productApi.setSubjectManager(selectedSubjectId, {
				managerSubjectId: optionalText(managerSubjectId),
				validFrom: optionalText(managerValidFrom)
			});
			managerValidFrom = '';
			await loadDirectory();
		} catch (error) {
			managerError = toProductApiErrorMessage(error, text.directory.managerSaveFailed);
		} finally {
			savingManager = false;
		}
	}

	function syncSelections(
		nextSubjects: SubjectDirectoryItemResponse[],
		nextGroups: SubjectGroupResponse[]
	) {
		if (!nextSubjects.some((subject) => subject.id === selectedSubjectId)) {
			selectedSubjectId = nextSubjects[0]?.id ?? '';
		}

		if (!nextGroups.some((group) => group.id === selectedGroupId)) {
			selectedGroupId = nextGroups[0]?.id ?? '';
		}

		if (newSubjectGroupId && !nextGroups.some((group) => group.id === newSubjectGroupId)) {
			newSubjectGroupId = '';
		}
	}

	function syncSelectedSubjectFields(nextSubjects: SubjectDirectoryItemResponse[]) {
		const subject = nextSubjects.find((candidate) => candidate.id === selectedSubjectId) ?? null;
		if (!subject) {
			editSubjectSourceId = '';
			editSubjectDisplayName = '';
			editSubjectEmail = '';
			editSubjectExternalId = '';
			editSubjectLocale = 'en';
			editSubjectAttributes = '{}';
			managerSubjectId = '';
			return;
		}

		editSubjectSourceId = subject.id;
		editSubjectDisplayName = subject.displayName ?? '';
		editSubjectEmail = subject.email ?? '';
		editSubjectExternalId = subject.externalId ?? '';
		editSubjectLocale = subject.locale;
		editSubjectAttributes = subject.attributes;
		managerSubjectId = subject.managerSubjectId ?? '';
	}

	function syncGraphImportSelections(workspace: DirectoryImportWorkspaceResponse) {
		if (
			!workspace.connections.some((connection) => connection.id === selectedDirectoryConnectionId)
		) {
			selectedDirectoryConnectionId = workspace.connections[0]?.id ?? '';
		}

		if (!workspace.rules.some((rule) => rule.id === selectedDirectoryImportRuleId)) {
			selectedDirectoryImportRuleId = workspace.rules[0]?.id ?? '';
		}
	}

	function mergeGraphWorkspace(
		updates: Partial<Pick<DirectoryImportWorkspaceResponse, 'connections' | 'rules' | 'recentRuns'>>
	) {
		return {
			tenantId: graphWorkspace?.tenantId ?? authSession?.tenantId ?? '',
			connections: updates.connections ?? graphWorkspace?.connections ?? [],
			rules: updates.rules ?? graphWorkspace?.rules ?? [],
			recentRuns: updates.recentRuns ?? graphWorkspace?.recentRuns ?? []
		};
	}

	function splitList(value: string) {
		return value
			.split(/[,\n]/)
			.map((item) => item.trim())
			.filter(Boolean);
	}

	function buildGraphImportCriteria() {
		const criteria: Record<string, unknown> = {
			accountEnabled: true,
			excludeGuests: true
		};
		const departments = splitList(graphRuleDepartments);
		const groupIds = splitList(graphRuleGroupIds);
		const jobTitleContains = graphRuleJobTitleContains.trim();

		if (departments.length > 0) {
			criteria.departments = departments;
		}

		if (groupIds.length > 0) {
			criteria.groupIds = groupIds;
		}

		if (jobTitleContains) {
			criteria.jobTitleContains = jobTitleContains;
		}

		if (graphRuleIncludeManagers) {
			criteria.includeManagerChain = true;
		}

		return criteria;
	}

	function buildGraphImportFieldSelection() {
		return {
			fields: ['displayName', 'mail', 'userPrincipalName', 'department', 'jobTitle']
		};
	}

	function optionalText(value: string) {
		const trimmed = value.trim();
		return trimmed.length > 0 ? trimmed : null;
	}

	function subjectLabel(subject: SubjectDirectoryItemResponse | null) {
		if (!subject) {
			return text.directory.noSubjectSelected;
		}

		return subject.displayName || subject.email || subject.externalId || subject.id;
	}

	function openSubjectDetails(subject: SubjectDirectoryItemResponse) {
		detailsSubjectId = subject.id;
		selectedSubjectId = subject.id;
		statusMutationReason = '';
		statusMutationError = null;
		syncSelectedSubjectFields(subjects);
		setTimeout(() => {
			personDetailsDialog?.focus({ preventScroll: true });
			personDetailsDialog?.scrollIntoView({ behavior: 'smooth', block: 'start' });
		}, 0);
	}

	function normalizeSelect(value: string, fallback: string) {
		const normalized = value.trim();

		return normalized.length > 0 ? normalized : fallback;
	}

	function groupParentLabel(group: SubjectGroupResponse) {
		if (!group.parentGroupId) {
			return text.directory.rootGroup;
		}

		return (
			groups.find((candidate) => candidate.id === group.parentGroupId)?.name ?? group.parentGroupId
		);
	}

	function formatImportAction(action: string) {
		return action
			.split(',')
			.filter(Boolean)
			.map((part) => part.replaceAll('_', ' '))
			.join(', ');
	}

	function formatDirectoryImportStatus(status: string) {
		return status
			.split('_')
			.filter(Boolean)
			.map((part) => part[0]?.toUpperCase() + part.slice(1))
			.join(' ');
	}

	function graphRunSummaryValue(summary: Record<string, unknown>, key: string) {
		const value = summary[key];

		return typeof value === 'number' ? value : 0;
	}

	function formatCount(value: number) {
		return countFormatter.format(value);
	}
</script>

<SurfaceHeader
	eyebrow={text.directory.eyebrow}
	title={text.directory.title}
	description={text.directory.description}
/>

{#if !canManageSetup}
	<InlineAlert
		variant="warning"
		title={text.directory.accessTitle}
		message={text.directory.accessMessage}
	/>
{:else}
	<section class="product-panel" aria-label={text.directory.title}>
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.directory.setup}</p>
				<h2 class="product-title">{text.directory.buildAudience}</h2>
				<p class="text-sm leading-6 text-[var(--color-text-muted)]">
					{text.directory.buildAudienceBody}
				</p>
			</div>
			<a class="primary-button" href="#directory-create">{text.directory.addPeopleOrGroups}</a>
		</div>

		<dl class="directory-count-list" role="group" aria-label={text.directory.countsAria}>
			<div class="directory-count-row">
				<dt class="directory-count-row__label">{text.directory.people}</dt>
				<dd class="directory-count-row__value">{directory?.summary.subjectCount ?? 0}</dd>
			</div>
			<div class="directory-count-row">
				<dt class="directory-count-row__label">{text.directory.groups}</dt>
				<dd class="directory-count-row__value">{directory?.summary.groupCount ?? 0}</dd>
			</div>
			<div class="directory-count-row">
				<dt class="directory-count-row__label">{text.directory.memberships}</dt>
				<dd class="directory-count-row__value">{membershipCount}</dd>
			</div>
			<div class="directory-count-row">
				<dt class="directory-count-row__label">{text.directory.managerLinks}</dt>
				<dd class="directory-count-row__value">
					{directory?.summary.managerRelationshipCount ?? 0}
				</dd>
			</div>
		</dl>

		<details
			class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3"
		>
			<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
				{text.directory.howUsed}
			</summary>
			<p class="mt-3 text-sm leading-6 text-[var(--color-text-muted)]">
				{text.directory.buildAudienceBody}
			</p>
		</details>
	</section>

	<section
		class="product-panel directory-graph-import-panel"
		data-priority="primary"
		aria-label="Microsoft Graph directory import"
	>
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">Microsoft Graph</p>
				<h2 class="product-title">Microsoft 365 directory import</h2>
				<p class="text-sm leading-6 text-[var(--color-text-muted)]">
					Connect a Microsoft 365 tenant, save an import rule, preview the diff, then apply the
					snapshot before study launch.
				</p>
			</div>
			<button type="button" class="secondary-button" onclick={loadGraphImportWorkspace}>
				<RefreshCcw size={16} aria-hidden="true" />
				<span>Refresh Graph state</span>
			</button>
		</div>

		{#if graphWorkspaceState === 'loading'}
			<p class="text-sm text-[var(--color-text-muted)]">
				Loading Microsoft Graph directory state...
			</p>
		{:else}
			{#if graphWorkspaceError}
				<InlineAlert
					variant="warning"
					title="Microsoft Graph import unavailable"
					message={graphWorkspaceError}
				/>
			{/if}
			{#if graphConnectionReturnStatus === 'connected'}
				<InlineAlert
					variant="success"
					title="Microsoft tenant connected"
					message="The tenant admin consent callback created a Microsoft Graph connection."
				/>
			{:else if graphConnectionReturnStatus === 'failed'}
				<InlineAlert
					variant="warning"
					title="Microsoft tenant connection failed"
					message="The Microsoft admin consent callback did not complete. Start consent again from this page."
				/>
			{/if}

			<dl class="directory-count-list" role="group" aria-label="Microsoft Graph import counts">
				<div class="directory-count-row">
					<dt class="directory-count-row__label">Connections</dt>
					<dd class="directory-count-row__value">{graphConnections.length}</dd>
				</div>
				<div class="directory-count-row">
					<dt class="directory-count-row__label">Saved rules</dt>
					<dd class="directory-count-row__value">{graphRules.length}</dd>
				</div>
				<div class="directory-count-row">
					<dt class="directory-count-row__label">Recent runs</dt>
					<dd class="directory-count-row__value">{graphRecentRuns.length}</dd>
				</div>
			</dl>

			<div class="directory-graph-grid">
				<div class="directory-graph-column" data-testid="graph-connection-column">
					<div>
						<p class="product-kicker">Connection</p>
						<h3 class="text-base font-semibold text-[var(--color-text)]">Microsoft tenant</h3>
					</div>

					<div class="grid gap-2">
						<button
							type="button"
							class="primary-button"
							disabled={startingGraphAdminConsent}
							onclick={startGraphAdminConsent}
						>
							{#if startingGraphAdminConsent}
								<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
							{:else}
								<Link2 size={16} aria-hidden="true" />
							{/if}
							<span>{startingGraphAdminConsent ? 'Opening Microsoft' : 'Connect Microsoft tenant'}</span>
						</button>
						{#if graphAdminConsentError}
							<p class="error-line" role="alert">{graphAdminConsentError}</p>
						{/if}
					</div>

					{#if graphConnections.length === 0}
						<InlineAlert
							variant="info"
							title="No Microsoft connection"
							message="Use Microsoft admin consent first. Open the manual fallback only if that flow cannot return."
						/>
					{:else}
						<div class="record-list">
							{#each graphConnections as connection (connection.id)}
								<article class="record-row" aria-label={connection.displayName}>
									<span class="record-row__header">
										<span class="record-row__title">{connection.displayName}</span>
										<span
											class="status-badge"
											data-status={connection.status === 'active' ? 'ready' : 'pending'}
										>
											{formatDirectoryImportStatus(connection.status)}
										</span>
									</span>
									<span class="record-grid">
										<span class="record-field">
											<span class="record-field__label">Domain</span>
											<span class="record-field__value">{connection.primaryDomain}</span>
										</span>
										<span class="record-field">
											<span class="record-field__label">Tenant</span>
											<span class="record-field__value">{connection.externalTenantId}</span>
										</span>
										<span class="record-field">
											<span class="record-field__label">Scopes</span>
											<span class="record-field__value">{connection.grantedScopes.join(', ')}</span>
										</span>
									</span>
								</article>
							{/each}
						</div>
					{/if}

					<details class="directory-graph-fallback">
						<summary>Manual connection fallback</summary>
						<form
							class="grid gap-3 pt-3"
							onsubmit={(event) => {
								event.preventDefault();
								void createGraphConnection();
							}}
						>
							<label class="field">
								<span>Connection name</span>
								<input bind:value={graphConnectionDisplayName} disabled={creatingGraphConnection} />
							</label>
							<label class="field">
								<span>Microsoft tenant ID</span>
								<input bind:value={graphConnectionTenantId} disabled={creatingGraphConnection} />
							</label>
							<label class="field">
								<span>Primary domain</span>
								<input bind:value={graphConnectionPrimaryDomain} disabled={creatingGraphConnection} />
							</label>
							<label class="field">
								<span>Granted scopes</span>
								<input bind:value={graphConnectionScopes} disabled={creatingGraphConnection} />
							</label>
							<button type="submit" class="secondary-button" disabled={creatingGraphConnection}>
								{#if creatingGraphConnection}
									<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
								{:else}
									<Plus size={16} aria-hidden="true" />
								{/if}
								<span>{creatingGraphConnection ? 'Saving connection' : 'Save connection'}</span>
							</button>
							{#if graphConnectionError}
								<p class="error-line" role="alert">{graphConnectionError}</p>
							{/if}
						</form>
					</details>
				</div>

				<div class="directory-graph-column directory-graph-column--rule" data-testid="graph-rule-column">
					<form
						class="grid gap-3"
						onsubmit={(event) => {
							event.preventDefault();
							void saveGraphImportRule();
						}}
					>
						<div>
							<p class="product-kicker">Import rule</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								Saved Microsoft filter
							</h3>
						</div>
						<label class="field">
							<span>Connection</span>
							<select
								bind:value={selectedDirectoryConnectionId}
								disabled={graphConnections.length === 0 || graphImportBusy}
							>
								{#each graphConnections as connection (connection.id)}
									<option value={connection.id}>{connection.displayName}</option>
								{/each}
							</select>
						</label>
						<div class="grid gap-3 md:grid-cols-2">
							<label class="field">
								<span>Rule name</span>
								<input bind:value={graphRuleName} disabled={graphImportBusy} />
							</label>
							<label class="field">
								<span>Departments</span>
								<input bind:value={graphRuleDepartments} disabled={graphImportBusy} />
							</label>
							<label class="field">
								<span>Job title contains</span>
								<input bind:value={graphRuleJobTitleContains} disabled={graphImportBusy} />
							</label>
							<label class="field">
								<span>Group IDs</span>
								<input bind:value={graphRuleGroupIds} disabled={graphImportBusy} />
							</label>
						</div>
						<div class="flex flex-wrap gap-4">
							<label class="checkbox-field">
								<input
									type="checkbox"
									bind:checked={graphRuleIncludeManagers}
									disabled={graphImportBusy}
								/>
								<span>Include manager links</span>
							</label>
							<label class="checkbox-field">
								<input
									type="checkbox"
									bind:checked={graphRuleMirrorMode}
									disabled={graphImportBusy}
								/>
								<span>Mirror Microsoft directory</span>
							</label>
						</div>
						{#if graphRuleMirrorMode}
							<label class="field">
								<span>Mirror confirmation</span>
								<input
									bind:value={graphRuleMirrorConfirmation}
									disabled={graphImportBusy}
									placeholder={graphMirrorConfirmationText}
									aria-describedby="graph-mirror-confirmation-help"
								/>
								<span id="graph-mirror-confirmation-help" class="field-help">
									Type {graphMirrorConfirmationText} to confirm full Microsoft directory
									mirroring.
								</span>
								<span class="field-help">
									Mirror mode is for broad Microsoft directory refreshes. For normal testing,
									use department or group filters first.
								</span>
							</label>
						{/if}
						<button type="submit" class="primary-button" disabled={graphSaveRuleDisabled}>
							{#if savingGraphRule}
								<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
							{:else}
								<Save size={17} aria-hidden="true" />
							{/if}
							<span>{savingGraphRule ? 'Saving import rule' : 'Save import rule'}</span>
						</button>
						{#if graphRuleError}
							<p class="error-line" role="alert">{graphRuleError}</p>
						{/if}
					</form>

					<div class="directory-graph-preview">
						<div class="product-panel__header">
							<div>
								<p class="product-kicker">Preview and apply</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{selectedDirectoryImportRule?.name ?? 'No saved rule selected'}
								</h3>
							</div>
							<div class="action-row">
								<button
									type="button"
									class="secondary-button"
									disabled={!selectedDirectoryImportRule || graphImportBusy}
									onclick={previewGraphImportRule}
								>
									{#if previewingGraphImport}
										<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
									{:else}
										<RefreshCcw size={16} aria-hidden="true" />
									{/if}
									<span>{previewingGraphImport ? 'Previewing import' : 'Preview import'}</span>
								</button>
								<button
									type="button"
									class="primary-button"
									disabled={!graphPreview || graphPreview.status !== 'previewed' || graphImportBusy}
									onclick={applyGraphImportRun}
								>
									{#if applyingGraphImport}
										<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
									{:else}
										<Save size={17} aria-hidden="true" />
									{/if}
									<span>{applyingGraphImport ? 'Applying import' : 'Apply import'}</span>
								</button>
							</div>
						</div>
						{#if graphImportError}
							<p class="error-line" role="alert">{graphImportError}</p>
						{/if}
						{#if graphPreview}
							<dl
								class="directory-count-list"
								role="group"
								aria-label="Microsoft Graph preview counts"
							>
								<div class="directory-count-row">
									<dt class="directory-count-row__label">Matched users</dt>
									<dd class="directory-count-row__value">
										{graphPreview.summary.matchedUserCount}
									</dd>
								</div>
								<div class="directory-count-row">
									<dt class="directory-count-row__label">Create subjects</dt>
									<dd class="directory-count-row__value">
										{graphPreview.summary.createSubjectCount}
									</dd>
								</div>
								<div class="directory-count-row">
									<dt class="directory-count-row__label">Update subjects</dt>
									<dd class="directory-count-row__value">
										{graphPreview.summary.updateSubjectCount}
									</dd>
								</div>
								<div class="directory-count-row">
									<dt class="directory-count-row__label">No change</dt>
									<dd class="directory-count-row__value">{graphPreview.summary.noChangeCount}</dd>
								</div>
							</dl>
							<p class="directory-sample-note">
								Showing {formatCount(graphPreview.summary.returnedItemCount)} of {formatCount(
									graphPreview.summary.totalItemCount
								)} planned actions
							</p>
							<div class="directory-compact-list" aria-label="Microsoft Graph preview rows">
								{#each graphPreview.items as item, index}
									<article
										class="directory-compact-row"
										aria-label={`Preview item ${index + 1}`}
										data-testid="graph-preview-row"
									>
										<span class="directory-compact-row__header">
											<span class="directory-compact-row__title">{formatImportAction(item.action)}</span>
											<span class="status-badge" data-status="pending">
												{formatDirectoryImportStatus(item.status)}
											</span>
										</span>
										<span class="directory-compact-row__body">
											<span>{item.displayName ?? 'Unknown'}</span>
											<span>{item.email ?? text.directory.notAvailable}</span>
											<span>{item.issueCode ?? 'No issue'}</span>
										</span>
									</article>
								{/each}
							</div>
						{/if}
						{#if graphApplyResult}
							<InlineAlert
								variant="info"
								title="Directory snapshot applied"
								message={`Created ${graphApplyResult.summary.createdSubjectCount} subjects, updated ${graphApplyResult.summary.updatedSubjectCount}, and added ${graphApplyResult.summary.addedMembershipCount} memberships.`}
							/>
						{/if}
					</div>

					{#if graphRecentRuns.length > 0}
						<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
							<div>
								<p class="product-kicker">Import history</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">Recent runs</h3>
							</div>
							<div class="record-list">
								{#each graphRecentRuns as run (run.id)}
									<article class="record-row" aria-label={run.ruleName}>
										<span class="record-row__header">
											<span class="record-row__title">{run.ruleName}</span>
											<span
												class="status-badge"
												data-status={run.status === 'applied' || run.status === 'previewed'
													? 'ready'
													: 'pending'}
											>
												{formatDirectoryImportStatus(run.status)}
											</span>
										</span>
										<span class="record-grid">
											<span class="record-field">
												<span class="record-field__label">Mode</span>
												<span class="record-field__value"
													>{formatDirectoryImportStatus(run.mode)}</span
												>
											</span>
											<span class="record-field">
												<span class="record-field__label">Created</span>
												<span class="record-field__value">
													{graphRunSummaryValue(run.summary, 'createSubjectCount') ||
														graphRunSummaryValue(run.summary, 'createdSubjectCount')}
												</span>
											</span>
											<span class="record-field">
												<span class="record-field__label">Updated</span>
												<span class="record-field__value">
													{graphRunSummaryValue(run.summary, 'updateSubjectCount') ||
														graphRunSummaryValue(run.summary, 'updatedSubjectCount')}
												</span>
											</span>
										</span>
									</article>
								{/each}
							</div>
						</div>
					{/if}
				</div>
			</div>
		{/if}
	</section>

	<details class="product-panel directory-maintenance-panel" aria-label="Import audience CSV">
		<summary class="directory-maintenance-summary">
			<span>
				<span class="product-kicker">{text.directory.csvImport}</span>
				<strong>{text.directory.csvTitle}</strong>
			</span>
			<span class="text-sm text-[var(--color-text-muted)]">Manual fallback</span>
		</summary>
		<div class="directory-maintenance-body">
			<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.directory.csvImport}</p>
				<h2 class="product-title">{text.directory.csvTitle}</h2>
				<p class="text-sm leading-6 text-[var(--color-text-muted)]">
					{text.directory.csvBody}
				</p>
			</div>
			<a class="secondary-button" href={csvTemplateHref} download="directory-import-template.csv">
				{text.directory.downloadTemplate}
			</a>
			</div>

			<form
			class="grid gap-4"
			onsubmit={(event) => {
				event.preventDefault();
				void previewSubjectDirectoryCsv();
			}}
		>
			<label class="field">
				<span>{text.directory.csvFile}</span>
				<input type="file" accept=".csv,text/csv" onchange={loadCsvFile} disabled={importBusy} />
			</label>
			<label class="field">
				<span>{text.directory.csvRows}</span>
				<textarea
					rows="7"
					bind:value={importCsvContent}
					disabled={importBusy}
					placeholder={'external_id,email,display_name,locale,group_type,group_name,role_in_group\nemp-001,ana@example.test,Ana Analyst,en,department,Research,member'}
				></textarea>
				<span class="text-sm text-[var(--color-text-muted)]">
					{text.directory.csvHelp}
				</span>
			</label>
			<div class="action-row">
				<button type="submit" class="primary-button" disabled={importBusy}>
					{#if previewingCsv}
						<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
					{:else}
						<Upload size={17} aria-hidden="true" />
					{/if}
					<span>{previewingCsv ? text.directory.previewing : text.directory.previewCsv}</span>
				</button>
				<button
					type="button"
					class="secondary-button"
					disabled={importBusy || !importResult?.dryRun || importHasFailures}
					onclick={applySubjectDirectoryCsv}
				>
					{#if applyingCsv}
						<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
					{:else}
						<Save size={17} aria-hidden="true" />
					{/if}
					<span>{applyingCsv ? text.directory.applying : text.directory.applyImport}</span>
				</button>
			</div>
			{#if importResult?.dryRun && importHasFailures}
				<p class="error-line" role="alert">
					{text.directory.fixFailedRows}
				</p>
			{/if}
			{#if importResult?.dryRun && !importHasFailures}
				<InlineAlert
					variant="info"
					title="Preview only"
					message="Nothing has been saved yet. Review the actions below, then apply the import."
				/>
			{/if}
			{#if applyingCsv}
				<p class="text-sm text-[var(--color-text-muted)]">
					{text.directory.applyingImport}
				</p>
			{/if}
			{#if previewingCsv}
				<p class="text-sm text-[var(--color-text-muted)]">
					{text.directory.checkingRows}
				</p>
			{/if}
			{#if importError}
				<p class="error-line" role="alert">{importError}</p>
			{/if}
			{#if importResult}
				<div
					class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3"
				>
					<p class="text-sm font-semibold text-[var(--color-text)]">
						{importResult.dryRun ? text.directory.previewed : text.directory.imported}
						{importResult.importedRowCount} of
						{importResult.rowCount} rows
					</p>
					<dl class="mt-3 grid gap-2 text-sm md:grid-cols-3">
						<div>
							<dt class="text-[var(--color-text-muted)]">
								{importResult.dryRun ? text.directory.peopleToCreate : text.directory.peopleCreated}
							</dt>
							<dd class="font-semibold">{importResult.createdSubjectCount}</dd>
						</div>
						<div>
							<dt class="text-[var(--color-text-muted)]">
								{importResult.dryRun ? text.directory.peopleToUpdate : text.directory.peopleUpdated}
							</dt>
							<dd class="font-semibold">{importResult.updatedSubjectCount}</dd>
						</div>
						<div>
							<dt class="text-[var(--color-text-muted)]">
								{importResult.dryRun ? text.directory.groupsToCreate : text.directory.groupsCreated}
							</dt>
							<dd class="font-semibold">{importResult.createdGroupCount}</dd>
						</div>
						<div>
							<dt class="text-[var(--color-text-muted)]">
								{importResult.dryRun
									? text.directory.membershipsToAdd
									: text.directory.membershipsAdded}
							</dt>
							<dd class="font-semibold">{importResult.addedMembershipCount}</dd>
						</div>
						<div>
							<dt class="text-[var(--color-text-muted)]">{text.directory.membershipsPresent}</dt>
							<dd class="font-semibold">{importResult.skippedMembershipCount}</dd>
						</div>
					</dl>
					{#if importResult.rows.some((row) => row.status === 'failed')}
						<div class="mt-4 grid gap-2" aria-label="CSV import row issues">
							<p class="text-sm font-semibold text-[var(--color-text)]">
								{text.directory.rowsNeedingAttention}
							</p>
							{#each importResult.rows.filter((row) => row.status === 'failed') as row}
								<article
									class="rounded border border-[var(--color-border)] bg-[var(--color-surface)] p-3 text-sm"
								>
									<p class="font-semibold">Row {row.rowNumber}</p>
									<p class="text-[var(--color-text-muted)]">
										{row.displayName ?? row.email ?? row.externalId ?? text.directory.unmatchedRow}
									</p>
									<ul class="mt-2 list-disc pl-5">
										{#each row.issues as issue}
											<li>{issue}</li>
										{/each}
									</ul>
								</article>
							{/each}
						</div>
					{:else}
						<p class="mt-4 text-sm text-[var(--color-text-muted)]">
							All imported rows were accepted.
						</p>
					{/if}
					<details class="mt-3">
						<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
							Import actions
						</summary>
						<div class="mt-2 grid gap-2">
							{#each importResult.rows.filter((row) => row.status === 'imported') as row}
								<p class="text-sm text-[var(--color-text-muted)]">
									Row {row.rowNumber}: {row.displayName ?? row.email ?? row.externalId}
									- {formatImportAction(row.action)}
								</p>
							{/each}
						</div>
					</details>
				</div>
			{/if}
			</form>
		</div>
	</details>

	<section
		class="product-panel"
		aria-label={text.directory.peopleDirectoryAria}
		data-testid="workspace-people-directory"
	>
		<LoadingBoundary loading={loadState === 'loading'} label={text.directory.loadingDirectory}>
			{#if loadState === 'error' && errorMessage}
				<ErrorPanel
					title={text.directory.unavailableTitle}
					message={errorMessage}
					retryLabel={text.directory.retryDirectory}
					onRetry={loadDirectory}
				/>
			{:else if directory && groupList}
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{text.directory.people}</p>
						<h2 class="product-title">{text.directory.peopleInWorkspace}</h2>
						<p class="text-sm leading-6 text-[var(--color-text-muted)]">
							Showing {formatCount(directoryReturnedCount)} of {formatCount(directoryFilteredCount)} people
							{#if activeDirectorySearch}
								matching "{activeDirectorySearch}"
							{/if}
							{#if directoryFilteredCount !== directoryTotalCount}
								from {formatCount(directoryTotalCount)} total
							{/if}
						</p>
					</div>
					<form
						class="directory-search directory-filters"
						onsubmit={(event) => {
							event.preventDefault();
							submitDirectoryFilters();
						}}
					>
						<label class="field">
							<span>Search people</span>
							<input
								bind:value={directorySearch}
								placeholder="Name, email, department"
								disabled={loadState === 'loading'}
							/>
						</label>
						<label class="field">
							<span>Source</span>
							<select
								aria-label="Source"
								bind:value={directorySource}
								disabled={loadState === 'loading'}
							>
								<option value="all">All sources</option>
								<option value="microsoft_graph">Microsoft 365</option>
								<option value="manual">Manual</option>
								<option value="csv">CSV</option>
							</select>
						</label>
						<label class="field">
							<span>Status</span>
							<select
								aria-label="Status"
								bind:value={directoryStatus}
								disabled={loadState === 'loading'}
							>
								<option value="active">Active</option>
								<option value="deactivated">Deactivated</option>
								<option value="excluded">Excluded</option>
								<option value="all">All statuses</option>
							</select>
						</label>
						<label class="field">
							<span>Group</span>
							<select
								aria-label="Group"
								bind:value={directoryGroupId}
								disabled={loadState === 'loading'}
							>
								<option value="all">All groups</option>
								{#each groups as group (group.id)}
									<option value={group.id}>{group.name}</option>
								{/each}
							</select>
						</label>
						<label class="field">
							<span>Manager</span>
							<select
								aria-label="Manager"
								bind:value={directoryManager}
								disabled={loadState === 'loading'}
							>
								<option value="any">Any manager state</option>
								<option value="assigned">Has manager</option>
								<option value="missing">Missing manager</option>
							</select>
						</label>
						<label class="field">
							<span>Contact</span>
							<select
								aria-label="Contact"
								bind:value={directoryContact}
								disabled={loadState === 'loading'}
							>
								<option value="any">Any contact state</option>
								<option value="has_email">Has email</option>
								<option value="missing_email">Missing email</option>
							</select>
						</label>
						<label class="field">
							<span>Sort by</span>
							<select
								aria-label="Sort by"
								bind:value={directorySort}
								disabled={loadState === 'loading'}
							>
								<option value="name_asc">Name A-Z</option>
								<option value="name_desc">Name Z-A</option>
								<option value="department_asc">Department</option>
								<option value="source_asc">Source</option>
								<option value="updated_desc">Recently updated</option>
							</select>
						</label>
						<div class="action-row">
							<button type="submit" class="secondary-button" disabled={loadState === 'loading'}>
								<RefreshCcw size={16} aria-hidden="true" />
								<span>Apply filters</span>
							</button>
							<button
								type="button"
								class="secondary-button"
								disabled={loadState === 'loading'}
								onclick={clearDirectoryFilters}
							>
								Clear
							</button>
						</div>
					</form>
				</div>

				<div class="directory-toolbar">
					<dl
						class="directory-count-list"
						role="group"
						aria-label={text.directory.directoryGraphCounts}
					>
						<div class="directory-count-row">
							<dt class="directory-count-row__label">{text.directory.subjects}</dt>
							<dd class="directory-count-row__value">{directory.summary.subjectCount}</dd>
						</div>
						<div class="directory-count-row">
							<dt class="directory-count-row__label">{text.directory.groups}</dt>
							<dd class="directory-count-row__value">{directory.summary.groupCount}</dd>
						</div>
						<div class="directory-count-row">
							<dt class="directory-count-row__label">{text.directory.managerLinks}</dt>
							<dd class="directory-count-row__value">
								{directory.summary.managerRelationshipCount}
							</dd>
						</div>
					</dl>
					<div class="directory-pagination" aria-label="People directory pagination">
						<p class="text-sm text-[var(--color-text-muted)]">
							Rows {formatCount(directoryPageStart)}-{formatCount(directoryPageEnd)} of {formatCount(
								directoryFilteredCount
							)}
						</p>
						<div class="action-row">
							<button
								type="button"
								class="secondary-button"
								disabled={!directoryCanPageBack || loadState === 'loading'}
								onclick={loadPreviousDirectoryPage}
							>
								Previous
							</button>
							<button
								type="button"
								class="secondary-button"
								disabled={!directoryCanPageForward || loadState === 'loading'}
								onclick={loadNextDirectoryPage}
							>
								Next
							</button>
						</div>
					</div>
				</div>

				{#if subjects.length === 0}
					<EmptyState
						title="No people yet"
						description="Add people before configuring audiences."
					/>
				{:else}
					<div class="table-wrap">
						<table class="directory-table directory-people-table">
							<thead>
								<tr>
									<th scope="col">Person</th>
									<th scope="col">Email</th>
									<th scope="col">Department / role</th>
									<th scope="col">{text.directory.manager}</th>
									<th scope="col">{text.directory.groups}</th>
									<th scope="col">Source</th>
									<th scope="col">Status</th>
									<th scope="col">Actions</th>
								</tr>
							</thead>
							<tbody>
								{#each subjects as subject (subject.id)}
									<tr
										data-testid="directory-person-row"
										data-selected={detailsSubjectId === subject.id ? 'true' : undefined}
									>
										<td>
											<strong>{subjectLabel(subject)}</strong>
											<span>{subject.locale}</span>
										</td>
										<td>{subject.email ?? text.directory.notAvailable}</td>
										<td>
											<strong>{subject.department ?? text.directory.notAvailable}</strong>
											<span>{subject.jobTitle ?? subject.employeeType ?? ''}</span>
										</td>
										<td>
											{subject.managerDisplayName ?? text.directory.noManager}
											{#if subject.directReportCount > 0}
												<span>{subject.directReportCount} direct reports</span>
											{/if}
										</td>
										<td>
											{#if subject.groups.length === 0}
												<span class="text-xs text-[var(--color-text-muted)]">
													{text.directory.noMemberships}
												</span>
											{:else}
												<div class="directory-chip-list">
													{#each subject.groups.slice(0, 3) as membership (membership.groupId)}
														<span class="directory-chip">
															<UserRound size={13} aria-hidden="true" />
															{membership.groupName}
														</span>
													{/each}
													{#if subject.groups.length > 3}
														<span class="directory-chip">+{subject.groups.length - 3}</span>
													{/if}
												</div>
											{/if}
										</td>
										<td>
											<span class="directory-badge directory-badge--source">{subject.sourceLabel}</span>
										</td>
										<td>
											<span
												class:directory-badge--muted={subject.status !== 'active'}
												class="directory-badge"
											>
												{subject.statusLabel}
											</span>
										</td>
										<td>
											<button
												type="button"
												class:directory-row-action--active={detailsSubjectId === subject.id}
												class="secondary-button directory-row-action"
												aria-label={`${detailsSubjectId === subject.id ? 'Viewing' : 'View'} ${subjectLabel(subject)}`}
												onclick={() => openSubjectDetails(subject)}
											>
												{detailsSubjectId === subject.id ? 'Viewing' : 'View'}
											</button>
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			{/if}
		</LoadingBoundary>
	</section>

	{#if detailsSubject}
		<dialog
			bind:this={personDetailsDialog}
			class="person-drawer"
			aria-label="Person details"
			tabindex="-1"
			open
		>
			<div class="person-drawer__header">
				<div>
					<p class="product-kicker">{detailsSubject.sourceLabel}</p>
					<h2 class="product-title">{subjectLabel(detailsSubject)}</h2>
					<p class="text-sm text-[var(--color-text-muted)]">
						{detailsSubject.email ?? 'No email'} · {detailsSubject.statusLabel}
					</p>
				</div>
				<button
					type="button"
					class="secondary-button"
					aria-label="Close person details"
					onclick={() => {
						detailsSubjectId = '';
						statusMutationReason = '';
						statusMutationError = null;
					}}
				>
					Close
				</button>
			</div>

			<div class="person-drawer__grid">
				<div class="record-field">
					<p class="record-field__label">Department</p>
					<p class="record-field__value">{detailsSubject.department ?? text.directory.notAvailable}</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">Job title</p>
					<p class="record-field__value">{detailsSubject.jobTitle ?? text.directory.notAvailable}</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">Manager</p>
					<p class="record-field__value">
						{detailsSubject.managerDisplayName ?? text.directory.noManager}
					</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">Direct reports</p>
					<p class="record-field__value">{detailsSubject.directReportCount}</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">Office</p>
					<p class="record-field__value">
						{detailsSubject.officeLocation ?? text.directory.notAvailable}
					</p>
				</div>
				<div class="record-field">
					<p class="record-field__label">Employee type</p>
					<p class="record-field__value">
						{detailsSubject.employeeType ?? text.directory.notAvailable}
					</p>
				</div>
			</div>

			<div class="person-drawer__section" role="region" aria-label="Current memberships">
				<h3>Current memberships</h3>
				{#if detailsSubject.groups.length === 0}
					<p class="text-sm text-[var(--color-text-muted)]">{text.directory.noMemberships}</p>
				{:else}
					<div class="directory-membership-list">
						{#each detailsSubject.groups as membership (membership.groupId)}
							<article class="directory-membership-row">
								<div>
									<p class="directory-membership-row__title">
										<UserRound size={13} aria-hidden="true" />
										{membership.groupName}
									</p>
									<p class="text-xs text-[var(--color-text-muted)]">
										{membership.groupType}
										{#if membership.roleInGroup}
											· {membership.roleInGroup}
										{/if}
									</p>
								</div>
								<div class="directory-membership-row__actions">
									<button
										type="button"
										class="secondary-button"
										aria-label={`Remove ${subjectLabel(detailsSubject)} from ${membership.groupName}`}
										disabled={removingMembershipKey === membershipRemovalKey(detailsSubject.id, membership.groupId)}
										onclick={() => beginMembershipRemoval(detailsSubject.id, membership)}
									>
										Remove
									</button>
								</div>
								{#if pendingMembershipRemovalKey === membershipRemovalKey(detailsSubject.id, membership.groupId)}
									<div class="directory-membership-confirmation">
										<p class="text-sm font-semibold text-[var(--color-text)]">
											Remove {subjectLabel(detailsSubject)} from {membership.groupName}?
										</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											This only changes ValidatedScale. It does not change Microsoft 365.
										</p>
										<div class="action-row">
											<button
												type="button"
												class="secondary-button"
												disabled={removingMembershipKey === membershipRemovalKey(detailsSubject.id, membership.groupId)}
												aria-label={`Confirm remove ${subjectLabel(detailsSubject)} from ${membership.groupName}`}
												onclick={() => void removeSubjectGroupMember(detailsSubject.id, membership)}
											>
												{removingMembershipKey === membershipRemovalKey(detailsSubject.id, membership.groupId)
													? 'Removing...'
													: 'Remove'}
											</button>
											<button
												type="button"
												class="secondary-button"
												disabled={removingMembershipKey === membershipRemovalKey(detailsSubject.id, membership.groupId)}
												onclick={cancelMembershipRemoval}
											>
												Cancel
											</button>
										</div>
										{#if membershipRemovalError}
											<p class="error-line" role="alert">{membershipRemovalError}</p>
										{/if}
									</div>
								{/if}
							</article>
						{/each}
					</div>
				{/if}
			</div>

			<details class="person-drawer__section">
				<summary>Technical details</summary>
				<div class="record-field mt-3">
					<p class="record-field__label">External id</p>
					<p class="record-field__value">{detailsSubject.externalId ?? text.directory.notAvailable}</p>
				</div>
			</details>

			<div class="person-drawer__section person-drawer__status">
				<h3>Status management</h3>
				<p class="text-sm text-[var(--color-text-muted)]">
					Local status controls study eligibility. Deactivation preserves identity links and
					historical study references.
				</p>
				<label class="field">
					<span>Reason</span>
					<input bind:value={statusMutationReason} disabled={statusMutatingSubject} />
				</label>
				{#if statusMutationError}
					<p class="error-line" role="alert">{statusMutationError}</p>
				{/if}
				<div class="action-row">
					{#if detailsSubject.status === 'active'}
						<button
							type="button"
							class="secondary-button"
							disabled={statusMutatingSubject}
							onclick={() => setDetailsSubjectStatus('excluded')}
						>
							{statusMutatingSubject ? 'Updating...' : 'Exclude from audiences'}
						</button>
						<button
							type="button"
							class="secondary-button"
							disabled={statusMutatingSubject}
							onclick={() => setDetailsSubjectStatus('deactivated')}
						>
							{statusMutatingSubject ? 'Updating...' : 'Deactivate'}
						</button>
					{:else if detailsSubject.status === 'excluded'}
						<button
							type="button"
							class="primary-button"
							disabled={statusMutatingSubject}
							onclick={() => setDetailsSubjectStatus('active')}
						>
							{statusMutatingSubject ? 'Updating...' : 'Return to active'}
						</button>
						<button
							type="button"
							class="secondary-button"
							disabled={statusMutatingSubject}
							onclick={() => setDetailsSubjectStatus('deactivated')}
						>
							{statusMutatingSubject ? 'Updating...' : 'Deactivate'}
						</button>
					{:else}
						<button
							type="button"
							class="primary-button"
							disabled={statusMutatingSubject}
							onclick={() => setDetailsSubjectStatus('active')}
						>
							{statusMutatingSubject ? 'Updating...' : 'Reactivate'}
						</button>
					{/if}
				</div>
			</div>
		</dialog>
	{/if}

	<section class="product-panel" aria-label="Audience groups">
		{#if groupList}
			<div class="product-panel__header">
				<div>
					<p class="product-kicker">{text.directory.groups}</p>
					<h2 class="product-title">{text.directory.audienceGroups}</h2>
				</div>
			</div>

			{#if groups.length === 0}
				<EmptyState title="No groups" description="Create groups before assigning subjects." />
			{:else}
				<div class="directory-sample-note">
					Showing {formatCount(visibleGroups.length)} of {formatCount(groups.length)} groups
				</div>
				<div class="table-wrap">
					<table class="directory-table directory-groups-table">
						<thead>
							<tr>
								<th scope="col">Group</th>
								<th scope="col">Type</th>
								<th scope="col">{text.directory.parent}</th>
								<th scope="col">{text.directory.members}</th>
							</tr>
						</thead>
						<tbody>
							{#each visibleGroups as group (group.id)}
								<tr>
									<td>{group.name}</td>
									<td>{group.type}</td>
									<td>{groupParentLabel(group)}</td>
									<td>{group.memberCount}</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			{/if}
		{/if}
	</section>

	<details
		id="directory-create"
		class="product-panel"
		role="region"
		aria-label={text.directory.createRecordsAria}
	>
		<summary class="directory-maintenance-summary">
			<span>
				<span class="product-kicker">{text.directory.addRecords}</span>
				<strong>{text.directory.title}</strong>
			</span>
			<span class="text-sm text-[var(--color-text-muted)]">Manual maintenance</span>
		</summary>
		<div class="directory-maintenance-body">
			<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.directory.addRecords}</p>
				<h2 class="product-title">{text.directory.title}</h2>
			</div>
			<button type="button" class="secondary-button" onclick={() => void loadDirectory()}>
				<RefreshCcw size={16} aria-hidden="true" />
				<span>{text.directory.refresh}</span>
			</button>
			</div>

			<div class="grid gap-4 xl:grid-cols-2">
			<form
				class="grid gap-3"
				onsubmit={(event) => {
					event.preventDefault();
					void createSubject();
				}}
			>
				<div>
					<p class="product-kicker">{text.directory.person}</p>
					<h3 class="text-base font-semibold text-[var(--color-text)]">
						{text.directory.newPerson}
					</h3>
				</div>
				<div class="grid gap-3 md:grid-cols-2">
					<label class="field">
						<span>{text.directory.displayName}</span>
						<input bind:value={newSubjectDisplayName} disabled={creatingSubject} />
					</label>
					<label class="field">
						<span>{text.directory.email}</span>
						<input type="email" bind:value={newSubjectEmail} disabled={creatingSubject} />
					</label>
					<label class="field">
						<span>Department / group</span>
						<select
							aria-label="Department / group"
							bind:value={newSubjectGroupId}
							disabled={creatingSubject || groups.length === 0}
						>
							<option value="">No group yet</option>
							{#each groups as group (group.id)}
								<option value={group.id}>{group.name}</option>
							{/each}
						</select>
					</label>
					<label class="field">
						<span>{text.directory.roleInGroup}</span>
						<input
							aria-label="Role in group"
							bind:value={newSubjectRoleInGroup}
							disabled={creatingSubject || !newSubjectGroupId}
							placeholder="member"
						/>
					</label>
				</div>
				<details class="directory-advanced-panel">
					<summary>Advanced identity and attributes</summary>
					<div class="grid gap-3 pt-3 md:grid-cols-2">
						<label class="field">
							<span>{text.directory.externalId}</span>
							<input
								bind:value={newSubjectExternalId}
								disabled={creatingSubject}
								placeholder="Optional HR or student-system id"
							/>
						</label>
						<label class="field">
							<span>{text.directory.locale}</span>
							<input bind:value={newSubjectLocale} disabled={creatingSubject} />
						</label>
					</div>
					<label class="field mt-3">
						<span>{text.directory.attributesJson}</span>
						<textarea rows="3" bind:value={newSubjectAttributes} disabled={creatingSubject}></textarea>
					</label>
				</details>
				<button type="submit" class="primary-button" disabled={creatingSubject}>
					{#if creatingSubject}
						<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
					{:else}
						<Plus size={17} aria-hidden="true" />
					{/if}
					<span>{creatingSubject ? text.directory.creating : text.directory.addPerson}</span>
				</button>
				{#if subjectMutationError}
					<p class="error-line" role="alert">{subjectMutationError}</p>
				{/if}
			</form>

			<form
				class="grid gap-3"
				onsubmit={(event) => {
					event.preventDefault();
					void createSubjectGroup();
				}}
			>
				<div>
					<p class="product-kicker">Group</p>
					<h3 class="text-base font-semibold text-[var(--color-text)]">
						{text.directory.newGroup}
					</h3>
				</div>
				<div class="grid gap-3 md:grid-cols-2">
					<label class="field">
						<span>Type</span>
						<select bind:value={newGroupType} disabled={creatingGroup}>
							<option value="department">Department</option>
							<option value="team">Team</option>
							<option value="cohort">Cohort</option>
							<option value="course">Course</option>
							<option value="org_unit">Organization unit</option>
						</select>
					</label>
					<label class="field">
						<span>Name</span>
						<input bind:value={newGroupName} disabled={creatingGroup} />
					</label>
				</div>
				<label class="field">
					<span>{text.directory.parentGroup}</span>
					<select bind:value={newGroupParentId} disabled={creatingGroup || groups.length === 0}>
						<option value="">{text.directory.noParent}</option>
						{#each groups as group (group.id)}
							<option value={group.id}>{group.name}</option>
						{/each}
					</select>
				</label>
				<details class="directory-advanced-panel">
					<summary>Advanced attributes</summary>
					<label class="field mt-3">
						<span>{text.directory.attributesJson}</span>
						<textarea rows="3" bind:value={newGroupAttributes} disabled={creatingGroup}></textarea>
					</label>
				</details>
				<button type="submit" class="primary-button" disabled={creatingGroup}>
					{#if creatingGroup}
						<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
					{:else}
						<Plus size={17} aria-hidden="true" />
					{/if}
					<span>{creatingGroup ? text.directory.creating : text.directory.createGroup}</span>
				</button>
				{#if groupMutationError}
					<p class="error-line" role="alert">{groupMutationError}</p>
				{/if}
			</form>
			</div>
		</div>
	</details>

	<details
		class="product-panel"
		role="region"
		aria-label={text.directory.directoryRelationshipsAria}
	>
		<summary class="directory-maintenance-summary">
			<span>
				<span class="product-kicker">{text.directory.hierarchySetup}</span>
				<strong>{text.directory.membershipManager}</strong>
			</span>
			<span class="text-sm text-[var(--color-text-muted)]">Manual maintenance</span>
		</summary>
		<div class="directory-maintenance-body">
			<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.directory.hierarchySetup}</p>
				<h2 class="product-title">{text.directory.membershipManager}</h2>
				<p class="mt-1 text-sm text-[var(--color-text-muted)]">
					Use memberships for audience targeting. Use manager links only when a study needs
					hierarchy-aware review or reports-of-target context.
				</p>
			</div>
			</div>

			<details class="directory-advanced-panel">
				<summary>Edit selected person profile</summary>
			<form
			class="grid gap-3 pt-3"
			onsubmit={(event) => {
				event.preventDefault();
				void saveSelectedSubject();
			}}
		>
			<div class="grid gap-3 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
				<label class="field">
					<span>{text.directory.selectedPerson}</span>
					<select bind:value={selectedSubjectId} disabled={subjects.length === 0 || savingSubject}>
						{#each subjects as subject (subject.id)}
							<option value={subject.id}>{subjectLabel(subject)}</option>
						{/each}
					</select>
				</label>
				<label class="field">
					<span>{text.directory.displayName}</span>
					<input bind:value={editSubjectDisplayName} disabled={savingSubject || !selectedSubject} />
				</label>
				<label class="field">
					<span>{text.directory.email}</span>
					<input
						type="email"
						bind:value={editSubjectEmail}
						disabled={savingSubject || !selectedSubject}
					/>
				</label>
				<label class="field">
					<span>{text.directory.externalId}</span>
					<input bind:value={editSubjectExternalId} disabled={savingSubject || !selectedSubject} />
				</label>
				<label class="field">
					<span>{text.directory.locale}</span>
					<input bind:value={editSubjectLocale} disabled={savingSubject || !selectedSubject} />
				</label>
			</div>
			<details class="directory-advanced-panel">
				<summary>Advanced attributes</summary>
				<label class="field mt-3">
					<span>{text.directory.attributesJson}</span>
					<textarea
						rows="3"
						bind:value={editSubjectAttributes}
						disabled={savingSubject || !selectedSubject}
					></textarea>
				</label>
			</details>
			<button type="submit" class="secondary-button" disabled={savingSubject || !selectedSubject}>
				{#if savingSubject}
					<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
				{:else}
					<Save size={16} aria-hidden="true" />
				{/if}
				<span>{savingSubject ? text.directory.saving : text.directory.savePerson}</span>
			</button>
			{#if editSubjectError}
				<p class="error-line" role="alert">{editSubjectError}</p>
			{/if}
			</form>
			</details>

			<section class="directory-membership-list" aria-label="Current memberships">
				<div>
					<p class="product-kicker">Current memberships</p>
					<h3 class="text-base font-semibold text-[var(--color-text)]">
						{selectedSubject ? subjectLabel(selectedSubject) : text.directory.noSubjectSelected}
					</h3>
				</div>
				{#if !selectedSubject}
					<p class="text-sm text-[var(--color-text-muted)]">{text.directory.selectPerson}</p>
				{:else if selectedSubject.groups.length === 0}
					<p class="text-sm text-[var(--color-text-muted)]">{text.directory.noMemberships}</p>
				{:else}
					{#each selectedSubject.groups as membership (membership.groupId)}
						<article class="directory-membership-row">
							<div>
								<p class="directory-membership-row__title">
									<UserRound size={13} aria-hidden="true" />
									{membership.groupName}
								</p>
								<p class="text-xs text-[var(--color-text-muted)]">
									{membership.groupType}
									{#if membership.roleInGroup}
										· {membership.roleInGroup}
									{/if}
								</p>
							</div>
							<div class="directory-membership-row__actions">
								<button
									type="button"
									class="secondary-button"
									aria-label={`Remove ${subjectLabel(selectedSubject)} from ${membership.groupName}`}
									disabled={removingMembershipKey === membershipRemovalKey(selectedSubject.id, membership.groupId)}
									onclick={() => beginMembershipRemoval(selectedSubject.id, membership)}
								>
									Remove
								</button>
							</div>
							{#if pendingMembershipRemovalKey === membershipRemovalKey(selectedSubject.id, membership.groupId)}
								<div class="directory-membership-confirmation">
									<p class="text-sm font-semibold text-[var(--color-text)]">
										Remove {subjectLabel(selectedSubject)} from {membership.groupName}?
									</p>
									<p class="text-sm text-[var(--color-text-muted)]">
										This only changes ValidatedScale. It does not change Microsoft 365.
									</p>
									<div class="action-row">
										<button
											type="button"
											class="secondary-button"
											disabled={removingMembershipKey === membershipRemovalKey(selectedSubject.id, membership.groupId)}
											aria-label={`Confirm remove ${subjectLabel(selectedSubject)} from ${membership.groupName}`}
											onclick={() => void removeSubjectGroupMember(selectedSubject.id, membership)}
										>
											{removingMembershipKey === membershipRemovalKey(selectedSubject.id, membership.groupId)
												? 'Removing...'
												: 'Remove'}
										</button>
										<button
											type="button"
											class="secondary-button"
											disabled={removingMembershipKey === membershipRemovalKey(selectedSubject.id, membership.groupId)}
											onclick={cancelMembershipRemoval}
										>
											Cancel
										</button>
									</div>
									{#if membershipRemovalError}
										<p class="error-line" role="alert">{membershipRemovalError}</p>
									{/if}
								</div>
							{/if}
						</article>
					{/each}
				{/if}
			</section>

			<div class="grid gap-4 xl:grid-cols-2">
			<form
				class="grid gap-3"
				onsubmit={(event) => {
					event.preventDefault();
					void addSubjectGroupMember();
				}}
			>
				<div>
					<p class="product-kicker">Place person in group</p>
					<h3 class="text-base font-semibold text-[var(--color-text)]">Add to group</h3>
				</div>
				<div class="grid gap-3 md:grid-cols-2">
					<label class="field">
						<span>{text.directory.person}</span>
						<select
							bind:value={selectedSubjectId}
							disabled={subjects.length === 0 || addingMembership}
						>
							{#each subjects as subject (subject.id)}
								<option value={subject.id}>{subjectLabel(subject)}</option>
							{/each}
						</select>
					</label>
					<label class="field">
						<span>Department / group</span>
						<select bind:value={selectedGroupId} disabled={groups.length === 0 || addingMembership}>
							{#each groups as group (group.id)}
								<option value={group.id}>{group.name}</option>
							{/each}
						</select>
					</label>
				</div>
				<label class="field">
					<span>{text.directory.roleInGroup}</span>
					<input bind:value={membershipRole} disabled={addingMembership} />
				</label>
				<button
					type="submit"
					class="secondary-button"
					disabled={addingMembership || subjects.length === 0 || groups.length === 0}
				>
					{#if addingMembership}
						<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
					{:else}
						<Link2 size={16} aria-hidden="true" />
					{/if}
					<span>{addingMembership ? text.directory.saving : text.directory.addMembership}</span>
				</button>
				{#if membershipError}
					<p class="error-line" role="alert">{membershipError}</p>
				{/if}
			</form>

			<form
				class="grid gap-3"
				onsubmit={(event) => {
					event.preventDefault();
					void setSubjectManager();
				}}
			>
				<div>
					<p class="product-kicker">Hierarchy</p>
					<h3 class="text-base font-semibold text-[var(--color-text)]">Set manager</h3>
				</div>
				<div class="grid gap-3 md:grid-cols-2">
					<label class="field">
						<span>{text.directory.person}</span>
						<select
							bind:value={selectedSubjectId}
							disabled={subjects.length === 0 || savingManager}
						>
							{#each subjects as subject (subject.id)}
								<option value={subject.id}>{subjectLabel(subject)}</option>
							{/each}
						</select>
					</label>
					<label class="field">
						<span>{text.directory.manager}</span>
						<select
							bind:value={managerSubjectId}
							disabled={managerOptions.length === 0 || savingManager}
						>
							<option value="">{text.directory.noManager}</option>
							{#each managerOptions as subject (subject.id)}
								<option value={subject.id}>{subjectLabel(subject)}</option>
							{/each}
						</select>
					</label>
				</div>
				<label class="field">
					<span>{text.directory.validFrom}</span>
					<input type="date" bind:value={managerValidFrom} disabled={savingManager} />
				</label>
				<button
					type="submit"
					class="secondary-button"
					disabled={savingManager || subjects.length === 0}
				>
					{#if savingManager}
						<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
					{:else}
						<Save size={16} aria-hidden="true" />
					{/if}
					<span>{savingManager ? text.directory.saving : text.directory.saveManager}</span>
				</button>
				{#if selectedSubject}
					<p class="text-sm text-[var(--color-text-muted)]">
						Current manager: {selectedSubject.managerDisplayName ?? 'None'}
					</p>
				{/if}
				{#if managerError}
					<p class="error-line" role="alert">{managerError}</p>
				{/if}
			</form>
			</div>
		</div>
	</details>
{/if}
