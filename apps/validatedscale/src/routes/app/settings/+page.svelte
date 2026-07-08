<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import {
		createProductApi,
		type TenantMemberRosterResponse,
		type TenantRoleListResponse,
		type TenantSettingsWorkspaceResponse
	} from '$lib/api/product';
	import { ApiError } from '$lib/api/client';
	import { api } from '$lib/core/client';
	import { DEFAULT_BRANDING_TOKENS, resolveTheme, themeStyle } from '$lib/core/branding';
	import { isHexColor } from '$lib/core/contrast';
	import { t } from '$lib/core/locale.svelte';
	import { formatCount, formatDate, humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());

	let settings = $state<TenantSettingsWorkspaceResponse | null>(null);
	let roster = $state<TenantMemberRosterResponse | null>(null);
	let roles = $state<TenantRoleListResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');

	let brandLabel = $state('');
	let brandTitle = $state('');
	let brandAccent = $state('#4530a6');
	let brandLayout = $state('');
	let brandBusy = $state(false);
	let brandNote = $state<string | null>(null);

	// Respondent + app-shell branding — the full surface palette.
	let appAccent = $state<string>(DEFAULT_BRANDING_TOKENS.accent);
	let appTopbar = $state<string>(DEFAULT_BRANDING_TOKENS.topbar);
	let appBackground = $state<string>(DEFAULT_BRANDING_TOKENS.background);
	let appSurface = $state<string>(DEFAULT_BRANDING_TOKENS.surface);
	let appInk = $state<string>(DEFAULT_BRANDING_TOKENS.ink);
	let appOrgLabel = $state('');
	let appLogoObjectKey = $state<string | null>(null);
	let appLogoContentType = $state<string | null>(null);
	let appAllowedTypes = $state<string[]>(['image/png', 'image/jpeg', 'image/webp']);
	let appMaxBytes = $state(262144);
	let appMaxDim = $state(1024);
	let appLogoPreview = $state<string | null>(null);
	let appLogoFile = $state<File | null>(null);
	let appLogoDirty = $state(false);
	let appLogoRemoved = $state(false);
	let appBusy = $state(false);
	let appNote = $state<string | null>(null);
	let appError = $state<string | null>(null);

	// Live preview: the exact palette respondents and the shell would receive,
	// resolved (contrast-guarded) with the same rules as the backend.
	const appPreviewTheme = $derived(
		resolveTheme({
			accent: appAccent,
			topbar: appTopbar,
			background: appBackground,
			surface: appSurface,
			ink: appInk
		})
	);
	const appPreviewStyle = $derived(themeStyle(appPreviewTheme));
	const appAccentAdjusted = $derived(
		isHexColor(appAccent) && appPreviewTheme.accent.toLowerCase() !== appAccent.toLowerCase()
	);

	function resetAppColors() {
		appAccent = DEFAULT_BRANDING_TOKENS.accent;
		appTopbar = DEFAULT_BRANDING_TOKENS.topbar;
		appBackground = DEFAULT_BRANDING_TOKENS.background;
		appSurface = DEFAULT_BRANDING_TOKENS.surface;
		appInk = DEFAULT_BRANDING_TOKENS.ink;
	}

	function revokeLogoPreview() {
		if (appLogoPreview?.startsWith('blob:')) {
			URL.revokeObjectURL(appLogoPreview);
		}
	}

	onDestroy(revokeLogoPreview);

	let memberEmail = $state('');
	let memberRole = $state('');
	let memberBusy = $state(false);
	let memberNote = $state<string | null>(null);

	async function load() {
		try {
			const [settingsResponse, rosterResponse, rolesResponse] = await Promise.all([
				product.getTenantSettings(),
				product.listTenantMembers().catch(() => null),
				product.listTenantRoles().catch(() => null)
			]);
			settings = settingsResponse;
			roster = rosterResponse;
			roles = rolesResponse;
			brandLabel = settingsResponse.reportBranding.organizationLabel;
			brandTitle = settingsResponse.reportBranding.reportTitle;
			brandAccent = settingsResponse.reportBranding.accentColorHex;
			brandLayout = settingsResponse.reportBranding.layoutVariant;

			const app = settingsResponse.appBranding;
			appAccent = app.accentColorHex ?? app.defaults.accent;
			appTopbar = app.topbarColorHex ?? app.defaults.topbar;
			appBackground = app.backgroundColorHex ?? app.defaults.background;
			appSurface = app.surfaceColorHex ?? app.defaults.surface;
			appInk = app.inkColorHex ?? app.defaults.ink;
			appOrgLabel = app.orgLabel;
			appLogoObjectKey = app.logoObjectKey;
			appLogoContentType = app.logoContentType;
			appAllowedTypes = app.allowedLogoContentTypes;
			appMaxBytes = app.maxLogoBytes;
			appMaxDim = app.maxLogoDimension;
			appLogoFile = null;
			appLogoRemoved = false;
			appLogoDirty = false;
			revokeLogoPreview();
			appLogoPreview = null;
			if (app.hasLogo) {
				product
					.getTenantAppBrandingLogoBlob()
					.then((blob) => {
						appLogoPreview = URL.createObjectURL(blob.body);
					})
					.catch(() => {});
			}

			memberRole ||= rolesResponse?.roles[0]?.code ?? '';
			loadState = 'ready';
		} catch {
			loadState = 'error';
		}
	}

	onMount(load);

	async function saveBranding(event: SubmitEvent) {
		event.preventDefault();
		if (brandBusy) return;
		brandBusy = true;
		brandNote = null;

		try {
			await product.updateTenantReportBranding({
				organizationLabel: brandLabel.trim(),
				reportTitle: brandTitle.trim(),
				accentColorHex: brandAccent.trim(),
				layoutVariant: brandLayout
			});
			brandNote = 'Saved. New report PDFs use this branding.';
		} catch {
			brandNote = 'Branding could not be saved. Check the values.';
		} finally {
			brandBusy = false;
		}
	}

	function onAppLogoSelected(event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const file = input.files?.[0] ?? null;
		appError = null;
		if (!file) return;

		if (!appAllowedTypes.includes(file.type)) {
			appError = t('Logo must be a PNG, JPEG, or WebP image.');
			input.value = '';
			return;
		}
		if (file.size > appMaxBytes) {
			appError = `${t('Logo is too large — the limit is')} ${Math.round(appMaxBytes / 1024)} KB.`;
			input.value = '';
			return;
		}

		revokeLogoPreview();
		appLogoFile = file;
		appLogoRemoved = false;
		appLogoDirty = true;
		appLogoPreview = URL.createObjectURL(file);
	}

	function removeAppLogo() {
		revokeLogoPreview();
		appLogoFile = null;
		appLogoRemoved = true;
		appLogoDirty = true;
		appLogoPreview = null;
	}

	function appBrandingErrorMessage(error: unknown): string {
		if (error instanceof ApiError && error.body && typeof error.body === 'object') {
			const detail = (error.body as { detail?: unknown }).detail;
			if (typeof detail === 'string' && detail.length > 0) {
				return detail;
			}
		}
		return t('Branding could not be saved. Check the values.');
	}

	async function saveAppBranding(event: SubmitEvent) {
		event.preventDefault();
		if (appBusy) return;
		if (!isHexColor(appAccent)) {
			appError = t('Pick an accent color.');
			return;
		}

		appBusy = true;
		appNote = null;
		appError = null;

		try {
			let logoObjectKey = appLogoObjectKey;
			let logoContentType = appLogoContentType;

			if (appLogoDirty) {
				if (appLogoRemoved) {
					logoObjectKey = null;
					logoContentType = null;
				} else if (appLogoFile) {
					const uploaded = await product.uploadTenantAppBrandingLogo(appLogoFile);
					logoObjectKey = uploaded.logoObjectKey;
					logoContentType = uploaded.logoContentType;
				}
			}

			const saved = await product.updateTenantAppBranding({
				accentColorHex: appAccent,
				logoObjectKey,
				logoContentType,
				topbarColorHex: appTopbar,
				backgroundColorHex: appBackground,
				surfaceColorHex: appSurface,
				inkColorHex: appInk
			});

			appLogoObjectKey = saved.logoObjectKey;
			appLogoContentType = saved.logoContentType;
			appLogoFile = null;
			appLogoRemoved = false;
			appLogoDirty = false;
			appNote = t('Saved. Your app and respondents now use this branding.');
		} catch (error) {
			appError = appBrandingErrorMessage(error);
		} finally {
			appBusy = false;
		}
	}

	async function addMember(event: SubmitEvent) {
		event.preventDefault();
		if (memberBusy) return;
		memberBusy = true;
		memberNote = null;

		try {
			await product.createTenantMember({ email: memberEmail.trim(), roleCode: memberRole });
			memberNote = `${memberEmail.trim()} added. They link their identity on first sign-in.`;
			memberEmail = '';
			await load();
		} catch {
			memberNote = 'The member could not be added. Check the email and role.';
		} finally {
			memberBusy = false;
		}
	}

	async function changeRole(userId: string, roleCode: string) {
		try {
			await product.changeTenantMemberRole(userId, { roleCode });
			await load();
		} catch {
			memberNote = 'The role change was not accepted.';
		}
	}
