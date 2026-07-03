<script lang="ts">
	import { onMount } from 'svelte';
	import {
		createProductApi,
		type TenantMemberRosterResponse,
		type TenantRoleListResponse,
		type TenantSettingsWorkspaceResponse
	} from '$lib/api/product';
	import { api } from '$lib/core/client';
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

<svelte:head><title>Settings — Spectra</title></svelte:head>

<header class="head">
	<p class="eyebrow">Workspace</p>
	<h1 class="doc-title">Settings</h1>
</header>

<LoadState state={loadState}>
	{#if settings}
		<div class="grid">
			<section class="panel block">
				<h2 class="eyebrow">Profile</h2>
				<dl>
					<div><dt>Organization</dt><dd>{settings.profile.name}</dd></div>
					<div><dt>Workspace</dt><dd class="datum">{settings.profile.slug}</dd></div>
					<div><dt>Region</dt><dd>{settings.profile.region}</dd></div>
					<div><dt>Default locale</dt><dd class="datum">{settings.profile.defaultLocale}</dd></div>
					<div><dt>Status</dt><dd>{humanizeToken(settings.profile.status)}</dd></div>
					<div><dt>Created</dt><dd>{formatDate(settings.profile.createdAt)}</dd></div>
				</dl>
			</section>

			<section class="panel block">
				<h2 class="eyebrow">Report branding</h2>
				<form class="brand-form" onsubmit={saveBranding}>
					<label class="eyebrow" for="b-label">Organization label</label>
					<input id="b-label" bind:value={brandLabel} />
					<label class="eyebrow" for="b-title">Report title</label>
					<input id="b-title" bind:value={brandTitle} />
					<label class="eyebrow" for="b-accent">Accent</label>
					<div class="accent-row">
						<input id="b-accent" type="color" bind:value={brandAccent} aria-label="Accent color" />
						<span class="datum">{brandAccent}</span>
					</div>
					{#if brandNote}<p class="note" role="status">{brandNote}</p>{/if}
					<button class="btn btn-ink" type="submit" disabled={brandBusy}>
						{brandBusy ? 'Saving…' : 'Save branding'}
					</button>
				</form>
			</section>

			<section class="panel block team">
				<h2 class="eyebrow">Team</h2>
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
					<input type="email" required bind:value={memberEmail} placeholder="colleague@institution.org" aria-label="New member email" />
					{#if roles}
						<select bind:value={memberRole} aria-label="New member role">
							{#each roles.roles as role (role.roleId)}
								<option value={role.code}>{role.name}</option>
							{/each}
						</select>
					{/if}
					<button class="btn btn-ghost" type="submit" disabled={memberBusy}>
						{memberBusy ? 'Adding…' : 'Add member'}
					</button>
				</form>
				{#if memberNote}<p class="note" role="status">{memberNote}</p>{/if}
			</section>

			<section class="panel block">
				<h2 class="eyebrow">Counts</h2>
				<dl>
					<div><dt>Studies</dt><dd class="datum">{formatCount(settings.counts.campaignSeriesCount)}</dd></div>
					<div><dt>Waves</dt><dd class="datum">{formatCount(settings.counts.campaignCount)}</dd></div>
					<div><dt>Responses</dt><dd class="datum">{formatCount(settings.counts.submittedResponseCount)}</dd></div>
					<div><dt>People</dt><dd class="datum">{formatCount(settings.counts.subjectCount)}</dd></div>
					<div><dt>Members</dt><dd class="datum">{formatCount(settings.counts.tenantMemberCount)}</dd></div>
					<div><dt>Exports</dt><dd class="datum">{formatCount(settings.counts.exportArtifactCount)}</dd></div>
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
