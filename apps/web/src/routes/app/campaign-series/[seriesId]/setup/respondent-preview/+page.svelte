<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { ArrowLeft, Check, Send } from 'lucide-svelte';
	import type { RespondentQuestionResponse } from '$lib/api/setup';
	import { appLocaleFromPageData, normalizeAppLocale } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';
	import RespondentQuestionRunner from '$lib/respondent/RespondentQuestionRunner.svelte';
	import {
		isQuestionAnswered,
		isRespondentQuestionVisible,
		questionInputConstraints,
		visibleRespondentQuestions
	} from '$lib/respondent/respondent-question-model';
	import {
		readRespondentPreviewSession,
		respondentPreviewStorageKey,
		type RespondentPreviewReadResult,
		type RespondentPreviewSession
	} from '$lib/respondent/respondent-preview-session';
	import { onMount } from 'svelte';

	type LoadState = 'loading' | 'ready' | 'missing';

	const seriesId = $derived(page.params.seriesId ?? '');
	const previewId = $derived(page.url.searchParams.get('previewId'));
	const explicitRouteLocale = $derived(page.url.searchParams.get('locale') ?? page.url.searchParams.get('lang'));
	const previewLocale = $derived(resolvePreviewLocale());
	const text = $derived(routePageCopy(previewLocale));
	const setupUrl = $derived(resolveSetupUrl());
	let loadState = $state<LoadState>('loading');
	let loadMessage = $state('');
	let preview = $state<RespondentPreviewSession | null>(null);
	let answers = $state<Record<string, string>>({});
	let reviewing = $state(false);
	let submitted = $state(false);
	let actionError = $state<string | null>(null);
	let saveStatus = $state<'local-only' | 'ready'>('local-only');
	let saveStatusMessage = $derived(
		saveStatus === 'ready'
			? text.respondent.previewSaveStatusReady
			: text.respondent.previewSaveStatusLocalOnly
	);

	const visibleQuestions = $derived(
		preview ? visibleRespondentQuestions(preview.questions, answers) : []
	);
	const answeredVisibleCount = $derived(
		visibleQuestions.filter((question) => isQuestionAnswered(question, answers[question.id])).length
	);

	onMount(() => {
		const result = readRespondentPreviewSession(
			window.sessionStorage,
			previewId,
			seriesId,
			Date.now()
		);
		applyPreviewReadResult(result);
	});

	function applyPreviewReadResult(result: RespondentPreviewReadResult) {
		if (result.status === 'ready') {
			preview = result.preview;
			answers = Object.fromEntries(result.preview.questions.map((question) => [question.id, '']));
			loadState = 'ready';
			return;
		}

		loadState = 'missing';
		loadMessage = previewLoadMessage(result.status);
	}

	function previewLoadMessage(status: RespondentPreviewReadResult['status']) {
		switch (status) {
			case 'wrong-series':
				return text.respondent.previewWrongSeries;
			case 'expired':
				return text.respondent.previewExpired;
			case 'invalid':
				return text.respondent.previewInvalid;
			default:
				return text.respondent.previewMissing;
		}
	}

	function handleAnswersChange(nextAnswers: Record<string, string>) {
		answers = nextAnswers;
		actionError = null;
		saveStatus = 'local-only';
	}

	function reviewPreviewAnswers() {
		if (!preview) {
			return;
		}

		const validationError = validatePreviewAnswers(preview.questions);
		if (validationError) {
			actionError = validationError;
			return;
		}

		actionError = null;
		reviewing = true;
		saveStatus = 'ready';
	}

	function finishPreview() {
		if (preview) {
			window.sessionStorage.removeItem(respondentPreviewStorageKey(preview.previewId));
		}

		reviewing = false;
		submitted = true;
	}

	function validatePreviewAnswers(questions: RespondentQuestionResponse[]) {
		for (const question of questions) {
			if (!isRespondentQuestionVisible(question, questions, answers)) {
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
					return text.respondent.questionBetween(
						question.code,
						question.scaleMinValue,
						question.scaleMaxValue
					);
				}
			}
		}

		return null;
	}

	function normalizeAnswerValue(value: string | undefined) {
		const trimmed = value?.trim() ?? '';
		return trimmed.length > 0 ? trimmed : null;
	}

	function hasExplicitRouteLocale() {
		return typeof explicitRouteLocale === 'string' && explicitRouteLocale.trim().length > 0;
	}

	function resolvePreviewLocale() {
		const pageLocale = appLocaleFromPageData(page.data);
		if (hasExplicitRouteLocale()) {
			return pageLocale;
		}

		return normalizeAppLocale(preview?.locale ?? pageLocale);
	}

	function resolveSetupUrl() {
		const path = resolve(`/app/campaign-series/${seriesId}/setup`);
		if (!hasExplicitRouteLocale()) {
			return path;
		}

		const url = new URL(path, page.url.origin);
		url.searchParams.set('locale', previewLocale);
		return `${url.pathname}${url.search}${url.hash}`;
	}
