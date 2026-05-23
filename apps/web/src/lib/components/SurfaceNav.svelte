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
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { surfaceNavCopy } from '$lib/i18n/ui-copy';

	const activeSeriesId = $derived(page.params.seriesId);
	const demoSurfacesEnabled = $derived(env.PUBLIC_DEMO_SURFACES_ENABLED === 'true');
	const locale = $derived(appLocaleFromPageData(page.data));
	const copy = $derived(surfaceNavCopy(locale));

	const globalNavigationSections = $derived([
		{
			id: 'studies',
			label: copy.sections.studies,
			surfaces: [
				{
					label: copy.surfaces.home,
					href: '/app',
					icon: Home,
					description: copy.descriptions.startHere
				},
				{
					label: copy.surfaces.studies,
					href: '/app/campaign-series',
					icon: FolderKanban,
					description: copy.descriptions.planStudies
				},
				{
					label: copy.surfaces.instrumentLibrary,
					href: '/app/instruments',
					icon: BookOpen,
					description: copy.descriptions.questionSets
				},
				{
					label: copy.surfaces.exports,
					href: '/app/exports',
					icon: FileDown,
					description: copy.descriptions.files
				}
			]
		},
		{
			id: 'people-access',
			label: copy.sections.peopleAccess,
			surfaces: [
				{
					label: copy.surfaces.directory,
					href: '/app/directory',
					icon: Network,
					description: copy.descriptions.audiencesGroups
				},
				{
					label: copy.surfaces.team,
					href: '/app/team',
					icon: UsersRound,
					description: copy.descriptions.workspaceAccess
				}
			]
		},
		{
			id: 'workspace-admin',
			label: copy.sections.workspaceAdmin,
			surfaces: [
				{
					label: copy.surfaces.settings,
					href: '/app/settings',
					icon: Building2,
					description: copy.descriptions.workspaceProfile
				}
			]
		}
	]);

	const selectedSeriesSection = $derived(
		activeSeriesId
			? {
					id: 'selected-study',
					label: copy.sections.selectedStudy,
					surfaces: [
						{
							label: copy.surfaces.overview,
							href: `/app/campaign-series/${activeSeriesId}`,
							icon: ClipboardList,
							description: copy.descriptions.planStatus
						},
						{
							label: copy.surfaces.setup,
							href: `/app/campaign-series/${activeSeriesId}/setup`,
							icon: Settings2,
							description: copy.descriptions.buildStudy
						},
						{
							label: copy.surfaces.collect,
							href: `/app/campaign-series/${activeSeriesId}/operations`,
							icon: RadioTower,
							description: copy.descriptions.collect
						},
						{
							label: copy.surfaces.results,
							href: `/app/campaign-series/${activeSeriesId}/reports`,
							icon: BarChart3,
							description: copy.descriptions.reportsExports
						},
						{
							label: copy.surfaces.waves,
							href: `/app/campaign-series/${activeSeriesId}/waves`,
							icon: Waves,
							description: copy.descriptions.compareWaves
						}
					]
				}
			: null
	);

	const utilitySection = $derived(
		demoSurfacesEnabled
			? {
					id: 'internal-tools',
					label: copy.sections.internalTools,
					surfaces: [
						{
							label: copy.surfaces.demoFixtures,
							href: '/app/demo',
							icon: ListChecks,
							description: copy.descriptions.localGatedStates
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

<nav class="product-nav" aria-label={copy.aria.productNavigation}>
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
