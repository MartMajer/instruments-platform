<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { onDestroy } from 'svelte';
	import { Link2, LoaderCircle, Plus, RefreshCcw, Save, Upload, UserRound } from 'lucide-svelte';
	import type {
		SubjectDirectoryCsvImportResponse,
		SubjectDirectoryItemResponse,
		SubjectDirectoryResponse,
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

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();
	const authContext = getProductAuthContext();
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));

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
	let managerSubjectId = $state('');
	let managerValidFrom = $state('');
	let savingManager = $state(false);
	let managerError = $state<string | null>(null);
	let importCsvContent = $state('');
	let previewingCsv = $state(false);
	let applyingCsv = $state(false);
	let importResult = $state<SubjectDirectoryCsvImportResponse | null>(null);
	let importError = $state<string | null>(null);

	const unsubscribeAuth = authContext.session.subscribe((value) => {
		authSession = value;
	});
	onDestroy(unsubscribeAuth);

	const canManageSetup = $derived(hasProductPermission(authSession, setupManagePermission));
	const subjects = $derived(directory?.subjects ?? []);
	const groups = $derived(groupList?.groups ?? []);
	const membershipCount = $derived(
		subjects.reduce((total, subject) => total + subject.groups.length, 0)
	);
	const selectedSubject = $derived(
		subjects.find((subject) => subject.id === selectedSubjectId) ?? null
	);
	const managerOptions = $derived(subjects.filter((subject) => subject.id !== selectedSubjectId));
	const importHasFailures = $derived(
		importResult?.rows.some((row) => row.status === 'failed') ?? false
	);
	const importBusy = $derived(previewingCsv || applyingCsv);
	const csvTemplateHref = $derived(
		`data:text/csv;charset=utf-8,${encodeURIComponent('external_id,email,display_name,locale,group_type,group_name,role_in_group\nemp-001,ana@example.test,Ana Analyst,en,department,Research,member\nemp-002,bo@example.test,Bo Builder,en,department,Operations,member')}`
	);

	$effect(() => {
		if (canManageSetup && loadState === 'idle') {
			void loadDirectory();
		}
	});

	$effect(() => {
		if ((selectedSubject?.id ?? '') !== editSubjectSourceId) {
			syncSelectedSubjectFields(subjects);
		}
	});

	async function loadDirectory() {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;

		try {
			const [nextDirectory, nextGroupList] = await Promise.all([
				productApi.listSubjects(),
				productApi.listSubjectGroups()
			]);

			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			directory = nextDirectory;
			groupList = nextGroupList;
			syncSelections(nextDirectory.subjects, nextGroupList.groups);
			syncSelectedSubjectFields(nextDirectory.subjects);
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
			newSubjectDisplayName = '';
			newSubjectEmail = '';
			newSubjectExternalId = '';
			newSubjectAttributes = '{}';
			await loadDirectory();
			selectedSubjectId = created.id;
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

		<details class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3">
			<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
				{text.directory.howUsed}
			</summary>
			<p class="mt-3 text-sm leading-6 text-[var(--color-text-muted)]">
				{text.directory.buildAudienceBody}
			</p>
		</details>
	</section>

	<section class="product-panel" aria-label="Import audience CSV">
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
				<div class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3">
					<p class="text-sm font-semibold text-[var(--color-text)]">
						{importResult.dryRun ? text.directory.previewed : text.directory.imported} {importResult.importedRowCount} of
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
								{importResult.dryRun ? text.directory.membershipsToAdd : text.directory.membershipsAdded}
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
							<p class="text-sm font-semibold text-[var(--color-text)]">{text.directory.rowsNeedingAttention}</p>
							{#each importResult.rows.filter((row) => row.status === 'failed') as row}
								<article class="rounded border border-[var(--color-border)] bg-[var(--color-surface)] p-3 text-sm">
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
	</section>

	<section class="product-panel" aria-label={text.directory.peopleDirectoryAria}>
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
					</div>
				</div>

				<dl class="directory-count-list" role="group" aria-label={text.directory.directoryGraphCounts}>
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

				{#if subjects.length === 0}
					<EmptyState title="No people yet" description="Add people before configuring audiences." />
				{:else}
					<div class="record-list">
						{#each subjects as subject (subject.id)}
							<article class="record-row" aria-label={subjectLabel(subject)}>
								<span class="record-row__header">
									<span class="record-row__title">{subjectLabel(subject)}</span>
									<span
										class="status-badge"
										data-status={subject.managerSubjectId ? 'ready' : 'pending'}
									>
										{subject.managerSubjectId ? text.directory.managed : text.directory.noManager}
									</span>
								</span>
								<span class="record-grid">
									<span class="record-field">
										<span class="record-field__label">Email</span>
										<span class="record-field__value">{subject.email ?? text.directory.notAvailable}</span>
									</span>
									<span class="record-field">
										<span class="record-field__label">{text.directory.externalId}</span>
										<span class="record-field__value">{subject.externalId ?? text.directory.notAvailable}</span>
									</span>
									<span class="record-field">
										<span class="record-field__label">{text.directory.locale}</span>
										<span class="record-field__value">{subject.locale}</span>
									</span>
									<span class="record-field">
										<span class="record-field__label">{text.directory.manager}</span>
										<span class="record-field__value">{subject.managerDisplayName ?? 'None'}</span>
									</span>
									<span class="record-field">
										<span class="record-field__label">{text.directory.directReports}</span>
										<span class="record-field__value">{subject.directReportCount}</span>
									</span>
								</span>
								<div>
									<p class="record-field__label">{text.directory.groups}</p>
									<div class="mt-2 flex flex-wrap gap-2">
										{#if subject.groups.length === 0}
											<span class="text-xs text-[var(--color-text-muted)]">{text.directory.noMemberships}</span>
										{:else}
											{#each subject.groups as membership (membership.groupId)}
												<span
													class="inline-flex items-center gap-1 rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] px-2 py-1 text-xs font-semibold"
												>
													<UserRound size={13} aria-hidden="true" />
													{membership.groupName}
													{#if membership.roleInGroup}
														<span class="text-[var(--color-text-muted)]"
															>({membership.roleInGroup})</span
														>
													{/if}
												</span>
											{/each}
										{/if}
									</div>
								</div>
							</article>
						{/each}
					</div>
				{/if}
			{/if}
		</LoadingBoundary>
	</section>

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
				<div class="record-list">
					{#each groups as group (group.id)}
						<article class="record-row" aria-label={group.name}>
							<span class="record-row__header">
								<span class="record-row__title">{group.name}</span>
								<span class="status-badge" data-status="neutral">{group.type}</span>
							</span>
							<span class="record-grid">
								<span class="record-field">
									<span class="record-field__label">{text.directory.parent}</span>
									<span class="record-field__value">{groupParentLabel(group)}</span>
								</span>
								<span class="record-field">
									<span class="record-field__label">{text.directory.members}</span>
									<span class="record-field__value">{group.memberCount}</span>
								</span>
							</span>
						</article>
					{/each}
				</div>
			{/if}
		{/if}
	</section>

	<section id="directory-create" class="product-panel" aria-label={text.directory.createRecordsAria}>
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.directory.addRecords}</p>
				<h2 class="product-title">{text.directory.title}</h2>
			</div>
			<button type="button" class="secondary-button" onclick={loadDirectory}>
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
					<h3 class="text-base font-semibold text-[var(--color-text)]">{text.directory.newPerson}</h3>
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
						<span>{text.directory.externalId}</span>
						<input bind:value={newSubjectExternalId} disabled={creatingSubject} />
					</label>
					<label class="field">
						<span>{text.directory.locale}</span>
						<input bind:value={newSubjectLocale} disabled={creatingSubject} />
					</label>
				</div>
				<details class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3">
					<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
						Advanced attributes
					</summary>
					<label class="field mt-3">
						<span>{text.directory.attributesJson}</span>
						<textarea rows="3" bind:value={newSubjectAttributes} disabled={creatingSubject}
						></textarea>
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
					<h3 class="text-base font-semibold text-[var(--color-text)]">{text.directory.newGroup}</h3>
				</div>
				<div class="grid gap-3 md:grid-cols-2">
					<label class="field">
						<span>Type</span>
						<input bind:value={newGroupType} disabled={creatingGroup} />
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
				<details class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3">
					<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
						Advanced attributes
					</summary>
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
	</section>

	<section class="product-panel" aria-label={text.directory.directoryRelationshipsAria}>
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

		<form
			class="grid gap-3 border-b border-[var(--color-border)] pb-4"
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
			<details class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3">
				<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
					Advanced attributes
				</summary>
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

		<div class="grid gap-4 xl:grid-cols-2">
			<form
				class="grid gap-3"
				onsubmit={(event) => {
					event.preventDefault();
					void addSubjectGroupMember();
				}}
			>
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
						<span>Group</span>
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
	</section>
{/if}


