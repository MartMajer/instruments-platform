<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { ApiError } from '$lib/api/client';
	import {
		createSetupApi,
		type OpenLinkEntryResponse,
		type RespondentQuestionResponse,
		type ResponseSessionResponse
	} from '$lib/api/setup';
	import { api } from '$lib/core/client';
	import { deserializeAnswer, isAnswered, serializeAnswer } from '$lib/respondent/answers';
	import { respondentCopy, respondentLocale } from '$lib/respondent/copy';
	import QuestionField from '$lib/respondent/QuestionField.svelte';

	const setup = createSetupApi(api());
	const token = $derived(page.params.token!);

	let phase = $state<'loading' | 'gone' | 'closed' | 'consent' | 'survey' | 'submitted'>('loading');
	let entry = $state<OpenLinkEntryResponse | null>(null);
	let session = $state<ResponseSessionResponse | null>(null);
	let identifiedEntry = $state(false);

	let consentAccepted = $state(false);
	let participantCode = $state('');
	let startError = $state<string | null>(null);
	let starting = $state(false);

	let answers = $state<Record<string, unknown>>({});
	let saveStatus = $state<'idle' | 'saving' | 'saved' | 'failed'>('idle');
	let submitError = $state<string | null>(null);
	let submitting = $state(false);
	let showMissing = $state(false);
	let startedAtMs = 0;
	let saveTimer: ReturnType<typeof setTimeout> | null = null;
	let dirty = $state(false);

	const localeOverride = $derived(page.url.searchParams.get('locale'));
	const locale = $derived(respondentLocale(entry?.defaultLocale ?? 'en', localeOverride));
	const copy = $derived(respondentCopy[locale]);

	const questions = $derived(
		(entry?.questions ?? []).slice().sort((a, b) => a.ordinal - b.ordinal)
	);

	const answeredCount = $derived(
		questions.filter((question) => isAnswered(question, answers[question.id])).length
	);

	const missingRequired = $derived(
		questions.filter((question) => question.required && !isAnswered(question, answers[question.id]))
	);

	const sessionStorageKey = $derived(`spectra.respondent.${token}`);

	onMount(async () => {
		try {
			entry = await setup.getOpenLinkEntry(token);
		} catch (error) {
			if (error instanceof ApiError && error.status === 404) {
				try {
					entry = await setup.getIdentifiedEntry(token);
					identifiedEntry = true;
				} catch {
					phase = 'gone';
					return;
				}
			} else {
				phase = 'gone';
				return;
			}
		}

		if (entry && entry.status.toLowerCase() !== 'live') {
			phase = 'closed';
			return;
		}

		// resume a draft if this browser already started a session for this link
		const storedSessionId = localStorage.getItem(sessionStorageKey);
		if (storedSessionId && !identifiedEntry) {
			try {
				const draft = await setup.getOpenLinkSessionDraft(token, storedSessionId);
				if (draft.session.submittedAt) {
					phase = 'submitted';
					return;
				}
				session = draft.session;
				answers = Object.fromEntries(
					draft.answers.map((saved) => {
						const question = (entry?.questions ?? []).find((q) => q.id === saved.questionId);
						return [
							saved.questionId,
							question ? deserializeAnswer(question, saved.value) : (saved.value ?? '')
						];
					})
				);
				startedAtMs = Date.now();
				phase = 'survey';
				return;
			} catch {
				localStorage.removeItem(sessionStorageKey);
			}
		}

		phase = 'consent';
	});

	async function begin() {
		if (!entry || starting) return;
		starting = true;
		startError = null;

		try {
			const request = {
				locale: entry.defaultLocale,
				acceptedConsentDocumentId: entry.consentDocument.id,
				acceptedGrants: entry.consentDocument.requiredGrants,
				participantCode: entry.requiresParticipantCode ? participantCode.trim() : null
			};
			session = identifiedEntry
				? await setup.createIdentifiedEntrySession(token, request)
				: await setup.createOpenLinkSession(token, request);
			if (!identifiedEntry) {
				localStorage.setItem(sessionStorageKey, session.id);
			}
			startedAtMs = Date.now();
			phase = 'survey';
		} catch {
			startError = copy.startFailed;
		} finally {
			starting = false;
		}
	}

	function queueSave() {
		dirty = true;
		showMissing = false;
		if (saveTimer) clearTimeout(saveTimer);
		saveTimer = setTimeout(() => void save(), 900);
	}

	async function save(): Promise<boolean> {
		if (!session || !entry) return false;
		if (saveTimer) {
			clearTimeout(saveTimer);
			saveTimer = null;
		}

		saveStatus = 'saving';
		try {
			const payload = {
				answers: questions
					.filter((question) => isAnswered(question, answers[question.id]))
					.map((question) => ({
						questionId: question.id,
						value: serializeAnswer(question, answers[question.id]),
						isNa: answers[question.id] === 'NA' || undefined
					}))
			};
			if (identifiedEntry) {
				await setup.saveAnswers(session.id, payload);
			} else {
				await setup.saveOpenLinkAnswers(token, session.id, payload);
			}
			saveStatus = 'saved';
			dirty = false;
			return true;
		} catch {
			saveStatus = 'failed';
			return false;
		}
	}

	async function submit() {
		if (!session || submitting) return;

		if (missingRequired.length > 0) {
			showMissing = true;
			document.getElementById(`q-${missingRequired[0].id}`)?.scrollIntoView({
				behavior: 'smooth',
				block: 'center'
			});
			return;
		}

		submitting = true;
		submitError = null;

		try {
			const saved = await save();
			if (!saved) throw new Error('save-failed');
			const submitRequest = { timeTakenMs: Date.now() - startedAtMs };
			if (identifiedEntry) {
				await setup.submitResponseSession(session.id, submitRequest);
			} else {
				await setup.submitOpenLinkSession(token, session.id, submitRequest);
			}
			localStorage.removeItem(sessionStorageKey);
			phase = 'submitted';
		} catch {
			submitError = copy.submitFailed;
		} finally {
			submitting = false;
		}
	}

	function questionState(question: RespondentQuestionResponse): 'answered' | 'missing' | 'open' {
		if (isAnswered(question, answers[question.id])) return 'answered';
		if (showMissing && question.required) return 'missing';
		return 'open';
	}
