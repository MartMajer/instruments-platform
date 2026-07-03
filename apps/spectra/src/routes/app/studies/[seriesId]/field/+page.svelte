<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import {
		createProductApi,
		type CampaignSeriesOperationsWorkspaceResponse
	} from '$lib/api/product';
	import { api } from '$lib/core/client';
	import { formatCount, formatDateTime, humanizeToken } from '$lib/core/format';
	import CoverageMeter from '$lib/ui/CoverageMeter.svelte';

	const product = createProductApi(api());
	const seriesId = $derived(page.params.seriesId!);

	let workspace = $state<CampaignSeriesOperationsWorkspaceResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');
	let lastReadAt = $state<Date | null>(null);

	const live = $derived((workspace?.summary.liveCampaignCount ?? 0) > 0);
	const selected = $derived(workspace?.selectedCampaign ?? null);

	async function read(silent = false) {
		if (!silent) loadState = 'loading';
		try {
			workspace = await product.getCampaignSeriesOperationsWorkspace(seriesId);
			lastReadAt = new Date();
			loadState = 'ready';
		} catch {
			if (!silent) loadState = 'error';
		}
	}

	onMount(() => {
		void read();
		const timer = setInterval(() => {
			if (document.visibilityState === 'visible' && live) {
				void read(true);
			}
		}, 20000);

		return () => clearInterval(timer);
	});
</script>

<svelte:head><title>Field — Spectra</title></svelte:head>

