<script lang="ts">
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { ArrowLeft, Check, LoaderCircle, RefreshCw, Send } from 'lucide-svelte';
	import { ApiError, createApiClient } from '$lib/api/client';
	import RespondentQuestionRunner from '$lib/respondent/RespondentQuestionRunner.svelte';
	import {
		createSetupApi,
		type CreateOpenLinkSessionRequest,
		type OpenLinkEntryResponse,
		type ResponseSessionResponse,
		type SavedAnswerResponse,
		type SaveAnswersResponse,
		type SubmitResponseSessionResponse
	} from '$lib/api/setup';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import { toRespondentReceiptView } from '$lib/respondent/receipt';
	import {
		isQuestionAnswered,
		isRespondentQuestionVisible,
		questionInputConstraints
	} from '$lib/respondent/respondent-question-model';

	const routeCredential = page.params.token ?? '';
	const api = createSetupApi(createApiClient());
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));

	type SaveStatus = 'idle' | 'dirty' | 'saving' | 'saved' | 'failed' | 'local-restored';

	type LocalUnsavedDraft = {
		version: 1;
		publicHandle: string;
		sessionId: string;
		assignmentId: string;
		updatedAt: string;
		entry: OpenLinkEntryResponse;
		answers: Record<string, string>;
	};

	let entry = $state<OpenLinkEntryResponse | null>(null);
	let session = $state<ResponseSessionResponse | null>(null);
	let savedAnswers = $state<SaveAnswersResponse | null>(null);
	let submitted = $state<SubmitResponseSessionResponse | null>(null);
	let answers = $state<Record<string, string>>({});
	let acceptedGrants = $state<Record<string, boolean>>({});
	let participantCode = $state('');
	let publicSessionHandle = $state<string | null>(
		isPublicSessionHandle(routeCredential) ? routeCredential : null
	);
	let loading = $state(true);
	let consentSubmitting = $state(false);
	let saving = $state(false);
	let submitting = $state(false);
	let reviewing = $state(false);
	let saveStatus = $state<SaveStatus>('idle');
	let loadError = $state<string | null>(null);
	let actionError = $state<string | null>(null);
	let autosaveTimer: ReturnType<typeof setTimeout> | null = null;
	let allConsentGrants = $derived(
		entry
			? Array.from(
					new Set([
						...entry.consentDocument.requiredGrants,
						...entry.consentDocument.optionalGrants
					])
				)
			: []
	);
	let requiredConsentAccepted = $derived(
		entry
			? entry.consentDocument.requiredGrants.every((grant) => acceptedGrants[grant])
			: false
	);
	let participantCodeReady = $derived(
		!entry?.requiresParticipantCode || participantCode.trim().length > 0
	);
	let saveStatusMessage = $derived(getSaveStatusMessage(saveStatus));
	let receiptView = $derived(
		entry && session && submitted
			? toRespondentReceiptView({ entry, session, savedAnswers, submitted })
			: null
	);

	onMount(() => {
		window.addEventListener('beforeunload', handleBeforeUnload);
		void loadEntry();

		return () => {
			window.removeEventListener('beforeunload', handleBeforeUnload);
			clearAutosaveTimer();
		};
	});

	async function loadEntry() {
		loading = true;
		loadError = null;
		actionError = null;

		if (routeCredential.length === 0) {
			loadError = text.respondent.linkUnavailable;
			loading = false;
			return;
		}

		try {
			if (isPublicSessionHandle(routeCredential)) {
				await loadPublicSessionDraft(routeCredential);
				return;
			}

			const loaded = isIdentifiedEntryToken(routeCredential)
				? await api.getIdentifiedEntry(routeCredential)
				: await api.getOpenLinkEntry(routeCredential);
			entry = loaded;
			session = null;
			savedAnswers = null;
			submitted = null;
			reviewing = false;
			saveStatus = 'idle';
			publicSessionHandle = null;
			participantCode = '';
			acceptedGrants = Object.fromEntries(
				Array.from(
					new Set([
						...loaded.consentDocument.requiredGrants,
						...loaded.consentDocument.optionalGrants
					])
				).map((grant) => [grant, false])
			);
			answers = Object.fromEntries(
				loaded.questions.map((question) => [question.id, defaultAnswerFor(question)])
			);
			await restoreStoredDraft(loaded);
		} catch (unknownError) {
			loadError = formatError(unknownError);
		} finally {
			loading = false;
		}
	}

	async function acceptConsent() {
		const currentEntry = entry;
		if (!currentEntry) {
			loadError = text.respondent.linkUnavailable;
			return;
		}

		if (!requiredConsentAccepted) {
			actionError = text.respondent.requiredConsent;
			return;
		}

		if (currentEntry.requiresParticipantCode && participantCode.trim().length === 0) {
			actionError = text.respondent.participantCodeRequired;
			return;
		}

		consentSubmitting = true;
		actionError = null;

		try {
			const request: CreateOpenLinkSessionRequest = {
				locale: currentEntry.defaultLocale,
				acceptedConsentDocumentId: currentEntry.consentDocument.id,
				acceptedGrants: selectedAcceptedGrants()
			};

			if (currentEntry.requiresParticipantCode) {
				request.participantCode = participantCode.trim();
			}

			session = isIdentifiedEntryToken(routeCredential)
				? await api.createIdentifiedEntrySession(routeCredential, request)
				: await api.createOpenLinkSession(routeCredential, request);
			publicSessionHandle = session.publicHandle ?? null;
			storeSessionPointer(currentEntry, {
				sessionId: session.id,
				publicHandle: publicSessionHandle
			});
			scrubTokenUrl(publicSessionHandle);
			savedAnswers = null;
			reviewing = false;
			saveStatus = 'idle';
			participantCode = '';
		} catch (unknownError) {
			actionError = formatError(unknownError);
		} finally {
			consentSubmitting = false;
		}
	}

	async function saveForReview() {
		const currentEntry = entry;
		const currentSession = session;
		if (!currentEntry || !currentSession) {
			actionError = text.respondent.sessionNotReady;
			return;
		}

		const validationError = validateAnswers(currentEntry);
		if (validationError) {
			actionError = validationError;
			return;
		}

		try {
			await saveCurrentAnswers({ manual: true });
			reviewing = true;
		} catch (unknownError) {
			actionError = formatError(unknownError);
		}
	}

	async function submitReviewedResponse() {
		const currentSession = session;
		if (!currentSession || !savedAnswers) {
			actionError = text.respondent.saveBeforeSubmit;
			return;
		}

		submitting = true;
		actionError = null;

		try {
			submitted = publicSessionHandle
				? await api.submitPublicSession(publicSessionHandle, {
						timeTakenMs: null
					})
				: await api.submitOpenLinkSession(routeCredential, currentSession.id, {
						timeTakenMs: null
					});
			clearLocalUnsavedDraft(publicSessionHandle);
		} catch (unknownError) {
			actionError = formatError(unknownError);
		} finally {
			submitting = false;
		}
	}

	async function saveCurrentAnswers({ manual }: { manual: boolean }) {
		const currentEntry = entry;
		const currentSession = session;
		if (!currentEntry || !currentSession) {
			throw new Error(text.respondent.sessionNotReady);
		}

		clearAutosaveTimer();
		if (manual) {
			saving = true;
		}
		saveStatus = 'saving';
		actionError = null;

		try {
			const request = {
				answers: currentEntry.questions.map((question) => ({
					questionId: question.id,
					value: isQuestionVisible(currentEntry, question.id)
						? normalizeAnswerValue(answers[question.id])
						: null,
					isSkipped: !isQuestionVisible(currentEntry, question.id)
				}))
			};
			savedAnswers = publicSessionHandle
				? await api.savePublicSessionAnswers(publicSessionHandle, request)
				: await api.saveOpenLinkAnswers(routeCredential, currentSession.id, request);
			clearLocalUnsavedDraft(publicSessionHandle);
			saveStatus = 'saved';
		} catch (unknownError) {
			saveStatus = 'failed';
			throw unknownError;
		} finally {
			if (manual) {
				saving = false;
			}
		}
	}

	function handleAnswersChange(nextAnswers: Record<string, string>) {
		if (answersEqual(answers, nextAnswers)) {
			return;
		}

		answers = nextAnswers;
		if (session && publicSessionHandle && !submitted && entry) {
			storeLocalUnsavedDraft(entry, nextAnswers);
		}
		savedAnswers = null;
		reviewing = false;

		if (session && !submitted) {
			saveStatus = 'dirty';
			scheduleAutosave();
		}
	}

	function scheduleAutosave() {
		clearAutosaveTimer();
		autosaveTimer = setTimeout(() => {
			void autosaveAnswers();
		}, 400);
	}

	async function autosaveAnswers() {
		try {
			await saveCurrentAnswers({ manual: false });
		} catch (unknownError) {
			actionError = formatError(unknownError);
		}
	}

	function clearAutosaveTimer() {
		if (autosaveTimer) {
			clearTimeout(autosaveTimer);
			autosaveTimer = null;
		}
	}

	function answersEqual(left: Record<string, string>, right: Record<string, string>) {
		const leftKeys = Object.keys(left);
		const rightKeys = Object.keys(right);
		if (leftKeys.length !== rightKeys.length) {
			return false;
		}

		return leftKeys.every((key) => left[key] === right[key]);
	}

	function handleBeforeUnload(event: BeforeUnloadEvent) {
		if (!hasUnsavedAnswers()) {
			return;
		}

		event.preventDefault();
		event.returnValue = '';
	}

	function hasUnsavedAnswers() {
		return Boolean(
			session &&
				!submitted &&
				(saveStatus === 'dirty' ||
					saveStatus === 'saving' ||
					saveStatus === 'failed' ||
					saveStatus === 'local-restored')
		);
	}

	function validateAnswers(currentEntry: OpenLinkEntryResponse) {
		for (const question of currentEntry.questions) {
			if (!isQuestionVisible(currentEntry, question.id)) {
				continue;
			}

			const rawAnswer = answers[question.id];
			const value = normalizeAnswerValue(rawAnswer);

			if (question.required && !isQuestionAnswered(question, rawAnswer)) {
				return text.respondent.questionRequiresAnswer(question.code);
			}

			if (value !== null && question.type === 'number') {
				const numericValue = Number(value);
				const constraints = questionInputConstraints(question);

				if (!Number.isFinite(numericValue)) {
					return text.respondent.questionMustBeNumber(question.code);
				}

				if (
					typeof constraints.min === 'number' &&
					typeof constraints.max === 'number' &&
					(numericValue < constraints.min || numericValue > constraints.max)
				) {
					return text.respondent.questionBetween(question.code, constraints.min, constraints.max);
				}
			}

			if (
				value !== null &&
				typeof question.scaleMinValue === 'number' &&
				typeof question.scaleMaxValue === 'number'
			) {
				const numericValue = Number(value);

				if (!Number.isFinite(numericValue)) {
					return text.respondent.questionMustBeNumber(question.code);
				}

				if (numericValue < question.scaleMinValue || numericValue > question.scaleMaxValue) {
					return text.respondent.questionBetween(question.code, question.scaleMinValue, question.scaleMaxValue);
				}
			}
		}

		return null;
	}

	function isQuestionVisible(currentEntry: OpenLinkEntryResponse, questionId: string) {
		const question = currentEntry.questions.find((candidate) => candidate.id === questionId);
		if (!question) {
			return true;
		}

		return isRespondentQuestionVisible(question, currentEntry.questions, answers);
	}

	async function loadPublicSessionDraft(handle: string) {
		let draft: Awaited<ReturnType<typeof api.getPublicSessionDraft>>;

		try {
			draft = await api.getPublicSessionDraft(handle);
		} catch (unknownError) {
			const localDraft = readLocalUnsavedDraft(handle);
			if (localDraft) {
				applyLocalUnsavedDraft(localDraft);
				return;
			}

			throw unknownError;
		}

		if (!draft.entry) {
			throw new Error(text.respondent.responseSessionUnavailable);
		}

		entry = draft.entry;
		acceptedGrants = Object.fromEntries(
			Array.from(
				new Set([
					...draft.entry.consentDocument.requiredGrants,
					...draft.entry.consentDocument.optionalGrants
				])
			).map((grant) => [grant, false])
		);
		applyDraft(draft.entry, draft, handle);
		if (!draft.session.submittedAt) {
			applyLocalUnsavedDraft(readLocalUnsavedDraft(handle));
		}
	}

	async function restoreStoredDraft(currentEntry: OpenLinkEntryResponse) {
		const pointer = readStoredSessionPointer(currentEntry);
		if (!pointer) {
			return;
		}

		try {
			if (pointer.publicHandle) {
				const draft = await api.getPublicSessionDraft(pointer.publicHandle);
				applyDraft(draft.entry ?? currentEntry, draft, pointer.publicHandle);
				if (!draft.session.submittedAt) {
					applyLocalUnsavedDraft(readLocalUnsavedDraft(pointer.publicHandle));
				}
				scrubTokenUrl(pointer.publicHandle);
				return;
			}

			const draft = await api.getOpenLinkSessionDraft(routeCredential, pointer.sessionId);
			applyDraft(currentEntry, draft, null);
		} catch {
			if (pointer.publicHandle) {
				const localDraft = readLocalUnsavedDraft(pointer.publicHandle);
				if (localDraft) {
					applyLocalUnsavedDraft(localDraft);
					scrubTokenUrl(pointer.publicHandle);
					return;
				}
			}
			clearSessionPointer(currentEntry);
		}
	}

	function applyDraft(
		currentEntry: OpenLinkEntryResponse,
		draft: { session: ResponseSessionResponse; answers: SavedAnswerResponse[]; savedAnswerCount: number },
		handle: string | null
	) {
		session = draft.session;
		publicSessionHandle = handle ?? draft.session.publicHandle ?? publicSessionHandle;
		answers = answersFromDraft(currentEntry, draft.answers);
		savedAnswers = {
			sessionId: draft.session.id,
			savedAnswerCount: draft.savedAnswerCount
		};
		saveStatus = 'saved';
		reviewing = false;
		actionError = null;
		participantCode = '';

		if (draft.session.submittedAt) {
			submitted = {
				id: draft.session.id,
				submittedAt: draft.session.submittedAt
			};
		}
	}

	function applyLocalUnsavedDraft(localDraft: LocalUnsavedDraft | null) {
		if (!localDraft) {
			return;
		}

		entry = localDraft.entry;
		session = {
			id: localDraft.sessionId,
			assignmentId: localDraft.assignmentId,
			locale: localDraft.entry.defaultLocale,
			startedAt: null,
			submittedAt: null,
			timeTakenMs: null,
			publicHandle: localDraft.publicHandle
		};
		publicSessionHandle = localDraft.publicHandle;
		acceptedGrants = Object.fromEntries(
			Array.from(
				new Set([
					...localDraft.entry.consentDocument.requiredGrants,
					...localDraft.entry.consentDocument.optionalGrants
				])
			).map((grant) => [grant, false])
		);
		answers = { ...localDraft.answers };
		savedAnswers = null;
		submitted = null;
		reviewing = false;
		saveStatus = 'local-restored';
		actionError = null;
		participantCode = '';
	}

	function answersFromDraft(
		currentEntry: OpenLinkEntryResponse,
		draftAnswers: SavedAnswerResponse[]
	) {
		const nextAnswers = Object.fromEntries(
			currentEntry.questions.map((question) => [question.id, defaultAnswerFor(question)])
		);

		for (const answer of draftAnswers) {
			if (answer.questionId in nextAnswers) {
				nextAnswers[answer.questionId] = answer.value ?? '';
			}
		}

		return nextAnswers;
	}

	function localDraftKey(handle: string) {
		return `respondent-unsaved-draft:${handle}`;
	}

	function storeLocalUnsavedDraft(
		currentEntry: OpenLinkEntryResponse,
		currentAnswers: Record<string, string>
	) {
		if (!session || !publicSessionHandle || submitted) {
			return;
		}

		const draft: LocalUnsavedDraft = {
			version: 1,
			publicHandle: publicSessionHandle,
			sessionId: session.id,
			assignmentId: currentEntry.assignmentId,
			updatedAt: new Date().toISOString(),
			entry: currentEntry,
			answers: { ...currentAnswers }
		};

		try {
			sessionStorage.setItem(localDraftKey(publicSessionHandle), JSON.stringify(draft));
		} catch {
			// Local draft resilience is best-effort; autosave/retry still works without it.
		}
	}

	function readLocalUnsavedDraft(handle: string): LocalUnsavedDraft | null {
		try {
			const stored = sessionStorage.getItem(localDraftKey(handle));
			if (!stored || stored.trim().length === 0) {
				return null;
			}

			const parsed = JSON.parse(stored) as unknown;
			if (!isLocalUnsavedDraft(parsed, handle)) {
				clearLocalUnsavedDraft(handle);
				return null;
			}

			return parsed;
		} catch {
			clearLocalUnsavedDraft(handle);
			return null;
		}
	}

	function clearLocalUnsavedDraft(handle: string | null) {
		if (!handle) {
			return;
		}

		try {
			sessionStorage.removeItem(localDraftKey(handle));
		} catch {
			// Ignore local cleanup failures; server save/submit state remains authoritative.
		}
	}

	function isLocalUnsavedDraft(value: unknown, handle: string): value is LocalUnsavedDraft {
		if (!isRecord(value)) {
			return false;
		}

		return (
			value.version === 1 &&
			value.publicHandle === handle &&
			typeof value.sessionId === 'string' &&
			value.sessionId.trim().length > 0 &&
			typeof value.assignmentId === 'string' &&
			value.assignmentId.trim().length > 0 &&
			typeof value.updatedAt === 'string' &&
			isOpenLinkEntrySnapshot(value.entry) &&
			value.entry.assignmentId === value.assignmentId &&
			isAnswerRecord(value.answers)
		);
	}

	function isOpenLinkEntrySnapshot(value: unknown): value is OpenLinkEntryResponse {
		if (!isRecord(value) || !isRecord(value.consentDocument) || !Array.isArray(value.questions)) {
			return false;
		}

		return (
			typeof value.campaignId === 'string' &&
			typeof value.assignmentId === 'string' &&
			typeof value.templateVersionId === 'string' &&
			typeof value.name === 'string' &&
			typeof value.status === 'string' &&
			typeof value.responseIdentityMode === 'string' &&
			typeof value.requiresParticipantCode === 'boolean' &&
			typeof value.defaultLocale === 'string' &&
			typeof value.consentDocument.id === 'string' &&
			Array.isArray(value.consentDocument.requiredGrants) &&
			Array.isArray(value.consentDocument.optionalGrants)
		);
	}

	function isAnswerRecord(value: unknown): value is Record<string, string> {
		if (!isRecord(value)) {
			return false;
		}

		return Object.values(value).every((answer) => typeof answer === 'string');
	}

	function isRecord(value: unknown): value is Record<string, unknown> {
		return typeof value === 'object' && value !== null && !Array.isArray(value);
	}

	function sessionPointerKey(currentEntry: OpenLinkEntryResponse) {
		return `respondent-session:${currentEntry.assignmentId}`;
	}

	type StoredSessionPointer = {
		sessionId: string;
		publicHandle: string | null;
	};

	function readStoredSessionPointer(currentEntry: OpenLinkEntryResponse): StoredSessionPointer | null {
		try {
			const stored = sessionStorage.getItem(sessionPointerKey(currentEntry));
			if (!stored || stored.trim().length === 0) {
				return null;
			}

			try {
				const parsed = JSON.parse(stored) as Partial<StoredSessionPointer>;
				if (typeof parsed.sessionId === 'string' && parsed.sessionId.trim().length > 0) {
					return {
						sessionId: parsed.sessionId,
						publicHandle:
							typeof parsed.publicHandle === 'string' && parsed.publicHandle.trim().length > 0
								? parsed.publicHandle
								: null
					};
				}
			} catch {
				// U05 stored only the response_session_id; keep that pointer usable.
			}

			return { sessionId: stored, publicHandle: null };
		} catch {
			return null;
		}
	}

	function storeSessionPointer(currentEntry: OpenLinkEntryResponse, pointer: StoredSessionPointer) {
		try {
			sessionStorage.setItem(sessionPointerKey(currentEntry), JSON.stringify(pointer));
		} catch {
			// Browser storage can be disabled; the respondent flow still works without resume.
		}
	}

	function clearSessionPointer(currentEntry: OpenLinkEntryResponse) {
		try {
			sessionStorage.removeItem(sessionPointerKey(currentEntry));
		} catch {
			// Ignore storage failures and continue the normal consent flow.
		}
	}

	function scrubTokenUrl(handle: string | null) {
		if (!handle || isPublicSessionHandle(routeCredential)) {
			return;
		}

		window.history.replaceState(window.history.state, '', `/r/${handle}`);
	}

	function isPublicSessionHandle(value: string) {
		return value.startsWith('rsh_');
	}

	function isIdentifiedEntryToken(value: string) {
		return value.startsWith('idn_');
	}

	function setGrantAccepted(grant: string, accepted: boolean) {
		acceptedGrants = { ...acceptedGrants, [grant]: accepted };
	}

	function selectedAcceptedGrants() {
		return Object.entries(acceptedGrants)
			.filter(([, accepted]) => accepted)
			.map(([grant]) => grant);
	}

	function defaultAnswerFor(question: OpenLinkEntryResponse['questions'][number]) {
		return '';
	}

	function normalizeAnswerValue(value: string | undefined) {
		const trimmed = value?.trim() ?? '';

		return trimmed.length > 0 ? trimmed : null;
	}

	function getSaveStatusMessage(status: SaveStatus) {
		switch (status) {
			case 'dirty':
				return 'Unsaved changes';
			case 'saving':
				return 'Saving answers';
			case 'saved':
				return 'Answers saved';
			case 'failed':
				return 'Answers could not be saved.';
			case 'local-restored':
				return 'Unsaved answers restored on this device';
			default:
				return 'Answers not saved yet';
		}
	}

	function grantLabel(grant: string) {
		const words = grant.replace(/[_-]+/g, ' ').trim().split(/\s+/);

		return words
			.map((word, index) =>
				index === 0
					? word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()
					: word.toLowerCase()
			)
			.join(' ');
	}

	function formatError(unknownError: unknown) {
		if (unknownError instanceof ApiError) {
			const detail =
				typeof unknownError.body === 'object' &&
				unknownError.body !== null &&
				'detail' in unknownError.body
					? String(unknownError.body.detail)
					: unknownError.message;

			return detail;
		}

		return unknownError instanceof Error ? unknownError.message : text.respondent.requestFailed;
	}
