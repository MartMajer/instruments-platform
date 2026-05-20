<script lang="ts">
	let {
		eyebrow,
		title,
		description,
		statusLabel = 'Study workspace',
		status = 'neutral'
	}: {
		eyebrow: string;
		title: string;
		description: string;
		statusLabel?: string;
		status?: string;
	} = $props();
</script>

<header class="surface-header">
	<div class="surface-header__frame">
		<div class="min-w-0">
			<p class="surface-header__eyebrow">{eyebrow}</p>
			<h1 class="surface-header__title">{title}</h1>
			<p class="surface-header__description">{description}</p>
		</div>
		<span class="status-badge" data-status={status}>{statusLabel}</span>
	</div>
</header>
