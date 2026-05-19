<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { resolve } from '$app/paths';
	import { onMount } from 'svelte';
	import { createApiClient } from '$lib/api/client';
	import { createSessionHeadersFromEnv } from '$lib/api/session-headers';
	import { createProductApi, type WorkspaceOverviewResponse } from '$lib/api/product';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import RouteGuidancePanel from '$lib/components/RouteGuidancePanel.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { toProductRouteGuidance } from '$lib/product/route-guidance';
	import { toProductApiErrorMessage, toWorkspaceOverviewView } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';

	const productApi = createProductApi(
		createApiClient({
			defaultHeaders: createSessionHeadersFromEnv(env),
			csrf: true
		})
	);

	let loadState = $state<LoadState>('loading');
	let overview = $state<WorkspaceOverviewResponse | null>(null);
	let errorMessage = $state<string | null>(null);

	const overviewView = $derived(overview ? toWorkspaceOverviewView(overview) : null);
	const routeGuidance = $derived(
		toProductRouteGuidance('home', {
			isEmpty: overviewView
				? overviewView.sampleStudies.length === 0 && overviewView.ownStudies.length === 0
				: false
		})
	);
	const firstRunActions = [
		{
			title: 'Create first study',
			status: 'Start here',
			description: 'Build the first campaign series and launch it when the instrument content is ready.',
			href: resolve('/app/campaign-series')
		},
		{
			title: 'Invite team',
			status: 'Access',
			description: 'Prepare tenant member access before sharing the first sign-in link.',
			href: resolve('/app/team')
		},
		{
			title: 'Set up directory',
			status: 'People',
			description: 'Create groups, subjects, memberships, and manager links for targeting and reporting.',
			href: resolve('/app/directory')
		},
		{
			title: 'Review instruments',
			status: 'Library',
			description: 'Confirm which instruments are available before starting production study work.',
			href: resolve('/app/instruments')
		}
	];

	onMount(() => {
		void loadWorkspaceOverview();
	});

	async function loadWorkspaceOverview() {
		loadState = 'loading';
		errorMessage = null;

		try {
			overview = await productApi.getWorkspaceOverview();
			loadState = 'ready';
		} catch (error) {
			overview = null;
			errorMessage = toProductApiErrorMessage(error, 'Workspace overview could not be loaded.');
			loadState = 'error';
		}
	}
</script>

<SurfaceHeader
	eyebrow="Authenticated workspace"
	title="Study cockpit"
	description="Start with read-only sample studies, then continue your own study setup, collection, reports, and exports."
/>

<RouteGuidancePanel guidance={routeGuidance} />

