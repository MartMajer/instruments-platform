<script lang="ts">
	import { page } from '$app/state';
	import SelectedSeriesSurface from '$lib/product/SelectedSeriesSurface.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';

	const seriesId = $derived(page.params.seriesId ?? '');
</script>

<SurfaceHeader
	eyebrow="Wave comparison"
	title="Waves"
	description="Linked trajectory checks, wave comparison, and disclosure-gated deltas live here."
/>

<SelectedSeriesSurface seriesId={seriesId} surface="waves" ariaLabel="Waves and linked trajectories" />
