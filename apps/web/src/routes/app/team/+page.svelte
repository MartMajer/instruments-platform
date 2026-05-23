<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { browser } from '$app/environment';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { onDestroy, onMount } from 'svelte';
	import { Check, LoaderCircle, Plus } from 'lucide-svelte';
	import EmptyState from '$lib/components/EmptyState.svelte';
	import ErrorPanel from '$lib/components/ErrorPanel.svelte';
	import LoadingBoundary from '$lib/components/LoadingBoundary.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import type {
		TenantMemberResponse,
		TenantMemberRosterResponse,
		TenantRoleResponse
	} from '$lib/api/product';
	import type { AuthSessionResponse } from '$lib/api/setup';
	import { createProductApiFromEnv, createProductRequestGate } from '$lib/product/route-state';
	import {
		getProductAuthContext,
		hasProductPermission,
		setupManagePermission,
		teamManagePermission
	} from '$lib/product/auth-context';
	import { toProductApiErrorMessage } from '$lib/product/view-models';

	type LoadState = 'loading' | 'ready' | 'error';
	type RoleLoadState = 'idle' | LoadState;
	type CapabilitySummary = {
		label: string;
		detail: string;
		status: 'ready' | 'neutral';
	};
	type CapabilityCoverage = CapabilitySummary & {
		count: number;
	};

	const productApi = createProductApiFromEnv(env);
	const requestGate = createProductRequestGate();
	const authContext = getProductAuthContext();
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));

	let authSession = $state<AuthSessionResponse | null>(null);
	let loadState = $state<LoadState>('loading');
	let roleLoadState = $state<RoleLoadState>('idle');
	let roster = $state<TenantMemberRosterResponse | null>(null);
	let tenantRoles = $state<TenantRoleResponse[]>([]);
	let errorMessage = $state<string | null>(null);
	let roleErrorMessage = $state<string | null>(null);
	let newMemberEmail = $state('');
	let newMemberRoleCode = $state('');
	let newMemberLocale = $state('en');
	let creatingMember = $state(false);
	let createMemberError = $state<string | null>(null);
	let createMemberNotice = $state<string | null>(null);
	let changingRoleUserId = $state<string | null>(null);
	let changeRoleError = $state<string | null>(null);
	let copiedMemberUserId = $state<string | null>(null);
	let roleSelections = $state<Record<string, string>>({});

	const unsubscribeAuth = authContext.session.subscribe((value) => {
		authSession = value;
	});
	onDestroy(unsubscribeAuth);

	const canManageTeam = $derived(hasProductPermission(authSession, teamManagePermission));

	onMount(() => {
		void loadTenantMembers();
	});

	$effect(() => {
		if (canManageTeam && roleLoadState === 'idle') {
			void loadTenantRoles();
		}
	});

	async function loadTenantMembers() {
		const requestId = requestGate.next();

		loadState = 'loading';
		errorMessage = null;

		try {
			const nextRoster = await productApi.listTenantMembers();
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			roster = nextRoster;
			syncRoleSelections(nextRoster);
			loadState = 'ready';
		} catch (error) {
			if (!requestGate.isCurrent(requestId)) {
				return;
			}

			roster = null;
			errorMessage = toProductApiErrorMessage(error, 'Team members could not be loaded.');
			loadState = 'error';
		}
	}

	async function loadTenantRoles() {
		roleLoadState = 'loading';
		roleErrorMessage = null;

		try {
			const nextRoles = await productApi.listTenantRoles();
			tenantRoles = nextRoles.roles;
			roleLoadState = 'ready';

			if (!tenantRoles.some((role) => role.code === newMemberRoleCode)) {
				newMemberRoleCode = tenantRoles[0]?.code ?? '';
			}

			syncRoleSelections(roster);
		} catch (error) {
			tenantRoles = [];
			roleErrorMessage = toProductApiErrorMessage(error, 'Tenant roles could not be loaded.');
			roleLoadState = 'error';
		}
	}

	async function createTenantMember() {
		if (!canManageTeam) {
			return;
		}

		const email = newMemberEmail.trim();
		const roleCode = newMemberRoleCode.trim();
		const locale = newMemberLocale.trim() || 'en';

		if (!email) {
			createMemberError = 'Enter a member email.';
			return;
		}

		if (!roleCode) {
			createMemberError = 'Select a member role.';
			return;
		}

		creatingMember = true;
		createMemberError = null;
		createMemberNotice = null;

		try {
			const response = await productApi.createTenantMember({ email, roleCode, locale });
			newMemberEmail = '';
			newMemberLocale = locale;
			createMemberNotice = `Member access prepared for ${response.member.email}. Share the first sign-in link from the roster.`;
			await loadTenantMembers();
		} catch (error) {
			createMemberError = toProductApiErrorMessage(error, 'Member access could not be prepared.');
		} finally {
			creatingMember = false;
		}
	}

	async function changeTenantMemberRole(member: TenantMemberResponse) {
		if (!canManageTeam || isCurrentUser(member.userId)) {
			return;
		}

		const roleCode = selectedRoleCode(member).trim();
		if (!roleCode || roleCode === currentTenantRole(member)?.code) {
			return;
		}

		changingRoleUserId = member.userId;
		changeRoleError = null;

		try {
			await productApi.changeTenantMemberRole(member.userId, { roleCode });
			await loadTenantMembers();
		} catch (error) {
			changeRoleError = toProductApiErrorMessage(error, 'Tenant member role could not be changed.');
		} finally {
			changingRoleUserId = null;
		}
	}

	function updateRoleSelection(member: TenantMemberResponse, roleCode: string) {
		roleSelections = {
			...roleSelections,
			[member.userId]: roleCode
		};
	}

	function syncRoleSelections(nextRoster: TenantMemberRosterResponse | null) {
		if (!nextRoster) {
			roleSelections = {};
			return;
		}

		const nextSelections: Record<string, string> = {};
		for (const member of nextRoster.members) {
			nextSelections[member.userId] = currentTenantRole(member)?.code ?? tenantRoles[0]?.code ?? '';
		}
		roleSelections = nextSelections;
	}

	function currentTenantRole(member: TenantMemberResponse) {
		return member.roles.find((role) => role.scopeType === 'tenant') ?? member.roles[0] ?? null;
	}

	function selectedRoleCode(member: TenantMemberResponse) {
		return roleSelections[member.userId] ?? currentTenantRole(member)?.code ?? '';
	}

	function canSubmitRoleChange(member: TenantMemberResponse) {
		const selectedRole = selectedRoleCode(member);
		return (
			canManageTeam &&
			!isCurrentUser(member.userId) &&
			selectedRole.length > 0 &&
			selectedRole !== currentTenantRole(member)?.code &&
			changingRoleUserId !== member.userId
		);
	}

	function toMemberCapabilities(permissions: string[]): CapabilitySummary[] {
		const capabilities: CapabilitySummary[] = [];
		const seenLabels = new Set<string>();
		const hasReportsOrExports = permissions.some(
			(permission) => permission.startsWith('report.') || permission.startsWith('export.')
		);
		const hasUnknownPlatformAccess = permissions.some(
			(permission) =>
				permission !== setupManagePermission &&
				permission !== teamManagePermission &&
				!permission.startsWith('report.') &&
				!permission.startsWith('export.')
		);

		function addCapability(capability: CapabilitySummary) {
			if (seenLabels.has(capability.label)) {
				return;
			}

			capabilities.push(capability);
			seenLabels.add(capability.label);
		}

		if (permissions.includes(setupManagePermission)) {
			addCapability({
				label: 'Study setup and launch',
				detail: 'Can create, prepare, duplicate, launch, and manage study workflows.',
				status: 'ready'
			});
		}

		if (permissions.includes(teamManagePermission)) {
			addCapability({
				label: 'Team access management',
				detail: 'Can prepare tenant members and change tenant roles.',
				status: 'ready'
			});
		}

		if (hasReportsOrExports) {
			addCapability({
				label: 'Reports and exports',
				detail: 'Can inspect allowed result and export surfaces for the tenant.',
				status: 'ready'
			});
		}

		if (hasUnknownPlatformAccess) {
			addCapability({
				label: 'Additional platform access',
				detail: 'Has extra platform access not yet summarized by this screen.',
				status: 'neutral'
			});
		}

		if (capabilities.length === 0) {
			addCapability({
				label: 'Read-only tenant access',
				detail: 'Can inspect tenant surfaces allowed by membership.',
				status: 'neutral'
			});
		}

		return capabilities;
	}

	function toTeamOverviewRows(nextRoster: TenantMemberRosterResponse | null) {
		const members = nextRoster?.members ?? [];
		const activeIdentities = members.filter((member) => member.identityStatus === 'active').length;
		const pendingIdentities = members.filter(
			(member) => member.identityStatus !== 'active'
		).length;
		const setupManagers = members.filter((member) =>
			member.permissions.includes(setupManagePermission)
		).length;
		const teamManagers = members.filter((member) =>
			member.permissions.includes(teamManagePermission)
		).length;

		return [
			{ label: 'Members', value: members.length.toString() },
			{ label: 'Active identities', value: activeIdentities.toString() },
			{ label: 'Pending links', value: pendingIdentities.toString() },
			{ label: 'Study setup and launch', value: setupManagers.toString() },
			{ label: 'Team access management', value: teamManagers.toString() }
		];
	}

	function toTeamCapabilityRows(nextRoster: TenantMemberRosterResponse | null): CapabilityCoverage[] {
		const members = nextRoster?.members ?? [];
		const capabilities: CapabilitySummary[] = [
			{
				label: 'Study setup and launch',
				detail: 'Can create, prepare, duplicate, launch, and manage study workflows.',
				status: 'ready'
			},
			{
				label: 'Team access management',
				detail: 'Can prepare tenant members and change tenant roles.',
				status: 'ready'
			},
			{
				label: 'Reports and exports',
				detail: 'Can inspect allowed result and export surfaces for the tenant.',
				status: 'ready'
			}
		];

		return capabilities
			.map((capability) => ({
				...capability,
				count: members.filter((member) =>
					toMemberCapabilities(member.permissions).some(
						(memberCapability) => memberCapability.label === capability.label
					)
				).length
			}))
			.filter((row) => row.count > 0);
	}

	function formatDate(value: string | null) {
		if (!value) {
			return 'Not recorded';
		}

		return new Intl.DateTimeFormat('hr-HR', {
			dateStyle: 'short',
			timeStyle: 'short',
			hourCycle: 'h23'
		}).format(new Date(value));
	}

	function identityStatusLabel(member: TenantMemberResponse) {
		if (member.identityStatus === 'active') {
			return 'Active';
		}

	return 'Invite pending';
	}

	function identityStatusBadge(member: TenantMemberResponse) {
		return member.identityStatus === 'active' ? 'ready' : 'pending';
	}

	function memberSignInUrl(member: TenantMemberResponse) {
		const tenantId = roster?.tenantId ?? authSession?.tenantId ?? '';
		const loginUrl =
			`/auth/login?tenantId=${encodeURIComponent(tenantId)}` +
			`&returnUrl=${encodeURIComponent(absoluteWebUrl(resolve('/app')))}` +
			`&prompt=login&login_hint=${encodeURIComponent(member.email)}`;

		return resolveAuthRedirectUrl(loginUrl);
	}

	async function copyMemberSignInUrl(member: TenantMemberResponse) {
		createMemberError = null;
		try {
			await navigator.clipboard.writeText(memberSignInUrl(member));
			copiedMemberUserId = member.userId;
		} catch {
			createMemberError = 'Could not copy the sign-in link. Open the link and copy it from the address bar.';
		}
	}

	function absoluteWebUrl(path: string) {
		if (!browser) {
			return path;
		}

		return new URL(path, window.location.origin).toString();
	}

	function resolveAuthRedirectUrl(loginUrl: string) {
		if (/^https?:\/\//i.test(loginUrl)) {
			return loginUrl;
		}

		const authOrigin = absoluteOrigin(env.PUBLIC_AUTH_LOGIN_URL);
		if (authOrigin) {
			return new URL(loginUrl, authOrigin).toString();
		}

		const apiOrigin = absoluteOrigin(env.PUBLIC_API_BASE_URL);
		if (apiOrigin) {
			return new URL(loginUrl, apiOrigin).toString();
		}

		return loginUrl;
	}

	function absoluteOrigin(value: string | undefined) {
		if (!value || !/^https?:\/\//i.test(value)) {
			return null;
		}

		try {
			return new URL(value).origin;
		} catch {
			return null;
		}
	}

	function isCurrentUser(userId: string) {
		return authSession?.userId === userId;
	}