</script>

<svelte:head><title>{entry?.name ?? 'Survey'} — Spectra</title></svelte:head>

<div class="canvas">
	{#if phase === 'loading'}
		<div class="notice" role="status">
			<span class="rail-h boot" aria-hidden="true"></span>
		</div>
	{:else if phase === 'gone'}
		<div class="notice">
			<p class="eyebrow">Spectra</p>
			<h1 class="doc-title">{copy.notFound}</h1>
		</div>
	{:else if phase === 'closed'}
		<div class="notice">
			<p class="eyebrow">Spectra</p>
			<h1 class="doc-title">{copy.notAvailable}</h1>
		</div>
	{:else if phase === 'consent' && entry}
		<article class="sheet">
			<p class="eyebrow study-kicker">{copy.invitedTo}</p>
			<h1 class="doc-title study-title">{entry.name}</h1>

			<section class="consent">
				<h2>{copy.consentTitle}</h2>
				<div class="consent-body">
					{#each entry.consentDocument.bodyMarkdown.split(/\n{2,}/) as block, i (i)}
						<p>{block.replace(/[#*_>]/g, '').trim()}</p>
					{/each}
				</div>
				<p class="datum consent-version">
					{entry.consentDocument.title} · v{entry.consentDocument.version}
				</p>

				<label class="agree" class:on={consentAccepted}>
					<input type="checkbox" bind:checked={consentAccepted} />
					<span>{copy.consentAgree}</span>
				</label>

				{#if entry.requiresParticipantCode}
					<div class="pcode">
						<label class="eyebrow" for="pcode">{copy.participantCodeLabel}</label>
						<input id="pcode" class="datum" bind:value={participantCode} autocomplete="off" />
						<p class="pcode-help">{copy.participantCodeHelp}</p>
					</div>
				{/if}

				{#if startError}<p class="error" role="alert">{startError}</p>{/if}

				<button
					class="btn btn-stain begin"
					disabled={!consentAccepted ||
						starting ||
						(entry.requiresParticipantCode && participantCode.trim().length === 0)}
					onclick={begin}
				>
					{starting ? '…' : copy.begin}
				</button>
				{#if !consentAccepted}
					<p class="consent-note">{copy.consentRequiredNote}</p>
				{/if}
			</section>
		</article>
	{:else if phase === 'survey' && entry}
		<div class="progress" role="progressbar" aria-valuenow={answeredCount} aria-valuemin={0} aria-valuemax={questions.length} aria-label={copy.progressLabel}>
			<div class="ticks">
				{#each questions as question (question.id)}
					<span class="tick" class:filled={isAnswered(question, answers[question.id])}></span>
				{/each}
			</div>
			<span class="datum progress-datum">{answeredCount}/{questions.length}</span>
			<span class="save-state" aria-live="polite">
				{#if saveStatus === 'saving'}{copy.saving}{:else if saveStatus === 'saved' && !dirty}{copy.saved}{/if}
			</span>
		</div>

		<article class="sheet">
			<h1 class="doc-title study-title small">{entry.name}</h1>

			<ol class="items">
				{#each questions as question, index (question.id)}
					<li id={`q-${question.id}`} class="item" class:missing={questionState(question) === 'missing'}>
						<div class="item-head">
							<span class="datum item-n">{String(index + 1).padStart(2, '0')}</span>
							<p class="item-text">
								{question.textDefault}
								{#if !question.required}
									<span class="optional">{copy.optionalHint}</span>
								{/if}
							</p>
						</div>
						<div class="item-field">
							<QuestionField
								{question}
								bind:value={
									() => answers[question.id],
									(next) => {
										answers[question.id] = next;
										queueSave();
									}
								}
								copy={{ naLabel: copy.naLabel, typeUnsupported: copy.typeUnsupported }}
							/>
						</div>
					</li>
				{/each}
			</ol>

			<footer class="submit-row">
				{#if showMissing && missingRequired.length > 0}
					<p class="error" role="alert">{copy.missingRequired}</p>
				{/if}
				{#if submitError}<p class="error" role="alert">{submitError}</p>{/if}
				<button class="btn btn-stain" disabled={submitting} onclick={submit}>
					{submitting ? copy.submitting : copy.submit}
				</button>
			</footer>
		</article>
	{:else if phase === 'submitted'}
		<div class="notice">
			<span class="done-mark" aria-hidden="true"></span>
			<h1 class="doc-title">{copy.thanksTitle}</h1>
			<p class="thanks-body">{copy.thanksBody}</p>
		</div>
	{/if}
</div>

<style>
	.canvas {
		min-height: 100dvh;
		padding: 1.25rem 1rem 5rem;
		max-width: 44rem;
		margin: 0 auto;
	}

	.notice {
		min-height: 70dvh;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 1rem;
		text-align: center;
	}

	.notice h1 {
		font-size: 1.5rem;
		max-width: 26ch;
	}

	.boot {
		width: 8rem;
		height: 7px;
		--rail-pitch: 7px;
	}

	.sheet {
		background: var(--color-surface);
		border: 1px solid var(--color-line);
		border-radius: var(--radius-instrument);
		border-top: 3px solid var(--color-stain);
		padding: clamp(1.25rem, 4vw, 2.5rem);
		margin-top: 1rem;
	}

	.study-kicker {
		color: var(--color-stain);
	}

	.study-title {
		font-size: clamp(1.5rem, 4.5vw, 2.125rem);
		margin-top: 0.5rem;
		line-height: 1.15;
	}

	.study-title.small {
		font-size: 1.25rem;
		margin-bottom: 1.5rem;
	}

	.consent {
		margin-top: 2rem;
	}

	.consent h2 {
		font-family: var(--font-doc);
		font-size: 1.125rem;
		font-weight: 580;
	}

	.consent-body {
		margin-top: 0.75rem;
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		font-size: 0.9375rem;
		line-height: 1.6;
		color: var(--color-ink-2);
	}

	.consent-version {
		margin-top: 0.75rem;
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.agree {
		display: flex;
		align-items: baseline;
		gap: 0.625rem;
		margin-top: 1.5rem;
		padding: 0.875rem 1rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		font-size: 0.9375rem;
		font-weight: 540;
		cursor: pointer;
	}

	.agree.on {
		border-color: var(--color-stain);
		background: var(--color-stain-wash);
	}

	.agree input {
		accent-color: var(--color-stain);
	}

	.pcode {
		margin-top: 1.25rem;
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
	}

	.pcode input {
		font-size: 1.125rem;
		letter-spacing: 0.08em;
		padding: 0.625rem 0.75rem;
		border: 1px solid var(--color-line-2);
		border-radius: var(--radius-instrument);
		max-width: 16rem;
		text-transform: uppercase;
	}

	.pcode-help {
		font-size: 0.8125rem;
		color: var(--color-ink-3);
		max-width: 44ch;
	}

	.begin {
		margin-top: 1.5rem;
		font-size: 1rem;
		padding: 0.875rem 2rem;
	}

	.consent-note {
		margin-top: 0.625rem;
		font-size: 0.8125rem;
		color: var(--color-ink-3);
	}

	/* progress rail: one fine tick per item */
	.progress {
		position: sticky;
		top: 0;
		z-index: 10;
		display: flex;
		align-items: center;
		gap: 0.75rem;
		padding: 0.75rem 0.25rem;
		background: color-mix(in oklab, var(--color-ground) 92%, transparent);
		backdrop-filter: blur(4px);
	}

	.ticks {
		display: flex;
		gap: 3px;
		flex: 1;
		min-width: 0;
	}

	.tick {
		flex: 1;
		height: 6px;
		background: var(--color-line);
		border-radius: 1px;
		transition: background-color 200ms ease;
	}

	.tick.filled {
		background: var(--color-stain);
	}

	.progress-datum {
		font-size: 0.75rem;
		color: var(--color-ink-2);
		white-space: nowrap;
	}

	.save-state {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
		min-width: 4rem;
		text-align: right;
	}

	.items {
		list-style: none;
		display: flex;
		flex-direction: column;
	}

	.item {
		padding: 1.5rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.item.missing {
		box-shadow: inset 3px 0 0 var(--color-danger);
		padding-left: 0.875rem;
	}

	.item-head {
		display: flex;
		gap: 0.75rem;
		align-items: baseline;
	}

	.item-n {
		font-size: 0.6875rem;
		color: var(--color-stain);
		flex-shrink: 0;
	}

	.item-text {
		font-size: 1rem;
		line-height: 1.5;
		font-weight: 520;
	}

	.optional {
		margin-left: 0.5rem;
		font-size: 0.6875rem;
		font-weight: 480;
		color: var(--color-ink-3);
		text-transform: uppercase;
		letter-spacing: 0.08em;
	}

	.item-field {
		margin-top: 0.875rem;
		padding-left: 1.75rem;
	}

	.submit-row {
		margin-top: 1.75rem;
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: 0.75rem;
	}

	.submit-row .btn {
		font-size: 1rem;
		padding: 0.875rem 2rem;
	}

	.error {
		font-size: 0.875rem;
		color: var(--color-danger);
	}

	.done-mark {
		width: 3rem;
		height: 3rem;
		border-radius: 999px;
		background: var(--color-stain);
		position: relative;
	}

	.done-mark::after {
		content: '';
		position: absolute;
		left: 1rem;
		top: 0.8rem;
		width: 0.9rem;
		height: 1.1rem;
		border-right: 3px solid #fff;
		border-bottom: 3px solid #fff;
		transform: rotate(40deg);
	}

	.thanks-body {
		color: var(--color-ink-2);
		max-width: 36ch;
	}

	@media (max-width: 30rem) {
		.item-field {
			padding-left: 0;
		}
	}
</style>
