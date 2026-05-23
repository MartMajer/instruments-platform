<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { onDestroy } from 'svelte';
	import type { CampaignSeriesListResponse, CampaignSeriesPortfolioQuery } from '$lib/api/product';
	import type { AuthSessionResponse } from '$lib/api/setup';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { Archive, Check, Copy, LoaderCircle, Pencil, Plus, RotateCcw, X } from 'lucide-svelte';
	import {
		createProductApiFromEnv,
		createProductRequestGate,
		createSetupApiFromEnv
	} from '$lib/product/route-state';
	import {
		buildStudyNamePlaceholder,
		defaultStudyBlueprintId,
		getStudyBlueprintOption,
		listStudyBlueprintOptions,
		type StudyBlueprintId
	} from '$lib/product/study-blueprint';
	import { toCampaignSeriesListView, toProductApiErrorMessage } from '$lib/product/view-models';
	import {
		getProductAuthContext,
		hasProductPermission,
		setupManagePermission
	} from '$lib/product/auth-context';

	type LoadState = 'loading' | 'ready' | 'error';
	type PortfolioQueryState = {
		search: string;
		status: string;
		sort: string;
		visibility: string;
	};

	const productApi = createProductApiFromEnv(env);
	const setupApi = createSetupApiFromEnv(env);
	const requestGate = createProductRequestGate();
	const authContext = getProductAuthContext();
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const blueprintOptions = $derived(listStudyBlueprintOptions(locale));

	let loadState = $state<LoadState>('loading');
	let authSession = $state<AuthSessionResponse | null>(null);
	let campaignSeriesList = $state<CampaignSeriesListResponse | null>(null);
	let errorMessage = $state<string | null>(null);
	let selectedBlueprintId = $state<StudyBlueprintId>(defaultStudyBlueprintId);
	let newSeriesName = $state('');
	let creatingSeries = $state(false);
	let createSeriesError = $state<string | null>(null);
	let renamingSeriesId = $state<string | null>(null);
	let renameValue = $state('');
	let renaming = $state(false);
	let renameError = $state<string | null>(null);
	let duplicateActionSeriesId = $state<string | null>(null);
	let duplicateActionError = $state<string | null>(null);
	let archiveActionSeriesId = $state<string | null>(null);
	let archiveActionError = $state<string | null>(null);

	const unsubscribeAuth = authContext.session.subscribe((value) => {
		authSession = value;
	});
	onDestroy(unsubscribeAuth);

	const portfolioQuery = $derived(readPortfolioQuery(page.url.searchParams));
	const canManageSetup = $derived(hasProductPermission(authSession, setupManagePermission));
	const listView = $derived(
		campaignSeriesList ? toCampaignSeriesListView(campaignSeriesList, portfolioQuery) : null
	);
	const selectedBlueprint = $derived(getStudyBlueprintOption(selectedBlueprintId, locale));
	const studyNamePlaceholder = $derived(buildStudyNamePlaceholder(selectedBlueprintId, locale));

	$effect(() => {
		void loadCampaignSeries(portfolioQuery);
	});

	async function loadCampaignSeries(query: PortfolioQueryState = portfolioQuery) {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;

		try {
			const nextCampaignSeriesList = await productApi.listCampaignSeries(toApiQuery(query));
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			campaignSeriesList = nextCampaignSeriesList;
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			campaignSeriesList = null;
			errorMessage = toProductApiErrorMessage(error, 'Studies could not be loaded.');
			loadState = 'error';
		}
	}

	async function createCampaignSeries() {
		if (!canManageSetup) {
			return;
		}

		const name = newSeriesName.trim();
		if (!name) {
			createSeriesError = 'Enter a study name.';
			return;
		}

		creatingSeries = true;
		createSeriesError = null;

		try {
			const created = await setupApi.createCampaignSeries({ name });
			await goto(resolve(`/app/campaign-series/${created.id}/setup`));
		} catch (error) {
			createSeriesError = toProductApiErrorMessage(error, 'Study could not be created.');
		} finally {
			creatingSeries = false;
		}
	}

	function updatePortfolioQuery(next: Partial<PortfolioQueryState>) {
		const query = {
			...portfolioQuery,
			...next
		};
		const parameters = new URLSearchParams();

		if (query.search.trim()) {
			parameters.set('search', query.search.trim());
		}

		if (query.status !== 'all') {
			parameters.set('status', query.status);
		}

		if (query.sort !== 'activity_desc') {
			parameters.set('sort', query.sort);
		}

		if (query.visibility !== 'active') {
			parameters.set('visibility', query.visibility);
		}

		const serialized = parameters.toString();
		const path = `${resolve('/app/campaign-series')}${serialized ? `?${serialized}` : ''}`;
		void goto(path);
	}

	function startRename(seriesId: string, title: string) {
		if (!canManageSetup) {
			return;
		}

		renamingSeriesId = seriesId;
		renameValue = title;
		renameError = null;
	}

	function cancelRename() {
		renamingSeriesId = null;
		renameValue = '';
		renameError = null;
	}

	async function saveRename(seriesId: string) {
		if (!canManageSetup) {
			return;
		}

		const name = renameValue.trim();
		if (!name) {
			renameError = 'Enter a study name.';
			return;
		}

		renaming = true;
		renameError = null;

		try {
			await productApi.renameCampaignSeries(seriesId, { name });
			cancelRename();
			await loadCampaignSeries(portfolioQuery);
		} catch (error) {
			renameError = toProductApiErrorMessage(error, 'Study could not be renamed.');
		} finally {
			renaming = false;
		}
	}

	async function saveArchiveState(seriesId: string, archived: boolean) {
		if (!canManageSetup) {
			return;
		}

		archiveActionSeriesId = seriesId;
		archiveActionError = null;

		try {
			if (archived) {
				await productApi.restoreCampaignSeries(seriesId);
			} else {
				await productApi.archiveCampaignSeries(seriesId, {});
			}
			await loadCampaignSeries(portfolioQuery);
		} catch (error) {
			archiveActionError = toProductApiErrorMessage(
				error,
				archived ? 'Study could not be restored.' : 'Study could not be archived.'
			);
		} finally {
			archiveActionSeriesId = null;
		}
	}

	async function duplicateCampaignSeries(seriesId: string, name: string) {
		if (!canManageSetup || duplicateActionSeriesId) {
			return;
		}

		duplicateActionSeriesId = seriesId;
		duplicateActionError = null;

		try {
			const created = await productApi.duplicateCampaignSeries(seriesId, { name });
			await goto(resolve(`/app/campaign-series/${created.id}/setup`));
		} catch (error) {
			duplicateActionError = toProductApiErrorMessage(
				error,
				'Sample study could not be duplicated.'
			);
		} finally {
			duplicateActionSeriesId = null;
		}
	}

	function readPortfolioQuery(parameters: URLSearchParams): PortfolioQueryState {
		return {
			search: parameters.get('search')?.trim() ?? '',
			status: parameters.get('status') || 'all',
			sort: parameters.get('sort') || 'activity_desc',
			visibility: parameters.get('visibility') || 'active'
		};
	}

	function toApiQuery(query: PortfolioQueryState): CampaignSeriesPortfolioQuery | undefined {
		const apiQuery: CampaignSeriesPortfolioQuery = {};

		if (query.search.trim()) {
			apiQuery.search = query.search.trim();
		}

		if (query.status !== 'all') {
			apiQuery.status = query.status;
		}

		if (query.sort !== 'activity_desc') {
			apiQuery.sort = query.sort;
		}

		if (query.visibility !== 'active') {
			apiQuery.visibility = query.visibility;
		}

		return Object.keys(apiQuery).length > 0 ? apiQuery : undefined;
	}
