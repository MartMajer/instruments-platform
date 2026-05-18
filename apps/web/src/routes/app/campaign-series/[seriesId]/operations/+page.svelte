<script lang="ts">
	import { page } from '$app/state';
	import SelectedSeriesSurface from '$lib/product/SelectedSeriesSurface.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';

	const seriesId = $derived(page.params.seriesId ?? '');
</script>

<SurfaceHeader
	eyebrow="Study collection"
	title="Collect responses"
	description="Monitor respondent access, response progress, and collection actions for this study."
/>

<SelectedSeriesSurface seriesId={seriesId} surface="operations" ariaLabel="Collection workspace" />
