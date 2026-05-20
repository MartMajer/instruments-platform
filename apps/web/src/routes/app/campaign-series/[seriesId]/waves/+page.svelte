<script lang="ts">
	import { page } from '$app/state';
	import SelectedSeriesSurface from '$lib/product/SelectedSeriesSurface.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';

	const seriesId = $derived(page.params.seriesId ?? '');
</script>

<SurfaceHeader
	eyebrow="Wave comparison"
	title="Waves"
	description="Create follow-up collection waves, then compare linked longitudinal change when the study is ready."
/>

<SelectedSeriesSurface seriesId={seriesId} surface="waves" ariaLabel="Waves and linked trajectories" />
