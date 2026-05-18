<script lang="ts">
	import { page } from '$app/state';
	import SelectedSeriesSurface from '$lib/product/SelectedSeriesSurface.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';

	const seriesId = $derived(page.params.seriesId ?? '');
</script>

<SurfaceHeader
	eyebrow="Study preparation"
	title="Prepare study"
	description="Check what is ready, what is missing, and the next setup task before this study moves into collection."
/>

<SelectedSeriesSurface seriesId={seriesId} surface="setup" ariaLabel="Setup workspace" />
