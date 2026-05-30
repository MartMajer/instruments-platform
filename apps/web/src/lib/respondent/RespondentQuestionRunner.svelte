<script lang="ts">
	import { ArrowLeft, ArrowRight, Check, LoaderCircle } from 'lucide-svelte';
	import type { RespondentQuestionResponse } from '$lib/api/setup';
	import {
		answerToInputValue,
		inputValueToAnswer,
		isQuestionAnswered,
		matrixQuestionChoices,
		questionChoices,
		questionInputConstraints,
		rankingQuestionOptions,
		scaleQuestionOptions,
		visibleRespondentQuestions
	} from '$lib/respondent/respondent-question-model';

	let {
		questions,
		answers,
		disabled = false,
		saving = false,
		saveStatusMessage = '',
		actionError = null,
		onAnswersChange,
		onSaveForReview,
		backLabel = 'Back',
		nextLabel = 'Next',
		reviewLabel = 'Review response',
		surveyQuestionsLabel = 'Survey questions',
		surveyProgressLabel = 'Survey progress',
		questionLabel = 'Question',
		ofLabel = 'of',
		leftLabel = 'left',
		requiredLabel = 'Required',
		chooseAnswerMessage = 'Choose an answer to continue.',
		onlyThisLabel = 'Only this',
		chooseUpToLabel = (count: number) => `Choose up to ${count} options in order.`,
		answerLabel = 'Answer',
		answerWithUnitLabel = (unit: string) => `Answer (${unit})`,
		noQuestionsAvailableLabel = 'No questions available'
	}: {
		questions: RespondentQuestionResponse[];
		answers: Record<string, string>;
		disabled?: boolean;
		saving?: boolean;
		saveStatusMessage?: string;
		actionError?: string | null;
		onAnswersChange?: (answers: Record<string, string>) => void;
		onSaveForReview?: () => void | Promise<void>;
		backLabel?: string;
		nextLabel?: string;
		reviewLabel?: string;
		surveyQuestionsLabel?: string;
		surveyProgressLabel?: string;
		questionLabel?: string;
		ofLabel?: string;
		leftLabel?: string;
		requiredLabel?: string;
		chooseAnswerMessage?: string;
		onlyThisLabel?: string;
		chooseUpToLabel?: (count: number) => string;
		answerLabel?: string;
		answerWithUnitLabel?: (unit: string) => string;
		noQuestionsAvailableLabel?: string;
	} = $props();

	let currentQuestionId = $state<string | null>(null);
	let localError = $state<string | null>(null);
	let visibleQuestions = $derived(visibleRespondentQuestions(questions, answers));
	let currentIndex = $derived(currentVisibleIndex());
	let currentQuestion = $derived(visibleQuestions[currentIndex] ?? null);
	let progressPercent = $derived(
		visibleQuestions.length > 0 ? Math.round(((currentIndex + 1) / visibleQuestions.length) * 100) : 0
	);

	$effect(() => {
		if (visibleQuestions.length === 0) {
			currentQuestionId = null;
			return;
		}

		if (!currentQuestionId || !visibleQuestions.some((question) => question.id === currentQuestionId)) {
			currentQuestionId = visibleQuestions[0]?.id ?? null;
		}
	});

	function currentVisibleIndex() {
		if (!currentQuestionId) {
			return 0;
		}

		const index = visibleQuestions.findIndex((question) => question.id === currentQuestionId);
		return index >= 0 ? index : 0;
	}

	function setAnswer(question: RespondentQuestionResponse, inputValue: unknown, autoAdvance = false) {
		const nextAnswers = {
			...answers,
			[question.id]: inputValueToAnswer(question, inputValue)
		};

		localError = null;
		onAnswersChange?.(nextAnswers);

		if (autoAdvance) {
			advanceAfterAnswer(question, nextAnswers);
		}
	}

	function advanceAfterAnswer(
		question: RespondentQuestionResponse,
		nextAnswers: Record<string, string>
	) {
		const nextVisibleQuestions = visibleRespondentQuestions(questions, nextAnswers);
		const nextIndex = nextVisibleQuestions.findIndex((candidate) => candidate.id === question.id);
		const nextQuestion = nextIndex >= 0 ? nextVisibleQuestions[nextIndex + 1] : null;
		if (nextQuestion) {
			currentQuestionId = nextQuestion.id;
		}
	}

	function goBack() {
		localError = null;
		const previous = visibleQuestions[currentIndex - 1];
		if (previous) {
			currentQuestionId = previous.id;
		}
	}

	function goNext() {
		if (!currentQuestion) {
			return;
		}

		if (!isQuestionAnswered(currentQuestion, answers[currentQuestion.id])) {
			localError = chooseAnswerMessage;
			return;
		}

		localError = null;
		const next = visibleQuestions[currentIndex + 1];
		if (next) {
			currentQuestionId = next.id;
		}
	}

	async function requestReview() {
		localError = null;
		await onSaveForReview?.();
	}

	function isLastQuestion() {
		return currentIndex >= visibleQuestions.length - 1;
	}

	function scalarInputValue(question: RespondentQuestionResponse) {
		const value = answerToInputValue(question, answers[question.id]);
		return typeof value === 'string' || typeof value === 'number' ? String(value) : '';
	}

	function selectedArrayValue(question: RespondentQuestionResponse) {
		const value = answerToInputValue(question, answers[question.id]);
		return Array.isArray(value) ? value.map(String) : [];
	}

	function selectedObjectValue(question: RespondentQuestionResponse) {
		const value = answerToInputValue(question, answers[question.id]);
		return value && typeof value === 'object' && !Array.isArray(value)
			? (value as Record<string, string>)
			: {};
	}

	function isScalarSelected(question: RespondentQuestionResponse, value: string | number) {
		return scalarInputValue(question) === String(value);
	}

	function toggleMulti(question: RespondentQuestionResponse, value: string, exclusive: boolean) {
		const selected = selectedArrayValue(question);
		const hasValue = selected.includes(value);
		const choices = questionChoices(question);
		const exclusiveValues = new Set(
			choices.filter((choice) => choice.isExclusive).map((choice) => choice.value)
		);
		let nextSelected = hasValue
			? selected.filter((candidate) => candidate !== value)
			: [...selected, value];

		if (!hasValue && exclusive) {
			nextSelected = [value];
		} else if (!hasValue) {
			nextSelected = nextSelected.filter((candidate) => !exclusiveValues.has(candidate));
		}

		setAnswer(question, nextSelected);
	}

	function toggleRanking(question: RespondentQuestionResponse, value: string) {
		const selected = selectedArrayValue(question);
		const hasValue = selected.includes(value);
		const options = rankingQuestionOptions(question);
		if (hasValue) {
			setAnswer(
				question,
				selected.filter((candidate) => candidate !== value)
			);
			return;
		}

		if (options.topN !== null && selected.length >= options.topN) {
			localError = `Choose at most ${options.topN} ranked options.`;
			return;
		}

		setAnswer(question, [...selected, value]);
	}

	function setMatrixCell(question: RespondentQuestionResponse, row: string, column: string) {
		setAnswer(question, {
			...selectedObjectValue(question),
			[row]: column
		});
	}

	function rankingPosition(question: RespondentQuestionResponse, value: string) {
		const index = selectedArrayValue(question).indexOf(value);
		return index >= 0 ? index + 1 : null;
	}
