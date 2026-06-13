<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import type { TenantSettingsWorkspaceResponse, UpdateTenantReportBrandingRequest } from '$lib/api/product';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import { toProductApiErrorMessage, toTenantSettingsView } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';
	type SaveState = 'idle' | 'saving' | 'saved' | 'error';

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();

	let loadState = $state<LoadState>('loading');
	let reportBrandingSaveState = $state<SaveState>('idle');
	let settings = $state<TenantSettingsWorkspaceResponse | null>(null);
	let errorMessage = $state<string | null>(null);
	let reportBrandingErrorMessage = $state<string | null>(null);
	let reportBrandingForm = $state<UpdateTenantReportBrandingRequest>({
		organizationLabel: '',
		reportTitle: '',
		accentColorHex: '#2563eb',
		layoutVariant: 'standard'
	});

	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const settingsView = $derived(settings ? toTenantSettingsView(settings, locale) : null);
	const canSaveReportBranding = $derived(
		loadState === 'ready' &&
			reportBrandingSaveState !== 'saving' &&
			reportBrandingForm.organizationLabel.trim().length > 0 &&
			reportBrandingForm.reportTitle.trim().length > 0 &&
			/^#[0-9A-Fa-f]{6}$/.test(reportBrandingForm.accentColorHex.trim()) &&
			['standard', 'compact', 'compliance'].includes(reportBrandingForm.layoutVariant)
	);

	onMount(() => {
		void loadTenantSettings();
	});

	async function loadTenantSettings() {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;

		try {
			const nextSettings = await productApi.getTenantSettings();
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			settings = nextSettings;
			resetReportBrandingForm(nextSettings);
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			settings = null;
			errorMessage = toProductApiErrorMessage(error, text.settings.errorTitle);
			loadState = 'error';
		}
	}

	function resetReportBrandingForm(nextSettings = settings) {
		if (!nextSettings) {
			return;
		}

		reportBrandingForm = {
			organizationLabel: nextSettings.reportBranding.organizationLabel,
			reportTitle: nextSettings.reportBranding.reportTitle,
			accentColorHex: nextSettings.reportBranding.accentColorHex,
			layoutVariant: nextSettings.reportBranding.layoutVariant
		};
		reportBrandingSaveState = 'idle';
		reportBrandingErrorMessage = null;
	}

	async function saveReportBranding() {
		if (!settings || !canSaveReportBranding) {
			return;
		}

		reportBrandingSaveState = 'saving';
		reportBrandingErrorMessage = null;

		try {
			const nextReportBranding = await productApi.updateTenantReportBranding({
				organizationLabel: reportBrandingForm.organizationLabel.trim(),
				reportTitle: reportBrandingForm.reportTitle.trim(),
				accentColorHex: reportBrandingForm.accentColorHex.trim(),
				layoutVariant: reportBrandingForm.layoutVariant
			});

			settings = {
				...settings,
				reportBranding: nextReportBranding
			};
			resetReportBrandingForm(settings);
			reportBrandingSaveState = 'saved';
		} catch (error) {
			reportBrandingErrorMessage = toProductApiErrorMessage(
				error,
				text.settings.reportBrandingSaveError
			);
			reportBrandingSaveState = 'error';
		}
	}
</script>

<SurfaceHeader
	eyebrow={text.settings.eyebrow}
	title={text.settings.title}
	description={text.settings.description}
/>

