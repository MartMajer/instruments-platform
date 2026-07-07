<script lang="ts">
	import { helpContent } from '$lib/core/help-content';
	import { t } from '$lib/core/locale.svelte';

	const content = $derived(helpContent());
</script>

<svelte:head><title>{t('How ValidatedScale works')} — ValidatedScale</title></svelte:head>

<header class="head">
	<p class="eyebrow">{t('Guide')}</p>
	<h1 class="doc-title">{t('How ValidatedScale works')}</h1>
	<p class="intro">{content.intro}</p>
</header>

<nav class="toc" aria-label={t('Guide')}>
	{#each content.sections as section (section.id)}
		<a href="#{section.id}">{section.title}</a>
	{/each}
	<a href="#glossary">{content.glossaryTitle}</a>
</nav>

{#each content.sections as section (section.id)}
	<section class="chapter" id={section.id}>
		<h2>{section.title}</h2>
		{#each section.paragraphs as paragraph (paragraph)}
			<p>{paragraph}</p>
		{/each}
	</section>
{/each}

<section class="chapter" id="glossary">
	<h2>{content.glossaryTitle}</h2>
	<dl class="glossary">
		{#each content.glossary as entry (entry.term)}
			<div>
				<dt>{entry.term}</dt>
				<dd>{entry.definition}</dd>
			</div>
		{/each}
	</dl>
</section>

<style>
	.head {
		margin-bottom: 1.5rem;
		max-width: 68ch;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.intro {
		margin-top: 0.75rem;
		font-size: 1rem;
		line-height: 1.6;
		color: var(--color-ink-2);
	}

	.toc {
		display: flex;
		flex-wrap: wrap;
		gap: 0.375rem 1.25rem;
		padding: 0.875rem 0;
		margin-bottom: 1.5rem;
		border-top: 1px solid var(--color-line);
		border-bottom: 1px solid var(--color-line);
	}

	.toc a {
		font-size: 0.8125rem;
		font-weight: 560;
		color: var(--color-stain);
		text-decoration: none;
	}

	.toc a:hover {
		text-decoration: underline;
	}

	.chapter {
		max-width: 68ch;
		margin-bottom: 2.5rem;
	}

	.chapter h2 {
		font-size: 1.25rem;
		margin-bottom: 0.75rem;
	}

	.chapter p {
		font-size: 0.9375rem;
		line-height: 1.65;
		color: var(--color-ink-2);
		margin-bottom: 0.75rem;
	}

	.glossary {
		display: grid;
		gap: 0.875rem;
	}

	.glossary dt {
		font-weight: 600;
		font-size: 0.9375rem;
	}

	.glossary dd {
		font-size: 0.875rem;
		line-height: 1.55;
		color: var(--color-ink-2);
		margin-top: 0.125rem;
	}
</style>
