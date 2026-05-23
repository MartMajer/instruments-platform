<script lang="ts">
	import { page } from '$app/state';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { productStatusLabel, type ProductStatus } from './status-badge-labels';

	let { status, label }: { status: ProductStatus; label?: string } = $props();

	const appLocale = $derived(appLocaleFromPageData(page.data));
	const displayLabel = $derived(label ?? productStatusLabel(status, appLocale));
</script>

<span class="status-badge" data-status={status}>{displayLabel}</span>
