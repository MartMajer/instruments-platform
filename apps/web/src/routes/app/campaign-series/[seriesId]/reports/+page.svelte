<script lang="ts">
	import { page } from '$app/state';
	import SelectedSeriesSurface from '$lib/product/SelectedSeriesSurface.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';

	const seriesId = $derived(page.params.seriesId ?? '');
	const appLocale = $derived(appLocaleFromPageData(page.data));
	const copy = $derived(routePageCopy(appLocale).selectedStudy.surfaces.reports);
</script>

<SurfaceHeader
	eyebrow={copy.eyebrow}
	title={copy.title}
	description={copy.description}
/>

<SelectedSeriesSurface {seriesId} surface="reports" ariaLabel={copy.ariaLabel} />