</script>

<SurfaceHeader
	eyebrow={text.team.eyebrow}
	title={text.team.title}
	description={text.team.description}
/>

{#if loadState === 'loading' || roster}
	<section class="product-panel" data-priority="primary" aria-label="Team access overview">
		<LoadingBoundary loading={loadState === 'loading'} label={text.team.loadingOverview}>
			{#if roster}
				<div class="product-panel__header">
					<div>
						<p class="product-kicker">{text.team.tenantTeam}</p>
						<h2 class="product-title">{text.team.overviewTitle}</h2>
						<p class="mt-1 text-sm text-[var(--color-text-muted)]">
							{text.team.overviewBody}
						</p>
					</div>
				</div>

				<dl class="team-stat-list" role="group" aria-label="Team access overview counts">
					{#each toTeamOverviewRows(roster) as row}
						<div class="team-stat-item">
							<dt class="team-stat-item__label">{row.label}</dt>
							<dd class="team-stat-item__value">{row.value}</dd>
						</div>
					{/each}
				</dl>

				{#if toTeamCapabilityRows(roster).length > 0}
					<div class="team-capability-list" aria-label="Team capability coverage">
						{#each toTeamCapabilityRows(roster) as row}
							<article class="team-capability-row" aria-label={row.label}>
								<div>
									<h3 class="team-capability-row__title">{row.label}</h3>
									<p class="team-capability-row__detail">{row.detail}</p>
								</div>
								<div class="team-capability-row__count">
									<span>{row.count}</span>
									<div>
										{row.count === 1 ? 'member' : 'members'}
									</div>
								</div>
							</article>
						{/each}
					</div>
				{/if}
			{/if}
		</LoadingBoundary>
	</section>
{/if}

{#if canManageTeam}
	<section class="product-panel" aria-label="Prepare tenant member">
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.team.tenantTeam}</p>
				<h2 class="product-title">{text.team.prepareTitle}</h2>
				<p class="mt-1 text-sm text-[var(--color-text-muted)]">
					{text.team.prepareBody}
				</p>
			</div>
		</div>

		{#if roleLoadState === 'error' && roleErrorMessage}
			<ErrorPanel
				title="Tenant roles unavailable"
				message={roleErrorMessage}
				retryLabel="Retry roles"
				onRetry={loadTenantRoles}
			/>
		{:else}
			<form
				class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(12rem,16rem)_minmax(7rem,9rem)_auto] lg:items-end"
				onsubmit={(event) => {
					event.preventDefault();
					void createTenantMember();
				}}
			>
				<label class="field">
					<span>Member email</span>
					<input
						type="email"
						bind:value={newMemberEmail}
						disabled={creatingMember}
						autocomplete="email"
					/>
				</label>

				<label class="field">
					<span>Member role</span>
					<select
						bind:value={newMemberRoleCode}
						disabled={creatingMember || roleLoadState !== 'ready' || tenantRoles.length === 0}
					>
						{#each tenantRoles as role (role.roleId)}
							<option value={role.code}>{role.name}</option>
						{/each}
					</select>
				</label>

				<label class="field">
					<span>Member locale</span>
					<input bind:value={newMemberLocale} disabled={creatingMember} />
				</label>

				<button
					type="submit"
					class="primary-button"
					disabled={creatingMember || roleLoadState !== 'ready' || tenantRoles.length === 0}
				>
					{#if creatingMember}
						<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
						<span>Adding...</span>
					{:else}
						<Plus size={17} aria-hidden="true" />
						<span>Add member</span>
					{/if}
				</button>
			</form>

			{#if roleLoadState === 'loading'}
				<p class="text-sm text-[var(--color-text-muted)]">Loading tenant roles.</p>
			{/if}

			{#if createMemberError}
				<p class="error-line" role="alert">{createMemberError}</p>
			{/if}

			{#if createMemberNotice}
				<p class="text-sm text-[var(--color-text-muted)]" role="status">
					{createMemberNotice} The roster marks the member pending until the first matching Auth0
					sign-in.
				</p>
			{/if}
		{/if}
	</section>
{:else}
	<section class="product-panel" aria-label="Read-only team access">
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.team.tenantTeam}</p>
				<h2 class="product-title">{text.team.readOnlyTitle}</h2>
			</div>
		</div>
		<p class="text-sm text-[var(--color-text-muted)]">
			{text.team.readOnlyBody}
		</p>
	</section>
{/if}

<section class="product-panel" aria-label="Team roster">
	<LoadingBoundary loading={loadState === 'loading'} label="Loading team members">
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title="Team members unavailable"
				message={errorMessage}
				retryLabel="Retry members"
				onRetry={loadTenantMembers}
			/>
		{:else if roster}
			<div class="product-panel__header">
				<div>
					<p class="product-kicker">Team roster</p>
					<h2 class="product-title">{text.team.rosterTitle}</h2>
				</div>
			</div>

			<dl class="team-stat-list" role="group" aria-label="Tenant member roster counts">
				<div class="team-stat-item">
					<dt class="team-stat-item__label">Members</dt>
					<dd class="team-stat-item__value">{roster.members.length}</dd>
				</div>
			</dl>

			{#if roster.members.length === 0}
				<EmptyState
					title="No tenant members"
					description="No active tenant role assignments are available for this tenant."
				/>
			{:else}
				<div class="record-list">
					{#each roster.members as member (member.userId)}
						<article class="record-row" aria-label={member.email}>
							<span class="record-row__header">
								<span class="record-row__title">{member.email}</span>
								<span class="flex flex-wrap gap-2">
									<span class="status-badge" data-status={identityStatusBadge(member)}>
										{identityStatusLabel(member)}
									</span>
									{#if isCurrentUser(member.userId)}
										<span class="status-badge" data-status="ready">Current user</span>
									{/if}
								</span>
							</span>

							<span class="record-grid">
								<span class="record-field">
									<span class="record-field__label">Locale</span>
									<span class="record-field__value">{member.locale}</span>
								</span>
								<span class="record-field">
									<span class="record-field__label">Created</span>
									<span class="record-field__value">{formatDate(member.createdAt)}</span>
								</span>
								<span class="record-field">
									<span class="record-field__label">Last login</span>
									<span class="record-field__value">{formatDate(member.lastLoginAt)}</span>
								</span>
							</span>

							<div class="grid gap-2">
								<div>
									<p class="record-field__label">Roles</p>
									<div
										class="mt-2 flex flex-wrap gap-2"
										role="group"
										aria-label={`Assigned roles for ${member.email}`}
									>
										{#each member.roles as role (role.roleId)}
											<span
												class="inline-flex items-center rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-1 text-xs font-semibold text-[var(--color-text)]"
											>
												{role.name}
											</span>
										{/each}
									</div>
								</div>
								<div>
									<p class="record-field__label">Capabilities</p>
									<div
										class="mt-2 flex flex-wrap gap-2"
										role="group"
										aria-label={`Capabilities for ${member.email}`}
									>
										{#each toMemberCapabilities(member.permissions) as capability}
											<span class="status-badge" data-status={capability.status}>
												{capability.label}
											</span>
										{/each}
									</div>
								</div>
							</div>

							{#if canManageTeam && member.identityStatus !== 'active'}
								<div class="grid gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center">
									<div>
										<p class="record-field__label">First sign-in</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											Send this link to {member.email}. They stay pending until Auth0 returns the
											same email for this workspace.
										</p>
									</div>
									<a class="secondary-button" href={memberSignInUrl(member)}>Open link</a>
									<button
										type="button"
										class="secondary-button"
										onclick={() => void copyMemberSignInUrl(member)}
									>
										{copiedMemberUserId === member.userId ? 'Copied' : 'Copy link'}
									</button>
								</div>
							{/if}

							{#if canManageTeam && !isCurrentUser(member.userId) && tenantRoles.length > 0}
								<form
									class="grid gap-3 md:grid-cols-[minmax(12rem,1fr)_auto] md:items-end"
									onsubmit={(event) => {
										event.preventDefault();
										void changeTenantMemberRole(member);
									}}
								>
									<label class="field">
										<span>Role for {member.email}</span>
										<select
											value={selectedRoleCode(member)}
											disabled={changingRoleUserId === member.userId}
											onchange={(event) => updateRoleSelection(member, event.currentTarget.value)}
										>
											{#each tenantRoles as role (role.roleId)}
												<option value={role.code}>{role.name}</option>
											{/each}
										</select>
									</label>
									<button
										type="submit"
										class="secondary-button"
										aria-label={`Change role for ${member.email}`}
										disabled={!canSubmitRoleChange(member)}
									>
										{#if changingRoleUserId === member.userId}
											<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
											<span>Saving...</span>
										{:else}
											<Check size={16} aria-hidden="true" />
											<span>Change role</span>
										{/if}
									</button>
								</form>
							{/if}
						</article>
					{/each}
				</div>

				{#if changeRoleError}
					<p class="error-line" role="alert">{changeRoleError}</p>
				{/if}
			{/if}
		{/if}
	</LoadingBoundary>
</section>
