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
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { toProductApiErrorMessage, toWorkspaceOverviewView } from '$lib/product/view-models';
	import SampleWorkspaceSetupLoader from './SampleWorkspaceSetupLoader.svelte';

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
	const sampleStudies = $derived(overviewView?.sampleStudies ?? []);

	onMount(() => {
		void loadSampleStudies();
	});

	async function loadSampleStudies() {
		loadState = 'loading';
		errorMessage = null;

		try {
			await productApi.ensureSampleStudies();
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
	eyebrow={text.workspaceHome.sampleDemo.eyebrow}
	title={text.workspaceHome.sampleDemo.title}
	description={text.workspaceHome.sampleDemo.description}
	statusLabel={text.workspaceHome.sampleDemo.status}
/>

<section class="sample-demo" aria-label={text.workspaceHome.sampleDemo.aria}>
	{#if loadState === 'loading'}
		<SampleWorkspaceSetupLoader
			kicker={text.workspaceHome.sampleDemo.loaderKicker}
			title={text.workspaceHome.sampleDemo.loaderTitle}
			body={text.workspaceHome.sampleDemo.loaderBody}
			progressLabel={text.workspaceHome.sampleDemo.loaderProgressAria}
			stepsLabel={text.workspaceHome.sampleDemo.loaderStepsAria}
			steps={text.workspaceHome.sampleDemo.loaderSteps}
		/>
	{:else if loadState === 'error' && errorMessage}
		<ErrorPanel
			title={text.workspaceHome.errorTitle}
			message={errorMessage}
			retryLabel={text.workspaceHome.retry}
			onRetry={loadSampleStudies}
		/>
	{:else if sampleStudies.length === 0}
		<EmptyState
			title={text.workspaceHome.sampleStudies}
			description={text.workspaceHome.sampleReadOnlyNote}
			actionHref={resolve('/app')}
			actionLabel={text.workspaceHome.sampleDemo.backToHome}
		/>
	{:else}
		<section class="sample-demo-hero" aria-label={text.workspaceHome.sampleStudies}>
			<div class="sample-demo-hero__copy">
				<p class="workspace-home-kicker">{text.workspaceHome.examples}</p>
				<h2>{text.workspaceHome.sampleStudies}</h2>
				<p>{text.workspaceHome.sampleReadOnlyNote}</p>
				<div class="sample-demo-hero__actions">
					<a class="secondary-button" href={resolve('/app#sample-studies')}>
						{text.workspaceHome.sampleDemo.backToHome}
					</a>
					<StatusBadge status="demo" label={text.workspaceHome.sampleDemo.synthetic} />
				</div>
			</div>
			<aside class="sample-demo-readonly">
				<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.readOnlyTitle}</p>
				<p>{text.workspaceHome.sampleDemo.readOnlyBody}</p>
			</aside>
		</section>

		<section class="workspace-home-section" aria-label={text.workspaceHome.sampleDemo.chooseSample}>
			<div class="workspace-home-section__header">
				<div>
					<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.snapshot}</p>
					<h2>{text.workspaceHome.sampleDemo.chooseSample}</h2>
				</div>
				<p>{text.workspaceHome.sampleDemo.chooseSampleBody}</p>
			</div>
			<div class="sample-study-grid">
				{#each sampleStudies as study (study.id)}
					<a class="sample-study-card" href={study.actionHref}>
						<span class="sample-study-card__kicker">{study.ownership.label}</span>
						<strong>{study.title}</strong>
						<span>{study.ownership.readOnlyMessage ?? text.workspaceHome.sampleReadOnlyNote}</span>
						<span class="flex flex-wrap gap-2">
							<StatusBadge status={study.ownership.badgeStatus} label={study.ownership.label} />
							<StatusBadge status={study.status} />
						</span>
						{#each study.rows.slice(0, 4) as row}
							<small>{row.label}: {row.value}</small>
						{/each}
						<span class="sample-study-card__action">{study.actionLabel}</span>
					</a>
				{/each}
			</div>
		</section>
	{/if}
</section>
