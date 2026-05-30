<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { browser } from '$app/environment';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { onDestroy, onMount } from 'svelte';
	import {
		Check,
		Copy,
		ExternalLink,
		LoaderCircle,
		RotateCcw,
		ShieldOff,
		Trash2,
		UserPlus
	} from 'lucide-svelte';
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
	type AccessMutation = 'suspend' | 'reactivate' | 'remove';
	type PermissionSummary = {
		label: string;
		detail: string;
		status: 'ready' | 'neutral';
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
	let permissionDetailsOpen = $state<Record<string, boolean>>({});
	let accessMutationUserId = $state<string | null>(null);
	let accessMutation = $state<AccessMutation | null>(null);
	let accessMutationError = $state<string | null>(null);

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
			errorMessage = toProductApiErrorMessage(error, 'Workspace access could not be loaded.');
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
			roleErrorMessage = toProductApiErrorMessage(error, 'Workspace roles could not be loaded.');
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
			createMemberNotice = `Workspace access prepared for ${response.member.email}.`;
			await loadTenantMembers();
		} catch (error) {
			createMemberError = toProductApiErrorMessage(
				error,
				'Workspace access could not be prepared.'
			);
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
			changeRoleError = toProductApiErrorMessage(error, 'Workspace role could not be changed.');
		} finally {
			changingRoleUserId = null;
		}
	}

	async function suspendTenantMember(member: TenantMemberResponse) {
		await runAccessMutation(
			member,
			'suspend',
			() => productApi.suspendTenantMember(member.userId),
			'Workspace access could not be suspended.'
		);
	}

	async function reactivateTenantMember(member: TenantMemberResponse) {
		await runAccessMutation(
			member,
			'reactivate',
			() => productApi.reactivateTenantMember(member.userId),
			'Workspace access could not be reactivated.'
		);
	}

	async function removeTenantMember(member: TenantMemberResponse) {
		if (
			browser &&
			!window.confirm(
				`Remove workspace access for ${member.email}? They will no longer be able to sign in.`
			)
		) {
			return;
		}

		await runAccessMutation(
			member,
			'remove',
			() => productApi.removeTenantMember(member.userId),
			'Workspace access could not be removed.'
		);
	}

	async function runAccessMutation<T>(
		member: TenantMemberResponse,
		mutation: AccessMutation,
		action: () => Promise<T>,
		fallbackMessage: string
	) {
		if (!canManageTeam || isCurrentUser(member.userId) || isMutatingAccess(member)) {
			return;
		}

		accessMutationUserId = member.userId;
		accessMutation = mutation;
		accessMutationError = null;

		try {
			await action();
			await loadTenantMembers();
		} catch (error) {
			accessMutationError = toProductApiErrorMessage(error, fallbackMessage);
		} finally {
			accessMutationUserId = null;
			accessMutation = null;
		}
	}

	function updateRoleSelection(member: TenantMemberResponse, roleCode: string) {
		roleSelections = {
			...roleSelections,
			[member.userId]: roleCode
		};
	}

	function togglePermissionDetails(member: TenantMemberResponse) {
		permissionDetailsOpen = {
			...permissionDetailsOpen,
			[member.userId]: !permissionDetailsOpen[member.userId]
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

	function toPermissionSummaries(permissions: string[]): PermissionSummary[] {
		const summaries: PermissionSummary[] = [];
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

		function addSummary(summary: PermissionSummary) {
			if (seenLabels.has(summary.label)) {
				return;
			}

			summaries.push(summary);
			seenLabels.add(summary.label);
		}

		if (permissions.includes(setupManagePermission)) {
			addSummary({
				label: 'Study setup and launch',
				detail: 'Can create, prepare, duplicate, launch, and manage study workflows.',
				status: 'ready'
			});
		}

		if (permissions.includes(teamManagePermission)) {
			addSummary({
				label: 'Workspace access management',
				detail:
					'Can add operators, change roles, suspend access, reactivate access, and remove access.',
				status: 'ready'
			});
		}

		if (hasReportsOrExports) {
			addSummary({
				label: 'Reports and exports',
				detail: 'Can inspect allowed result and export surfaces for the workspace.',
				status: 'ready'
			});
		}

		if (hasUnknownPlatformAccess) {
			addSummary({
				label: 'Additional platform access',
				detail: 'Has platform permissions not yet summarized by this screen.',
				status: 'neutral'
			});
		}

		if (summaries.length === 0) {
			addSummary({
				label: 'Workspace member',
				detail: 'Can inspect workspace surfaces allowed by membership.',
				status: 'neutral'
			});
		}

		return summaries;
	}

	function toWorkspaceAccessRows(nextRoster: TenantMemberRosterResponse | null) {
		const members = nextRoster?.members ?? [];
		const summary = nextRoster?.summary;

		return [
			{ label: 'Operators', value: (summary?.totalCount ?? members.length).toString() },
			{
				label: 'Active',
				value: (
					summary?.activeCount ??
					members.filter((member) => memberStatus(member) === 'active').length
				).toString()
			},
			{
				label: 'Invited',
				value: (
					summary?.invitedCount ??
					members.filter((member) => memberStatus(member) === 'invited').length
				).toString()
			},
			{
				label: 'Suspended',
				value: (
					summary?.suspendedCount ??
					members.filter((member) => memberStatus(member) === 'suspended').length
				).toString()
			},
			{
				label: 'Access managers',
				value: (
					summary?.teamManagerCount ??
					members.filter((member) => member.permissions.includes(teamManagePermission)).length
				).toString()
			}
		];
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

	function memberStatus(member: TenantMemberResponse) {
		if (member.status) {
			return member.status;
		}

		return member.identityStatus === 'active' ? 'active' : 'invited';
	}

	function memberStatusLabel(member: TenantMemberResponse) {
		return member.statusLabel || (memberStatus(member) === 'active' ? 'Active' : 'Invited');
	}

	function memberStatusBadge(member: TenantMemberResponse) {
		const status = memberStatus(member);
		if (status === 'active') {
			return 'ready';
		}

		return status === 'suspended' ? 'blocked' : 'pending';
	}

	function isMutatingAccess(member: TenantMemberResponse, mutation?: AccessMutation) {
		return accessMutationUserId === member.userId && (!mutation || accessMutation === mutation);
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
			createMemberError =
				'Could not copy the sign-in link. Open the link and copy it from the address bar.';
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
	<section class="product-panel" data-priority="primary" aria-label="Workspace access summary">
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

				<dl class="team-stat-list" role="group" aria-label={text.team.teamOverviewCountsAria}>
					{#each toWorkspaceAccessRows(roster) as row}
						<div class="team-stat-item">
							<dt class="team-stat-item__label">{row.label}</dt>
							<dd class="team-stat-item__value">{row.value}</dd>
						</div>
					{/each}
				</dl>
			{/if}
		</LoadingBoundary>
	</section>
{/if}

{#if canManageTeam}
	<section class="product-panel" aria-label="Add workspace operator">
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
				title={text.team.tenantRolesUnavailable}
				message={roleErrorMessage}
				retryLabel={text.team.retryRoles}
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
					<span>{text.team.memberEmail}</span>
					<input
						type="email"
						bind:value={newMemberEmail}
						disabled={creatingMember}
						autocomplete="email"
					/>
				</label>

				<label class="field">
					<span>{text.team.memberRole}</span>
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
					<span>{text.team.memberLocale}</span>
					<input bind:value={newMemberLocale} disabled={creatingMember} />
				</label>

				<button
					type="submit"
					class="primary-button"
					disabled={creatingMember || roleLoadState !== 'ready' || tenantRoles.length === 0}
				>
					{#if creatingMember}
						<LoaderCircle size={17} aria-hidden="true" class="animate-spin" />
						<span>{text.team.adding}</span>
					{:else}
						<UserPlus size={17} aria-hidden="true" />
						<span>{text.team.addMember}</span>
					{/if}
				</button>
			</form>

			{#if roleLoadState === 'loading'}
				<p class="text-sm text-[var(--color-text-muted)]">{text.team.loadingRoles}</p>
			{/if}

			{#if createMemberError}
				<p class="error-line" role="alert">{createMemberError}</p>
			{/if}

			{#if createMemberNotice}
				<p class="text-sm text-[var(--color-text-muted)]" role="status">
					{createMemberNotice}
					{text.team.pendingNoticeSuffix}
				</p>
			{/if}
		{/if}
	</section>
{:else}
	<section class="product-panel" aria-label={text.team.readOnlyAria}>
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

<section class="product-panel" aria-label="Workspace operator roster">
	<LoadingBoundary loading={loadState === 'loading'} label={text.team.loadingMembers}>
		{#if loadState === 'error' && errorMessage}
			<ErrorPanel
				title={text.team.teamMembersUnavailable}
				message={errorMessage}
				retryLabel={text.team.retryMembers}
				onRetry={loadTenantMembers}
			/>
		{:else if roster}
			<div class="product-panel__header">
				<div>
					<p class="product-kicker">{text.team.teamRoster}</p>
					<h2 class="product-title">{text.team.rosterTitle}</h2>
				</div>
			</div>

			{#if roster.members.length === 0}
				<EmptyState title={text.team.noMembersTitle} description={text.team.noMembersBody} />
			{:else}
				<div class="record-list">
					{#each roster.members as member (member.userId)}
						<article class="record-row" aria-label={member.email}>
							<span class="record-row__header">
								<span>
									<span class="record-row__title">{member.email}</span>
									<span class="mt-1 block text-sm text-[var(--color-text-muted)]">
										{currentTenantRole(member)?.name ?? 'Workspace role unavailable'}
									</span>
								</span>
								<span class="flex flex-wrap gap-2">
									<span class="status-badge" data-status={memberStatusBadge(member)}>
										{memberStatusLabel(member)}
									</span>
									{#if isCurrentUser(member.userId)}
										<span class="status-badge" data-status="ready">{text.team.currentUser}</span>
									{/if}
								</span>
							</span>

							<span class="record-grid">
								<span class="record-field">
									<span class="record-field__label">{text.team.localeLabel}</span>
									<span class="record-field__value">{member.locale}</span>
								</span>
								<span class="record-field">
									<span class="record-field__label">{text.team.created}</span>
									<span class="record-field__value">{formatDate(member.createdAt)}</span>
								</span>
								<span class="record-field">
									<span class="record-field__label">{text.team.lastLogin}</span>
									<span class="record-field__value">{formatDate(member.lastLoginAt)}</span>
								</span>
							</span>

							<div
								class="rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3"
							>
								<button
									type="button"
									class="quiet-button"
									aria-expanded={Boolean(permissionDetailsOpen[member.userId])}
									onclick={() => togglePermissionDetails(member)}
								>
									Role permissions
								</button>
								{#if permissionDetailsOpen[member.userId]}
									<div class="mt-3 grid gap-2">
										{#each toPermissionSummaries(member.permissions) as permission}
											<div
												class="rounded border border-[var(--color-border)] bg-[var(--color-surface)] p-2"
											>
												<span class="status-badge" data-status={permission.status}>
													{permission.label}
												</span>
												<p class="mt-2 text-sm text-[var(--color-text-muted)]">
													{permission.detail}
												</p>
											</div>
										{/each}
									</div>
								{/if}
							</div>

							{#if canManageTeam && memberStatus(member) === 'invited'}
								<div
									class="grid gap-3 rounded border border-[var(--color-border)] bg-[var(--color-surface-muted)] p-3 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center"
								>
									<div>
										<p class="record-field__label">{text.team.firstSignIn}</p>
										<p class="text-sm text-[var(--color-text-muted)]">
											{text.team.firstSignInBody(member.email)}
										</p>
									</div>
									<a class="secondary-button" href={memberSignInUrl(member)}>
										<ExternalLink size={16} aria-hidden="true" />
										<span>{text.team.openLink}</span>
									</a>
									<button
										type="button"
										class="secondary-button"
										onclick={() => void copyMemberSignInUrl(member)}
									>
										<Copy size={16} aria-hidden="true" />
										<span
											>{copiedMemberUserId === member.userId
												? text.team.copied
												: text.team.copyLink}</span
										>
									</button>
								</div>
							{/if}

							{#if canManageTeam && !isCurrentUser(member.userId)}
								<div class="grid gap-3 xl:grid-cols-[minmax(14rem,1fr)_auto] xl:items-end">
									{#if tenantRoles.length > 0}
										<form
											class="grid gap-3 md:grid-cols-[minmax(12rem,1fr)_auto] md:items-end"
											onsubmit={(event) => {
												event.preventDefault();
												void changeTenantMemberRole(member);
											}}
										>
											<label class="field">
												<span>{text.team.roleFor(member.email)}</span>
												<select
													value={selectedRoleCode(member)}
													disabled={changingRoleUserId === member.userId}
													onchange={(event) =>
														updateRoleSelection(member, event.currentTarget.value)}
												>
													{#each tenantRoles as role (role.roleId)}
														<option value={role.code}>{role.name}</option>
													{/each}
												</select>
											</label>
											<button
												type="submit"
												class="secondary-button"
												aria-label={text.team.changeRoleAria(member.email)}
												disabled={!canSubmitRoleChange(member)}
											>
												{#if changingRoleUserId === member.userId}
													<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
													<span>{text.team.saving}</span>
												{:else}
													<Check size={16} aria-hidden="true" />
													<span>{text.team.changeRole}</span>
												{/if}
											</button>
										</form>
									{/if}

									<div class="flex flex-wrap gap-2 xl:justify-end">
										{#if memberStatus(member) === 'active'}
											<button
												type="button"
												class="secondary-button"
												aria-label={`Suspend ${member.email}`}
												disabled={isMutatingAccess(member)}
												onclick={() => void suspendTenantMember(member)}
											>
												{#if isMutatingAccess(member, 'suspend')}
													<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
												{:else}
													<ShieldOff size={16} aria-hidden="true" />
												{/if}
												<span>Suspend</span>
											</button>
										{:else if memberStatus(member) === 'suspended'}
											<button
												type="button"
												class="secondary-button"
												aria-label={`Reactivate ${member.email}`}
												disabled={isMutatingAccess(member)}
												onclick={() => void reactivateTenantMember(member)}
											>
												{#if isMutatingAccess(member, 'reactivate')}
													<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
												{:else}
													<RotateCcw size={16} aria-hidden="true" />
												{/if}
												<span>Reactivate</span>
											</button>
										{/if}

										<button
											type="button"
											class="secondary-button"
											aria-label={`Remove ${member.email}`}
											disabled={isMutatingAccess(member)}
											onclick={() => void removeTenantMember(member)}
										>
											{#if isMutatingAccess(member, 'remove')}
												<LoaderCircle size={16} aria-hidden="true" class="animate-spin" />
											{:else}
												<Trash2 size={16} aria-hidden="true" />
											{/if}
											<span>Remove</span>
										</button>
									</div>
								</div>
							{/if}
						</article>
					{/each}
				</div>

				{#if changeRoleError}
					<p class="error-line" role="alert">{changeRoleError}</p>
				{/if}

				{#if accessMutationError}
					<p class="error-line" role="alert">{accessMutationError}</p>
				{/if}
			{/if}
		{/if}
	</LoadingBoundary>
</section>
