<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { ArrowLeft, Check, Send } from 'lucide-svelte';
	import type { RespondentQuestionResponse } from '$lib/api/setup';
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
	const setupUrl = $derived(resolve(`/app/campaign-series/${seriesId}/setup`));
	let loadState = $state<LoadState>('loading');
	let loadMessage = $state('');
	let preview = $state<RespondentPreviewSession | null>(null);
	let answers = $state<Record<string, string>>({});
	let reviewing = $state(false);
	let submitted = $state(false);
	let actionError = $state<string | null>(null);
	let saveStatusMessage = $state('Preview answers are local only.');

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
				return 'This preview belongs to a different study.';
			case 'expired':
				return 'This preview expired. Return to setup and open a fresh preview.';
			case 'invalid':
				return 'This preview could not be read safely. Return to setup and open it again.';
			default:
				return 'No preview draft was found. Return to setup and open Preview as respondent.';
		}
	}

	function handleAnswersChange(nextAnswers: Record<string, string>) {
		answers = nextAnswers;
		actionError = null;
		saveStatusMessage = 'Preview answers are local only.';
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
		saveStatusMessage = 'Preview answers are ready for local review.';
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
				return `${question.code} needs an answer before review.`;
			}

			if (value !== null && question.type === 'number') {
				const numericValue = Number(value);
				const constraints = questionInputConstraints(question);

				if (!Number.isFinite(numericValue)) {
					return `${question.code} must be a number.`;
				}

				if (
					typeof constraints.min === 'number' &&
					typeof constraints.max === 'number' &&
					(numericValue < constraints.min || numericValue > constraints.max)
				) {
					return `${question.code} must be between ${constraints.min} and ${constraints.max}.`;
				}
			}

			if (
				value !== null &&
				typeof question.scaleMinValue === 'number' &&
				typeof question.scaleMaxValue === 'number'
			) {
				const numericValue = Number(value);

				if (!Number.isFinite(numericValue)) {
					return `${question.code} must be a number.`;
				}

				if (numericValue < question.scaleMinValue || numericValue > question.scaleMaxValue) {
					return `${question.code} must be between ${question.scaleMinValue} and ${question.scaleMaxValue}.`;
				}
			}
		}

		return null;
	}

	function normalizeAnswerValue(value: string | undefined) {
		const trimmed = value?.trim() ?? '';
		return trimmed.length > 0 ? trimmed : null;
	}
</script>

<svelte:head>
	<title>{preview?.seriesName ?? 'Respondent preview'} - Validated Scale</title>
</svelte:head>

{#if loadState === 'loading'}
	<section class="product-panel" aria-label="Respondent preview">
		<p class="text-sm font-semibold text-[var(--color-text-muted)]">Loading respondent preview</p>
	</section>
{:else if loadState === 'missing'}
	<section class="product-panel" aria-label="Respondent preview unavailable">
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">Respondent preview</p>
				<h1 class="product-title">Preview unavailable</h1>
				<p class="mt-1 text-sm text-[var(--color-text-muted)]">{loadMessage}</p>
			</div>
		</div>
		<a class="secondary-button" href={setupUrl}>
			<ArrowLeft size={17} aria-hidden="true" />
			<span>Back to setup</span>
		</a>
	</section>
{:else if preview}
	<section class="product-panel" aria-label="Respondent preview">
		<div class="product-panel__header">
			<div>
				<p class="product-kicker">Respondent preview</p>
				<h1 class="product-title">{preview.seriesName} respondent preview</h1>
				<p class="mt-1 text-sm text-[var(--color-text-muted)]">
					Preview answers stay in this browser and do not count in results.
				</p>
			</div>
			<a class="secondary-button" href={setupUrl}>
				<ArrowLeft size={17} aria-hidden="true" />
				<span>Back to setup</span>
			</a>
		</div>
	</section>

	{#if submitted}
		<section class="product-panel" aria-labelledby="preview-complete-title">
			<div class="record-row">
				<div class="record-row__header">
					<div>
						<p class="record-field__label">Local preview</p>
						<h2 id="preview-complete-title" class="record-row__title">Preview complete</h2>
					</div>
					<Check size={18} aria-hidden="true" />
				</div>
				<p class="text-sm text-[var(--color-text-muted)]">
					No response session, answer row, score, or submission was created.
				</p>
			</div>
			<a class="primary-button" href={setupUrl}>
				<span>Return to setup</span>
			</a>
		</section>
	{:else if reviewing}
		<section class="product-panel" aria-labelledby="preview-review-title">
			<div class="record-row">
				<div class="record-row__header">
					<div>
						<p class="record-field__label">Local review</p>
						<h2 id="preview-review-title" class="record-row__title">Preview review</h2>
					</div>
					<Check size={18} aria-hidden="true" />
				</div>
				<p class="text-sm text-[var(--color-text-muted)]">
					{answeredVisibleCount} of {visibleQuestions.length} visible questions have answers. These answers
					are local only.
				</p>
			</div>
			{#if actionError}
				<p class="error-line" role="alert">{actionError}</p>
			{/if}
			<div class="action-row">
				<button type="button" class="secondary-button" onclick={() => (reviewing = false)}>
					<ArrowLeft size={17} aria-hidden="true" />
					<span>Back to edit</span>
				</button>
				<button type="button" class="primary-button" onclick={finishPreview}>
					<Send size={17} aria-hidden="true" />
					<span>Finish preview</span>
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
				reviewLabel="Review response"
			/>
		</form>
	{/if}
{/if}
