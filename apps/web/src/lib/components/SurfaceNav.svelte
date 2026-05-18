<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import {
		BarChart3,
		BookOpen,
		Building2,
		ClipboardList,
		FileDown,
		FolderKanban,
		Home,
		ListChecks,
		Network,
		RadioTower,
		Settings2,
		UsersRound,
		Waves
	} from 'lucide-svelte';

	const activeSeriesId = $derived(page.params.seriesId);
	const demoSurfacesEnabled = $derived(env.PUBLIC_DEMO_SURFACES_ENABLED === 'true');

	const globalNavigationSections = $derived([
		{
			id: 'studies',
			label: 'Studies',
			surfaces: [
				{
					label: 'Home',
					href: '/app',
					icon: Home,
					description: 'Study cockpit'
				},
				{
					label: 'Studies',
					href: '/app/campaign-series',
					icon: FolderKanban,
					description: 'Create and select'
				},
				{
					label: 'Instrument library',
					href: '/app/instruments',
					icon: BookOpen,
					description: 'Reusable instruments'
				},
				{
					label: 'Exports',
					href: '/app/exports',
					icon: FileDown,
					description: 'Artifacts'
				}
			]
		},
		{
			id: 'people-access',
			label: 'People and access',
			surfaces: [
				{
					label: 'Directory',
					href: '/app/directory',
					icon: Network,
					description: 'Subjects and hierarchy'
				},
				{
					label: 'Team',
					href: '/app/team',
					icon: UsersRound,
					description: 'Members and roles'
				}
			]
		},
		{
			id: 'workspace-admin',
			label: 'Workspace admin',
			surfaces: [
				{
					label: 'Settings',
					href: '/app/settings',
					icon: Building2,
					description: 'Tenant profile'
				}
			]
		}
	]);

	const selectedSeriesSection = $derived(
		activeSeriesId
			? {
					id: 'selected-study',
					label: 'Selected study',
					surfaces: [
						{
							label: 'Overview',
							href: `/app/campaign-series/${activeSeriesId}`,
							icon: ClipboardList,
							description: 'Selected study'
						},
						{
							label: 'Setup',
							href: `/app/campaign-series/${activeSeriesId}/setup`,
							icon: Settings2,
							description: 'Configuration'
						},
						{
							label: 'Collect',
							href: `/app/campaign-series/${activeSeriesId}/operations`,
							icon: RadioTower,
							description: 'Launch and delivery'
						},
						{
							label: 'Results',
							href: `/app/campaign-series/${activeSeriesId}/reports`,
							icon: BarChart3,
							description: 'Reports and exports'
						},
						{
							label: 'Waves',
							href: `/app/campaign-series/${activeSeriesId}/waves`,
							icon: Waves,
							description: 'Linked trajectories'
						}
					]
				}
			: null
	);

	const utilitySection = $derived(
		demoSurfacesEnabled
			? {
					id: 'internal-tools',
					label: 'Internal tools',
					surfaces: [
						{
							label: 'Demo fixtures',
							href: '/app/demo',
							icon: ListChecks,
							description: 'Local gated states'
						}
					]
				}
			: null
	);

	const navigationSections = $derived([
		...globalNavigationSections,
		...(selectedSeriesSection ? [selectedSeriesSection] : []),
		...(utilitySection ? [utilitySection] : [])
	]);
</script>

<nav class="product-nav" aria-label="Product navigation">
	{#each navigationSections as section (section.id)}
		{@const sectionLabelId = `product-nav-${section.id}-label`}
		<div class="product-nav__section" role="group" aria-labelledby={sectionLabelId}>
			<p id={sectionLabelId} class="product-nav__section-heading">{section.label}</p>
			<ol class="product-nav__list">
				{#each section.surfaces as surface (surface.href)}
					{@const Icon = surface.icon}
					{@const isCurrent = page.url.pathname === surface.href}
					<li>
						<a
							href={surface.href}
							aria-current={isCurrent ? 'page' : undefined}
							class="product-nav__link"
						>
							<span class="product-nav__icon">
								<Icon size={17} strokeWidth={2.1} aria-hidden="true" />
							</span>
							<span class="min-w-0">
								<span class="product-nav__label">{surface.label}</span>
								<span class="product-nav__description">{surface.description}</span>
							</span>
						</a>
					</li>
				{/each}
			</ol>
		</div>
	{/each}
</nav>