<div class="console" class:idle={!live}>
	<div class="inner">
		<header class="head">
			<p class="eyebrow crumbs">
				<a href="/app/studies">Studies</a> /
				<a href={`/app/studies/${seriesId}`}>{workspace?.series.name ?? 'Study'}</a>
			</p>

			<div class="title-row">
				<h1 class="doc-title">Field</h1>
				{#if loadState === 'ready'}
					<span class="live-flag" class:on={live}>
						<span class="pip" aria-hidden="true"></span>
						{live ? 'Collecting' : 'Not collecting'}
					</span>
				{/if}
				{#if lastReadAt}
					<span class="datum read-at">read {lastReadAt.toLocaleTimeString('en-GB')}</span>
				{/if}
			</div>

			<nav class="phases" aria-label="Study phases">
				<a class="phase" href={`/app/studies/${seriesId}`}>Protocol</a>
				<span class="phase current">Field</span>
				<a class="phase" href={`/app/studies/${seriesId}/evidence`}>Evidence</a>
			</nav>
		</header>

		{#if loadState === 'loading'}
			<p class="reading-note" role="status">Reading the field…</p>
		{:else if loadState === 'error'}
			<p class="reading-note" role="alert">
				The field did not respond. <button class="retry" onclick={() => read()}>Read again</button>
			</p>
		{:else if workspace}
			<section class="board">
				<div class="tile">
					<span class="eyebrow dim-label">Invited</span>
					<span class="datum value">
						{formatCount(
							workspace.summary.sentInvitationCount + workspace.summary.openLinkAssignmentCount
						)}
					</span>
					<span class="sub">
						{formatCount(workspace.summary.sentInvitationCount)} sent ·
						{formatCount(workspace.summary.openLinkAssignmentCount)} open link
					</span>
				</div>
				<div class="tile">
					<span class="eyebrow dim-label">Started</span>
					<span class="datum value">{formatCount(workspace.summary.startedResponseCount)}</span>
					<span class="sub">{formatCount(workspace.summary.draftResponseCount)} in draft</span>
				</div>
				<div class="tile accent">
					<span class="eyebrow dim-label">Submitted</span>
					<span class="datum value">{formatCount(workspace.summary.submittedResponseCount)}</span>
					<span class="sub">
						last {formatDateTime(workspace.summary.latestResponseSubmittedAt)}
					</span>
				</div>
				<div class="tile">
					<span class="eyebrow dim-label">Delivery</span>
					<span class="datum value">
						{formatCount(workspace.summary.failedInvitationCount)}
						<span class="value-unit">failed</span>
					</span>
					<span class="sub">
						{formatCount(workspace.summary.bouncedInvitationCount ?? 0)} bounced ·
						{formatCount(workspace.summary.deliveryAttemptCount)} attempts
					</span>
				</div>
			</section>

			<p class="guidance">{workspace.summary.collectionGuidance}</p>

			{#if workspace.groupCoverage && workspace.groupCoverage.groups.length > 0}
				<section class="coverage">
					<h2 class="eyebrow dim-label">
						Group coverage — reporting threshold k = {workspace.groupCoverage.kMin}
					</h2>
					<div class="meters">
						{#each workspace.groupCoverage.groups as group (group.groupId)}
							<CoverageMeter
								label={group.groupName}
								submitted={group.submittedCount}
								invited={group.invitedCount}
								kMin={workspace.groupCoverage.kMin}
								meets={group.meetsThreshold}
							/>
						{/each}
					</div>
					{#if workspace.groupCoverage.unattributedSubmittedCount > 0}
						<p class="unattributed">
							{formatCount(workspace.groupCoverage.unattributedSubmittedCount)} submissions
							are not attributed to a group.
						</p>
					{/if}
				</section>
			{/if}

			<section class="waves">
				<h2 class="eyebrow dim-label">Waves</h2>
				<ul>
					{#each workspace.campaigns as wave (wave.id)}
						<li class:selected={wave.id === selected?.id}>
							<div class="wave-main">
								<strong>{wave.name}</strong>
								<span class="datum wave-meta">
									{humanizeToken(wave.status)} · {humanizeToken(wave.responseIdentityMode)}
									{#if wave.latestLaunchAt}· launched {formatDateTime(wave.latestLaunchAt)}{/if}
								</span>
							</div>
							<span class="datum wave-count">
								{formatCount(wave.submittedResponseCount)}
								<span class="dim">submitted</span>
							</span>
						</li>
					{:else}
						<li class="none">No waves launched yet. Launch from the protocol.</li>
					{/each}
				</ul>
			</section>

			{#if workspace.missingPrerequisites.length > 0}
				<section class="prereqs">
					<h2 class="eyebrow dim-label">Field notes</h2>
					<ul>
						{#each workspace.missingPrerequisites as item (item.code)}
							<li>{item.message}</li>
						{/each}
					</ul>
				</section>
			{/if}
		{/if}
	</div>
</div>

<style>
	/* The console claims the full canvas: the app's one dark surface. */
	.console {
		margin-top: -2rem;
		margin-bottom: -4rem;
		margin-left: calc(50% - 50vw);
		margin-right: calc(50% - 50vw);
		min-height: calc(100dvh - 3.25rem);
		background: var(--color-console);
		color: var(--color-console-ink);
	}

	.inner {
		max-width: 74rem;
		margin: 0 auto;
		padding: 2rem 1.5rem 4rem;
	}

	.crumbs,
	.crumbs a {
		color: var(--color-console-dim);
		text-decoration: none;
	}

	.crumbs a:hover {
		color: var(--color-console-ink);
	}

	.title-row {
		display: flex;
		align-items: baseline;
		gap: 1.25rem;
		margin-top: 0.5rem;
	}

	.title-row h1 {
		font-size: 2.25rem;
		color: var(--color-console-ink);
	}

	.live-flag {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-console-dim);
	}

	.live-flag .pip {
		width: 9px;
		height: 9px;
		border-radius: 999px;
		background: var(--color-console-line);
	}

	.live-flag.on {
		color: var(--color-console-ink);
	}

	.live-flag.on .pip {
		background: var(--color-chart-violet-dark);
		box-shadow: 0 0 0 4px color-mix(in oklab, var(--color-chart-violet-dark) 22%, transparent);
		animation: breathe 2.4s ease-in-out infinite;
	}

	@keyframes breathe {
		0%,
		100% {
			box-shadow: 0 0 0 3px color-mix(in oklab, var(--color-chart-violet-dark) 16%, transparent);
		}
		50% {
			box-shadow: 0 0 0 6px color-mix(in oklab, var(--color-chart-violet-dark) 28%, transparent);
		}
	}

	.read-at {
		margin-left: auto;
		font-size: 0.6875rem;
		color: var(--color-console-dim);
	}

	.phases {
		display: flex;
		gap: 0.25rem;
		margin-top: 1.5rem;
		border-bottom: 1px solid var(--color-console-line);
	}

	.phase {
		padding: 0.5rem 0.875rem;
		font-size: 0.875rem;
		font-weight: 540;
		color: var(--color-console-dim);
		text-decoration: none;
		border-bottom: 2px solid transparent;
		margin-bottom: -1px;
	}

	.phase:hover {
		color: var(--color-console-ink);
	}

	.phase.current {
		color: var(--color-chart-violet-dark);
		border-bottom-color: var(--color-chart-violet-dark);
	}

	.reading-note {
		margin-top: 3rem;
		color: var(--color-console-dim);
	}

	.retry {
		background: none;
		border: none;
		color: var(--color-chart-violet-dark);
		font: inherit;
		cursor: pointer;
		text-decoration: underline;
	}

	.board {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 1px;
		background: var(--color-console-line);
		border: 1px solid var(--color-console-line);
		border-radius: var(--radius-instrument);
		overflow: hidden;
		margin-top: 2rem;
	}

	.tile {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
		padding: 1.25rem;
		background: var(--color-console-2);
	}

	.tile.accent {
		box-shadow: inset 0 3px 0 var(--color-chart-violet-dark);
	}

	.dim-label {
		color: var(--color-console-dim);
	}

	.value {
		font-size: 2rem;
		font-weight: 500;
		color: var(--color-console-ink);
	}

	.value-unit {
		font-size: 0.8125rem;
		color: var(--color-console-dim);
	}

	.sub {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.guidance {
		margin-top: 1.25rem;
		font-size: 0.9375rem;
		color: var(--color-console-dim);
		max-width: 64ch;
	}

	.coverage {
		margin-top: 2.5rem;
	}

	.meters {
		margin-top: 1rem;
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
		max-width: 44rem;
	}

	.unattributed {
		margin-top: 1rem;
		font-size: 0.8125rem;
		color: var(--color-console-dim);
	}

	.waves {
		margin-top: 2.75rem;
	}

	.waves ul {
		list-style: none;
		margin-top: 0.75rem;
	}

	.waves li {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 1.5rem;
		padding: 0.875rem 0.875rem;
		border-bottom: 1px solid var(--color-console-line);
	}

	.waves li.selected {
		background: var(--color-console-2);
		border-radius: var(--radius-instrument);
	}

	.waves li.none {
		color: var(--color-console-dim);
		font-size: 0.9375rem;
	}

	.wave-main {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.wave-main strong {
		font-weight: 560;
		color: var(--color-console-ink);
	}

	.wave-meta {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.wave-count {
		font-size: 1rem;
		color: var(--color-console-ink);
		white-space: nowrap;
	}

	.wave-count .dim {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.prereqs {
		margin-top: 2.75rem;
	}

	.prereqs ul {
		list-style: none;
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		font-size: 0.875rem;
		color: var(--color-console-dim);
	}

	.prereqs li {
		padding-left: 0.875rem;
		border-left: 2px solid var(--color-console-line);
	}

	@media (max-width: 54rem) {
		.board {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.live-flag.on .pip {
			animation: none;
		}
	}
</style>
