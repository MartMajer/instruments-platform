<script lang="ts">
	import { page } from '$app/state';
	import {
		appLocaleFromPageData,
		localizedHref,
		supportedAppLocales,
		type AppLocale
	} from '$lib/i18n/localization';
	import { appShellCopy } from '$lib/i18n/ui-copy';

	let { compact = false }: { compact?: boolean } = $props();

	const locale = $derived(appLocaleFromPageData(page.data));
	const copy = $derived(appShellCopy(locale));

	function hrefFor(nextLocale: AppLocale) {
		return localizedHref(page.url, nextLocale);
	}
</script>

<nav
	class={compact
		? 'flex flex-wrap items-center gap-1 text-xs'
		: 'flex flex-wrap items-center gap-2 text-sm'}
	aria-label={copy.language.label}
>
	<span class="text-[var(--color-text-muted)]">{copy.language.label}</span>
	{#each supportedAppLocales as option (option.id)}
		<a
			class={option.id === locale
				? 'rounded-full border border-[var(--color-accent)] bg-[var(--color-accent-soft)] px-2.5 py-1 font-semibold text-[var(--color-accent-strong)]'
				: 'rounded-full border border-[var(--color-border)] bg-[var(--color-surface)] px-2.5 py-1 font-semibold text-[var(--color-text-muted)] hover:border-[var(--color-border-strong)] hover:text-[var(--color-text)]'}
			href={hrefFor(option.id)}
			aria-current={option.id === locale ? 'true' : undefined}
			aria-label={`${copy.language.switchTo} ${option.nativeLabel}`}
		>
			{option.nativeLabel}
		</a>
	{/each}
</nav>