</script>

<SurfaceHeader
	eyebrow={text.portfolio.eyebrow}
	title={text.portfolio.title}
	description={text.portfolio.description}
/>

<section class="product-panel" data-priority="primary" aria-label={text.portfolio.title}>
	<LoadingBoundary loading={loadState === 'loading'} label={text.portfolio.loading}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={text.portfolio.errorTitle}
				message={errorMessage}
				retryLabel={text.portfolio.retry}
				onRetry={loadCampaignSeries}
			/>
		{:else if listView}
			{#if canManageSetup}
				<section class="portfolio-create" aria-label={text.portfolio.title}>
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{text.portfolio.guidedDesign}</p>
							<h2 class="product-title">{text.portfolio.startBlueprint}</h2>
						</div>
					</div>

					<div class="study-blueprint-picker" aria-label={text.portfolio.startBlueprint}>
						{#each blueprintOptions as option}
							<button
								type="button"
								class="study-blueprint-card"
								class:study-blueprint-card--selected={selectedBlueprintId === option.id}
								aria-pressed={selectedBlueprintId === option.id}
								onclick={() => {
									selectedBlueprintId = option.id;
								}}
							>
								<span class="study-blueprint-card__eyebrow">{option.eyebrow}</span>
								<strong>{option.title}</strong>
								<span>{option.summary}</span>
								<small>{option.bestFor}</small>
								<span class="study-blueprint-card__highlights">
									{#each option.highlights as highlight}
										<span>{highlight}</span>
									{/each}
								</span>
							</button>
						{/each}
					</div>

					<section class="study-blueprint-plan" aria-label={text.portfolio.selectedStartingPoint}>
						<div>
							<p class="product-kicker">{text.portfolio.selectedStartingPoint}</p>
							<h3>{selectedBlueprint.nextStepsTitle}</h3>
							<p>{selectedBlueprint.summary}</p>
						</div>
						<ol>
							{#each selectedBlueprint.nextSteps as step}
								<li>
									<strong>{step.label}</strong>
									<span>{step.description}</span>
								</li>
							{/each}
						</ol>
					</section>

					<form
						class="portfolio-create__form"
						onsubmit={(event) => {
							event.preventDefault();
							void createCampaignSeries();
						}}
					>
						<label class="field">
							<span>{text.portfolio.studyName}</span>
							<input
								bind:value={newSeriesName}
								disabled={creatingSeries}
								placeholder={studyNamePlaceholder}
							/>
						</label>
						<button type="submit" class="primary-button" disabled={creatingSeries}>
							{#if creatingSeries}
								<LoaderCircle size={17} aria-hidden="true" />
							{:else}
								<Plus size={17} aria-hidden="true" />
							{/if}
							<span>{creatingSeries ? text.portfolio.creating : text.portfolio.continueSetup}</span>
						</button>
					</form>

					{#if createSeriesError}
						<p class="error-line" role="alert">{createSeriesError}</p>
					{/if}
				</section>
			{:else}
				<section class="portfolio-create" aria-label={text.portfolio.readOnlyAccess}>
					<div class="product-panel__header">
						<div>
							<p class="product-kicker">{text.portfolio.title}</p>
							<h2 class="product-title">{text.portfolio.readOnlyAccess}</h2>
						</div>
					</div>
					<p class="text-sm text-[var(--color-text-muted)]">
						{text.portfolio.readOnlyBody}
					</p>
				</section>
			{/if}

			<div class="product-panel__header">
				<div>
					<p class="product-kicker">{text.portfolio.studyList}</p>
					<h2 class="product-title">{text.portfolio.openStudy}</h2>
				</div>
			</div>

			<div class="portfolio-toolbar" role="group" aria-label={text.portfolio.studyList}>
				<label class="field">
					<span>{text.portfolio.searchStudies}</span>
					<input
						value={portfolioQuery.search}
						placeholder={text.portfolio.searchPlaceholder}
						oninput={(event) => updatePortfolioQuery({ search: event.currentTarget.value })}
					/>
				</label>

				<label class="field">
					<span>{text.portfolio.readiness}</span>
					<select
						value={portfolioQuery.status}
						onchange={(event) => updatePortfolioQuery({ status: event.currentTarget.value })}
					>
						{#each listView.statusOptions as option}
							<option value={option.value}>{option.label}</option>
						{/each}
					</select>
				</label>

				<label class="field">
					<span>{text.portfolio.sort}</span>
					<select
						value={portfolioQuery.sort}
						onchange={(event) => updatePortfolioQuery({ sort: event.currentTarget.value })}
					>
						{#each listView.sortOptions as option}
							<option value={option.value}>{option.label}</option>
						{/each}
					</select>
				</label>

				<label class="field">
					<span>{text.portfolio.visibility}</span>
					<select
						value={portfolioQuery.visibility}
						onchange={(event) => updatePortfolioQuery({ visibility: event.currentTarget.value })}
					>
						{#each listView.visibilityOptions as option}
							<option value={option.value}>{option.label}</option>
						{/each}
					</select>
				</label>
			</div>

			{#if listView.emptyState}
				<EmptyState title={listView.emptyState.title} description={listView.emptyState.message} />
			{:else}
				<div class="study-section-list">
					{#each listView.studySections as section (section.id)}
						<section class="study-section" aria-label={section.title}>
							<div class="study-section__header">
								<div>
									<h3 class="study-section__title">{section.title}</h3>
									<p class="study-section__description">{section.description}</p>
								</div>
							</div>

							{#if section.groups.length === 0}
								<p class="study-section__empty">{section.emptyState}</p>
							{:else}
								<div class="study-lifecycle-list">
									{#each section.groups as group (group.id)}
										<div class="study-lifecycle-group">
											<div class="study-lifecycle-group__header">
												<StatusBadge status={group.items[0].lifecycle.status} label={group.label} />
												<p>{group.description}</p>
											</div>
											<div class="record-list">
												{#each group.items as item (item.id)}
													<article class="record-row" aria-label={item.title}>
														{#if canManageSetup && renamingSeriesId === item.id}
															<form
																class="portfolio-rename"
																onsubmit={(event) => {
																	event.preventDefault();
																	void saveRename(item.id);
																}}
															>
																<label class="field portfolio-rename__field">
																	<span>Rename study name</span>
																	<input bind:value={renameValue} disabled={renaming} />
																</label>
																<div class="action-row portfolio-rename__actions">
																	<button type="submit" class="primary-button" disabled={renaming}>
																		{#if renaming}
																			<LoaderCircle size={17} aria-hidden="true" />
																		{:else}
																			<Check size={17} aria-hidden="true" />
																		{/if}
																		<span>{renaming ? 'Saving...' : 'Save name'}</span>
																	</button>
																	<button
																		type="button"
																		class="secondary-button"
																		disabled={renaming}
																		onclick={cancelRename}
																	>
																		<X size={17} aria-hidden="true" />
																		<span>Cancel</span>
																	</button>
																</div>
															</form>
															{#if renameError}
																<p class="error-line" role="alert">{renameError}</p>
															{/if}
														{:else}
															<div class="portfolio-row">
																<div class="portfolio-row__body">
																	<div class="record-row__header">
																		<a
																			class="record-row__title portfolio-row__title-link"
																			href={item.href}>{item.title}</a
																		>
																		<span class="action-row">
																			<StatusBadge
																				status={item.ownership.badgeStatus}
																				label={item.ownership.label}
																			/>
																			<StatusBadge status={item.status} />
																		</span>
																	</div>
																	{#if item.ownership.readOnlyMessage}
																		<p class="text-sm text-[var(--color-text-muted)]">
																			{item.ownership.readOnlyMessage}
																		</p>
																	{/if}
																	<div class="record-grid">
																		{#each item.rows as row}
																			<span class="record-field">
																				<span class="record-field__label">{row.label}</span>
																				<span class="record-field__value">{row.value}</span>
																			</span>
																		{/each}
																	</div>
																	<a class="secondary-button" href={item.primaryAction.href}>
																		<span>{item.primaryAction.label}</span>
																	</a>
																</div>
																{#if canManageSetup && item.canMutate}
																	<div class="portfolio-row__actions">
																		<button
																			type="button"
																			class="secondary-button portfolio-row__rename"
																			aria-label={`Rename ${item.title}`}
																			onclick={() => startRename(item.id, item.title)}
																		>
																			<Pencil size={17} aria-hidden="true" />
																			<span>Rename</span>
																		</button>
																		<button
																			type="button"
																			class="secondary-button portfolio-row__archive"
																			aria-label={`${item.archiveActionLabel} ${item.title}`}
																			disabled={archiveActionSeriesId === item.id}
																			onclick={() => saveArchiveState(item.id, item.archived)}
																		>
																			{#if archiveActionSeriesId === item.id}
																				<LoaderCircle size={17} aria-hidden="true" />
																			{:else if item.archived}
																				<RotateCcw size={17} aria-hidden="true" />
																			{:else}
																				<Archive size={17} aria-hidden="true" />
																			{/if}
																			<span
																				>{archiveActionSeriesId === item.id
																					? 'Saving...'
																					: item.archiveActionLabel}</span
																			>
																		</button>
																	</div>
																{:else if canManageSetup && item.duplicateAction}
																	<div class="portfolio-row__actions">
																		<button
																			type="button"
																			class="secondary-button portfolio-row__duplicate"
																			aria-label={`${item.duplicateAction.label} ${item.title}`}
																			disabled={duplicateActionSeriesId === item.id}
																			onclick={() =>
																				duplicateCampaignSeries(
																					item.id,
																					item.duplicateAction?.defaultName ?? `Copy of ${item.title}`
																				)}
																		>
																			{#if duplicateActionSeriesId === item.id}
																				<LoaderCircle size={17} aria-hidden="true" />
																			{:else}
																				<Copy size={17} aria-hidden="true" />
																			{/if}
																			<span
																				>{duplicateActionSeriesId === item.id
																					? 'Duplicating...'
																					: item.duplicateAction.label}</span
																			>
																		</button>
																	</div>
																{/if}
															</div>
														{/if}
													</article>
												{/each}
											</div>
										</div>
									{/each}
								</div>
							{/if}
						</section>
					{/each}
				</div>
				{#if archiveActionError}
					<p class="error-line" role="alert">{archiveActionError}</p>
				{/if}
				{#if duplicateActionError}
					<p class="error-line" role="alert">{duplicateActionError}</p>
				{/if}
			{/if}

		{/if}
	</LoadingBoundary>
</section>
