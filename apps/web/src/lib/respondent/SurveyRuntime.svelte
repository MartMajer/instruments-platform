<script lang="ts">
	import { onDestroy } from 'svelte';
	import type { SurveyModel } from 'survey-core';
	import type { RespondentQuestionResponse } from '$lib/api/setup';
	import {
		buildRespondentSurveyJson,
		normalizeSurveyDataToAnswers,
		toSurveyInitialData
	} from '$lib/respondent/surveyjs-adapter';

	let {
		questions,
		answers,
		disabled = false,
		onAnswersChange,
		onRuntimeError
	}: {
		questions: RespondentQuestionResponse[];
		answers: Record<string, string>;
		disabled?: boolean;
		onAnswersChange?: (answers: Record<string, string>) => void;
		onRuntimeError?: (message: string) => void;
	} = $props();

	let container = $state<HTMLDivElement | null>(null);
	let surveyModel = $state<SurveyModel | null>(null);
	let renderVersion = 0;
	let unsubscribeInputBridge: (() => void) | null = null;

	$effect(() => {
		if (!container || questions.length === 0) {
			return;
		}

		const currentVersion = ++renderVersion;
		void renderSurveyRuntime(currentVersion, container);

		return () => {
			disposeSurvey();
		};
	});

	$effect(() => {
		if (surveyModel) {
			surveyModel.mode = disabled ? 'display' : 'edit';
		}
	});

	onDestroy(() => {
		disposeSurvey();
	});

	async function renderSurveyRuntime(version: number, target: HTMLDivElement) {
		try {
			await import('survey-core/survey-core.min.css');
			const [{ Model }, { renderSurvey }] = await Promise.all([
				import('survey-core'),
				import('survey-js-ui')
			]);

			if (version !== renderVersion) {
				return;
			}

			disposeSurvey();

			const model = new Model(buildRespondentSurveyJson(questions));
			model.data = toSurveyInitialData(questions, answers);
			model.mode = disabled ? 'display' : 'edit';
			model.showCompleteButton = false;
			model.showNavigationButtons = false;
			model.onValueChanging.add((sender, options) => {
				onAnswersChange?.({
					...normalizeSurveyDataToAnswers(questions, sender.data as Record<string, unknown>),
					[options.name]: options.value === undefined || options.value === null ? '' : String(options.value)
				});
			});
			model.onValueChanged.add((sender) => {
				onAnswersChange?.(
					normalizeSurveyDataToAnswers(questions, sender.data as Record<string, unknown>)
				);
			});

			renderSurvey(model, target);
			attachInputBridge(target, model);
			surveyModel = model;
		} catch (error) {
			const message =
				error instanceof Error ? error.message : 'Survey runtime could not be loaded.';
			onRuntimeError?.(message);
		}
	}

	function disposeSurvey() {
		unsubscribeInputBridge?.();
		unsubscribeInputBridge = null;
		surveyModel?.dispose?.();
		surveyModel = null;

		if (container) {
			container.innerHTML = '';
		}
	}

	function attachInputBridge(target: HTMLDivElement, model: SurveyModel) {
		const handleInput = () => {
			onAnswersChange?.(readRenderedInputAnswers(target, model));
		};

		target.addEventListener('input', handleInput, true);
		unsubscribeInputBridge = () => target.removeEventListener('input', handleInput, true);
	}

	function readRenderedInputAnswers(target: HTMLDivElement, model: SurveyModel) {
		const nextAnswers = normalizeSurveyDataToAnswers(
			questions,
			model.data as Record<string, unknown>
		);
		const sortedQuestions = questions.slice().sort((left, right) => left.ordinal - right.ordinal);
		const inputs = Array.from(
			target.querySelectorAll<HTMLInputElement>(
				'input:not([type="button"]):not([type="submit"]):not([type="hidden"])'
			)
		);

		for (const [index, question] of sortedQuestions.entries()) {
			const input = inputs[index];
			if (input) {
				nextAnswers[question.id] = input.value;
			}
		}

		return nextAnswers;
	}
</script>

<section aria-label="Survey questions" data-testid="surveyjs-runtime">
	<div bind:this={container}></div>
</section>
