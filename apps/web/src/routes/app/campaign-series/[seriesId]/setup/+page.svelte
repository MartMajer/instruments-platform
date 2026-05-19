<script lang="ts">
	import { page } from '$app/state';
	import SelectedSeriesSurface from '$lib/product/SelectedSeriesSurface.svelte';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';

	const seriesId = $derived(page.params.seriesId ?? '');
</script>

<SurfaceHeader
	eyebrow="Setup"
	title="Study setup"
	description="Work through the setup steps in order before collection starts."
/>

<SelectedSeriesSurface seriesId={seriesId} surface="setup" ariaLabel="Setup workspace" />