<section class="product-panel" data-priority="primary" aria-label="Self-serve study cockpit">
	<LoadingBoundary loading={loadState === 'loading'} label="Loading workspace overview">
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title="Workspace overview unavailable"
				message={errorMessage}
				retryLabel="Retry overview"
				onRetry={loadWorkspaceOverview}
			/>
		{:else if overviewView}
			<div class="grid gap-5">
				<section class="grid gap-3" aria-label="First-run workspace runway">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Workspace setup</p>
							<h2 class="product-title">
								{overviewView.sampleStudies.length === 0 && overviewView.ownStudies.length === 0
									? 'Set up your workspace'
									: 'Choose the next workspace move'}
							</h2>
						</div>
					</div>

					<div class="record-list">
						{#each firstRunActions as action}
							<a class="record-row" href={action.href}>
								<span class="record-row__header">
									<span class="record-row__title">{action.title}</span>
									<StatusBadge status="neutral" label={action.status} />
								</span>
								<span class="text-sm leading-6 text-[var(--color-text-muted)]">
									{action.description}
								</span>
							</a>
						{/each}
					</div>
				</section>

				<section class="grid gap-3" aria-label="Suggested next actions">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Command center</p>
							<h2 class="product-title">Suggested next actions</h2>
						</div>
						<a class="secondary-button" href={resolve('/app/campaign-series')}>Open Studies</a>
					</div>

					{#if overviewView.commandItems.length === 0}
						<EmptyState
							title="No workspace actions"
							description="Open Studies to review visible sample and own study work."
							actionHref={resolve('/app/campaign-series')}
							actionLabel="Open Studies"
						/>
					{:else}
						<div class="record-list">
							{#each overviewView.commandItems as command (command.id)}
								<a class="record-row" href={command.href}>
									<span class="record-row__header">
										<span class="record-row__title">{command.title}</span>
										<StatusBadge status={command.status} />
									</span>
									<span class="text-sm leading-6 text-[var(--color-text-muted)]">
										{command.description}
									</span>
									<span class="record-grid">
										{#each command.rows as row}
											<span class="record-field">
												<span class="record-field__label">{row.label}</span>
												<span class="record-field__value">{row.value}</span>
											</span>
										{/each}
										<span class="record-field">
											<span class="record-field__label">Action</span>
											<span class="record-field__value">{command.actionLabel}</span>
										</span>
									</span>
								</a>
							{/each}
						</div>
					{/if}
				</section>

				<section class="grid gap-3" aria-label="Sample studies">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Learn from examples</p>
							<h2 class="product-title">Sample studies</h2>
						</div>
						<a class="secondary-button" href={resolve('/app/campaign-series')}>View all studies</a>
					</div>

					{#if overviewView.sampleStudies.length === 0}
						<EmptyState
							title="No sample studies"
							description="Sample studies appear here when starter content is available. Open Studies to inspect all visible studies."
							actionHref={resolve('/app/campaign-series')}
							actionLabel="Open Studies"
						/>
					{:else}
						<div class="record-list">
							{#each overviewView.sampleStudies as study (study.id)}
								<a class="record-row" href={study.actionHref}>
									<span class="record-row__header">
										<span class="record-row__title">{study.title}</span>
										<span class="flex flex-wrap gap-2">
											<StatusBadge
												status={study.ownership.badgeStatus}
												label={study.ownership.label}
											/>
											<StatusBadge status={study.status} />
										</span>
									</span>
									{#if study.ownership.readOnlyMessage}
										<span class="text-sm leading-6 text-[var(--color-text-muted)]">
											{study.ownership.readOnlyMessage}
										</span>
									{/if}
									<span class="record-grid">
										{#each study.rows as row}
											<span class="record-field">
												<span class="record-field__label">{row.label}</span>
												<span class="record-field__value">{row.value}</span>
											</span>
										{/each}
										<span class="record-field">
											<span class="record-field__label">Next action</span>
											<span class="record-field__value">{study.actionLabel}</span>
										</span>
									</span>
								</a>
							{/each}
						</div>
					{/if}
				</section>

				<section class="grid gap-3" aria-label="Your studies">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Continue real work</p>
							<h2 class="product-title">Your studies</h2>
						</div>
						<a class="secondary-button" href={resolve('/app/campaign-series')}>Open portfolio</a>
					</div>

					{#if overviewView.ownStudies.length === 0}
						<EmptyState
							title="No own studies"
							description="Your editable studies appear here after creation or duplication. Open Studies to create tenant-owned work."
							actionHref={resolve('/app/campaign-series')}
							actionLabel="Open Studies"
						/>
					{:else}
						<div class="record-list">
							{#each overviewView.ownStudies as study (study.id)}
								<a class="record-row" href={study.actionHref}>
									<span class="record-row__header">
										<span class="record-row__title">{study.title}</span>
										<span class="flex flex-wrap gap-2">
											<StatusBadge
												status={study.ownership.badgeStatus}
												label={study.ownership.label}
											/>
											<StatusBadge status={study.status} />
										</span>
									</span>
									<span class="record-grid">
										{#each study.rows as row}
											<span class="record-field">
												<span class="record-field__label">{row.label}</span>
												<span class="record-field__value">{row.value}</span>
											</span>
										{/each}
										<span class="record-field">
											<span class="record-field__label">Next action</span>
											<span class="record-field__value">{study.actionLabel}</span>
										</span>
									</span>
								</a>
							{/each}
						</div>
					{/if}
				</section>

				<section class="home-lifecycle-list" role="group" aria-label="Study lifecycle">
					{#each overviewView.lifecycleSteps as step}
						<article class="home-lifecycle-row">
							<h3 class="home-lifecycle-row__title">{step.label}</h3>
							<p class="home-lifecycle-row__description">
								{step.description}
							</p>
						</article>
					{/each}
				</section>

				<section class="grid gap-4 border-t border-[var(--color-border)] pt-4">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">Workspace totals</p>
							<h2 class="product-title">Study portfolio scale</h2>
						</div>
					</div>

					<dl class="home-total-list" role="group" aria-label="Workspace totals">
						{#each overviewView.totalRows as row}
							<div class="home-total-row">
								<dt class="home-total-row__label">{row.label}</dt>
								<dd class="home-total-row__value">{row.value}</dd>
							</div>
						{/each}
					</dl>
				</section>

			</div>
		{/if}
	</LoadingBoundary>
</section>
