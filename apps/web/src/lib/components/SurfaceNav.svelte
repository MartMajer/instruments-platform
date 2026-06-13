<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { page } from '$app/state';
	import {
		BarChart3,
		Building2,
		ClipboardList,
		FolderKanban,
		Home,
		ListChecks,
		Network,
		RadioTower,
		Settings2,
		UsersRound
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
					id: 'home',
					label: copy.surfaces.home,
					href: '/app',
					icon: Home,
					description: copy.descriptions.startHere,
					isDisabled: false
				},
				{
					id: 'studies',
					label: copy.surfaces.studies,
					href: '/app/campaign-series',
					icon: FolderKanban,
					description: copy.descriptions.planStudies,
					isDisabled: false
				}
			]
		},
		{
			id: 'people-access',
			label: copy.sections.peopleAccess,
			surfaces: [
				{
					id: 'directory',
					label: copy.surfaces.directory,
					href: '/app/directory',
					icon: Network,
					description: copy.descriptions.audiencesGroups,
					isDisabled: false
				},
				{
					id: 'team',
					label: copy.surfaces.team,
					href: '/app/team',
					icon: UsersRound,
					description: copy.descriptions.workspaceAccess,
					isDisabled: false
				}
			]
		},
		{
			id: 'workspace-admin',
			label: copy.sections.workspaceAdmin,
			surfaces: [
				{
					id: 'settings',
					label: copy.surfaces.settings,
					href: '/app/settings',
					icon: Building2,
					description: copy.descriptions.workspaceProfile,
					isDisabled: false
				}
			]
		}
	]);

	const selectedSeriesSection = $derived({
		id: 'active-study',
		label: copy.sections.selectedStudy,
		surfaces: [
			{
				id: 'overview',
				label: copy.surfaces.overview,
				href: activeSeriesId ? `/app/campaign-series/${activeSeriesId}` : '/app/campaign-series',
				icon: ClipboardList,
				description: activeSeriesId ? copy.descriptions.planStatus : copy.descriptions.selectStudyFirst,
				isDisabled: !activeSeriesId
			},
			{
				id: 'setup',
				label: copy.surfaces.setup,
				href: activeSeriesId ? `/app/campaign-series/${activeSeriesId}/setup` : '/app/campaign-series',
				icon: Settings2,
				description: activeSeriesId ? copy.descriptions.buildStudy : copy.descriptions.selectStudyFirst,
				isDisabled: !activeSeriesId
			},
			{
				id: 'collect',
				label: copy.surfaces.collect,
				href: activeSeriesId
					? `/app/campaign-series/${activeSeriesId}/operations`
					: '/app/campaign-series',
				icon: RadioTower,
				description: activeSeriesId ? copy.descriptions.collect : copy.descriptions.selectStudyFirst,
				isDisabled: !activeSeriesId
			},
			{
				id: 'results',
				label: copy.surfaces.results,
				href: activeSeriesId
					? `/app/campaign-series/${activeSeriesId}/reports`
					: '/app/campaign-series',
				icon: BarChart3,
				description: activeSeriesId
					? copy.descriptions.reportsExports
					: copy.descriptions.selectStudyFirst,
				isDisabled: !activeSeriesId
			}
		]
	});

	const utilitySections = $derived(
		demoSurfacesEnabled
			? [
					{
						id: 'internal-tools',
						label: copy.sections.internalTools,
						surfaces: [
							{
								id: 'demo-fixtures',
								label: copy.surfaces.demoFixtures,
								href: '/app/demo',
								icon: ListChecks,
								description: copy.descriptions.localGatedStates,
								isDisabled: false
							}
						]
					}
				]
			: []
	);

	const navigationSections = $derived([
		...globalNavigationSections,
		selectedSeriesSection,
		...utilitySections
	]);
</script>

<nav class="product-nav" aria-label={copy.aria.productNavigation}>
	{#each navigationSections as section (section.id)}
		{@const sectionLabelId = `product-nav-${section.id}-label`}
		<div class="product-nav__section" role="group" aria-labelledby={sectionLabelId}>
			<p id={sectionLabelId} class="product-nav__section-heading">{section.label}</p>
			<ol class="product-nav__list">
				{#each section.surfaces as surface (surface.id)}
					{@const Icon = surface.icon}
					{@const isCurrent = page.url.pathname === surface.href}
					<li>
						<a
							href={surface.href}
							aria-current={!surface.isDisabled && isCurrent ? 'page' : undefined}
							aria-disabled={surface.isDisabled ? 'true' : undefined}
							data-disabled={surface.isDisabled ? 'true' : undefined}
							tabindex={surface.isDisabled ? -1 : undefined}
							class="product-nav__link"
							onclick={(event) => {
								if (surface.isDisabled) event.preventDefault();
							}}
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
