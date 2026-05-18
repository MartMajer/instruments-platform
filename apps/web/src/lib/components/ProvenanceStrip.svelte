<script lang="ts">
	export type ProvenanceItem = {
		label: string;
		value: string | number | null | undefined;
		mono?: boolean;
	};

	let {
		items,
		label = 'Provenance'
	}: {
		items: ProvenanceItem[];
		label?: string;
	} = $props();
</script>

<section class="provenance-strip" aria-label={label}>
	{#each items as item (item.label)}
		<div class="provenance-item">
			<span class="provenance-item__key">{item.label}</span>
			<span
				class="provenance-item__value"
				class:provenance-item__value--mono={item.mono}
			>
				{item.value ?? 'Not available'}
			</span>
		</div>
	{/each}
</section>
