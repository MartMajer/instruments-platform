<script lang="ts">
	import '../app.css';

	let { children } = $props();
</script>

<svelte:head>
	<title>Spectra</title>
	<meta
		name="description"
		content="Run validated instruments as rigorous studies — protocol, field, evidence."
	/>
</svelte:head>

{@render children()}