</script>

<svelte:head>
	<title>{preview?.seriesName ?? text.respondent.previewMetaFallback} - Validated Scale</title>
</svelte:head>

{#if loadState === 'loading'}
	<section class="product-panel" aria-label={text.respondent.previewAria}>
		<p class="text-sm font-semibold text-[var(--color-text-muted)]">
			{text.respondent.previewLoading}
		</p>
	</section>
{:else if loadState === 'missing'}
	<section class="product-panel" aria-label={text.respondent.previewUnavailableAria}>
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.respondent.previewKicker}</p>
				<h1 class="product-title">{text.respondent.previewUnavailableTitle}</h1>
				<p class="mt-1 text-sm text-[var(--color-text-muted)]">{loadMessage}</p>
			</div>
		</div>
		<a class="secondary-button" href={setupUrl}>
			<ArrowLeft size={17} aria-hidden="true" />
			<span>{text.respondent.previewBackToSetup}</span>
		</a>
	</section>
{:else if preview}
	<section class="product-panel" aria-label={text.respondent.previewAria}>
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">{text.respondent.previewKicker}</p>
				<h1 class="product-title">{text.respondent.previewTitle(preview.seriesName)}</h1>
				<p class="mt-1 text-sm text-[var(--color-text-muted)]">
					{text.respondent.previewLocalOnlyBody}
				</p>
			</div>
			<a class="secondary-button" href={setupUrl}>
				<ArrowLeft size={17} aria-hidden="true" />
				<span>{text.respondent.previewBackToSetup}</span>
			</a>
		</div>
	</section>

	{#if submitted}
		<section class="product-panel" aria-labelledby="preview-complete-title">
			<div class="record-row">
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{text.respondent.previewCompleteKicker}</p>
						<h2 id="preview-complete-title" class="record-row__title">
							{text.respondent.previewCompleteTitle}
						</h2>
					</div>
					<Check size={18} aria-hidden="true" />
				</div>
				<p class="text-sm text-[var(--color-text-muted)]">
					{text.respondent.previewNoPersistence}
				</p>
			</div>
			<a class="primary-button" href={setupUrl}>
				<span>{text.respondent.previewReturnToSetup}</span>
			</a>
		</section>
	{:else if reviewing}
		<section class="product-panel" aria-labelledby="preview-review-title">
			<div class="record-row">
				<div class="record-row__header">
					<div>
						<p class="record-field__label">{text.respondent.previewReviewKicker}</p>
						<h2 id="preview-review-title" class="record-row__title">
							{text.respondent.previewReviewTitle}
						</h2>
					</div>
					<Check size={18} aria-hidden="true" />
				</div>
				<p class="text-sm text-[var(--color-text-muted)]">
					{text.respondent.previewAnsweredVisible(answeredVisibleCount, visibleQuestions.length)}
				</p>
			</div>
			{#if actionError}
				<p class="error-line" role="alert">{actionError}</p>
			{/if}
			<div class="action-row">
				<button type="button" class="secondary-button" onclick={() => (reviewing = false)}>
					<ArrowLeft size={17} aria-hidden="true" />
					<span>{text.respondent.previewBackToEdit}</span>
				</button>
				<button type="button" class="primary-button" onclick={finishPreview}>
					<Send size={17} aria-hidden="true" />
					<span>{text.respondent.previewFinish}</span>
				</button>
			</div>
		</section>
	{:else}
		<form class="grid gap-4" onsubmit={(event) => event.preventDefault()}>
			<RespondentQuestionRunner
				questions={preview.questions}
				{answers}
				{saveStatusMessage}
				{actionError}
				onAnswersChange={handleAnswersChange}
				onSaveForReview={reviewPreviewAnswers}
				backLabel={text.respondent.back}
				nextLabel={text.respondent.next}
				reviewLabel={text.respondent.reviewTitle}
				surveyQuestionsLabel={text.respondent.runnerSurveyQuestions}
				surveyProgressLabel={text.respondent.runnerProgress}
				questionLabel={text.respondent.runnerQuestion}
				ofLabel={text.respondent.runnerOf}
				leftLabel={text.respondent.runnerLeft}
				requiredLabel={text.respondent.runnerRequired}
				chooseAnswerMessage={text.respondent.runnerChooseAnswer}
				onlyThisLabel={text.respondent.runnerOnlyThis}
				chooseUpToLabel={text.respondent.runnerChooseUpTo}
				answerLabel={text.respondent.runnerAnswer}
				answerWithUnitLabel={text.respondent.runnerAnswerWithUnit}
				noQuestionsAvailableLabel={text.respondent.runnerNoQuestionsAvailable}
			/>
		</form>
	{/if}
{/if}