</script>

<svelte:head>
	<title>{entry?.name ?? text.respondent.metaFallback} - Validated Scale</title>
</svelte:head>

<main class="min-h-screen bg-[var(--color-background)] px-4 py-6 text-[var(--color-text)] sm:px-6">
	<section class="mx-auto grid max-w-2xl gap-5">
		{#if loading}
			<div class="flex items-center gap-2 text-sm font-semibold text-[var(--color-text-muted)]">
				<LoaderCircle size={18} aria-hidden="true" />
				<span>{text.respondent.loadingSurvey}</span>
			</div>
		{:else if loadError}
			<section class="setup-panel" aria-label={text.respondent.surveyUnavailable}>
				<p class="error-line" role="alert">{loadError}</p>
				<button type="button" class="secondary-button" onclick={loadEntry}>
					<RefreshCw size={17} aria-hidden="true" />
					<span>{text.respondent.tryAgain}</span>
				</button>
			</section>
		{:else if entry}
			<header class="grid gap-2 border-b border-[var(--color-border)] pb-4">
				<p class="text-xs font-semibold text-[var(--color-text-muted)] uppercase">
					{entry.responseIdentityMode}
				</p>
				<h1 class="serif-heading text-3xl">{entry.name}</h1>
			</header>

			{#if submitted && receiptView}
				<section class="setup-panel" aria-label={text.respondent.responseReceipt}>
					<div class="grid gap-2">
						<div class="flex items-center gap-2">
							<Check size={18} aria-hidden="true" />
							<h2 class="text-lg font-semibold">{receiptView.title}</h2>
						</div>
						<p class="text-sm text-[var(--color-text-muted)]">{receiptView.headline}</p>
						<p class="text-sm text-[var(--color-text-muted)]">{receiptView.submittedAt}</p>
					</div>

					<dl class="setup-definition-list">
						{#each receiptView.metrics as metric (metric.label)}
							<div>
								<dt>{metric.label}</dt>
								<dd>{metric.value}</dd>
							</div>
						{/each}
					</dl>

					<ul class="grid gap-2 text-sm text-[var(--color-text-muted)]">
						{#each receiptView.guidance as guidance (guidance)}
							<li>{guidance}</li>
						{/each}
					</ul>
				</section>
			{:else if !session}
				<section class="setup-panel" aria-labelledby="consent-title">
					<div class="grid gap-2">
						<h2 id="consent-title" class="setup-panel__title">{entry.consentDocument.title}</h2>
						<p class="whitespace-pre-wrap text-sm text-[var(--color-text-muted)]">
							{entry.consentDocument.bodyMarkdown}
						</p>
					</div>

					<div class="grid gap-2">
						{#each allConsentGrants as grant (grant)}
							<label class="checkbox-field">
								<input
									type="checkbox"
									checked={acceptedGrants[grant] ?? false}
									onchange={(event) => setGrantAccepted(grant, event.currentTarget.checked)}
								/>
								<span>{grantLabel(grant)}</span>
							</label>
						{/each}
					</div>

					{#if entry.requiresParticipantCode}
						<label class="field">
							<span>{text.respondent.participantCode}</span>
							<input
								aria-label={text.respondent.participantCode}
								autocomplete="off"
								spellcheck="false"
								value={participantCode}
								oninput={(event) => (participantCode = event.currentTarget.value)}
							/>
						</label>
					{/if}

					{#if actionError}
						<p class="error-line" role="alert">{actionError}</p>
					{/if}

					<button
						type="button"
						class="primary-button"
						disabled={consentSubmitting || !requiredConsentAccepted || !participantCodeReady}
						onclick={acceptConsent}
					>
						{#if consentSubmitting}
							<LoaderCircle size={17} aria-hidden="true" />
						{:else}
							<Check size={17} aria-hidden="true" />
						{/if}
						<span>{text.respondent.continue}</span>
					</button>
				</section>
			{:else if reviewing && savedAnswers}
				<section class="setup-panel" aria-labelledby="review-title">
					<div class="grid gap-2">
						<p class="text-xs font-semibold text-[var(--color-text-muted)] uppercase">
							{text.respondent.reviewKicker}
						</p>
						<h2 id="review-title" class="setup-panel__title">{text.respondent.reviewTitle}</h2>
						<p class="text-sm text-[var(--color-text-muted)]">
							{text.respondent.savedAnswers(savedAnswers.savedAnswerCount)}
						</p>
					</div>

					<dl class="setup-definition-list">
						<div>
							<dt>{text.respondent.session}</dt>
							<dd>{savedAnswers.sessionId}</dd>
						</div>
					</dl>

					{#if actionError}
						<p class="error-line" role="alert">{actionError}</p>
					{/if}

					<div class="grid gap-2 sm:grid-cols-2">
						<button
							type="button"
							class="secondary-button"
							disabled={submitting}
							onclick={() => (reviewing = false)}
						>
							<ArrowLeft size={17} aria-hidden="true" />
							<span>{text.respondent.backToEdit}</span>
						</button>
						<button
							type="button"
							class="primary-button"
							disabled={submitting}
							onclick={submitReviewedResponse}
						>
							{#if submitting}
								<LoaderCircle size={17} aria-hidden="true" />
							{:else}
								<Send size={17} aria-hidden="true" />
							{/if}
							<span>{text.respondent.submitReviewed}</span>
						</button>
					</div>
				</section>
			{:else}
				<form class="grid gap-4" onsubmit={(event) => event.preventDefault()}>
					<RespondentQuestionRunner
						questions={entry.questions}
						{answers}
						disabled={!session}
						{saving}
						{saveStatusMessage}
						{actionError}
						onAnswersChange={handleAnswersChange}
						onSaveForReview={saveForReview}
						reviewLabel={text.respondent.reviewTitle}
					/>
				</form>
			{/if}
		{/if}
	</section>
</main>
