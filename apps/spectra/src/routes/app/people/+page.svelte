<script lang="ts">
	import { onMount } from 'svelte';
	import { createProductApi, type SubjectDirectoryResponse } from '$lib/api/product';
	import { api } from '$lib/core/client';
	import { formatCount } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());

	let directory = $state<SubjectDirectoryResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'empty' | 'ready'>('loading');
	let search = $state('');

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

	onMount(async () => {
		try {
			directory = await product.listSubjects();
			loadState = directory.subjects.length === 0 ? 'empty' : 'ready';
		} catch {
			loadState = 'error';
		}
	});
</script>

<svelte:head><title>People — Spectra</title></svelte:head>

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
	<input type="search" placeholder="Find a person or group" aria-label="Find a person or group" bind:value={search} />
</header>

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

	.head input {
		font: inherit;
		font-size: 0.875rem;
		padding: 0.5rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
		width: 16rem;
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

		.head input {
			width: auto;
		}
	}
</style>
