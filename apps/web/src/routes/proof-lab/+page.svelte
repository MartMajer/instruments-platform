<script lang="ts">
	import ProofWorkflowSurface from '$lib/product/ProofWorkflowSurface.svelte';
</script>

<svelte:head>
	<title>Proof Lab</title>
	<meta
		name="description"
		content="Internal proof workflow for operator and local validation checks."
	/>
</svelte:head>

<section class="setup-callout" aria-label="Internal proof lab boundary">
	<p class="setup-callout__key">Internal proof lab</p>
	<p class="setup-callout__value">Operator workflow</p>
	<p class="setup-callout__note">
		This route keeps the all-in-one proof workflow out of the product entry path.
	</p>
</section>

<ProofWorkflowSurface surface="all" />
