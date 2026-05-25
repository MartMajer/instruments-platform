<script lang="ts">
	import type { ProductRouteGuidanceView } from '$lib/product/route-guidance';

	let { guidance }: { guidance: ProductRouteGuidanceView } = $props();
</script>

<section class="route-guidance" aria-label={guidance.ariaLabel}>
	<div class="route-guidance__header">
		<p class="product-kicker">{guidance.kicker}</p>
		<h2 class="route-guidance__title">{guidance.title}</h2>
		<p class="route-guidance__summary">{guidance.summary}</p>
	</div>

	<dl class="route-guidance__grid">
		<div class="route-guidance__field">
			<dt>{guidance.inspectFirstLabel}</dt>
			<dd>{guidance.inspectFirst}</dd>
		</div>
		<div class="route-guidance__field">
			<dt>{guidance.whenItMattersLabel}</dt>
			<dd>{guidance.whenItMatters}</dd>
		</div>
		<div class="route-guidance__field">
			<dt>{guidance.nextMoveLabel}</dt>
			<dd>{guidance.nextMove}</dd>
		</div>
		{#if guidance.limits}
			<div class="route-guidance__field">
				<dt>{guidance.limitsLabel}</dt>
				<dd>{guidance.limits}</dd>
			</div>
		{/if}
	</dl>
</section>
