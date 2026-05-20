<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { env } from '$env/dynamic/public';
	import {
		ClipboardCheck,
		FileDown,
		FileStack,
		FolderKanban,
		Gauge,
		Home,
		LibraryBig,
		Menu,
		Network,
		PanelLeft,
		Send,
		Settings2,
		UsersRound,
		X
	} from 'lucide-svelte';
	import SurfaceNav from '$lib/components/SurfaceNav.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { setupStages } from '$lib/setup/stages';

	let { children } = $props();
	let mobileMenuOpen = $state(false);

	const stageIcons = [LibraryBig, FileStack, Gauge, Send, ClipboardCheck];
	const isProductShell = $derived(page.url.pathname.startsWith('/app'));
	const activeSeriesId = $derived(page.params.seriesId);
	const isProductEntry = $derived(page.url.pathname === '/');
	const isRegistrationEntry = $derived(page.url.pathname === '/register');
	const isSignInEntry = $derived(page.url.pathname === '/signin');
	const isPublicEntry = $derived(isProductEntry || isRegistrationEntry || isSignInEntry);
	const shellLabel = $derived(
		isProductShell ? 'Product workspace' : isPublicEntry ? 'Private beta' : 'Tenant setup'
	);
	const headerKicker = $derived(
		isProductShell
			? 'Tenant workspace'
			: isProductEntry
				? 'Product entry'
				: isRegistrationEntry
					? 'Registration'
					: isSignInEntry
						? 'Sign in'
						: 'Tenant setup path'
	);
	const headerTitle = $derived(
		isProductShell
			? 'Tenant command workspace'
			: isProductEntry
				? 'Authenticated workspace gateway'
				: isRegistrationEntry
					? 'Create workspace'
					: isSignInEntry
						? 'Workspace sign-in'
						: 'Setup APIs and launch readiness'
	);
	const mainLabel = $derived(
		isProductShell
			? 'Product workspace'
			: isProductEntry
				? 'Product entry'
				: isRegistrationEntry
					? 'Registration'
					: isSignInEntry
						? 'Workspace sign-in'
					: 'Tenant setup workspace'
	);
	const currentAppArea = $derived(toCurrentAppArea(page.url.pathname));
	const currentStudyHref = $derived(
		activeSeriesId ? `/app/campaign-series/${activeSeriesId}` : '/app/campaign-series'
	);
	const logoutUrl = $derived(env.PUBLIC_AUTH_LOGOUT_URL || resolve('/'));
	const bottomNavItems = $derived([
		{ id: 'home', label: 'Home', href: '/app', icon: Home, match: (path: string) => path === '/app' },
		{
			id: 'studies',
			label: 'Studies',
			href: '/app/campaign-series',
			icon: FolderKanban,
			match: (path: string) => path === '/app/campaign-series'
		},
		...(activeSeriesId
			? [
					{
						id: 'study',
						label: 'Study',
						href: currentStudyHref,
						icon: FileStack,
						match: (path: string) => /^\/app\/campaign-series\/[^/]+/.test(path)
					}
				]
			: []),
		{
			id: 'directory',
			label: 'Directory',
			href: '/app/directory',
			icon: Network,
			match: (path: string) => path === '/app/directory'
		}
	]);
	const moreNavItems = [
		{ label: 'Team', href: '/app/team', icon: UsersRound, description: 'Workspace access' },
		{ label: 'Exports', href: '/app/exports', icon: FileDown, description: 'Files and downloads' },
		{ label: 'Settings', href: '/app/settings', icon: Settings2, description: 'Workspace settings' }
	];

	$effect(() => {
		page.url.pathname;
		mobileMenuOpen = false;
	});

	function toCurrentAppArea(pathname: string) {
		if (pathname === '/app') return 'Home';
		if (pathname === '/app/campaign-series') return 'Studies';
		if (pathname.startsWith('/app/campaign-series/')) return 'Study';
		if (pathname === '/app/directory') return 'Directory';
		if (pathname === '/app/team') return 'Team';
		if (pathname === '/app/exports') return 'Exports';
		if (pathname === '/app/settings') return 'Settings';
		if (pathname === '/app/instruments') return 'Instruments';
		return 'Workspace';
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
					<div class="app-brand__mark" aria-hidden="true">
						<PanelLeft size={18} strokeWidth={2.1} />
					</div>
					<div class="min-w-0">
						<p class="app-brand__name">Instruments Platform</p>
						<p class="app-brand__context">{shellLabel}</p>
					</div>
				</div>

				{#if isProductShell}
					<SurfaceNav />
				{:else}
					<nav class="mt-5" aria-label="Setup stages">
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
				<header class="app-mobile-topbar" aria-label="Mobile workspace navigation">
					<a class="app-mobile-topbar__brand" href={resolve('/app')}>
						<span class="app-mobile-topbar__mark" aria-hidden="true">IP</span>
						<span>
							<strong>Instruments Platform</strong>
							<small>{currentAppArea}</small>
						</span>
					</a>
					<button
						type="button"
						class="app-mobile-topbar__menu"
						aria-label={mobileMenuOpen ? 'Close navigation menu' : 'Open navigation menu'}
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
					<div class="app-mobile-menu" role="dialog" aria-label="Workspace menu">
						<nav class="app-mobile-menu__section" aria-label="Primary workspace routes">
							<p class="app-mobile-menu__heading">Workspace</p>
							{#each bottomNavItems as item (item.id)}
								{@const Icon = item.icon}
								<a
									class="app-mobile-menu__link"
									data-current={item.match(page.url.pathname) ? 'true' : undefined}
									href={resolve(item.href)}
								>
									<Icon size={18} strokeWidth={2.1} aria-hidden="true" />
									<span>{item.label}</span>
								</a>
							{/each}
						</nav>
						<nav class="app-mobile-menu__section" aria-label="More workspace routes">
							<p class="app-mobile-menu__heading">More</p>
							{#each moreNavItems as item (item.href)}
								{@const Icon = item.icon}
								<a class="app-mobile-menu__link" href={resolve(item.href)}>
									<Icon size={18} strokeWidth={2.1} aria-hidden="true" />
									<span>
										<strong>{item.label}</strong>
										<small>{item.description}</small>
									</span>
								</a>
							{/each}
							<a class="app-mobile-menu__link app-mobile-menu__link--muted" href={logoutUrl}>
								<span>Sign out</span>
							</a>
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
						<div class="app-topbar__meta" aria-label="Workspace posture">
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

			{#if isProductShell}
				<nav class="app-mobile-bottom-nav" aria-label="Primary mobile navigation">
					{#each bottomNavItems as item (item.id)}
						{@const Icon = item.icon}
						<a
							href={resolve(item.href)}
							class="app-mobile-bottom-nav__link"
							aria-current={item.match(page.url.pathname) ? 'page' : undefined}
						>
							<Icon size={18} strokeWidth={2.1} aria-hidden="true" />
							<span>{item.label}</span>
						</a>
					{/each}
					<button
						type="button"
						class="app-mobile-bottom-nav__link"
						aria-current={mobileMenuOpen ? 'page' : undefined}
						aria-expanded={mobileMenuOpen}
						onclick={() => (mobileMenuOpen = !mobileMenuOpen)}
					>
						<Menu size={18} strokeWidth={2.1} aria-hidden="true" />
						<span>More</span>
					</button>
				</nav>
			{/if}
		</div>
	</div>
</div>