</script>

<section
	class="respondent-runner"
	aria-label={surveyQuestionsLabel}
	data-testid="respondent-question-runner"
>
	{#if currentQuestion}
		<header class="runner-header">
			<div class="runner-progress-copy">
				<p>{questionLabel} {currentIndex + 1} {ofLabel} {visibleQuestions.length}</p>
				<p>{Math.max(0, visibleQuestions.length - currentIndex - 1)} {leftLabel}</p>
			</div>
			<div
				class="runner-progress"
				role="progressbar"
				aria-label={surveyProgressLabel}
				aria-valuemin="0"
				aria-valuemax="100"
				aria-valuenow={progressPercent}
			>
				<span style={`width: ${progressPercent}%`}></span>
			</div>
		</header>

		<article class="question-stage" aria-labelledby={`question-title-${currentQuestion.id}`}>
			<div class="question-heading">
				<p class="question-code">{currentQuestion.code}</p>
				<h2 id={`question-title-${currentQuestion.id}`}>{currentQuestion.textDefault}</h2>
				{#if currentQuestion.required}
					<p class="question-required">{requiredLabel}</p>
				{/if}
			</div>

			{#if currentQuestion.type === 'likert' || currentQuestion.type === 'nps'}
				{@const scale = scaleQuestionOptions(currentQuestion)}
				<div class="scale-answers" role="group" aria-label={currentQuestion.textDefault}>
					{#each scale.options as option (option.value)}
						<button
							type="button"
							class="answer-option scale-answer"
							class:selected={isScalarSelected(currentQuestion, option.value)}
							aria-pressed={isScalarSelected(currentQuestion, option.value)}
							aria-label={option.anchorLabel ? `${option.label} ${option.anchorLabel}` : option.label}
							disabled={disabled || saving}
							onclick={() => setAnswer(currentQuestion, option.value, true)}
						>
							<span class="answer-option__value">{option.label}</span>
						</button>
					{/each}
				</div>
				{#if scale.lowLabel || scale.highLabel}
					<div class="scale-anchors" aria-hidden="true">
						<span>{scale.lowLabel ?? ''}</span>
						<span>{scale.highLabel ?? ''}</span>
					</div>
				{/if}
			{:else if currentQuestion.type === 'single'}
				<div class="choice-answers" role="group" aria-label={currentQuestion.textDefault}>
					{#each questionChoices(currentQuestion) as choice (choice.value)}
						<button
							type="button"
							class="answer-option"
							class:selected={isScalarSelected(currentQuestion, choice.value)}
							aria-pressed={isScalarSelected(currentQuestion, choice.value)}
							disabled={disabled || saving}
							onclick={() => setAnswer(currentQuestion, choice.value, true)}
						>
							<span>{choice.label}</span>
						</button>
					{/each}
				</div>
			{:else if currentQuestion.type === 'multi'}
				<div class="choice-answers" role="group" aria-label={currentQuestion.textDefault}>
					{#each questionChoices(currentQuestion) as choice (choice.value)}
						<button
							type="button"
							class="answer-option"
							class:selected={selectedArrayValue(currentQuestion).includes(choice.value)}
							aria-pressed={selectedArrayValue(currentQuestion).includes(choice.value)}
							disabled={disabled || saving}
							onclick={() => toggleMulti(currentQuestion, choice.value, choice.isExclusive)}
						>
							<span>{choice.label}</span>
							{#if choice.isExclusive}
								<span class="answer-option__meta">{onlyThisLabel}</span>
							{/if}
						</button>
					{/each}
				</div>
			{:else if currentQuestion.type === 'ranking'}
				{@const rankingOptions = rankingQuestionOptions(currentQuestion)}
				<div class="ranking-answers" role="group" aria-label={currentQuestion.textDefault}>
					{#if rankingOptions.topN}
						<p class="control-help">{chooseUpToLabel(rankingOptions.topN)}</p>
					{/if}
					{#each questionChoices(currentQuestion) as choice (choice.value)}
						{@const position = rankingPosition(currentQuestion, choice.value)}
						<button
							type="button"
							class="answer-option ranking-answer"
							class:selected={position !== null}
							aria-pressed={position !== null}
							disabled={disabled || saving}
							onclick={() => toggleRanking(currentQuestion, choice.value)}
						>
							<span class="ranking-position">{position ?? ''}</span>
							<span>{choice.label}</span>
						</button>
					{/each}
				</div>
			{:else if currentQuestion.type === 'matrix'}
				{@const matrix = matrixQuestionChoices(currentQuestion)}
				<div class="matrix-answers" role="group" aria-label={currentQuestion.textDefault}>
					{#each matrix.rows as row (row.value)}
						<div class="matrix-row">
							<p>{row.label}</p>
							<div class="matrix-cells">
								{#each matrix.columns as column (column.value)}
									<button
										type="button"
										class="answer-option matrix-cell"
										class:selected={selectedObjectValue(currentQuestion)[row.value] === column.value}
										aria-pressed={selectedObjectValue(currentQuestion)[row.value] === column.value}
										disabled={disabled || saving}
										onclick={() => setMatrixCell(currentQuestion, row.value, column.value)}
									>
										<span>{column.label}</span>
									</button>
								{/each}
							</div>
						</div>
					{/each}
				</div>
			{:else if currentQuestion.type === 'number'}
				{@const constraints = questionInputConstraints(currentQuestion)}
				<label class="runner-field">
					<span>{constraints.unit ? answerWithUnitLabel(constraints.unit) : answerLabel}</span>
					<input
						type="number"
						value={scalarInputValue(currentQuestion)}
						min={constraints.min ?? undefined}
						max={constraints.max ?? undefined}
						step={constraints.step ?? undefined}
						disabled={disabled || saving}
						oninput={(event) => setAnswer(currentQuestion, event.currentTarget.value)}
					/>
				</label>
			{:else if currentQuestion.type === 'date'}
				{@const constraints = questionInputConstraints(currentQuestion)}
				<label class="runner-field">
					<span>{answerLabel}</span>
					<input
						type="date"
						value={scalarInputValue(currentQuestion)}
						min={constraints.minDate ?? undefined}
						max={constraints.maxDate ?? undefined}
						disabled={disabled || saving}
						oninput={(event) => setAnswer(currentQuestion, event.currentTarget.value)}
					/>
				</label>
			{:else}
				{@const constraints = questionInputConstraints(currentQuestion)}
				<label class="runner-field">
					<span>{answerLabel}</span>
					{#if constraints.multiline}
						<textarea
							value={scalarInputValue(currentQuestion)}
							maxlength={constraints.maxLength ?? undefined}
							disabled={disabled || saving}
							oninput={(event) => setAnswer(currentQuestion, event.currentTarget.value)}
						></textarea>
					{:else}
						<input
							type="text"
							value={scalarInputValue(currentQuestion)}
							maxlength={constraints.maxLength ?? undefined}
							disabled={disabled || saving}
							oninput={(event) => setAnswer(currentQuestion, event.currentTarget.value)}
						/>
					{/if}
				</label>
			{/if}
		</article>

		<div class="runner-footer">
			<div class="runner-status" aria-live="polite">{saveStatusMessage}</div>

			{#if localError || actionError}
				<p class="error-line" role="alert">{localError ?? actionError}</p>
			{/if}

			<div class="runner-actions">
				<button
					type="button"
					class="secondary-button"
					disabled={disabled || saving || currentIndex === 0}
					onclick={goBack}
				>
					<ArrowLeft size={17} aria-hidden="true" />
					<span>{backLabel}</span>
				</button>

				{#if isLastQuestion()}
					<button type="button" class="primary-button" disabled={disabled || saving} onclick={requestReview}>
						{#if saving}
							<LoaderCircle size={17} aria-hidden="true" />
						{:else}
							<Check size={17} aria-hidden="true" />
						{/if}
						<span>{reviewLabel}</span>
					</button>
				{:else}
					<button type="button" class="primary-button" disabled={disabled || saving} onclick={goNext}>
						<span>{nextLabel}</span>
						<ArrowRight size={17} aria-hidden="true" />
					</button>
				{/if}
			</div>
		</div>
	{:else}
		<section class="question-stage">
			<h2>{noQuestionsAvailableLabel}</h2>
		</section>
	{/if}
</section>

<style>
	.respondent-runner {
		display: grid;
		gap: 1rem;
	}

	.runner-header,
	.question-stage,
	.runner-footer {
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		box-shadow: 0 8px 20px color-mix(in srgb, var(--color-text) 6%, transparent);
	}

	.runner-header {
		display: grid;
		gap: 0.65rem;
		padding: 0.9rem 1rem;
	}

	.runner-progress-copy {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		font-size: 0.8rem;
		font-weight: 700;
		color: var(--color-text-muted);
		text-transform: uppercase;
	}

	.runner-progress {
		height: 0.45rem;
		overflow: hidden;
		border-radius: 999px;
		background: color-mix(in srgb, var(--color-border) 72%, transparent);
	}

	.runner-progress span {
		display: block;
		height: 100%;
		border-radius: inherit;
		background: var(--color-primary);
		transition: width 160ms ease;
	}

	.question-stage {
		display: grid;
		gap: 1.25rem;
		min-height: min(58vh, 34rem);
		align-content: start;
		padding: clamp(1rem, 4vw, 1.5rem);
	}

	.question-heading {
		display: grid;
		gap: 0.45rem;
	}

	.question-heading h2 {
		margin: 0;
		font-size: clamp(1.45rem, 4vw, 2rem);
		line-height: 1.12;
		letter-spacing: 0;
	}

	.question-code,
	.question-required,
	.control-help,
	.runner-status {
		margin: 0;
		font-size: 0.82rem;
		font-weight: 700;
		color: var(--color-text-muted);
		text-transform: uppercase;
	}

	.question-required {
		color: var(--color-primary);
	}

	.scale-answers {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(4.25rem, 1fr));
		gap: 0.65rem;
	}

	.choice-answers,
	.ranking-answers,
	.matrix-answers {
		display: grid;
		gap: 0.7rem;
	}

	.answer-option {
		display: flex;
		min-height: 3.25rem;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		color: var(--color-text);
		padding: 0.85rem 1rem;
		text-align: left;
		font-weight: 750;
		transition:
			border-color 140ms ease,
			background 140ms ease,
			color 140ms ease,
			transform 140ms ease;
	}

	.answer-option:hover:not(:disabled) {
		border-color: var(--color-primary);
		transform: translateY(-1px);
	}

	.answer-option.selected {
		border-color: var(--color-primary);
		background: color-mix(in srgb, var(--color-primary) 11%, var(--color-surface));
		color: var(--color-primary);
	}

	.answer-option:disabled {
		cursor: not-allowed;
		opacity: 0.65;
	}

	.scale-answer {
		min-height: 5rem;
		flex-direction: column;
		align-items: flex-start;
		justify-content: center;
	}

	.answer-option__value {
		font-size: 1.55rem;
		line-height: 1;
	}

	.answer-option__meta {
		font-size: 0.8rem;
		color: var(--color-text-muted);
	}

	.scale-anchors {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		color: var(--color-text-muted);
		font-size: 0.85rem;
	}

	.ranking-answer {
		justify-content: flex-start;
	}

	.ranking-position {
		display: inline-grid;
		width: 2rem;
		height: 2rem;
		place-items: center;
		border: 1px solid var(--color-border);
		color: var(--color-text-muted);
		font-size: 0.85rem;
	}

	.selected .ranking-position {
		border-color: var(--color-primary);
		background: var(--color-primary);
		color: var(--color-surface);
	}

	.matrix-row {
		display: grid;
		gap: 0.55rem;
	}

	.matrix-row > p {
		margin: 0;
		font-weight: 750;
	}

	.matrix-cells {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(8rem, 1fr));
		gap: 0.5rem;
	}

	.matrix-cell {
		justify-content: center;
		text-align: center;
	}

	.runner-field {
		display: grid;
		gap: 0.45rem;
	}

	.runner-field span {
		font-size: 0.82rem;
		font-weight: 700;
		color: var(--color-text-muted);
		text-transform: uppercase;
	}

	.runner-field input,
	.runner-field textarea {
		width: 100%;
		border: 1px solid var(--color-border);
		background: var(--color-surface);
		color: var(--color-text);
		padding: 0.8rem 0.9rem;
		font: inherit;
	}

	.runner-field textarea {
		min-height: 9rem;
		resize: vertical;
	}

	.runner-footer {
		display: grid;
		gap: 0.85rem;
		padding: 1rem;
	}

	.runner-actions {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
		gap: 0.75rem;
	}

	.runner-actions :global(.primary-button),
	.runner-actions :global(.secondary-button) {
		width: 100%;
		justify-content: center;
	}

	@media (max-width: 520px) {
		.runner-actions {
			grid-template-columns: 1fr;
		}

		.question-stage {
			min-height: min(54vh, 30rem);
		}
	}
</style>