<section class="product-panel" data-priority="primary" aria-label={text.settings.title}>
	<LoadingBoundary loading={loadState === 'loading'} label={text.settings.loading}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={text.settings.errorTitle}
				message={errorMessage}
				retryLabel={text.settings.retry}
				onRetry={loadTenantSettings}
			/>
		{:else if settingsView}
			<div class="grid gap-5">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{text.settings.hub}</p>
						<h2 class="product-title">{text.settings.whatManage}</h2>
						<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.whatManageBody}
						</p>
					</div>
					<StatusBadge status={settingsView.status} />
				</div>

				<div class="record-list" aria-label={text.settings.shortcutsAria}>
					<a class="record-row" href="/app/team">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.teamAccess}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.teamBody}
						</span>
					</a>
					<a class="record-row" href="/app/directory">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.directoryShortcut}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.directoryBody}
						</span>
					</a>
					<a class="record-row" href="/app/campaign-series">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.studySetup}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.studySetupBody}
						</span>
					</a>
					<a class="record-row" href="/app/exports">
						<span class="record-row__header">
							<span class="record-row__title">{text.settings.exportsShortcut}</span>
							<span class="secondary-button">{text.common.open}</span>
						</span>
						<span class="text-sm leading-6 text-[var(--color-text-muted)]">
							{text.settings.exportsBody}
						</span>
					</a>
				</div>

				<details class="record-row" aria-label={text.settings.workspaceDetailsAria}>
					<summary class="record-row__title">{text.settings.workspaceDetails}</summary>
					<div class="grid gap-4 pt-4">
						<div>
							<p class="product-kicker">{text.settings.profile}</p>
							<h3 class="text-base font-semibold text-[var(--color-text)]">
								{settingsView.title}
							</h3>
						</div>

						<dl class="record-grid" role="group" aria-label={text.settings.profileDetailsAria}>
							{#each settingsView.profileRows.filter((row) => !row.mono) as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>

						<div
							class="grid gap-3 border-t border-[var(--color-border)] pt-4"
							role="group"
							aria-label={text.settings.reportBrandingAria}
						>
							<div>
								<p class="product-kicker">{settingsView.reportBranding.title}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{settingsView.reportBranding.rows[0]?.value}
								</h3>
								<p class="mt-1 text-sm leading-6 text-[var(--color-text-muted)]">
									{settingsView.reportBranding.description}
								</p>
							</div>

							<dl class="record-grid">
								{#each settingsView.reportBranding.rows as row}
									<div class="record-field">
										<dt class="record-field__label">{row.label}</dt>
										<dd class="record-field__value">{row.value}</dd>
									</div>
								{/each}
							</dl>

							<form
								class="grid gap-4 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface-subtle)] p-4"
								aria-label={text.settings.reportBrandingFormAria}
								onsubmit={(event) => {
									event.preventDefault();
									void saveReportBranding();
								}}
							>
								<div class="grid gap-3 md:grid-cols-2">
									<label class="grid gap-1 text-sm font-medium text-[var(--color-text)]">
										<span>{settingsView.reportBranding.rows[0]?.label}</span>
										<input
											class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
											bind:value={reportBrandingForm.organizationLabel}
											maxlength="256"
											required
										/>
									</label>
									<label class="grid gap-1 text-sm font-medium text-[var(--color-text)]">
										<span>{settingsView.reportBranding.rows[1]?.label}</span>
										<input
											class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
											bind:value={reportBrandingForm.reportTitle}
											maxlength="256"
											required
										/>
									</label>
									<label class="grid gap-1 text-sm font-medium text-[var(--color-text)]">
										<span>{settingsView.reportBranding.rows[4]?.label}</span>
										<input
											class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
											bind:value={reportBrandingForm.accentColorHex}
											pattern="#[0-9A-Fa-f]{6}"
											maxlength="7"
											required
										/>
									</label>
									<label class="grid gap-1 text-sm font-medium text-[var(--color-text)]">
										<span>{settingsView.reportBranding.rows[5]?.label}</span>
										<select
											class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-text)]"
											bind:value={reportBrandingForm.layoutVariant}
										>
											<option value="standard">{text.settings.layoutStandard}</option>
											<option value="compact">{text.settings.layoutCompact}</option>
											<option value="compliance">{text.settings.layoutCompliance}</option>
										</select>
									</label>
								</div>
								<div class="flex flex-wrap items-center gap-3">
									<button
										class="primary-button"
										type="button"
										disabled={!canSaveReportBranding}
										onclick={() => void saveReportBranding()}
									>
										{reportBrandingSaveState === 'saving'
											? text.settings.reportBrandingSaving
											: text.settings.reportBrandingSave}
									</button>
									<button class="secondary-button" type="button" onclick={() => resetReportBrandingForm()}>
										{text.settings.reportBrandingReset}
									</button>
									{#if reportBrandingSaveState === 'saved'}
										<p class="text-sm font-medium text-[var(--color-success)]">
											{text.settings.reportBrandingSaved}
										</p>
									{:else if reportBrandingSaveState === 'error' && reportBrandingErrorMessage}
										<p class="text-sm font-medium text-[var(--color-danger)]">
											{reportBrandingErrorMessage}
										</p>
									{/if}
								</div>
							</form>

							{#if settingsView.reportBranding.deferredItems.length > 0}
								<div class="record-row">
									<p class="record-row__title">{settingsView.reportBranding.deferredTitle}</p>
									<p class="text-sm leading-6 text-[var(--color-text-muted)]">
										{settingsView.reportBranding.deferredItems.join(', ')}
									</p>
								</div>
							{/if}
						</div>

						<div class="grid gap-3 border-t border-[var(--color-border)] pt-4">
							<div>
								<p class="product-kicker">{text.settings.scale}</p>
								<h3 class="text-base font-semibold text-[var(--color-text)]">
									{text.settings.footprint}
								</h3>
							</div>

							<dl class="settings-count-list" role="group" aria-label={text.settings.countsAria}>
								{#each settingsView.metricRows as row}
									<div class="settings-count-row">
										<dt class="settings-count-row__label">{row.label}</dt>
										<dd class="settings-count-row__value">{row.value}</dd>
									</div>
								{/each}
							</dl>
						</div>
					</div>
				</details>
			</div>
		{/if}
	</LoadingBoundary>
</section>
