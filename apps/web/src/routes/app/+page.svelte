<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { createApiClient } from '$lib/api/client';
	import { createSessionHeadersFromEnv } from '$lib/api/session-headers';
	import { createProductApi, type WorkspaceOverviewResponse } from '$lib/api/product';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
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

	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const overviewView = $derived(overview ? toWorkspaceOverviewView(overview) : null);
	const firstRunActions = $derived([
		{
			title: text.workspaceHome.firstRunActions[0],
			status: 'Start here',
			description: 'Start a real study and continue through setup, collection, and results.',
			href: resolve('/app/campaign-series')
		},
		{
			title: text.workspaceHome.firstRunActions[1],
			status: 'Access',
			description: 'Prepare tenant member access before sharing the first sign-in link.',
			href: resolve('/app/team')
		},
		{
			title: text.workspaceHome.firstRunActions[2],
			status: 'People',
			description: 'Create people, groups, memberships, and manager links for targeting.',
			href: resolve('/app/directory')
		},
		{
			title: text.workspaceHome.firstRunActions[3],
			status: 'Library',
			description: 'Confirm which instruments are available before starting production study work.',
			href: resolve('/app/instruments')
		}
	]);

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
			errorMessage = toProductApiErrorMessage(error, text.workspaceHome.errorTitle);
			loadState = 'error';
		}
	}
</script>

<SurfaceHeader
	eyebrow={text.workspaceHome.eyebrow}
	title={text.workspaceHome.title}
	description={text.workspaceHome.description}
/>

<section class="product-panel" data-priority="primary" aria-label="Self-serve study cockpit">
	<LoadingBoundary loading={loadState === 'loading'} label={text.workspaceHome.loading}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={text.workspaceHome.errorTitle}
				message={errorMessage}
				retryLabel={text.workspaceHome.retry}
				onRetry={loadWorkspaceOverview}
			/>
		{:else if overviewView}
			<div class="grid gap-5">
				<section class="grid gap-3" aria-label="First-run workspace runway">
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{text.workspaceHome.start}</p>
							<h2 class="product-title">
								{overviewView.sampleStudies.length === 0 && overviewView.ownStudies.length === 0
									? text.workspaceHome.setupWorkspace
									: text.workspaceHome.chooseNext}
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
							<p class="product-kicker">{text.workspaceHome.nextActions}</p>
							<h2 class="product-title">{text.workspaceHome.nextActions}</h2>
						</div>
						<a class="secondary-button" href={resolve('/app/campaign-series')}>{text.workspaceHome.openStudies}</a>
					</div>

					{#if overviewView.commandItems.length === 0}
						<EmptyState
							title="No workspace actions"
							description="Open Studies to create or continue study work."
							actionHref={resolve('/app/campaign-series')}
							actionLabel={text.workspaceHome.openStudies}
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
											<span class="record-field__label">{text.workspaceHome.nextActions}</span>
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
							<p class="product-kicker">{text.workspaceHome.examples}</p>
							<h2 class="product-title">{text.workspaceHome.sampleStudies}</h2>
						</div>
						<a class="secondary-button" href={resolve('/app/campaign-series')}>View all studies</a>
					</div>

					{#if overviewView.sampleStudies.length === 0}
						<EmptyState
							title="No sample studies"
							description="Sample studies appear here when starter content is available."
							actionHref={resolve('/app/campaign-series')}
							actionLabel={text.workspaceHome.openStudies}
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
							<p class="product-kicker">{text.workspaceHome.yourWork}</p>
							<h2 class="product-title">{text.workspaceHome.yourStudies}</h2>
						</div>
						<a class="secondary-button" href={resolve('/app/campaign-series')}>Open portfolio</a>
					</div>

					{#if overviewView.ownStudies.length === 0}
						<EmptyState
							title="No studies yet"
							description="Your editable studies appear here after you create one."
							actionHref={resolve('/app/campaign-series')}
							actionLabel={text.workspaceHome.openStudies}
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

				<details
					class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3"
				>
					<summary class="cursor-pointer text-sm font-semibold text-[var(--color-text)]">
						{text.workspaceHome.workspaceOverview}
					</summary>
					<section class="home-lifecycle-list mt-4" role="group" aria-label="Study lifecycle">
						{#each overviewView.lifecycleSteps as step}
							<article class="home-lifecycle-row">
								<h3 class="home-lifecycle-row__title">{step.label}</h3>
								<p class="home-lifecycle-row__description">
									{step.description}
								</p>
							</article>
						{/each}
					</section>

					<dl class="home-total-list mt-4" role="group" aria-label="Workspace totals">
						{#each overviewView.totalRows as row}
							<div class="home-total-row">
								<dt class="home-total-row__label">{row.label}</dt>
								<dd class="home-total-row__value">{row.value}</dd>
							</div>
						{/each}
					</dl>
				</details>

			</div>
		{/if}
	</LoadingBoundary>
</section>