</script>

<svelte:head><title>Settings — ValidatedScale</title></svelte:head>

<header class="head">
	<p class="eyebrow">{t('Workspace')}</p>
	<h1 class="doc-title">{t('Settings')}</h1>
</header>

<LoadState state={loadState}>
	{#if settings}
		<div class="grid">
			<section class="panel block">
				<h2 class="eyebrow">{t('Profile')}</h2>
				<dl>
					<div><dt>{t('Organization')}</dt><dd>{settings.profile.name}</dd></div>
					<div><dt>{t('Workspace')}</dt><dd class="datum">{settings.profile.slug}</dd></div>
					<div><dt>{t('Region')}</dt><dd>{settings.profile.region}</dd></div>
					<div><dt>{t('Default locale')}</dt><dd class="datum">{settings.profile.defaultLocale}</dd></div>
					<div><dt>{t('Status')}</dt><dd>{t(humanizeToken(settings.profile.status))}</dd></div>
					<div><dt>{t('Created')}</dt><dd>{formatDate(settings.profile.createdAt)}</dd></div>
				</dl>
			</section>

			<section class="panel block">
				<h2 class="eyebrow">{t('Report branding')}</h2>
				<form class="brand-form" onsubmit={saveBranding}>
					<label class="eyebrow" for="b-label">{t('Organization label')}</label>
					<input id="b-label" bind:value={brandLabel} />
					<label class="eyebrow" for="b-title">{t('Report title')}</label>
					<input id="b-title" bind:value={brandTitle} />
					<label class="eyebrow" for="b-accent">{t('Accent')}</label>
					<div class="accent-row">
						<input id="b-accent" type="color" bind:value={brandAccent} aria-label="Accent color" />
						<span class="datum">{brandAccent}</span>
					</div>
					{#if brandNote}<p class="note" role="status">{brandNote}</p>{/if}
					<button class="btn btn-ink" type="submit" disabled={brandBusy}>
						{brandBusy ? t('Saving…') : t('Save branding')}
					</button>
				</form>
			</section>

			<section class="panel block app-branding">
				<h2 class="eyebrow">{t('App & respondent branding')}</h2>
				<p class="brand-hint">{t('Your logo and colors theme the researcher app and the survey your respondents answer.')}</p>
				<form class="brand-form" onsubmit={saveAppBranding}>
					<label class="eyebrow" for="a-logo">{t('Logo')}</label>
					<input
						id="a-logo"
						type="file"
						accept="image/png,image/jpeg,image/webp"
						onchange={onAppLogoSelected}
					/>
					<p class="logo-hint">
						{t('PNG, JPEG or WebP')} · ≤ {Math.round(appMaxBytes / 1024)} KB · ≤ {appMaxDim}px
					</p>
					{#if appLogoPreview}
						<button type="button" class="btn btn-ghost tiny" onclick={removeAppLogo}>
							{t('Remove logo')}
						</button>
					{/if}

					<div class="swatches">
						<label class="swatch">
							<span class="eyebrow">{t('Accent')}</span>
							<span class="swatch-row">
								<input type="color" bind:value={appAccent} aria-label="Accent color" />
								<span class="datum">{appAccent}</span>
							</span>
							{#if appAccentAdjusted}
								<span class="adjust-note" title={t('Adjusted for legibility (WCAG AA)')}>→ {appPreviewTheme.accent}</span>
							{/if}
						</label>
						<label class="swatch">
							<span class="eyebrow">{t('Topbar')}</span>
							<span class="swatch-row">
								<input type="color" bind:value={appTopbar} aria-label="Topbar color" />
								<span class="datum">{appTopbar}</span>
							</span>
						</label>
						<label class="swatch">
							<span class="eyebrow">{t('Background')}</span>
							<span class="swatch-row">
								<input type="color" bind:value={appBackground} aria-label="Background color" />
								<span class="datum">{appBackground}</span>
							</span>
						</label>
						<label class="swatch">
							<span class="eyebrow">{t('Surface')}</span>
							<span class="swatch-row">
								<input type="color" bind:value={appSurface} aria-label="Surface color" />
								<span class="datum">{appSurface}</span>
							</span>
						</label>
						<label class="swatch">
							<span class="eyebrow">{t('Text')}</span>
							<span class="swatch-row">
								<input type="color" bind:value={appInk} aria-label="Text color" />
								<span class="datum">{appInk}</span>
							</span>
						</label>
					</div>
					<button type="button" class="btn btn-ghost tiny" onclick={resetAppColors}>
						{t('Reset colors')}
					</button>

					<div class="preview" style={appPreviewStyle}>
						<span class="preview-label eyebrow">{t('Live preview')}</span>
						<div class="preview-shell">
							<div class="preview-topbar">
								{#if appLogoPreview}
									<img class="preview-logo" src={appLogoPreview} alt={appOrgLabel} />
								{:else}
									<span class="preview-org">{appOrgLabel || 'ValidatedScale'}</span>
								{/if}
								<span class="preview-nav active">{t('Today')}</span>
								<span class="preview-nav">{t('Studies')}</span>
							</div>
							<div class="preview-body">
								<div class="preview-card">
									<p class="preview-kicker eyebrow">{t('You are invited to take part in')}</p>
									<p class="preview-title">{appOrgLabel || t('Your study')}</p>
									<button class="btn btn-stain preview-begin" type="button" tabindex="-1">{t('Begin')}</button>
								</div>
							</div>
						</div>
					</div>

					{#if appError}<p class="error-note" role="alert">{appError}</p>{/if}
					{#if appNote}<p class="note" role="status">{appNote}</p>{/if}
					<button class="btn btn-ink" type="submit" disabled={appBusy}>
						{appBusy ? t('Saving…') : t('Save app branding')}
					</button>
				</form>
			</section>

			<section class="panel block team">
				<h2 class="eyebrow">{t('Team')}</h2>
				{#if roster}
					<ul class="members">
						{#each roster.members as member (member.userId)}
							<li>
								<div class="member-main">
									<span class="member-email">{member.email}</span>
									<span class="datum member-meta">
										{member.identityStatus === 'pending_provider_link' ? 'invited — pending first sign-in' : `last sign-in ${formatDate(member.lastLoginAt)}`}
									</span>
								</div>
								{#if roles}
									<select
										aria-label={`Role for ${member.email}`}
										value={member.roles[0]?.code ?? ''}
										onchange={(event) => changeRole(member.userId, event.currentTarget.value)}
									>
										{#each roles.roles as role (role.roleId)}
											<option value={role.code}>{role.name}</option>
										{/each}
									</select>
								{/if}
							</li>
						{/each}
					</ul>
				{/if}
				<form class="add-member" onsubmit={addMember}>
					<input type="email" required bind:value={memberEmail} placeholder="colleague@institution.org" aria-label={t('New member email')} />
					{#if roles}
						<select bind:value={memberRole} aria-label="New member role">
							{#each roles.roles as role (role.roleId)}
								<option value={role.code}>{role.name}</option>
							{/each}
						</select>
					{/if}
					<button class="btn btn-ghost" type="submit" disabled={memberBusy}>
						{memberBusy ? t('Adding…') : t('Add member')}
					</button>
				</form>
				{#if memberNote}<p class="note" role="status">{memberNote}</p>{/if}
			</section>

			<section class="panel block">
				<h2 class="eyebrow">{t('Counts')}</h2>
				<dl>
					<div><dt>{t('Studies')}</dt><dd class="datum">{formatCount(settings.counts.campaignSeriesCount)}</dd></div>
					<div><dt>{t('Waves')}</dt><dd class="datum">{formatCount(settings.counts.campaignCount)}</dd></div>
					<div><dt>{t('Responses')}</dt><dd class="datum">{formatCount(settings.counts.submittedResponseCount)}</dd></div>
					<div><dt>{t('People')}</dt><dd class="datum">{formatCount(settings.counts.subjectCount)}</dd></div>
					<div><dt>{t('Members')}</dt><dd class="datum">{formatCount(settings.counts.tenantMemberCount)}</dd></div>
					<div><dt>{t('Exports')}</dt><dd class="datum">{formatCount(settings.counts.exportArtifactCount)}</dd></div>
				</dl>
			</section>
		</div>
	{/if}
</LoadState>

<style>
	.head {
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
		gap: 1.25rem;
		align-items: start;
	}

	.block {
		padding: 1.25rem;
	}

	dl {
		margin-top: 0.75rem;
	}

	dl div {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		padding: 0.5rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.875rem;
	}

	dl div:last-child {
		border-bottom: none;
	}

	dt {
		color: var(--color-ink-3);
	}

	dd {
		text-align: right;
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
	}

	.brand-form {
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.brand-form input:not([type='color']),
	.add-member input,
	.team select {
		font: inherit;
		font-size: 0.875rem;
		padding: 0.5rem 0.625rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: var(--color-surface);
	}

	.accent-row {
		display: flex;
		align-items: center;
		gap: 0.625rem;
	}

	.accent-row input {
		width: 3rem;
		height: 2rem;
		padding: 0;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: none;
	}

	.brand-form .btn {
		align-self: flex-start;
		margin-top: 0.375rem;
	}

	.brand-hint,
	.logo-hint {
		font-size: 0.75rem;
		color: var(--color-ink-3);
		margin-top: 0.25rem;
	}

	.brand-hint {
		margin-top: 0.5rem;
	}

	.adjust-note {
		font-family: var(--font-mono);
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.btn.tiny {
		align-self: flex-start;
		font-size: 0.75rem;
		padding: 0.25rem 0.625rem;
		margin-top: 0;
	}

	.error-note {
		font-size: 0.8125rem;
		color: var(--color-danger);
	}

	/* Colour swatches: one picker per surface. */
	.swatches {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(7rem, 1fr));
		gap: 0.5rem 0.875rem;
		margin-top: 0.25rem;
	}

	.swatch {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.swatch-row {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
	}

	.swatch-row input[type='color'] {
		width: 2.25rem;
		height: 1.75rem;
		padding: 0;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		background: none;
	}

	.swatch .datum {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	/* Live preview — a miniature of the app shell + the survey card, themed with
	   the resolved (guarded) palette applied to the .preview container. */
	.preview {
		margin-top: 0.75rem;
	}

	.preview-label {
		display: block;
		margin-bottom: 0.375rem;
	}

	.preview-shell {
		border: 1px solid var(--color-line);
		border-radius: var(--radius-instrument);
		overflow: hidden;
	}

	.preview-topbar {
		display: flex;
		align-items: center;
		gap: 1rem;
		height: 2.5rem;
		padding: 0 0.875rem;
		background: var(--color-topbar);
		color: var(--color-topbar-ink);
	}

	.preview-nav {
		font-size: 0.75rem;
		font-weight: 520;
		color: color-mix(in oklab, var(--color-topbar-ink) 70%, transparent);
		border-bottom: 2px solid transparent;
		height: 100%;
		display: inline-flex;
		align-items: center;
	}

	.preview-nav.active {
		color: var(--color-topbar-ink);
		border-bottom-color: var(--color-stain-bright);
	}

	.preview-body {
		background: var(--color-ground);
		padding: 1rem;
	}

	.preview-card {
		border: 1px solid var(--color-line);
		border-top: 3px solid var(--color-stain);
		border-radius: var(--radius-instrument);
		padding: 0.875rem 1rem 1rem;
		background: var(--color-surface);
	}

	.preview-logo {
		max-height: 1.5rem;
		width: auto;
		max-width: 8rem;
		object-fit: contain;
	}

	.preview-org {
		font-weight: 620;
		font-size: 0.8125rem;
		color: var(--color-topbar-ink);
		margin-right: auto;
	}

	.preview-topbar .preview-logo {
		margin-right: auto;
	}

	.preview-kicker {
		color: var(--color-stain);
	}

	.preview-title {
		font-family: var(--font-doc);
		font-size: 1.0625rem;
		margin: 0.125rem 0 0.75rem;
		color: var(--color-ink);
	}

	.preview-begin {
		font-size: 0.8125rem;
		padding: 0.4375rem 1.125rem;
	}

	.note {
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	.members {
		list-style: none;
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
	}

	.members li {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
		padding: 0.5625rem 0;
		border-bottom: 1px dashed var(--color-line);
	}

	.member-main {
		display: flex;
		flex-direction: column;
		gap: 0.125rem;
		min-width: 0;
	}

	.member-email {
		font-size: 0.875rem;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.member-meta {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.add-member {
		display: flex;
		gap: 0.5rem;
		margin-top: 0.875rem;
		flex-wrap: wrap;
	}

	.add-member input {
		flex: 1;
		min-width: 11rem;
	}
</style>
