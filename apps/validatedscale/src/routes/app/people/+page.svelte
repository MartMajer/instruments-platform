<script lang="ts">
	import { onMount } from 'svelte';
	import {
		createProductApi,
		type DirectoryConnectionStateResponse,
		type DirectoryImportRuleListResponse,
		type DirectoryImportRunHistoryResponse,
		type SubjectDirectoryResponse
	} from '$lib/api/product';
	import { api } from '$lib/core/client';
	import { formatCount, formatDateTime, humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());

	let directory = $state<SubjectDirectoryResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'empty' | 'ready'>('loading');
	let search = $state('');

	let panel = $state<'none' | 'add' | 'import' | 'graph'>('none');
	let graphState = $state<DirectoryConnectionStateResponse | null>(null);
	let graphRules = $state<DirectoryImportRuleListResponse | null>(null);
	let graphRuns = $state<DirectoryImportRunHistoryResponse | null>(null);
	let graphBusy = $state(false);
	let graphNote = $state<string | null>(null);

	async function loadGraph() {
		graphState = await product.getMicrosoftGraphDirectoryConnectionState().catch(() => null);
		graphRules = await product.listMicrosoftGraphDirectoryImportRules().catch(() => null);
		graphRuns = await product.listMicrosoftGraphDirectoryImportRuns().catch(() => null);
	}

	async function requestConsent() {
		if (graphBusy) return;
		graphBusy = true;
		graphNote = null;
		try {
			const consent = await product.createMicrosoftGraphConsentRequest({});
			if (consent.adminConsentUrl) {
				location.assign(consent.adminConsentUrl);
			} else {
				graphNote =
					'Consent request created, but no admin-consent URL was returned. Check the Microsoft Graph client configuration on the API.';
			}
		} catch {
			graphNote =
				'The consent request failed. Microsoft Graph may not be configured on this environment.';
		} finally {
			graphBusy = false;
		}
	}

	let personName = $state('');
	let personEmail = $state('');
	let personBusy = $state(false);
	let personError = $state<string | null>(null);

	let csvContent = $state('');
	let csvBusy = $state(false);
	let csvNote = $state<string | null>(null);

	async function load() {
		try {
			directory = await product.listSubjects();
			loadState = directory.subjects.length === 0 ? 'empty' : 'ready';
		} catch {
			loadState = 'error';
		}
	}

	async function addPerson(event: SubmitEvent) {
		event.preventDefault();
		if (personBusy) return;
		personBusy = true;
		personError = null;

		try {
			await product.createSubject({
				displayName: personName.trim() || null,
				email: personEmail.trim() || null
			});
			personName = '';
			personEmail = '';
			panel = 'none';
			await load();
		} catch {
			personError = 'The person could not be added. Check the fields and try again.';
		} finally {
			personBusy = false;
		}
	}

	async function importCsv(dryRun: boolean) {
		if (csvBusy || csvContent.trim().length === 0) return;
		csvBusy = true;
		csvNote = null;

		try {
			const result = await product.importSubjectDirectoryCsv({ csvContent, dryRun });
			const issues = result.rows.filter((row) => row.issues.length > 0).length;
			csvNote = dryRun
				? `Preview: ${result.rowCount} rows read, ${issues} with issues. Nothing was saved.`
				: `Imported: ${result.createdSubjectCount} created, ${result.updatedSubjectCount} updated, ${issues} rows with issues.`;
			if (!dryRun) {
				csvContent = '';
				await load();
			}
		} catch {
			csvNote = 'The CSV was not accepted. Expected headers like display_name,email,external_id.';
		} finally {
			csvBusy = false;
		}
	}

	const filtered = $derived(
		(directory?.subjects ?? []).filter((subject) => {
			const term = search.trim().toLowerCase();
			if (!term) return true;
			return (
				(subject.displayName ?? '').toLowerCase().includes(term) ||
				(subject.email ?? '').toLowerCase().includes(term) ||
				subject.groups.some((group) => group.groupName.toLowerCase().includes(term))
			);
		})
	);

	onMount(load);
</script>

<svelte:head><title>People — ValidatedScale</title></svelte:head>

