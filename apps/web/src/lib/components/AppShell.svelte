<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { env } from '$env/dynamic/public';
	import type { Snippet } from 'svelte';
	import {
		ClipboardCheck,
		FileStack,
		Gauge,
		LibraryBig,
		Menu,
		Network,
		Send,
		X
	} from 'lucide-svelte';
	import SurfaceNav from '$lib/components/SurfaceNav.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import LocaleSwitcher from '$lib/components/LocaleSwitcher.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { appShellCopy, surfaceNavCopy } from '$lib/i18n/ui-copy';
	import { setupStages } from '$lib/setup/stages';

	let { children, account }: { children: Snippet; account?: Snippet } = $props();
	let mobileMenuOpen = $state(false);

	const stageIcons = [LibraryBig, FileStack, Gauge, Send, ClipboardCheck];
	const locale = $derived(appLocaleFromPageData(page.data));
	const copy = $derived(appShellCopy(locale));
	const surfaceCopy = $derived(surfaceNavCopy(locale));
	const isProductShell = $derived(page.url.pathname.startsWith('/app'));
	const activeSeriesId = $derived(page.params.seriesId);
	const isProductEntry = $derived(page.url.pathname === '/');
	const isRegistrationEntry = $derived(page.url.pathname === '/register');
	const isSignInEntry = $derived(page.url.pathname === '/signin');
	const isRespondentEntry = $derived(page.url.pathname.startsWith('/r/'));
	const isPublicEntry = $derived(
		isProductEntry || isRegistrationEntry || isSignInEntry || isRespondentEntry
	);
	const shellLabel = $derived(
		isProductShell
			? copy.shell.productWorkspace
			: isPublicEntry
				? copy.shell.privateBeta
				: copy.shell.tenantSetup
	);
	const headerKicker = $derived(
		isProductShell
			? copy.shell.tenantWorkspace
			: isProductEntry
				? copy.shell.productEntry
				: isRegistrationEntry
					? copy.shell.registration
					: isSignInEntry
						? copy.shell.signIn
						: isRespondentEntry
							? copy.shell.respondent
						: copy.shell.tenantSetupPath
	);
	const headerTitle = $derived(
		isProductShell
			? copy.shell.tenantCommandWorkspace
			: isProductEntry
				? copy.shell.authenticatedWorkspaceGateway
				: isRegistrationEntry
					? copy.shell.createWorkspace
					: isSignInEntry
						? copy.shell.workspaceSignIn
						: isRespondentEntry
							? copy.shell.respondentAccess
						: copy.shell.setupApisLaunchReadiness
	);
	const mainLabel = $derived(
		isProductShell
			? copy.shell.productWorkspace
			: isProductEntry
				? copy.shell.productEntry
				: isRegistrationEntry
					? copy.shell.registration
					: isSignInEntry
						? copy.shell.workspaceSignIn
					: isRespondentEntry
						? copy.shell.respondentAccess
					: copy.shell.tenantSetupWorkspace
	);
	const currentAppArea = $derived(toCurrentAppArea(page.url.pathname));
	const currentStudyHref = $derived(
		activeSeriesId ? `/app/campaign-series/${activeSeriesId}` : '/app/campaign-series'
	);
	const logoutUrl = $derived(env.PUBLIC_AUTH_LOGOUT_URL || resolve('/'));
	const bottomNavItems = $derived(
		activeSeriesId
			? [
					{
						id: 'overview',
						label: surfaceCopy.surfaces.overview,
						href: `/app/campaign-series/${activeSeriesId}`,
						icon: ClipboardCheck,
						match: (path: string) => path === `/app/campaign-series/${activeSeriesId}`
					},
					{
						id: 'setup',
						label: surfaceCopy.surfaces.setup,
						href: `/app/campaign-series/${activeSeriesId}/setup`,
						icon: FileStack,
						match: (path: string) => path === `/app/campaign-series/${activeSeriesId}/setup`
					},
					{
						id: 'collect',
						label: surfaceCopy.surfaces.collect,
						href: `/app/campaign-series/${activeSeriesId}/operations`,
						icon: Send,
						match: (path: string) => path === `/app/campaign-series/${activeSeriesId}/operations`
					},
					{
						id: 'results',
						label: surfaceCopy.surfaces.results,
						href: `/app/campaign-series/${activeSeriesId}/reports`,
						icon: Gauge,
						match: (path: string) => path === `/app/campaign-series/${activeSeriesId}/reports`
					},
					{
						id: 'waves',
						label: surfaceCopy.surfaces.waves,
						href: `/app/campaign-series/${activeSeriesId}/waves`,
						icon: Network,
						match: (path: string) => path === `/app/campaign-series/${activeSeriesId}/waves`
					}
				]
			: []
	);
	$effect(() => {
		page.url.pathname;
		mobileMenuOpen = false;
	});

	function toCurrentAppArea(pathname: string) {
		if (pathname === '/app') return copy.nav.home;
		if (pathname === '/app/campaign-series') return copy.nav.studies;
		if (pathname.startsWith('/app/campaign-series/')) return copy.nav.study;
		if (pathname === '/app/directory') return copy.nav.directory;
		if (pathname === '/app/team') return copy.nav.team;
		if (pathname === '/app/exports') return copy.nav.exports;
		if (pathname === '/app/settings') return copy.nav.settings;
		if (pathname === '/app/instruments') return copy.nav.instruments;
		return copy.nav.workspace;
	}
