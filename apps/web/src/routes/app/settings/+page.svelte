<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import {
		AlertTriangle,
		CheckCircle2,
		Languages,
		Mail,
		RotateCcw,
		Save
	} from 'lucide-svelte';
	import type {
		EmailTemplateValidationIssueResponse,
		TenantEmailTemplateSettingsResponse,
		TenantSettingsWorkspaceResponse
	} from '$lib/api/product';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import { toProductApiErrorMessage, toTenantSettingsView } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';
	type TemplateField = 'subject' | 'bodyText';
	type EmailTemplateForm = TenantEmailTemplateSettingsResponse & {
		key: string;
		originalSubject: string;
		originalBodyText: string;
	};

	const fallbackLocales = ['en', 'hr-HR'];
	const requiredVariables = ['{{respondent_link}}', '{{unsubscribe_link}}', '{{workspace_name}}'];

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();

	let loadState = $state<LoadState>('loading');
	let settings = $state<TenantSettingsWorkspaceResponse | null>(null);
	let selectedLocale = $state('en');
	let selectedTemplateKey = $state('');
	let templateForms = $state<Record<string, EmailTemplateForm>>({});
	let errorMessage = $state<string | null>(null);
	let mutationErrorMessage = $state<string | null>(null);
	let successMessage = $state<string | null>(null);
	let savingLanguage = $state(false);
	let savingTemplateKey = $state<string | null>(null);
	let resettingTemplateKey = $state<string | null>(null);

	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const settingsView = $derived(settings ? toTenantSettingsView(settings, locale) : null);
	const supportedLocales = $derived(settings?.supportedLocales?.length ? settings.supportedLocales : fallbackLocales);
	const templateList = $derived(Object.values(templateForms));
	const currentTemplate = $derived(selectedTemplateKey ? (templateForms[selectedTemplateKey] ?? null) : null);
	const languageDirty = $derived(!!settings && selectedLocale !== settings.profile.defaultLocale);
	const currentTemplateDirty = $derived(currentTemplate ? isTemplateDirty(currentTemplate) : false);
	const currentTemplateIssues = $derived(currentTemplate ? visibleTemplateIssues(currentTemplate) : []);
	const currentTemplatePreview = $derived(currentTemplate ? previewTemplate(currentTemplate) : null);

	onMount(() => {
		void loadTenantSettings();
	});

	async function loadTenantSettings() {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;
		mutationErrorMessage = null;
		successMessage = null;

		try {
			const nextSettings = await productApi.getTenantSettings();
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			settings = nextSettings;
			initializeEditors(nextSettings);
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

	function initializeEditors(nextSettings: TenantSettingsWorkspaceResponse) {
		selectedLocale = nextSettings.profile.defaultLocale || fallbackLocales[0];
		templateForms = Object.fromEntries(
			nextSettings.emailTemplates.map((template) => [templateKey(template), toTemplateForm(template)])
		);

		const nextKeys = Object.keys(templateForms);
		selectedTemplateKey = nextKeys.includes(selectedTemplateKey)
			? selectedTemplateKey
			: (nextKeys[0] ?? '');
	}

	async function saveLanguage() {
		if (!settings || !languageDirty || savingLanguage) {
			return;
		}

		savingLanguage = true;
		mutationErrorMessage = null;
		successMessage = null;

		try {
			const response = await productApi.updateTenantLanguage({ defaultLocale: selectedLocale });
			settings = {
				...settings,
				profile: {
					...settings.profile,
					defaultLocale: response.defaultLocale,
					updatedAt: new Date().toISOString()
				},
				supportedLocales: response.supportedLocales
			};
			selectedLocale = response.defaultLocale;
			successMessage = text.settings.languageSaved;
		} catch (error) {
			mutationErrorMessage = toProductApiErrorMessage(error, text.settings.languageSaveFailed);
		} finally {
			savingLanguage = false;
		}
	}

	async function saveCurrentTemplate(event: SubmitEvent) {
		event.preventDefault();
		if (
			!settings ||
			!currentTemplate ||
			!currentTemplateDirty ||
			currentTemplateIssues.length > 0 ||
			savingTemplateKey
		) {
			return;
		}

		const form = currentTemplate;
		savingTemplateKey = form.key;
		mutationErrorMessage = null;
		successMessage = null;

		try {
			const response = await productApi.updateTenantEmailTemplate(form.templateCode, form.locale, {
				subject: form.subject,
				bodyText: form.bodyText
			});
			replaceTemplate(response);
			successMessage = text.settings.templateSaved;
		} catch (error) {
			mutationErrorMessage = toProductApiErrorMessage(error, text.settings.templateSaveFailed);
		} finally {
			savingTemplateKey = null;
		}
	}

	async function resetCurrentTemplate() {
		if (!settings || !currentTemplate || resettingTemplateKey) {
			return;
		}

		const form = currentTemplate;
		resettingTemplateKey = form.key;
		mutationErrorMessage = null;
		successMessage = null;

		try {
			const response = await productApi.resetTenantEmailTemplate(form.templateCode, form.locale);
			replaceTemplate(response.template);
			successMessage = text.settings.templateReset;
		} catch (error) {
			mutationErrorMessage = toProductApiErrorMessage(error, text.settings.templateResetFailed);
		} finally {
			resettingTemplateKey = null;
		}
	}

	function replaceTemplate(template: TenantEmailTemplateSettingsResponse) {
		if (!settings) {
			return;
		}

		const key = templateKey(template);
		templateForms = {
			...templateForms,
			[key]: toTemplateForm(template)
		};
		settings = {
			...settings,
			emailTemplates: settings.emailTemplates.some((item) => templateKey(item) === key)
				? settings.emailTemplates.map((item) => (templateKey(item) === key ? template : item))
				: [...settings.emailTemplates, template]
		};
		selectedTemplateKey = key;
	}

	function updateCurrentTemplateField(field: TemplateField, value: string) {
		if (!currentTemplate) {
			return;
		}

		const form = currentTemplate;
		templateForms = {
			...templateForms,
			[form.key]: {
				...form,
				[field]: value,
				validationIssues: []
			}
		};
		mutationErrorMessage = null;
		successMessage = null;
	}

	function toTemplateForm(template: TenantEmailTemplateSettingsResponse): EmailTemplateForm {
		return {
			...template,
			key: templateKey(template),
			originalSubject: template.subject,
			originalBodyText: template.bodyText
		};
	}

	function templateKey(template: Pick<TenantEmailTemplateSettingsResponse, 'templateCode' | 'locale'>) {
		return `${template.templateCode}:${template.locale}`;
	}

	function isTemplateDirty(form: EmailTemplateForm) {
		return form.subject !== form.originalSubject || form.bodyText !== form.originalBodyText;
	}

	function visibleTemplateIssues(form: EmailTemplateForm) {
		const issues = validateTemplateForm(form);
		return issues.length > 0 ? issues : form.validationIssues;
	}

	function validateTemplateForm(form: EmailTemplateForm): EmailTemplateValidationIssueResponse[] {
		const issues: EmailTemplateValidationIssueResponse[] = [];
		const subject = form.subject.trim();
		const bodyText = form.bodyText.trim();

		if (!subject) {
			issues.push({ code: 'subject_required', message: text.settings.subjectRequired });
		}

		if (bodyText.length < 80) {
			issues.push({ code: 'body_too_short', message: text.settings.bodyTooShort });
		}

		for (const variable of ['{{respondent_link}}', '{{unsubscribe_link}}']) {
			if (!bodyText.includes(variable)) {
				issues.push({
					code: `missing_${variable}`,
					message: text.settings.missingVariable(variable)
				});
			}
		}

		if (/<\/?[a-z][\s\S]*>/i.test(subject) || /<\/?[a-z][\s\S]*>/i.test(bodyText)) {
			issues.push({ code: 'html_not_allowed', message: text.settings.htmlNotAllowed });
		}

		return issues;
	}

	function previewTemplate(form: EmailTemplateForm) {
		return {
			subject: replaceTemplateVariables(form.subject),
			bodyText: replaceTemplateVariables(form.bodyText)
		};
	}

	function replaceTemplateVariables(value: string) {
		return value
			.replaceAll('{{workspace_name}}', settings?.profile.name || 'ValidatedScale')
			.replaceAll('{{respondent_link}}', 'https://staging.validatedscale.com/r/inv_preview')
			.replaceAll(
				'{{unsubscribe_link}}',
				'https://staging.validatedscale.com/r/inv_preview/unsubscribe'
			);
	}

	function localeLabel(value: string) {
		if (value === 'hr-HR' || value === 'hr') {
			return text.settings.croatian;
		}

		return text.settings.english;
	}

	function templateCodeLabel(value: string) {
		return value === 'reminder' ? text.settings.reminder : text.settings.invitation;
	}

	function inputValue(event: Event) {
		return (event.currentTarget as HTMLInputElement | HTMLTextAreaElement).value;
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
			<div class="settings-layout">
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{text.settings.workspaceControls}</p>
						<h2 class="product-title">{settingsView.title}</h2>
					</div>
					<StatusBadge status={settingsView.status} />
				</div>

				{#if mutationErrorMessage || successMessage}
					<div class:settings-alert={!!mutationErrorMessage} class:settings-success={!!successMessage}>
						{#if mutationErrorMessage}
							<AlertTriangle size={16} aria-hidden="true" />
							<span>{mutationErrorMessage}</span>
						{:else if successMessage}
							<CheckCircle2 size={16} aria-hidden="true" />
							<span>{successMessage}</span>
						{/if}
					</div>
				{/if}

				<div class="settings-grid">
					<section class="settings-section" aria-labelledby="settings-language-title">
						<div class="settings-section__header">
							<Languages size={18} aria-hidden="true" />
							<div>
								<p class="product-kicker">{text.settings.languageKicker}</p>
								<h3 id="settings-language-title">{text.settings.languageTitle}</h3>
							</div>
						</div>

						<div class="settings-segmented" role="group" aria-label={text.settings.defaultLanguage}>
							{#each supportedLocales as localeOption}
								<button
									type="button"
									class:active={selectedLocale === localeOption}
									onclick={() => (selectedLocale = localeOption)}
								>
									{localeLabel(localeOption)}
								</button>
							{/each}
						</div>

						<div class="settings-action-row">
							<span class="settings-muted">
								{languageDirty ? text.settings.unsavedChanges : text.settings.noChanges}
							</span>
							<button
								type="button"
								class="primary-button"
								disabled={!languageDirty || savingLanguage}
								onclick={() => void saveLanguage()}
							>
								<Save size={16} aria-hidden="true" />
								{savingLanguage ? text.settings.saving : text.settings.saveLanguage}
							</button>
						</div>
					</section>

					<section class="settings-section" aria-labelledby="settings-profile-title">
						<div>
							<p class="product-kicker">{text.settings.profile}</p>
							<h3 id="settings-profile-title">{text.settings.workspaceDetails}</h3>
						</div>

						<dl class="record-grid" role="group" aria-label={text.settings.profileDetailsAria}>
							{#each settingsView.profileRows.filter((row) => !row.mono) as row}
								<div class="record-field">
									<dt class="record-field__label">{row.label}</dt>
									<dd class="record-field__value">{row.value}</dd>
								</div>
							{/each}
						</dl>

						<dl class="settings-count-list" role="group" aria-label={text.settings.countsAria}>
							{#each settingsView.metricRows as row}
								<div class="settings-count-row">
									<dt class="settings-count-row__label">{row.label}</dt>
									<dd class="settings-count-row__value">{row.value}</dd>
								</div>
							{/each}
						</dl>
					</section>
				</div>

				<section class="settings-section settings-section--wide" aria-labelledby="settings-email-title">
					<div class="settings-section__header">
						<Mail size={18} aria-hidden="true" />
						<div>
							<p class="product-kicker">{text.settings.emailKicker}</p>
							<h3 id="settings-email-title">{text.settings.emailTitle}</h3>
						</div>
					</div>

					{#if templateList.length > 0 && currentTemplate}
						<div class="settings-template-tabs" role="tablist" aria-label={text.settings.templateTabsAria}>
							{#each templateList as template}
								<button
									type="button"
									role="tab"
									aria-selected={selectedTemplateKey === template.key}
									class:active={selectedTemplateKey === template.key}
									onclick={() => (selectedTemplateKey = template.key)}
								>
									<span>{templateCodeLabel(template.templateCode)}</span>
									<small>{localeLabel(template.locale)}</small>
								</button>
							{/each}
						</div>

						<form class="settings-template-editor" onsubmit={(event) => void saveCurrentTemplate(event)}>
							<div class="settings-template-meta">
								<span class="settings-pill">
									{currentTemplate.isCustom ? text.settings.customTemplate : text.settings.defaultTemplate}
								</span>
								<span class="settings-muted">
									{currentTemplateDirty ? text.settings.unsavedChanges : text.settings.noChanges}
								</span>
							</div>

							<label class="settings-field">
								<span>{text.settings.subject}</span>
								<input
									value={currentTemplate.subject}
									maxlength="160"
									oninput={(event) => updateCurrentTemplateField('subject', inputValue(event))}
								/>
							</label>

							<label class="settings-field">
								<span>{text.settings.bodyText}</span>
								<textarea
									rows="13"
									value={currentTemplate.bodyText}
									oninput={(event) => updateCurrentTemplateField('bodyText', inputValue(event))}
								></textarea>
							</label>

							<div class="settings-variable-row" aria-label={text.settings.requiredVariables}>
								<span>{text.settings.requiredVariables}</span>
								{#each requiredVariables as variable}
									<code>{variable}</code>
								{/each}
							</div>

							{#if currentTemplateIssues.length > 0}
								<ul class="settings-issues" aria-label={text.settings.validationIssues}>
									{#each currentTemplateIssues as issue}
										<li>
											<AlertTriangle size={15} aria-hidden="true" />
											<span>{issue.message}</span>
										</li>
									{/each}
								</ul>
							{/if}

							<div class="settings-template-preview" aria-label={text.settings.preview}>
								<p class="product-kicker">{text.settings.preview}</p>
								<strong>{currentTemplatePreview?.subject}</strong>
								<pre>{currentTemplatePreview?.bodyText}</pre>
							</div>

							<div class="settings-action-row">
								<button
									type="button"
									class="secondary-button"
									disabled={resettingTemplateKey === currentTemplate.key || !currentTemplate.isCustom}
									onclick={() => void resetCurrentTemplate()}
								>
									<RotateCcw size={16} aria-hidden="true" />
									{resettingTemplateKey === currentTemplate.key
										? text.settings.resetting
										: text.settings.resetTemplate}
								</button>
								<button
									type="submit"
									class="primary-button"
									disabled={
										!currentTemplateDirty ||
										currentTemplateIssues.length > 0 ||
										savingTemplateKey === currentTemplate.key
									}
								>
									<Save size={16} aria-hidden="true" />
									{savingTemplateKey === currentTemplate.key
										? text.settings.saving
										: text.settings.saveTemplate}
								</button>
							</div>
						</form>
					{/if}
				</section>

				<section class="settings-section settings-section--wide" aria-labelledby="settings-shortcuts-title">
					<div>
						<p class="product-kicker">{text.settings.hub}</p>
						<h3 id="settings-shortcuts-title">{text.settings.managementDestinations}</h3>
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
				</section>
			</div>
		{/if}
	</LoadingBoundary>
</section>

<style>
	.settings-layout {
		display: grid;
		gap: 1.25rem;
	}

	.settings-grid {
		display: grid;
		grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.1fr);
		gap: 1rem;
	}

	.settings-section {
		display: grid;
		align-content: start;
		gap: 1rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		padding: 1rem;
		background: var(--color-surface);
	}

	.settings-section--wide {
		grid-column: 1 / -1;
	}

	.settings-section__header {
		display: flex;
		align-items: flex-start;
		gap: 0.75rem;
	}

	.settings-section h3 {
		margin: 0;
		font-size: 1rem;
		font-weight: 700;
		color: var(--color-text);
	}

	.settings-segmented,
	.settings-template-tabs {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	.settings-segmented button,
	.settings-template-tabs button {
		display: inline-grid;
		gap: 0.15rem;
		min-height: 2.5rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		padding: 0.5rem 0.75rem;
		background: var(--color-surface);
		color: var(--color-text);
		font-size: 0.875rem;
		font-weight: 700;
		text-align: left;
	}

	.settings-template-tabs button small {
		color: var(--color-text-muted);
		font-size: 0.75rem;
		font-weight: 600;
	}

	.settings-segmented button.active,
	.settings-template-tabs button.active {
		border-color: var(--color-accent);
		background: var(--color-accent-soft);
	}

	.settings-template-editor {
		display: grid;
		gap: 1rem;
	}

	.settings-template-meta,
	.settings-action-row,
	.settings-variable-row {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
	}

	.settings-field {
		display: grid;
		gap: 0.4rem;
		font-size: 0.875rem;
		font-weight: 700;
		color: var(--color-text);
	}

	.settings-field input,
	.settings-field textarea {
		width: 100%;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		background: var(--color-surface);
		color: var(--color-text);
		font: inherit;
		font-weight: 500;
		line-height: 1.5;
		padding: 0.7rem 0.8rem;
	}

	.settings-field textarea {
		min-height: 16rem;
		resize: vertical;
		white-space: pre-wrap;
	}

	.settings-variable-row {
		justify-content: flex-start;
		color: var(--color-text-muted);
		font-size: 0.8125rem;
	}

	.settings-variable-row code {
		border: 1px solid var(--color-border);
		border-radius: 6px;
		padding: 0.2rem 0.4rem;
		background: var(--color-surface-muted);
		color: var(--color-text);
		font-size: 0.78rem;
	}

	.settings-pill {
		display: inline-flex;
		align-items: center;
		min-height: 1.75rem;
		border-radius: 999px;
		padding: 0.2rem 0.65rem;
		background: var(--color-surface-muted);
		color: var(--color-text);
		font-size: 0.75rem;
		font-weight: 800;
	}

	.settings-muted {
		color: var(--color-text-muted);
		font-size: 0.875rem;
	}

	.settings-alert,
	.settings-success {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		border-radius: 8px;
		padding: 0.75rem 0.9rem;
		font-size: 0.875rem;
		font-weight: 700;
	}

	.settings-alert {
		border: 1px solid var(--color-danger-border, #f3b4b4);
		background: var(--color-danger-soft, #fff1f1);
		color: var(--color-danger-text, #8a1f1f);
	}

	.settings-success {
		border: 1px solid var(--color-success-border, #9bd5b1);
		background: var(--color-success-soft, #effaf3);
		color: var(--color-success-text, #17663a);
	}

	.settings-issues {
		display: grid;
		gap: 0.45rem;
		margin: 0;
		padding: 0;
		list-style: none;
		color: var(--color-danger-text, #8a1f1f);
		font-size: 0.875rem;
	}

	.settings-issues li {
		display: flex;
		align-items: flex-start;
		gap: 0.45rem;
	}

	.settings-template-preview {
		display: grid;
		gap: 0.5rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		padding: 0.9rem;
		background: var(--color-surface-muted);
	}

	.settings-template-preview strong {
		color: var(--color-text);
		font-size: 0.95rem;
	}

	.settings-template-preview pre {
		margin: 0;
		white-space: pre-wrap;
		word-break: break-word;
		color: var(--color-text-muted);
		font-family: inherit;
		font-size: 0.875rem;
		line-height: 1.55;
	}

	.settings-action-row :global(.primary-button),
	.settings-action-row :global(.secondary-button) {
		display: inline-flex;
		align-items: center;
		gap: 0.45rem;
	}

	@media (max-width: 900px) {
		.settings-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