<header class="head">
	<div>
		<p class="eyebrow">Directory</p>
		<h1 class="doc-title">People</h1>
		{#if directory}
			<p class="datum meta">
				{formatCount(directory.summary.subjectCount)} people ·
				{formatCount(directory.summary.groupCount)} groups
			</p>
		{/if}
	</div>
	<div class="tools">
		<input type="search" placeholder="Find a person or group" aria-label="Find a person or group" bind:value={search} />
		<button class="btn btn-ghost" onclick={() => (panel = panel === 'add' ? 'none' : 'add')}>Add person</button>
		<button class="btn btn-ghost" onclick={() => (panel = panel === 'import' ? 'none' : 'import')}>Import CSV</button>
		<button
			class="btn btn-ghost"
			onclick={() => {
				panel = panel === 'graph' ? 'none' : 'graph';
				if (panel === 'graph') void loadGraph();
			}}
		>
			Microsoft directory
		</button>
	</div>
</header>

{#if panel === 'add'}
	<form class="panel author" onsubmit={addPerson}>
		<div class="field">
			<label class="eyebrow" for="p-name">Name</label>
			<input id="p-name" bind:value={personName} />
		</div>
		<div class="field">
			<label class="eyebrow" for="p-email">Email</label>
			<input id="p-email" type="email" bind:value={personEmail} />
		</div>
		{#if personError}<p class="error" role="alert">{personError}</p>{/if}
		<button class="btn btn-ink" type="submit" disabled={personBusy || (!personName.trim() && !personEmail.trim())}>
			{personBusy ? 'Adding…' : 'Add person'}
		</button>
	</form>
{:else if panel === 'import'}
	<div class="panel author import">
		<label class="eyebrow" for="csv">CSV — headers like display_name,email,external_id,group_name</label>
		<textarea id="csv" rows="6" bind:value={csvContent} placeholder={'display_name,email\nAna Kovač,ana@example.org'}></textarea>
		{#if csvNote}<p class="note" role="status">{csvNote}</p>{/if}
		<div class="import-actions">
			<button class="btn btn-ghost" disabled={csvBusy} onclick={() => importCsv(true)}>Preview (dry run)</button>
			<button class="btn btn-ink" disabled={csvBusy} onclick={() => importCsv(false)}>Import</button>
		</div>
	</div>
{:else if panel === 'graph'}
	<div class="panel author import">
		<span class="eyebrow">Microsoft Entra directory</span>
		{#if graphState?.connected}
			<p class="note">
				Connected to <strong>{graphState.displayName}</strong>
				{#if graphState.primaryDomain}({graphState.primaryDomain}){/if}
				· last import {formatDateTime(graphState.lastSuccessfulImportAt)}
			</p>
			{#if graphRules && graphRules.rules.length > 0}
				<ul class="graph-list">
					{#each graphRules.rules as rule (rule.id)}
						<li>
							<span>{rule.name} <span class="datum graph-meta">{humanizeToken(rule.status)}</span></span>
						</li>
					{/each}
				</ul>
			{:else}
				<p class="note">No import rules defined yet. Rules are created per group/scope after connection.</p>
			{/if}
			{#if graphRuns && graphRuns.runs.length > 0}
				<ul class="graph-list">
					{#each graphRuns.runs.slice(0, 5) as run (run.id)}
						<li>
							<span class="datum graph-meta">
								{formatDateTime(run.createdAt)} · {humanizeToken(run.status)} ·
								{formatCount(run.importedRowCount)}/{formatCount(run.rowCount)} rows
							</span>
						</li>
					{/each}
				</ul>
			{/if}
		{:else}
			<p class="note">
				Connect your Microsoft 365 directory to import people and groups automatically. This
				opens Microsoft's admin-consent page for your organization.
			</p>
			<button class="btn btn-ink graph-connect" disabled={graphBusy} onclick={requestConsent}>
				{graphBusy ? 'Opening consent…' : 'Connect Microsoft directory'}
			</button>
		{/if}
		{#if graphNote}<p class="note" role="status">{graphNote}</p>{/if}
	</div>
{/if}

<LoadState
	state={loadState}
	emptyTitle="Nobody here yet"
	emptyBody="People are added by CSV import or a Microsoft directory connection, and are only needed for identified or invite-only studies. Anonymous open-link studies work without a directory."
>
	<div class="table-wrap">
		<table>
			<thead>
				<tr>
					<th>Name</th>
					<th>Email</th>
					<th>Groups</th>
					<th>Manager</th>
				</tr>
			</thead>
			<tbody>
				{#each filtered as subject (subject.id)}
					<tr>
						<td class="name">{subject.displayName ?? '—'}</td>
						<td class="datum email">{subject.email ?? '—'}</td>
						<td>
							{#each subject.groups as group, i (group.groupId)}{i > 0 ? ', ' : ''}{group.groupName}{:else}—{/each}
						</td>
						<td>{subject.managerDisplayName ?? '—'}</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
</LoadState>

<style>
	.head {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		gap: 1.5rem;
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.meta {
		margin-top: 0.5rem;
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.tools {
		display: flex;
		gap: 0.625rem;
		flex-wrap: wrap;
	}

	.tools input {
		font: inherit;
		font-size: 0.875rem;
		padding: 0.5rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
		width: 14rem;
	}

	.author {
		display: flex;
		align-items: flex-end;
		gap: 1rem;
		flex-wrap: wrap;
		padding: 1.25rem;
		margin-bottom: 2rem;
		border-top: 3px solid var(--color-stain);
	}

	.author.import {
		flex-direction: column;
		align-items: stretch;
	}

	.field {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
	}

	.author input,
	.author textarea {
		font: inherit;
		font-size: 0.9375rem;
		padding: 0.5rem 0.625rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	.author textarea {
		font-family: var(--font-mono);
		font-size: 0.8125rem;
		resize: vertical;
	}

	.import-actions {
		display: flex;
		gap: 0.625rem;
	}

	.graph-list {
		list-style: none;
		display: flex;
		flex-direction: column;
	}

	.graph-list li {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		padding: 0.5rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.875rem;
	}

	.graph-list li:last-child {
		border-bottom: none;
	}

	.graph-meta {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.graph-connect {
		align-self: flex-start;
	}

	.note {
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	.error {
		font-size: 0.8125rem;
		color: var(--color-danger);
	}

	.table-wrap {
		overflow-x: auto;
	}

	table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.875rem;
	}

	th {
		text-align: left;
		font-size: 0.6875rem;
		font-weight: 600;
		letter-spacing: 0.1em;
		text-transform: uppercase;
		color: var(--color-ink-3);
		padding: 0.5rem 1rem 0.5rem 0;
		border-bottom: 1px solid var(--color-line-2);
	}

	td {
		padding: 0.625rem 1rem 0.625rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.name {
		font-weight: 540;
	}

	.email {
		font-size: 0.8125rem;
	}

	@media (max-width: 44rem) {
		.head {
			flex-direction: column;
			align-items: stretch;
		}

		.tools input {
			flex: 1;
			width: auto;
		}
	}
</style>
