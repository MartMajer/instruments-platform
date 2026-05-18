<script lang="ts">
	import type { ReportWidget } from '$lib/api/product';
	import ReportWidgetShell from './ReportWidgetShell.svelte';

	let { widget }: { widget: ReportWidget } = $props();

	const fallbackWidget = $derived({
		...widget,
		state: 'unsupported' as const,
		message: widget.message ?? 'This report widget is not supported by this app version.'
	});
</script>

<ReportWidgetShell widget={fallbackWidget}>
	<p class="result-line">
		<span>Kind</span>
		<code>{widget.kind}</code>
	</p>
</ReportWidgetShell>
