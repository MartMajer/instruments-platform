<script lang="ts">
	import { onMount } from 'svelte';

	let root = $state<HTMLElement | null>(null);

	// Scroll-in effects ported from the approved landing design:
	// fill (meters grow), draw (rail reveals), count (numbers count up).
	onMount(() => {
		if (!root || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

		const ease = 'cubic-bezier(0.22, 1, 0.36, 1)';
		const run = (el: HTMLElement) => {
			const kind = el.getAttribute('data-fx');
			if (kind === 'fill') {
				const width = getComputedStyle(el).width;
				el.animate([{ width: '0px' }, { width }], { duration: 950, easing: ease });
			} else if (kind === 'draw') {
				el.animate(
					[{ clipPath: 'inset(-16px 100% -48px 0)' }, { clipPath: 'inset(-16px 0% -48px 0)' }],
					{ duration: 1250, easing: ease }
				);
			} else if (kind === 'count') {
				const original = el.textContent ?? '';
				const t0 = performance.now();
				const duration = 1100;
				const step = (t: number) => {
					const p = Math.min(1, (t - t0) / duration);
					const q = 1 - Math.pow(1 - p, 3);
					el.textContent = original.replace(/\d+/g, (m) => String(Math.round(parseInt(m, 10) * q)));
					if (p < 1) requestAnimationFrame(step);
					else el.textContent = original;
				};
				requestAnimationFrame(step);
			}
		};

		const io = new IntersectionObserver(
			(entries) => {
				for (const entry of entries) {
					if (entry.isIntersecting) {
						io.unobserve(entry.target);
						run(entry.target as HTMLElement);
					}
				}
			},
			{ threshold: 0.3 }
		);
		root.querySelectorAll('[data-fx]').forEach((el) => io.observe(el));

		return () => io.disconnect();
	});
</script>

<svelte:head>
	<title>ValidatedScale — Measurement you can defend</title>
	<meta
		name="description"
		content="ValidatedScale runs your validated instruments as rigorous studies: locked protocols, live field monitoring under a k-anonymity threshold, and evidence that is ready for analysis."
	/>
</svelte:head>

<div class="page" bind:this={root}>
	<header class="topbar">
		<div class="topbar-inner">
			<span class="brand">
				<img src="/logo.svg" alt="" class="brand-logo" />
				<span class="eyebrow brand-word">ValidatedScale</span>
			</span>
			<nav>
				<a href="#protocol">Protocol</a>
				<a href="#field">Field</a>
				<a href="#evidence">Evidence</a>
				<a href="#pricing">Pricing</a>
				<a href="/signin" class="signin">Sign in</a>
				<a href="/register" class="btn btn-stain cta-small">Request access</a>
			</nav>
		</div>
	</header>

	<!-- ============ Hero ============ -->
	<div class="hero-wrap">
		<div aria-hidden="true" class="rail-v hero-ruler"></div>
		<span aria-hidden="true" class="datum hero-calib">CALIBRATED · V1</span>

		<section class="hero">
			<span class="eyebrow eyebrow-stain">Validated-instrument-first study platform</span>
			<h1 class="doc-title">Measurement you can defend.</h1>
			<p class="lede">
				ValidatedScale runs your validated instruments as rigorous studies. The protocol locks
				at launch, the field is monitored against a k-anonymity threshold, and the evidence
				exports arrive ready for analysis, with a codebook your statistician won't have to fix.
			</p>
			<div class="hero-actions">
				<a href="/register" class="btn btn-stain">Start a study</a>
				<a href="#protocol" class="btn btn-ghost">How it works</a>
				<span class="datum hero-note">EU-hosted · GDPR-native</span>
			</div>

			<!-- Hero device: the calibrated rail -->
			<div class="panel hero-device">
				<div class="device-head">
					<div>
						<span class="eyebrow">Study timeline</span>
						<div class="doc-title device-title">Nurse workload · longitudinal, anonymous</div>
					</div>
					<span class="chip chip-live"><span class="live-dot"></span>Collecting</span>
				</div>

				<div class="rail-h wave-rail" data-fx="draw">
					<div class="wave" style="left: 10%">
						<span class="datum wave-date">Sep 2025</span>
						<span class="wave-name">Wave 1</span>
						<span class="pip done"></span>
						<span class="datum wave-n">n = 412</span>
					</div>
					<div class="wave" style="left: 50%">
						<span class="datum wave-date">Jan 2026</span>
						<span class="wave-name">Wave 2</span>
						<span class="pip done"></span>
						<span class="datum wave-n">n = 398</span>
					</div>
					<div class="wave" style="left: 90%">
						<span class="datum wave-date">Jul 2026</span>
						<span class="wave-name live-name">Wave 3</span>
						<span class="pip live"></span>
						<span class="datum wave-n live-n">collecting</span>
					</div>
				</div>

				<div class="hero-coverage">
					<span class="eyebrow">Coverage</span>
					<div class="meter">
						<div class="meter-fill" data-fx="fill"></div>
						<div class="meter-k"></div>
						<span class="datum meter-k-label">k = 5</span>
					</div>
					<span class="meter-verdict">
						Reportable: <span class="datum" data-fx="count">12 of 14</span> units above threshold
					</span>
				</div>
			</div>
		</section>
	</div>

	<!-- ============ Trust strip ============ -->
	<section class="trust">
		<div class="trust-grid">
			<div>
				<span class="eyebrow">EU-hosted</span>
				<p>Data is stored and processed in the EU. No transfer clauses to negotiate.</p>
			</div>
			<div>
				<span class="eyebrow">GDPR-native</span>
				<p>Purpose, retention, and lawful basis are declared in the protocol, not in a PDF afterthought.</p>
			</div>
			<div>
				<span class="eyebrow">k-anonymity</span>
				<p>Every report enforces a minimum group size. Below threshold, values are suppressed and say so.</p>
			</div>
			<div>
				<span class="eyebrow">Consent records</span>
				<p>Versioned consent text and timestamped agreement, ready for your IRB or ethics board.</p>
			</div>
		</div>
	</section>

	<!-- ============ Product shot ============ -->
	<section class="shot" aria-label="The product">
		<div class="shot-inner">
			<div class="browser">
				<div class="browser-bar">
					<span class="dot"></span><span class="dot"></span><span class="dot"></span>
					<span class="datum browser-url">validatedscale.com/app</span>
				</div>
				<img
					src="/marketing/protocol.png"
					alt="A study protocol in ValidatedScale: numbered chapters for design, instrument, scoring and policies, with the launch check in the margin"
					loading="lazy"
					width="1440"
					height="860"
				/>
			</div>
			<div class="phone">
				<img
					src="/marketing/respondent.png"
					alt="The respondent's consent sheet on a phone: study title, plain-language consent, participant code, and a Begin button"
					loading="lazy"
					width="390"
					height="780"
				/>
			</div>
		</div>
		<p class="shot-caption datum">The protocol a researcher writes, and the sheet a respondent sees. Synthetic data.</p>
	</section>

	<!-- ============ Works with your stack ============ -->
	<section class="stack" aria-label="Integrations">
		<span class="eyebrow">Works with what you already use</span>
		<div class="stack-grid">
			<div class="stack-tile">
				<span class="stack-name">Microsoft Entra ID</span>
				<p>Your people sign in with their workplace account. No new passwords to manage.</p>
			</div>
			<div class="stack-tile">
				<span class="stack-name">Microsoft 365 directory</span>
				<p>Import people and groups from your organization's directory, with admin consent.</p>
			</div>
			<div class="stack-tile">
				<span class="stack-name">Email invitations</span>
				<p>Invitations and reminders from a verified sender domain, with delivery tracking.</p>
			</div>
			<div class="stack-tile">
				<span class="stack-name">CSV import</span>
				<p>Bring a cohort from any spreadsheet. Preview first, import when the rows are clean.</p>
			</div>
			<div class="stack-tile">
				<span class="stack-name">SPSS · R · Stata</span>
				<p>Exports ship with a codebook that documents every variable, so any stats tool reads them.</p>
			</div>
			<div class="stack-tile">
				<span class="stack-name">Your questionnaire</span>
				<p>Any instrument you hold the rights to imports as a first-class citizen.</p>
			</div>
		</div>
	</section>

	<!-- ============ 01 Protocol ============ -->
	<section id="protocol" class="split">
		<span aria-hidden="true" class="datum ghost-number">01</span>
		<div class="split-copy">
			<div class="ch-row"><span class="datum ch">01</span><span class="eyebrow">Protocol</span></div>
			<h2 class="doc-title">Declared once. Locked at launch.</h2>
			<p>
				A study begins as a document: the instrument you hold the rights to, the identity mode,
				the consent text, the scoring rules. When you launch, the protocol locks. What you
				report later is exactly what you declared.
			</p>
			<p>
				Anonymous longitudinal linking uses self-generated participant codes, so waves connect
				without anyone holding a name.
			</p>
		</div>
		<div class="panel spec">
			<div class="spec-head">
				<div class="doc-title spec-title">Nurse workload, wave 3</div>
				<span class="chip chip-stain">Locked at launch</span>
			</div>
			<div class="spec-rows">
				<div><span class="datum n">01</span><span class="k">Design</span><span>Longitudinal, 4 waves, 14 units</span></div>
				<div><span class="datum n">02</span><span class="k">Instrument</span><span>Tenant-provided, <span class="datum">22</span> items · <span class="datum">3</span> subscales · <span class="datum">v2.1</span></span></div>
				<div><span class="datum n">03</span><span class="k">Identity</span><span>Anonymous · self-generated codes</span></div>
				<div><span class="datum n">04</span><span class="k">Consent</span><span>Version <span class="datum">3</span> · ethics ref <span class="datum">2026-114</span></span></div>
				<div><span class="datum n">05</span><span class="k">Scoring</span><span>Subscale means · reverse-keys declared · k = <span class="datum">5</span></span></div>
			</div>
		</div>
	</section>

	<!-- ============ 02 Field (console band) ============ -->
	<section id="field" class="console-band">
		<div class="console-inner">
			<span aria-hidden="true" class="datum ghost-number ghost-light">02</span>
			<div class="ch-row"><span class="datum ch bright">02</span><span class="eyebrow dim">Field</span></div>
			<h2 class="doc-title console-h">Watch coverage, not people.</h2>
			<p class="console-lede">
				Share open links or send email invitations. The console shows what a defensible dataset needs:
				response counts, unit coverage against the k threshold, and nothing that identifies an
				individual.
			</p>

			<div class="board">
				<div class="tile accent">
					<span class="eyebrow dim">Responses</span>
					<div class="datum value" data-fx="count">412</div>
					<span class="sub">of ~560 invited</span>
				</div>
				<div class="tile">
					<span class="eyebrow dim">Units reportable</span>
					<div class="datum value" data-fx="count">12 / 14</div>
					<span class="sub">above k = 5</span>
				</div>
				<div class="tile">
					<span class="eyebrow dim">Median duration</span>
					<div class="datum value" data-fx="count">6m 40s</div>
					<span class="sub">complete responses</span>
				</div>
				<div class="tile">
					<span class="eyebrow dim">Field closes</span>
					<div class="datum value">18 Jul</div>
					<span class="sub">as declared in protocol</span>
				</div>
			</div>

			<div class="meters">
				<div class="row">
					<span class="unit">Ward B, nights</span>
					<div class="track"><div class="fill" data-fx="fill" style="width: 78%"></div><div class="k"></div></div>
					<span class="datum reading">n = 25 · reportable</span>
				</div>
				<div class="row">
					<span class="unit">Ward C, days</span>
					<div class="track"><div class="fill" data-fx="fill" style="width: 56%"></div><div class="k"></div></div>
					<span class="datum reading">n = 18 · reportable</span>
				</div>
				<div class="row">
					<span class="unit">Outpatient</span>
					<div class="track"><div class="fill suppressed"></div><div class="k"></div></div>
					<span class="datum reading">below k = 5</span>
				</div>
			</div>

			<div class="console-live">
				<span class="pulse-dot"></span>
				<span>Collecting. The console updates as responses arrive, and individuals never appear.</span>
			</div>
		</div>
	</section>

	<!-- ============ 03 Evidence ============ -->
	<section id="evidence" class="split evidence">
		<span aria-hidden="true" class="datum ghost-number ghost-right">03</span>
		<div class="panel results">
			<div class="doc-title spec-title">Subscale scores · wave 3</div>
			<div class="datum results-meta">n = 412 · 3 scores · 1 unit suppressed · k = 5 · scoring v2.1</div>
			<div class="results-table">
				<div class="thead">
					<span class="eyebrow">Subscale</span><span class="eyebrow num">n</span>
					<span class="eyebrow num">Score</span><span class="eyebrow num">Δ wave 2</span>
				</div>
				<div class="trow"><span>Demands</span><span class="datum num">412</span><span class="datum num">3.42</span><span class="datum num delta">+0.11</span></div>
				<div class="trow"><span>Control</span><span class="datum num">409</span><span class="datum num">3.87</span><span class="datum num delta">−0.04</span></div>
				<div class="trow"><span>Support</span><span class="datum num">411</span><span class="datum num">4.05</span><span class="datum num delta">+0.19</span></div>
				<div class="suppressed-note">Outpatient: suppressed, below reporting threshold (k = 5)</div>
			</div>
			<div class="results-files">
				<span class="datum file">scores_w3_v2.1.csv</span>
				<span class="datum file">codebook_w3.pdf</span>
				<span class="files-note">SPSS-friendly · labeled · versioned</span>
			</div>
		</div>
		<div class="split-copy">
			<div class="ch-row"><span class="datum ch">03</span><span class="eyebrow">Evidence</span></div>
			<h2 class="doc-title">Results your statistician can cite.</h2>
			<p>
				Scores are computed by the locked scoring rules and versioned. Re-open a study in two
				years and you get the same numbers. Waves compare side by side, linked by participant codes
				no one can reverse.
			</p>
			<p>
				Exports arrive ready for analysis: clean CSV, labels SPSS understands, and a codebook that
				documents every variable, recode, and suppression.
			</p>
		</div>
	</section>

	<!-- ============ Audiences ============ -->
	<section class="audiences">
		<span class="eyebrow">Who runs studies here</span>
		<div class="aud-rows">
			<div class="aud">
				<div class="aud-who">
					<div class="doc-title">Academic researchers</div>
					<div class="aud-sub">Occupational health · ergonomics · psychology</div>
				</div>
				<p>
					Anonymous longitudinal linking via self-generated codes, consent records your IRB will accept,
					and exports that go straight into analysis. EU hosting means no data-processing
					agreement odyssey.
				</p>
			</div>
			<div class="aud">
				<div class="aud-who">
					<div class="doc-title">OSH consultants</div>
					<div class="aud-sub">Psychosocial risk assessment · EU</div>
				</div>
				<p>
					White-label workspaces per client, legally mandated assessments run to protocol, and
					reports with the suppression rules written in, ready for compliance review and defensible in front
					of any works council.
				</p>
			</div>
			<div class="aud">
				<div class="aud-who">
					<div class="doc-title">Wellbeing officers</div>
					<div class="aud-sub">Hospitals · HR · continuous monitoring</div>
				</div>
				<p>
					Monitoring wave over wave, on dashboards where the protection is structural: nobody,
					including you, can re-identify an individual from a report.
				</p>
			</div>
		</div>

		<div class="more-aud">
			<span class="eyebrow">And anywhere questionnaires carry weight</span>
			<div class="more-grid">
				<div>
					<div class="doc-title more-t">Universities &amp; student affairs</div>
					<p>Student wellbeing pulses, course evaluations, PhD programme monitoring. The anonymity is structural, so students actually answer.</p>
				</div>
				<div>
					<div class="doc-title more-t">HR &amp; people teams</div>
					<p>Engagement and workload tracking employees can trust: nobody, including you, can drill a dashboard down to one person.</p>
				</div>
				<div>
					<div class="doc-title more-t">Healthcare quality teams</div>
					<p>Patient-experience and staff safety-climate surveys with consent records and retention rules an auditor will accept.</p>
				</div>
				<div>
					<div class="doc-title more-t">Public sector</div>
					<p>Workforce stress monitoring for police, emergency services and schools, with governance that survives procurement. EU-hosted.</p>
				</div>
				<div>
					<div class="doc-title more-t">Consultancies &amp; agencies</div>
					<p>Run client studies in white-label workspaces; deliver defensible reports instead of spreadsheets.</p>
				</div>
				<div>
					<div class="doc-title more-t">Any longitudinal study</div>
					<p>If you measure the same people twice and need to link answers without knowing names, this is the instrument for it.</p>
				</div>
			</div>
			<p class="more-note">Bring any questionnaire you have the right to use. The platform runs it with the same rigor.</p>
		</div>
	</section>

	<!-- ============ Respondent glimpse ============ -->
	<section class="respondent split">
		<div class="split-copy">
			<span class="eyebrow">The respondent's side</span>
			<h2 class="doc-title">Consent first. Five quiet minutes.</h2>
			<p>
				Respondents see one calm sheet: the study in plain words, consent before a single item,
				and a progress rail that tells the truth. No accounts, no app, no dark patterns to
				inflate completion.
			</p>
			<p>
				In anonymous studies they build their own participant code from stable personal facts.
				It links their waves without ever identifying them.
			</p>
		</div>
		<div class="panel sheet">
			<span class="eyebrow eyebrow-stain">Consent · v3</span>
			<div class="doc-title sheet-title">Nurse workload, wave 3</div>
			<p class="sheet-body">
				Your answers are anonymous. Results are only reported for groups of 5 or more. You can
				stop at any time; partial answers are discarded.
			</p>
			<div class="agree">
				<span class="check">✓</span>
				<span>I have read the above and agree to take part</span>
			</div>
			<div class="btn btn-stain begin">Begin · 22 items</div>
			<div class="sheet-progress">
				<div class="rail-h progress-rail"></div>
				<span class="datum">8 / 22</span>
			</div>
		</div>
	</section>

	<!-- ============ Security posture ============ -->
	<section class="posture" aria-label="Security and compliance">
		<span class="eyebrow">Security posture, in plain words</span>
		<h2 class="doc-title posture-h">No badges. Mechanisms.</h2>
		<div class="posture-grid">
			<div class="posture-tile">
				<span class="datum posture-k">EU</span>
				<span class="posture-name">Data residency</span>
				<p>Stored and processed in the EU, full stop.</p>
			</div>
			<div class="posture-tile">
				<span class="datum posture-k">RLS</span>
				<span class="posture-name">Tenant isolation</span>
				<p>Row-level security in the database, failing closed. One workspace can never read another.</p>
			</div>
			<div class="posture-tile">
				<span class="datum posture-k">k ≥ 5</span>
				<span class="posture-name">Anonymity floor</span>
				<p>The reporting threshold can be raised, never lowered. It is a constraint in the engine, not a setting.</p>
			</div>
			<div class="posture-tile">
				<span class="datum posture-k">v1 · v2</span>
				<span class="posture-name">Consent evidence</span>
				<p>Every consent text is versioned; every agreement is timestamped against its exact version.</p>
			</div>
			<div class="posture-tile">
				<span class="datum posture-k">2y →</span>
				<span class="posture-name">Retention, automated</span>
				<p>Each study declares how long data lives and what happens then. Anonymization is the default.</p>
			</div>
			<div class="posture-tile">
				<span class="datum posture-k">log</span>
				<span class="posture-name">Audit trail</span>
				<p>Who did what and when, recorded across setup, launch, collection and export.</p>
			</div>
			<div class="posture-tile">
				<span class="datum posture-k">Art. 17</span>
				<span class="posture-name">Right to erasure</span>
				<p>A built-in withdrawal workflow: requests are tracked, approved and executed, not emailed around.</p>
			</div>
			<div class="posture-tile">
				<span class="datum posture-k">DPA</span>
				<span class="posture-name">Paperwork</span>
				<p>A signed data-processing agreement and a methods description for your ethics board, on request.</p>
			</div>
		</div>
		<p class="posture-note">
			Formal certifications are on our roadmap. The mechanisms above are in the product today,
			and we would rather show you how they work than show you a logo.
		</p>
	</section>

	<!-- ============ Pricing ============ -->
	<section id="pricing" class="pricing">
		<span class="eyebrow">Pricing</span>
		<h2 class="doc-title">Priced for grants and mandates.</h2>
		<div class="price-grid">
			<div>
				<div class="doc-title tier">Pilot</div>
				<p>One study, one wave. For trying the method on a real question.</p>
				<span class="datum price">€ — / study</span>
			</div>
			<div>
				<div class="doc-title tier">Research group</div>
				<p>Longitudinal studies, collaborator seats, invoicing that fits grant budgets.</p>
				<span class="datum price">€ — / year</span>
			</div>
			<div>
				<div class="doc-title tier">Consultancy</div>
				<p>White-label client workspaces and reports that stand up to compliance review.</p>
				<span class="datum price">€ — / client</span>
			</div>
		</div>
		<p class="price-note">
			Pricing is being finalised with pilot partners, so
			<a href="/register">ask us for current numbers</a>.
		</p>
	</section>

	<!-- ============ CTA ============ -->
	<section id="contact" class="cta-section">
		<div class="panel cta">
			<div>
				<span class="eyebrow eyebrow-stain">For your next proposal</span>
				<h2 class="doc-title">Write us into your next grant.</h2>
				<p>
					We'll give you a description of the platform ready to paste into your methods section: data flows,
					anonymity model, consent handling, hosting. Plus a signed DPA and whatever your
					ethics board asks for.
				</p>
			</div>
			<div class="cta-actions">
				<a href="/register" class="btn btn-stain">Request access</a>
				<a href="mailto:majeric.martin@gmail.com?subject=ValidatedScale%20methods%20description" class="btn btn-ghost">
					Get the methods description
				</a>
				<span class="datum cta-note">Replies from the founders, usually within a day.</span>
			</div>
		</div>
	</section>

	<footer>
		<div class="footer-cols">
			<div class="footer-brand">
				<span class="eyebrow">ValidatedScale</span>
				<span class="tagline">Your validated instruments, run rigorously.</span>
			</div>
			<div>
				<span class="eyebrow footer-h">Product</span>
				<a href="#protocol">Protocol</a>
				<a href="#field">Field</a>
				<a href="#evidence">Evidence</a>
				<a href="#pricing">Pricing</a>
			</div>
			<div>
				<span class="eyebrow footer-h">Trust</span>
				<a href="#contact">Data processing agreement</a>
				<a href="#contact">Methods description</a>
				<a href="#contact">Ethics board support</a>
			</div>
			<div>
				<span class="eyebrow footer-h">Contact</span>
				<a href="/register">Request access</a>
				<a href="mailto:majeric.martin@gmail.com">Email the founders</a>
			</div>
		</div>
		<div class="footer-inner">
			<span class="datum footer-note">EU-hosted · GDPR · k-anonymity enforced</span>
		</div>
	</footer>
</div>

<style>
	.page {
		min-height: 100dvh;
		background: var(--color-ground);
		color: var(--color-ink);
	}

	/* topbar */
	.topbar {
		background: var(--color-ink);
	}

	.topbar-inner {
		max-width: 74rem;
		margin: 0 auto;
		padding: 0 1.5rem;
		height: 3.25rem;
		display: flex;
		align-items: center;
		gap: 2rem;
	}

	.brand {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
	}

	.brand-logo {
		width: 1.375rem;
		height: 1.375rem;
		border-radius: 4px;
	}

	.brand-word {
		color: #fff;
		letter-spacing: 0.18em;
	}

	.topbar nav {
		display: flex;
		gap: 1.5rem;
		margin-left: auto;
		align-items: center;
	}

	.topbar nav a {
		color: #c6cdd6;
		text-decoration: none;
		font-size: 0.875rem;
	}

	.topbar nav a:hover {
		color: #fff;
	}

	.topbar nav .signin {
		color: #fff;
		font-weight: 540;
	}

	.cta-small {
		padding: 0.5rem 0.875rem;
	}

	/* hero */
	.hero-wrap {
		position: relative;
	}

	.hero-ruler {
		position: absolute;
		top: 0;
		bottom: 0;
		right: max(1.5rem, calc((100% - 74rem) / 2 + 1.5rem));
		width: 6px;
		--rail-pitch: 14px;
		pointer-events: none;
		mask-image: linear-gradient(to bottom, #000 60%, transparent);
	}

	.hero-calib {
		position: absolute;
		top: 5rem;
		right: max(2.5rem, calc((100% - 74rem) / 2 + 2.5rem));
		font-size: 0.6875rem;
		color: var(--color-ink-3);
		writing-mode: vertical-rl;
		letter-spacing: 0.14em;
		pointer-events: none;
		user-select: none;
	}

	.hero {
		position: relative;
		max-width: 74rem;
		margin: 0 auto;
		padding: 5rem 1.5rem 3.5rem;
	}

	.hero h1 {
		font-size: clamp(2.5rem, 6vw, 3.25rem);
		line-height: 1.08;
		margin: 1rem 0 1.25rem;
		max-width: 44rem;
	}

	.lede {
		font-size: 1.125rem;
		line-height: 1.6;
		color: var(--color-ink-2);
		max-width: 40rem;
		margin-bottom: 2rem;
	}

	.hero-actions {
		display: flex;
		gap: 0.75rem;
		align-items: center;
		flex-wrap: wrap;
	}

	.hero-actions a {
		text-decoration: none;
	}

	.hero-note {
		font-size: 0.75rem;
		color: var(--color-ink-3);
		margin-left: 0.75rem;
	}

	.hero-device {
		margin-top: 3.5rem;
		padding: 2rem 2.5rem 1.75rem;
	}

	.device-head {
		display: flex;
		align-items: baseline;
		justify-content: space-between;
		gap: 1rem;
		flex-wrap: wrap;
	}

	.device-title {
		font-size: 1.125rem;
		margin-top: 0.25rem;
	}

	.live-dot {
		width: 6px;
		height: 6px;
		border-radius: 999px;
		background: var(--color-live);
		display: inline-block;
	}

	.wave-rail {
		--rail-pitch: 12px;
		position: relative;
		height: 5.25rem;
		margin-top: 2.25rem;
		background-size: 100% 10px;
	}

	.wave {
		position: absolute;
		bottom: 2px;
		transform: translateX(-50%);
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.4375rem;
	}

	.wave-date {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
		white-space: nowrap;
	}

	.wave-name {
		font-size: 0.8125rem;
		font-weight: 560;
		white-space: nowrap;
	}

	.live-name {
		color: var(--color-stain);
	}

	.pip {
		width: 11px;
		height: 11px;
		border-radius: 999px;
		border: 2px solid var(--color-surface);
	}

	.pip.done {
		background: var(--color-stain);
	}

	.pip.live {
		background: var(--color-stain-bright);
	}

	.wave-n {
		position: absolute;
		top: 100%;
		margin-top: 0.375rem;
		font-size: 0.6875rem;
		white-space: nowrap;
		color: var(--color-ink-3);
	}

	.live-n {
		color: var(--color-stain);
	}

	.hero-coverage {
		margin-top: 3.25rem;
		display: flex;
		align-items: center;
		gap: 1.25rem;
		flex-wrap: wrap;
	}

	.meter {
		flex: 1 1 16rem;
		position: relative;
		height: 10px;
		background: var(--color-sunk);
		border-radius: 2px;
	}

	.meter-fill {
		position: absolute;
		inset: 0 auto 0 0;
		width: 72%;
		background: var(--color-chart-violet);
		border-radius: 2px 0 0 2px;
	}

	.meter-k {
		position: absolute;
		top: -5px;
		bottom: -5px;
		left: 40%;
		width: 2px;
		background: var(--color-ink);
	}

	.meter-k-label {
		position: absolute;
		top: -1.375rem;
		left: 40%;
		transform: translateX(-50%);
		font-size: 0.6875rem;
		color: var(--color-ink-2);
	}

	.meter-verdict {
		font-size: 0.8125rem;
		color: var(--color-ink-2);
	}

	/* trust strip */
	.trust {
		max-width: 74rem;
		margin: 0 auto;
		padding: 1.5rem 1.5rem 4rem;
	}

	.trust-grid {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		border-top: 1px solid var(--color-line);
	}

	.trust-grid div {
		padding: 1.5rem 1.5rem 0;
	}

	.trust-grid div:first-child {
		padding-left: 0;
	}

	.trust-grid div + div {
		border-left: 1px solid var(--color-line);
	}

	.trust-grid p {
		margin-top: 0.5rem;
		font-size: 0.875rem;
		line-height: 1.55;
		color: var(--color-ink-2);
	}

	/* product shot */
	.shot {
		max-width: 74rem;
		margin: 0 auto;
		padding: 0 1.5rem 4rem;
	}

	.shot-inner {
		position: relative;
	}

	.browser {
		border: 1px solid var(--color-line);
		border-radius: 8px;
		overflow: hidden;
		background: var(--color-surface);
		box-shadow: 0 24px 60px rgb(21 28 37 / 0.12);
	}

	.browser-bar {
		display: flex;
		align-items: center;
		gap: 0.375rem;
		padding: 0.625rem 0.875rem;
		border-bottom: 1px solid var(--color-line);
		background: var(--color-sunk);
	}

	.dot {
		width: 9px;
		height: 9px;
		border-radius: 999px;
		background: var(--color-line-2);
	}

	.browser-url {
		margin-left: 0.75rem;
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.browser img {
		display: block;
		width: 100%;
		height: auto;
	}

	.phone {
		position: absolute;
		right: 2.5rem;
		bottom: -2.5rem;
		width: clamp(11rem, 16vw, 14rem);
		border: 1px solid var(--color-line);
		border-radius: 14px;
		overflow: hidden;
		background: var(--color-surface);
		box-shadow: 0 18px 44px rgb(21 28 37 / 0.18);
	}

	.phone img {
		display: block;
		width: 100%;
		height: auto;
		aspect-ratio: 390 / 640;
		object-fit: cover;
		object-position: top;
	}

	.shot-caption {
		margin-top: 4rem;
		font-size: 0.6875rem;
		color: var(--color-ink-3);
		text-align: center;
	}

	@media (max-width: 44rem) {
		.phone {
			display: none;
		}

		.shot-caption {
			margin-top: 1rem;
		}
	}

	/* numbered splits */
	.split {
		position: relative;
		max-width: 74rem;
		margin: 0 auto;
		padding: 4rem 1.5rem;
		display: grid;
		grid-template-columns: 1fr 1.1fr;
		gap: 4rem;
		align-items: center;
	}

	.ghost-number {
		position: absolute;
		top: 0.5rem;
		left: 0.75rem;
		font-size: 11rem;
		line-height: 1;
		font-weight: 500;
		letter-spacing: -0.06em;
		color: rgb(69 48 166 / 0.055);
		pointer-events: none;
		user-select: none;
	}

	.ghost-right {
		left: auto;
		right: 0.75rem;
		top: 2rem;
	}

	.ghost-light {
		color: rgb(236 239 244 / 0.045);
		left: auto;
		right: 1.5rem;
		top: 2.5rem;
	}

	.ch-row {
		display: flex;
		align-items: baseline;
		gap: 0.75rem;
	}

	.ch {
		color: var(--color-stain);
		font-size: 0.875rem;
	}

	.ch.bright {
		color: var(--color-stain-bright);
	}

	.split-copy {
		position: relative;
	}

	.split-copy h2 {
		font-size: 2rem;
		line-height: 1.15;
		margin: 0.875rem 0 1rem;
	}

	.split-copy p {
		line-height: 1.65;
		color: var(--color-ink-2);
		margin-bottom: 1rem;
	}

	/* protocol spec card */
	.spec {
		padding: 1.75rem 2rem;
	}

	.spec-head {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		gap: 1rem;
		flex-wrap: wrap;
	}

	.spec-title {
		font-size: 1.125rem;
	}

	.spec-rows {
		margin-top: 1.25rem;
	}

	.spec-rows > div {
		display: flex;
		gap: 0.875rem;
		padding: 0.75rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.875rem;
	}

	.spec-rows > div:last-child {
		border-bottom: none;
	}

	.spec-rows .n {
		color: var(--color-stain);
		font-size: 0.75rem;
		width: 1.25rem;
		flex-shrink: 0;
	}

	.spec-rows .k {
		color: var(--color-ink-3);
		width: 6.5rem;
		flex-shrink: 0;
	}

	/* field console band */
	.console-band {
		background: var(--color-console);
	}

	.console-inner {
		position: relative;
		max-width: 74rem;
		margin: 0 auto;
		padding: 4.5rem 1.5rem;
	}

	.eyebrow.dim {
		color: var(--color-console-dim);
	}

	.console-h {
		font-size: 2rem;
		line-height: 1.15;
		margin: 0.875rem 0 1rem;
		color: var(--color-console-ink);
	}

	.console-lede {
		line-height: 1.65;
		color: var(--color-console-dim);
		margin-bottom: 2.5rem;
		max-width: 38rem;
	}

	.board {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 1px;
		background: var(--color-console-line);
		border: 1px solid var(--color-console-line);
	}

	.tile {
		background: var(--color-console-2);
		padding: 1.25rem 1.5rem;
		border-top: 3px solid transparent;
	}

	.tile.accent {
		border-top-color: var(--color-chart-violet-dark);
	}

	.tile .value {
		font-size: 2rem;
		color: var(--color-console-ink);
		margin-top: 0.375rem;
	}

	.tile .sub {
		font-size: 0.75rem;
		color: var(--color-console-dim);
	}

	.meters {
		margin-top: 2.5rem;
		display: grid;
		gap: 1.125rem;
		max-width: 44rem;
	}

	.meters .row {
		display: flex;
		align-items: center;
		gap: 1rem;
	}

	.meters .unit {
		font-size: 0.8125rem;
		color: var(--color-console-ink);
		width: 8.5rem;
		flex: 0 0 auto;
	}

	.meters .track {
		flex: 1;
		position: relative;
		height: 8px;
		background: var(--color-console-3);
	}

	.meters .fill {
		position: absolute;
		inset: 0 auto 0 0;
		background: var(--color-chart-violet-dark);
	}

	.meters .fill.suppressed {
		width: 12%;
		border: 1px dashed var(--color-console-dim);
		background: transparent;
	}

	.meters .k {
		position: absolute;
		top: -4px;
		bottom: -4px;
		left: 31%;
		width: 2px;
		background: var(--color-console-ink);
	}

	.meters .reading {
		font-size: 0.75rem;
		color: var(--color-console-dim);
		width: 8rem;
		flex: 0 0 auto;
	}

	.console-live {
		margin-top: 2rem;
		display: flex;
		align-items: center;
		gap: 0.625rem;
		font-size: 0.8125rem;
		color: var(--color-console-dim);
	}

	.pulse-dot {
		width: 9px;
		height: 9px;
		border-radius: 999px;
		background: var(--color-stain-bright);
	}

	@media (prefers-reduced-motion: no-preference) {
		.pulse-dot,
		.pip.live {
			animation: vs-pulse 2.2s ease-out infinite;
		}
	}

	@keyframes vs-pulse {
		0% {
			box-shadow: 0 0 0 0 rgb(156 139 245 / 0.55);
		}
		70% {
			box-shadow: 0 0 0 10px rgb(156 139 245 / 0);
		}
		100% {
			box-shadow: 0 0 0 0 rgb(156 139 245 / 0);
		}
	}

	/* evidence */
	.split.evidence {
		grid-template-columns: 1.1fr 1fr;
		padding-top: 4.5rem;
		padding-bottom: 4.5rem;
	}

	.results {
		padding: 1.75rem 2rem;
	}

	.results-meta {
		font-size: 0.75rem;
		color: var(--color-ink-3);
		margin-top: 0.375rem;
	}

	.results-table {
		margin-top: 1.25rem;
	}

	.results-table .thead,
	.results-table .trow {
		display: grid;
		grid-template-columns: 1fr 4rem 4.5rem 5rem;
		gap: 0.75rem;
		align-items: center;
	}

	.results-table .thead {
		padding-bottom: 0.5rem;
		border-bottom: 1px solid var(--color-line);
	}

	.results-table .thead .eyebrow {
		font-size: 0.625rem;
	}

	.results-table .trow {
		padding: 0.75rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.875rem;
	}

	.results-table .num {
		text-align: right;
		font-size: 0.8125rem;
	}

	.results-table .delta {
		color: var(--color-ink-2);
	}

	.suppressed-note {
		padding: 0.75rem 0 0.25rem;
		font-size: 0.8125rem;
		font-style: italic;
		color: var(--color-ink-3);
	}

	.results-files {
		margin-top: 1.25rem;
		padding-top: 1rem;
		border-top: 1px solid var(--color-line);
		display: flex;
		gap: 0.5rem;
		align-items: center;
		flex-wrap: wrap;
	}

	.file {
		font-size: 0.75rem;
		color: var(--color-ink-2);
		background: var(--color-sunk);
		padding: 0.3125rem 0.5rem;
		border-radius: 2px;
	}

	.files-note {
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	/* audiences */
	.audiences {
		max-width: 74rem;
		margin: 0 auto;
		padding: 2rem 1.5rem 4.5rem;
	}

	.aud-rows {
		margin-top: 1rem;
		border-top: 1px solid var(--color-line);
	}

	.aud {
		display: flex;
		align-items: baseline;
		gap: 2rem;
		padding: 1.75rem 0;
		border-bottom: 1px solid var(--color-line);
		flex-wrap: wrap;
	}

	.aud-who {
		flex: 1 1 24rem;
	}

	.aud-who .doc-title {
		font-size: 1.375rem;
	}

	.aud-sub {
		font-size: 0.875rem;
		color: var(--color-ink-3);
		margin-top: 0.375rem;
	}

	.aud p {
		flex: 2 1 24rem;
		margin: 0;
		font-size: 0.9375rem;
		line-height: 1.6;
		color: var(--color-ink-2);
	}

	.more-aud {
		margin-top: 3rem;
	}

	.more-grid {
		margin-top: 1.25rem;
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		gap: 2rem 2.5rem;
	}

	.more-t {
		font-size: 1.0625rem;
	}

	.more-grid p {
		margin-top: 0.375rem;
		font-size: 0.875rem;
		line-height: 1.55;
		color: var(--color-ink-2);
	}

	.more-note {
		margin-top: 2rem;
		font-size: 0.875rem;
		color: var(--color-ink-3);
		border-top: 1px solid var(--color-line);
		padding-top: 1.25rem;
	}

	@media (max-width: 62rem) {
		.more-grid {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	@media (max-width: 44rem) {
		.more-grid {
			grid-template-columns: 1fr;
		}
	}

	/* respondent glimpse */
	.respondent {
		grid-template-columns: 1fr 24rem;
		padding-top: 2rem;
		padding-bottom: 4.5rem;
	}

	.sheet {
		padding: 1.75rem 1.75rem 1.5rem;
	}

	.sheet-title {
		font-size: 1.25rem;
		margin-top: 0.5rem;
	}

	.sheet-body {
		font-size: 0.8125rem;
		line-height: 1.6;
		color: var(--color-ink-2);
		margin: 0.75rem 0 1rem;
	}

	.agree {
		display: flex;
		align-items: center;
		gap: 0.625rem;
		padding: 0.75rem 0.875rem;
		background: var(--color-stain-wash);
		border: 1px solid var(--color-stain-line);
		border-radius: var(--radius-instrument);
		font-size: 0.8125rem;
		color: var(--color-stain);
		font-weight: 560;
	}

	.check {
		width: 16px;
		height: 16px;
		border-radius: 3px;
		background: var(--color-stain);
		color: #fff;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-size: 0.6875rem;
		flex-shrink: 0;
	}

	.begin {
		width: 100%;
		justify-content: center;
		margin-top: 1rem;
		padding: 0.8125rem 1rem;
		cursor: default;
	}

	.sheet-progress {
		margin-top: 1.25rem;
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.sheet-progress .datum {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.progress-rail {
		--rail-pitch: 6px;
		--rail-ink: var(--color-stain);
		flex: 1;
		height: 6px;
		background-size: 38% 6px;
	}

	/* pricing */
	.pricing {
		max-width: 74rem;
		margin: 0 auto;
		padding: 2rem 1.5rem 4.5rem;
	}

	.pricing h2 {
		font-size: 2rem;
		margin: 0.875rem 0 1.5rem;
	}

	.price-grid {
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		border-top: 1px solid var(--color-line);
	}

	.price-grid > div {
		padding: 1.5rem 1.5rem 0;
	}

	.price-grid > div:first-child {
		padding-left: 0;
	}

	.price-grid > div + div {
		border-left: 1px solid var(--color-line);
	}

	.tier {
		font-size: 1.125rem;
	}

	.price-grid p {
		font-size: 0.875rem;
		color: var(--color-ink-2);
		margin: 0.5rem 0 0.75rem;
		line-height: 1.55;
	}

	.price {
		font-size: 0.875rem;
		color: var(--color-ink-3);
	}

	.price-note {
		font-size: 0.8125rem;
		color: var(--color-ink-3);
		margin-top: 1.25rem;
	}

	.price-note a {
		color: var(--color-stain);
	}

	/* CTA */
	.cta-section {
		max-width: 74rem;
		margin: 0 auto;
		padding: 0 1.5rem 5rem;
	}

	.cta {
		padding: 3rem;
		display: grid;
		grid-template-columns: 1.4fr 1fr;
		gap: 3rem;
		align-items: center;
	}

	.cta h2 {
		font-size: 2.25rem;
		line-height: 1.12;
		margin: 0.875rem 0 1rem;
	}

	.cta p {
		line-height: 1.65;
		color: var(--color-ink-2);
	}

	.cta-actions {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		align-items: stretch;
	}

	.cta-actions a {
		justify-content: center;
		text-decoration: none;
		padding: 0.8125rem 1rem;
	}

	.cta-note {
		font-size: 0.75rem;
		color: var(--color-ink-3);
		text-align: center;
	}

	/* stack + posture */
	.stack {
		max-width: 74rem;
		margin: 0 auto;
		padding: 0 1.5rem 4rem;
	}

	.stack-grid {
		margin-top: 1.25rem;
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		gap: 1px;
		background: var(--color-line);
		border: 1px solid var(--color-line);
		border-radius: var(--radius-instrument);
		overflow: hidden;
	}

	.stack-tile {
		background: var(--color-surface);
		padding: 1.25rem 1.5rem;
	}

	.stack-name {
		font-weight: 600;
		font-size: 0.9375rem;
	}

	.stack-tile p {
		margin-top: 0.375rem;
		font-size: 0.8125rem;
		line-height: 1.55;
		color: var(--color-ink-2);
	}

	.posture {
		max-width: 74rem;
		margin: 0 auto;
		padding: 2rem 1.5rem 4.5rem;
	}

	.posture-h {
		font-size: 2rem;
		margin: 0.875rem 0 1.5rem;
	}

	.posture-grid {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 2rem 2.5rem;
		border-top: 1px solid var(--color-line);
		padding-top: 2rem;
	}

	.posture-k {
		display: inline-block;
		font-size: 0.8125rem;
		color: var(--color-stain);
		border: 1px solid var(--color-stain-line);
		border-radius: 3px;
		padding: 0.1875rem 0.4375rem;
		background: var(--color-stain-wash);
	}

	.posture-name {
		display: block;
		margin-top: 0.625rem;
		font-weight: 600;
		font-size: 0.9375rem;
	}

	.posture-tile p {
		margin-top: 0.375rem;
		font-size: 0.8125rem;
		line-height: 1.55;
		color: var(--color-ink-2);
	}

	.posture-note {
		margin-top: 2.25rem;
		font-size: 0.875rem;
		color: var(--color-ink-3);
		max-width: 56ch;
	}

	.footer-cols {
		max-width: 74rem;
		margin: 0 auto;
		padding: 2.5rem 1.5rem 1rem;
		display: grid;
		grid-template-columns: 2fr 1fr 1fr 1fr;
		gap: 2rem;
	}

	.footer-brand {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.footer-cols a {
		display: block;
		font-size: 0.875rem;
		color: var(--color-ink-2);
		text-decoration: none;
		padding: 0.1875rem 0;
	}

	.footer-cols a:hover {
		color: var(--color-stain);
	}

	.footer-h {
		display: block;
		margin-bottom: 0.5rem;
	}

	@media (max-width: 62rem) {
		.stack-grid {
			grid-template-columns: repeat(2, 1fr);
		}

		.posture-grid {
			grid-template-columns: repeat(2, 1fr);
		}

		.footer-cols {
			grid-template-columns: 1fr 1fr;
		}
	}

	@media (max-width: 44rem) {
		.stack-grid,
		.posture-grid {
			grid-template-columns: 1fr;
		}
	}

	/* footer */
	footer {
		border-top: 1px solid var(--color-line);
	}

	.footer-inner {
		max-width: 74rem;
		margin: 0 auto;
		padding: 2rem 1.5rem;
		display: flex;
		align-items: baseline;
		gap: 2rem;
		flex-wrap: wrap;
	}

	.tagline {
		font-size: 0.8125rem;
		color: var(--color-ink-3);
	}

	.footer-note {
		margin-left: auto;
		font-size: 0.75rem;
		color: var(--color-ink-3);
	}

	/* responsive */
	@media (max-width: 62rem) {
		.split,
		.split.evidence,
		.respondent,
		.cta {
			grid-template-columns: 1fr;
			gap: 2rem;
		}

		.board,
		.trust-grid {
			grid-template-columns: repeat(2, 1fr);
		}

		.trust-grid div,
		.price-grid > div {
			border-left: none !important;
			padding-left: 0;
		}

		.ghost-number {
			font-size: 7rem;
		}
	}

	@media (max-width: 44rem) {
		.topbar nav a:not(.signin):not(.cta-small) {
			display: none;
		}

		.board,
		.trust-grid,
		.price-grid {
			grid-template-columns: 1fr;
		}

		.hero-device {
			padding: 1.5rem 1.25rem;
		}

		.meters .row {
			flex-wrap: wrap;
		}

		.hero-ruler,
		.hero-calib {
			display: none;
		}
	}
</style>
