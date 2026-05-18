<script lang="ts">
	import { page } from '$app/state';
	import SelectedSeriesSurface from '$lib/product/SelectedSeriesSurface.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';

	const seriesId = $derived(page.params.seriesId ?? '');
</script>

<SurfaceHeader
	eyebrow="Study results"
	title="Review results"
	description="Review available findings, score coverage, limitations, and export next use for this study."
/>

<SelectedSeriesSurface {seriesId} surface="reports" ariaLabel="Results workspace" />