</script>

<div
	class="app-shell"
	class:app-shell--entry={isPublicEntry}
	class:app-shell--product={isProductShell}
>
	<div class="app-shell__grid" class:app-shell__grid--entry={isPublicEntry}>
		{#if !isPublicEntry}
			<aside class="app-sidebar">
				<div class="app-brand">
					<img class="app-brand__mark" src="/brand/validated-scale-mark.svg" alt="" aria-hidden="true" />
					<div class="min-w-0">
						<p class="app-brand__name">Validated Scale</p>
						<p class="app-brand__context">{shellLabel}</p>
					</div>
				</div>
				<LocaleSwitcher compact />

				{#if isProductShell}
					<div class="app-sidebar__body">
						<SurfaceNav />
					</div>
					{#if account}
						<div class="app-sidebar__account" aria-label={copy.aria.workspacePosture}>
							{@render account()}
						</div>
					{/if}
				{:else}
					<nav class="mt-5" aria-label={copy.aria.setupStages}>
						<ol class="grid gap-2">
							{#each setupStages as stage, index (stage.href)}
								{@const Icon = stageIcons[index]}
								<li>
									<a
										href={resolve(stage.href)}
										aria-current={stage.status === 'next' ? 'step' : undefined}
										class="group grid grid-cols-[2.125rem_1fr] gap-3 rounded border border-transparent px-2 py-2 transition hover:border-[var(--color-border)] hover:bg-[var(--color-surface-muted)]"
									>
										<span
											class="flex size-8 items-center justify-center rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-muted)] group-hover:text-[var(--color-text)]"
										>
											<Icon size={17} strokeWidth={2.1} aria-hidden="true" />
										</span>
										<span class="min-w-0">
											<span class="flex items-center justify-between gap-2">
												<span class="truncate text-sm font-semibold">{stage.label}</span>
												<StatusBadge status={stage.status} />
											</span>
											<span class="mt-1 line-clamp-2 block text-xs text-[var(--color-text-muted)]">
												{stage.description}
											</span>
										</span>
									</a>
								</li>
							{/each}
						</ol>
					</nav>
				{/if}
			</aside>
		{/if}

		<div class="min-w-0">
			{#if isProductShell}
				<header class="app-mobile-topbar" aria-label={copy.aria.mobileWorkspaceNavigation}>
					<a class="app-mobile-topbar__brand" href={resolve('/app')}>
						<img class="app-mobile-topbar__mark" src="/brand/validated-scale-mark.svg" alt="" aria-hidden="true" />
						<span>
							<strong>Validated Scale</strong>
							<small>{currentAppArea}</small>
						</span>
					</a>
					<button
						type="button"
						class="app-mobile-topbar__menu"
						aria-label={mobileMenuOpen ? copy.actions.closeNavigationMenu : copy.actions.openNavigationMenu}
						aria-expanded={mobileMenuOpen}
						onclick={() => (mobileMenuOpen = !mobileMenuOpen)}
					>
						{#if mobileMenuOpen}
							<X size={19} strokeWidth={2.2} aria-hidden="true" />
						{:else}
							<Menu size={19} strokeWidth={2.2} aria-hidden="true" />
						{/if}
					</button>
				</header>

				{#if mobileMenuOpen}
					<div class="app-mobile-menu" role="dialog" aria-label={copy.aria.workspaceMenu}>
						{#if account}
							<div class="app-mobile-menu__account">
								{@render account()}
							</div>
						{/if}
						<SurfaceNav />
						<nav class="app-mobile-menu__section" aria-label={copy.aria.moreWorkspaceRoutes}>
							<p class="app-mobile-menu__heading">{copy.nav.more}</p>
							<LocaleSwitcher compact />
							{#if !account}
								<a class="app-mobile-menu__link app-mobile-menu__link--muted" href={logoutUrl}>
									<span>{copy.actions.signOut}</span>
								</a>
							{/if}
						</nav>
					</div>
				{/if}
			{/if}

			{#if !isProductShell && !isPublicEntry}
				<header class="app-topbar">
					<div class="app-topbar__inner">
						<div>
							<p class="app-topbar__kicker">{headerKicker}</p>
							<p class="app-topbar__title">{headerTitle}</p>
						</div>
						<div class="app-topbar__meta" aria-label={copy.aria.workspacePosture}>
							<span class="app-chip">
								{isProductEntry ? 'App-first route' : 'Rights-attested content'}
							</span>
							<span class="app-chip">
								{isProductEntry ? 'Tenant data in /app' : 'Local dev auth'}
							</span>
						</div>
					</div>
				</header>
			{/if}

			<main class="app-main" aria-label={mainLabel}>
				<div class="app-main__inner">
					{@render children()}
				</div>
			</main>

			{#if isProductShell && activeSeriesId}
				<nav class="app-mobile-bottom-nav" aria-label={surfaceCopy.sections.selectedStudy}>
					{#each bottomNavItems as item (item.id)}
						{@const Icon = item.icon}
						<a
							href={item.href}
							class="app-mobile-bottom-nav__link"
							aria-current={item.match(page.url.pathname) ? 'page' : undefined}
						>
							<Icon size={18} strokeWidth={2.1} aria-hidden="true" />
							<span>{item.label}</span>
						</a>
					{/each}
				</nav>
			{/if}
		</div>
	</div>
</div>
