<script lang="ts">
	const claims = [
		{
			n: 'Protocol',
			body: 'A study is a document, not a form builder. Instrument, identity mode, consent and scoring are declared before launch — and locked at launch.'
		},
		{
			n: 'Field',
			body: 'While a wave is collecting, coverage is a live reading. Reporting thresholds are engraved on every gauge — below k, nothing is shown.'
		},
		{
			n: 'Evidence',
			body: 'Results arrive citable: versioned scores, wave comparison, and analysis-ready exports with a codebook.'
		}
	];
</script>

<div class="page">
	<header class="top">
		<span class="brand eyebrow">Spectra</span>
		<nav>
			<a class="btn btn-ghost" href="/signin">Sign in</a>
			<a class="btn btn-ink" href="/register">Create a workspace</a>
		</nav>
	</header>

	<main>
		<section class="hero">
			<p class="eyebrow eyebrow-stain">Validated-instrument studies</p>
			<h1 class="doc-title">Measurement you<br />can defend.</h1>
			<p class="lede">
				Spectra runs psychometric instruments as rigorous studies — anonymous longitudinal
				linking, k-protected reporting, and exports your statistician will not have to fix.
			</p>

			<!-- The instrument: a calibrated rail reading a study across waves -->
			<figure class="reading" aria-hidden="true">
				<div class="scale rail-h"></div>
				<div class="needle" style="left: 62%">
					<span class="needle-line"></span>
					<span class="needle-datum datum">wave 2 · 148 responses</span>
				</div>
				<div class="pip" style="left: 18%"></div>
				<div class="pip live" style="left: 62%"></div>
				<div class="pip hollow" style="left: 88%"></div>
				<span class="datum caption-l">wave 1</span>
				<span class="datum caption-r">wave 3 · planned</span>
			</figure>
		</section>

		<section class="claims">
			{#each claims as claim (claim.n)}
				<article>
					<h2 class="eyebrow">{claim.n}</h2>
					<p>{claim.body}</p>
				</article>
			{/each}
		</section>
	</main>

	<footer>
		<span class="datum">EU-hosted · GDPR-native · tenant-isolated</span>
	</footer>
</div>

<style>
	.page {
		max-width: 68rem;
		margin: 0 auto;
		padding: 1.5rem 2rem 3rem;
		display: flex;
		flex-direction: column;
		min-height: 100dvh;
	}

	.top {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.brand {
		color: var(--color-ink);
		font-size: 0.875rem;
	}

	.top nav {
		display: flex;
		gap: 0.625rem;
	}

	.hero {
		padding: 5.5rem 0 4rem;
		max-width: 46rem;
	}

	.hero h1 {
		font-size: clamp(2.6rem, 6vw, 4.2rem);
		line-height: 1.02;
		margin: 1rem 0 1.25rem;
	}

	.lede {
		font-size: 1.0625rem;
		line-height: 1.6;
		color: var(--color-ink-2);
		max-width: 38rem;
	}

	.reading {
		position: relative;
		margin-top: 4rem;
		height: 4.5rem;
	}

	.scale {
		position: absolute;
		top: 1.5rem;
		left: 0;
		right: 0;
		height: 10px;
		--rail-pitch: 9px;
		--rail-ink: var(--color-line-2);
	}

	.pip {
		position: absolute;
		top: 1.35rem;
		width: 10px;
		height: 10px;
		border-radius: 999px;
		background: var(--color-ink);
		transform: translateX(-50%);
	}

	.pip.hollow {
		background: var(--color-ground);
		border: 2px solid var(--color-ink);
	}

	.pip.live {
		background: var(--color-stain);
		box-shadow: 0 0 0 5px var(--color-stain-wash);
	}

	.needle {
		position: absolute;
		top: 0;
		transform: translateX(-50%);
		display: flex;
		flex-direction: column;
		align-items: center;
	}

	.needle-line {
		width: 2px;
		height: 1.25rem;
		background: var(--color-stain);
	}

	.needle-datum {
		position: absolute;
		top: 2.9rem;
		white-space: nowrap;
		font-size: 0.75rem;
		color: var(--color-stain);
	}

	.caption-l,
	.caption-r {
		position: absolute;
		top: 2.9rem;
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	.caption-l {
		left: 18%;
		transform: translateX(-50%);
	}

	.caption-r {
		left: 88%;
		transform: translateX(-50%);
	}

	.claims {
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		gap: 2.5rem;
		padding: 3rem 0;
		border-top: 1px solid var(--color-line);
	}

	.claims p {
		margin-top: 0.625rem;
		font-size: 0.9375rem;
		line-height: 1.55;
		color: var(--color-ink-2);
	}

	footer {
		margin-top: auto;
		padding-top: 3rem;
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	@media (max-width: 44rem) {
		.claims {
			grid-template-columns: 1fr;
		}

		.hero {
			padding-top: 3.5rem;
		}
	}
</style>
