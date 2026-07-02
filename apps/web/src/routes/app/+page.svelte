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
	const overviewView = $derived(overview ? toWorkspaceOverviewView(overview, locale) : null);

	const firstRunActions = $derived([
		{
			title: text.workspaceHome.firstRunActions[0],
			status: text.workspaceHome.firstRunActionStatuses[0],
			description: text.workspaceHome.firstRunActionDescriptions[0],
			href: resolve('/app/campaign-series')
		},
		{
			title: text.workspaceHome.firstRunActions[1],
			status: text.workspaceHome.firstRunActionStatuses[1],
			description: text.workspaceHome.firstRunActionDescriptions[1],
			href: resolve('/app/team')
		},
		{
			title: text.workspaceHome.firstRunActions[2],
			status: text.workspaceHome.firstRunActionStatuses[2],
			description: text.workspaceHome.firstRunActionDescriptions[2],
			href: resolve('/app/directory')
		}
	]);

	onMount(() => {
		void loadWorkspaceOverview();
	});

	async function loadWorkspaceOverview() {
		loadState = 'loading';
		errorMessage = null;

		try {
			await productApi.ensureSampleStudies().catch(() => null);
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

<section class="workspace-home" aria-label={text.workspaceHome.homeAria}>
	<LoadingBoundary loading={loadState === 'loading'} label={text.workspaceHome.loading}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={text.workspaceHome.errorTitle}
				message={errorMessage}
				retryLabel={text.workspaceHome.retry}
				onRetry={loadWorkspaceOverview}
			/>
		{:else if overviewView}
			<section class="workspace-home-hero" aria-label={text.workspaceHome.heroAria}>
				<div class="workspace-home-hero__copy">
					<p class="workspace-home-kicker">{text.workspaceHome.heroKicker}</p>
					<h2>{text.workspaceHome.heroTitle}</h2>
					<p>{text.workspaceHome.heroBody}</p>
					<div class="workspace-home-hero__actions" aria-label={text.workspaceHome.primaryActionsAria}>
						<a class="primary-button" href={resolve('/app/campaign-series')}>{text.workspaceHome.createStudy}</a>
						<a class="secondary-button" href="#sample-studies">{text.workspaceHome.exploreSamples}</a>
					</div>
				</div>
				<div class="workspace-home-hero__panel" aria-label={text.workspaceHome.workflowAria}>
					<p class="workspace-home-kicker">{text.workspaceHome.workflowLabel}</p>
					<ol>
						{#each overviewView.lifecycleSteps as step}
							<li>
								<span>{step.label}</span>
								<small>{step.description}</small>
							</li>
						{/each}
					</ol>
				</div>
			</section>

			<section class="workspace-home-section" aria-label={text.workspaceHome.nextActions}>
				<div class="workspace-home-section__header">
					<div>
						<p class="workspace-home-kicker">{text.workspaceHome.nextActions}</p>
						<h2>{text.workspaceHome.nextActionsTitle}</h2>
					</div>
					<a class="secondary-button" href={resolve('/app/campaign-series')}>{text.workspaceHome.openStudies}</a>
				</div>

				{#if overviewView.commandItems.length === 0}
					<EmptyState
						title={text.workspaceHome.noWorkspaceActions}
						description={text.workspaceHome.noWorkspaceActionsBody}
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

			<section class="workspace-home-section" aria-label={text.workspaceHome.startAria}>
				<div class="workspace-home-section__header">
					<div>
						<p class="workspace-home-kicker">{text.workspaceHome.start}</p>
						<h2>{text.workspaceHome.startTitle}</h2>
					</div>
					<p>{text.workspaceHome.startBody}</p>
				</div>
				<div class="workspace-home-action-grid">
					{#each firstRunActions as action}
						<a class="workspace-home-action" href={action.href}>
							<span class="workspace-home-action__header">
								<strong>{action.title}</strong>
								<StatusBadge status="neutral" label={action.status} />
							</span>
							<span>{action.description}</span>
						</a>
					{/each}
				</div>
			</section>

			<section id="sample-studies" class="workspace-home-section workspace-home-section--samples" aria-label={text.workspaceHome.sampleStudies}>
				<div class="workspace-home-section__header">
					<div>
						<p class="workspace-home-kicker">{text.workspaceHome.examples}</p>
						<h2>{text.workspaceHome.sampleStudies}</h2>
					</div>
					<p>{text.workspaceHome.sampleStudiesBody}</p>
				</div>
				{#if overviewView.sampleStudies.length === 0}
					<EmptyState
						title={text.workspaceHome.sampleStudies}
						description={text.workspaceHome.sampleReadOnlyNote}
						actionHref={resolve('/app/demo')}
						actionLabel={text.workspaceHome.openSample}
					/>
				{:else}
					<div class="sample-study-grid">
						{#each overviewView.sampleStudies as study (study.id)}
							<a class="sample-study-card" href={study.actionHref}>
								<span class="sample-study-card__kicker">{study.ownership.label}</span>
							<strong>{study.title}</strong>
								<span>{study.ownership.readOnlyMessage ?? text.workspaceHome.sampleReadOnlyNote}</span>
								{#each study.rows.slice(0, 3) as row}
									<small>{row.label}: {row.value}</small>
								{/each}
								<span class="sample-study-card__action">{study.actionLabel}</span>
							</a>
						{/each}
					</div>
				{/if}
				<p class="workspace-home-note">{text.workspaceHome.sampleReadOnlyNote}</p>
			</section>

			<section class="workspace-home-section" aria-label={text.workspaceHome.yourStudies}>
				<div class="workspace-home-section__header">
					<div>
						<p class="workspace-home-kicker">{text.workspaceHome.yourWork}</p>
						<h2>{text.workspaceHome.yourStudies}</h2>
					</div>
					<a class="secondary-button" href={resolve('/app/campaign-series')}>{text.workspaceHome.openPortfolio}</a>
				</div>

				{#if overviewView.ownStudies.length === 0}
					<EmptyState
						title={text.workspaceHome.noStudiesYet}
						description={text.workspaceHome.noStudiesYetBody}
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
										<span class="record-field__label">{text.workspaceHome.nextActions}</span>
										<span class="record-field__value">{study.actionLabel}</span>
									</span>
								</span>
							</a>
						{/each}
					</div>
				{/if}
			</section>

			<details class="workspace-home-reference">
				<summary>{text.workspaceHome.workspaceOverview}</summary>
				<dl class="home-total-list mt-4" role="group" aria-label="Workspace totals">
					{#each overviewView.totalRows as row}
						<div class="home-total-row">
							<dt class="home-total-row__label">{row.label}</dt>
							<dd class="home-total-row__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			</details>
		{/if}
	</LoadingBoundary>
</section>
