import type { AppLocale } from './localization';

export type AppMessageValues = Record<string, number | string | null | undefined>;

type AppMessageTemplate = string | ((values: AppMessageValues, locale: AppLocale) => string);
type PluralCategory = 'zero' | 'one' | 'two' | 'few' | 'many' | 'other';
type CountNounId =
	| 'answerMetadataField'
	| 'answerVariable'
	| 'column'
	| 'comparedOutput'
	| 'comparisonScore'
	| 'invitationPair'
	| 'linkedPair'
	| 'measurement'
	| 'question'
	| 'reportSummaryColumn'
	| 'response'
	| 'row'
	| 'score'
	| 'scoreMetadataField'
	| 'scoreOutput'
	| 'selection';

type CountNounForms = Record<PluralCategory | 'other', string>;

const enMessages = {
	'results.packet.responses.label': 'Responses',
	'results.packet.scores.label': 'Scores',
	'results.packet.exportFiles.label': 'Export files',
	'results.packet.useStatus.label': 'Use status',
	'results.packet.responses.collected': (values, locale) =>
		localizedCountPhrase(locale, numberValue(values, 'count'), collectedResponsePhrases),
	'results.packet.scores.visible': (values, locale) =>
		localizedCountPhrase(locale, numberValue(values, 'count'), visibleScorePhrases),
	'results.packet.rawResponses.detail':
		'Raw submitted responses exist. They can be exported for internal analysis when an export file is created.',
	'results.packet.scoredResults.detail':
		'Scored results are visible for internal review. Keep score meaning and method notes with any exported analysis.',
	'results.packet.responseDatasetReady.summary': 'Response dataset ready',
	'results.packet.responseDatasetReady.detail':
		'Use this CSV and codebook for analysis. Keep method and interpretation notes with the file.',
	'results.packet.internalReviewOnly.summary': 'Internal review only',
	'results.packet.internalReviewOnly.detail':
		'Use these results inside the workspace while export, interpretation, or collection status still needs review.',
	'results.packet.noWaveSelected.summary': 'No wave selected',
	'results.packet.noWaveSelected.detail': 'Select a wave before deciding how results can be used.',
	'results.packet.noResultData.summary': 'No result data yet',
	'results.packet.noResultData.detail': 'Collect responses before using or exporting results.',
	'results.packet.rawResponsesOnly.summary': 'Raw responses only',
	'results.packet.rawResponsesOnly.detail':
		'Do not present scored results yet. Use raw responses internally or fix scoring, missing-answer rules, and disclosure.',
	'results.packet.controlledSharing.summary': 'Ready for controlled sharing',
	'results.packet.controlledSharing.detail':
		'Response dataset, visible scores, reviewed interpretation, and closed collection are in place. Keep disclosure and study-method notes with anything shared.',

	'results.method.outputs.label': 'Score outputs',
	'results.method.coverage.label': 'Response coverage',
	'results.method.directionScale.label': 'Direction and scale',
	'results.method.missingAnswers.label': 'Missing answers',
	'results.method.interpretationBoundary.label': 'Interpretation boundary',
	'results.method.outputs.pending.summary': 'Output names available after reviewing results',
	'results.method.outputs.pending.detail':
		'Use Review results to load the score output rows. Results does not infer score meaning from hidden setup data.',
	'results.method.outputs.countList': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'scoreOutput')}: ${textValue(
			values,
			'outputs'
		)}`,
	'results.method.coverage.scored': (values, locale) =>
		locale === 'hr-HR'
			? `${formatNumber(locale, numberValue(values, 'scored'))} od ${formatNumber(
					locale,
					numberValue(values, 'total')
				)} predanih odgovora ima izračun rezultata`
			: `${formatNumber(locale, numberValue(values, 'scored'))} of ${formatNumber(
					locale,
					numberValue(values, 'total')
				)} submitted responses scored`,
	'results.method.coverage.submittedVisible': (values, locale) =>
		locale === 'hr-HR'
			? `${formatNumber(locale, numberValue(values, 'submitted'))} predanih odgovora, ${formatNumber(
					locale,
					numberValue(values, 'visible')
				)} vidljivih redaka rezultata`
			: `${formatNumber(locale, numberValue(values, 'submitted'))} submitted responses, ${formatNumber(
					locale,
					numberValue(values, 'visible')
				)} visible score rows`,
	'results.method.coverage.allScored.detail': 'All submitted responses have successful scoring activity.',
	'results.method.directionScale.pending.summary': 'Direction and scale family need setup context',
	'results.method.directionScale.pending.detail':
		'Results can show output rows and missingness, but question-to-output coverage, reverse scoring, and answer scale family still need the Setup scoring plan. Do not infer better/worse meaning from output codes alone.',
	'results.method.missingAnswers.pending.summary':
		'Missing-answer metadata available after reviewing results',
	'results.method.missingAnswers.pending.detail':
		'Use Review results to load valid/expected answer contribution counts where available.',
	'results.method.missingAnswers.incomplete.summary': 'Some score inputs were incomplete',
	'results.method.missingAnswers.incomplete.detail': (values, locale) =>
		locale === 'hr-HR'
			? `${textValue(values, 'dimension')} koristi ${formatNumber(
					locale,
					numberValue(values, 'valid')
				)} od ${formatNumber(locale, numberValue(values, 'expected'))} očekivanih doprinosa odgovora`
			: `${textValue(values, 'dimension')} used ${formatNumber(
					locale,
					numberValue(values, 'valid')
				)} of ${formatNumber(locale, numberValue(values, 'expected'))} expected answer contributions`,
	'results.method.interpretation.custom.summary':
		'Custom-study interpretation, not externally validated',
	'results.method.interpretation.custom.detail':
		'Use these scores as tenant-defined custom-study calculations. Do not present them as official norms, benchmarks, clinical thresholds, or externally validated claims.',

	'results.export.filePurpose.label': 'File purpose',
	'results.export.rowShape.label': 'Row shape',
	'results.export.waveFields.label': 'Wave fields',
	'results.export.trajectoryKeys.label': 'Trajectory keys',
	'results.export.variablesValues.label': 'Variables and values',
	'results.export.missingness.label': 'Missingness',
	'results.export.scoreOutputs.label': 'Score outputs',
	'results.export.pending.filePurpose.summary': 'Review export file to inspect contents',
	'results.export.pending.rowShape.summary': 'Row shape available after file review',
	'results.export.pending.waveFields.summary': 'Wave fields available after file review',
	'results.export.pending.trajectoryKeys.summary':
		'Trajectory key policy available after file review',
	'results.export.pending.variablesValues.summary':
		'Variables and values available after file review',
	'results.export.pending.missingness.summary':
		'Missingness policy available after file review',
	'results.export.pending.scoreOutputs.summary': 'Score outputs available after file review',
	'results.export.responseDataset.summary': 'Response dataset CSV and codebook',
	'results.export.responseDataset.detail':
		'Use this for row-level analysis. Keep method, disclosure, and interpretation notes with the file.',
	'results.export.reportSummary.summary': 'Report-summary CSV, not row-level response data',
	'results.export.reportSummary.detail':
		'Use this for internal aggregate review. Create a response dataset when you need submitted-response rows.',
	'results.export.unknownFile.detail': 'Review this file type before using it outside the workspace.',
	'results.export.rowShape.responseRows': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'row')}; ${
			locale === 'hr-HR'
				? 'jedan redak po predanom odgovoru'
				: 'one row per submitted response'
		}`,
	'results.export.rowShape.responseRows.detail':
		'Each row represents one submitted response session in this study export.',
	'results.export.rowShape.scoreRows': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'row')}; ${
			locale === 'hr-HR'
				? 'jedan redak po vidljivom ili skrivenom izlazu rezultata'
				: 'one row per visible or suppressed score output'
		}`,
	'results.export.rowShape.scoreRows.detail':
		'Each row summarizes one score output for the selected wave, not one respondent.',
	'results.export.rowShape.rows': (values, locale) =>
		formatCount(locale, numberValue(values, 'count'), 'row'),
	'results.export.rowShape.generic.detail': 'Review the codebook before interpreting row meaning.',
	'results.export.waveFields.included.summary': 'Wave fields included',
	'results.export.waveFields.includedForWaves': (values, locale) =>
		locale === 'hr-HR'
			? `Polja mjerenja uključena su za ${formatCount(
					locale,
					numberValue(values, 'count'),
					'measurement'
				)}`
			: `Wave fields included for ${formatCount(
					locale,
					numberValue(values, 'count'),
					'measurement'
				)}`,
	'results.export.waveFields.reportSummary.summary': 'Selected-wave lifecycle fields included',
	'results.export.waveFields.reportSummary.detail':
		'The report-summary file describes the selected wave and its finality, not every wave in the study.',
	'results.export.waveFields.notDetected.summary': 'Wave fields not detected',
	'results.export.waveFields.grouping.detail':
		'Review the codebook column list before using wave-level grouping.',
	'results.export.trajectoryKeys.reportSummary.summary':
		'No trajectory keys in report-summary export',
	'results.export.trajectoryKeys.reportSummary.detail':
		'Report-summary exports are aggregate files and do not include respondent trajectory rows.',
	'results.export.trajectoryKeys.included.summary': 'Artifact-local trajectory keys included',
	'results.export.trajectoryKeys.policy.detail': (values, locale) =>
		locale === 'hr-HR'
			? `Ključevi praćenja su ${textValue(
					values,
					'policy'
				)} i ne smiju se tretirati kao izvorni kodovi sudionika ili ponovno upotrebljivi identifikatori.`
			: `Trajectory ids are ${textValue(
					values,
					'policy'
				)} and should not be treated as raw participant codes or reusable identifiers.`,
	'results.export.trajectoryKeys.noColumn.summary': 'No trajectory key column detected',
	'results.export.trajectoryKeys.none.summary': 'No trajectory keys',
	'results.export.trajectoryKeys.usage.detail':
		'Use trajectory fields only when anonymous longitudinal linking is present and disclosure allows it.',
	'results.export.variables.responseDatasetSummary': (values, locale) => {
		const answerMetadataCount = numberValue(values, 'answerMetadataCount');
		const answerMetadata =
			answerMetadataCount > 0
				? `, ${formatCount(locale, answerMetadataCount, 'answerMetadataField')}`
				: '';
		return `${formatCount(locale, numberValue(values, 'answerCount'), 'answerVariable')}, ${formatCount(
			locale,
			numberValue(values, 'scoreMetadataCount'),
			'scoreMetadataField'
		)}${answerMetadata}, ${
			locale === 'hr-HR'
				? `ukupno ${formatCount(locale, numberValue(values, 'columnCount'), 'column')}`
				: `${formatCount(locale, numberValue(values, 'columnCount'), 'column')} total`
		}`;
	},
	'results.export.variables.responseDataset.detail':
		'Question columns include codebook metadata such as question type, missing codes, scale anchors, value labels and answer constraints when available.',
	'results.export.variables.reportSummaryColumns': (values, locale) =>
		formatCount(locale, numberValue(values, 'count'), 'reportSummaryColumn'),
	'results.export.variables.reportSummary.detail':
		'Columns describe aggregate score output, disclosure, finality, score metadata, and tenant-defined interpretation fields.',
	'results.export.variables.columnsDetected': (values, locale) =>
		locale === 'hr-HR'
			? `Otkriveno ${formatCount(locale, numberValue(values, 'count'), 'column')}`
			: `${formatCount(locale, numberValue(values, 'count'), 'column')} detected`,
	'results.export.variables.codebook.detail':
		'Review the codebook before using column values.',
	'results.export.missingness.codesDocumented.summary': 'Missing-answer codes documented',
	'results.export.missingness.codesNotDetected.summary': 'Missing-answer codes not detected',
	'results.export.missingness.responseDataset.detail':
		'Use the codebook missing-treatment fields and question missing codes before treating blanks as real answers.',
	'results.export.missingness.scoreFields.summary': 'Score missingness fields included',
	'results.export.missingness.scoreFieldsNotDetected.summary':
		'Score missingness fields not detected',
	'results.export.missingness.reportSummary.detail':
		'Report-summary missingness describes valid/expected score contributions, not respondent-level skipped answers.',
	'results.export.missingness.fields.summary': 'Missingness fields included',
	'results.export.missingness.fieldsNotDetected.summary': 'Missingness fields not detected',
	'results.export.missingness.review.detail': 'Review missingness fields before analysis.',
	'results.export.scoreOutputs.metadataFor': (values, locale) =>
		locale === 'hr-HR'
			? `Metapodaci rezultata za ${textValue(values, 'dimensions')}`
			: `Score metadata for ${textValue(values, 'dimensions')}`,
	'results.export.scoreOutputs.noMetadata.summary': 'No score metadata columns detected',
	'results.export.scoreOutputs.responseDataset.detail':
		'Response datasets include score metadata fields when scores exist; keep score method notes with analysis.',
	'results.export.scoreOutputs.dimensionCode.summary':
		'Score outputs listed in dimension_code',
	'results.export.scoreOutputs.noColumn.summary': 'Score output column not detected',
	'results.export.scoreOutputs.dimensionCode.detail':
		'Use dimension_code with score_count and disclosure fields to understand aggregate score rows.',
	'results.export.scoreOutputs.notDetected.summary': 'Score outputs not detected',
	'results.export.scoreOutputs.review.detail': 'Review score output fields before interpretation.',

	'waves.review.waveSequence.label': 'Wave sequence',
	'waves.review.waveSequence.noWave.summary': 'No wave exists yet',
	'waves.review.waveSequence.noWave.detail':
		'Create Wave 1 in Setup, launch it from Collection, then return here after responses arrive.',
	'waves.review.waveSequence.oneWave.summary': 'Only Wave 1 exists',
	'waves.review.waveSequence.oneWave.detail':
		'Review Wave 1 in Results before deciding whether Wave 2 is needed for a follow-up collection.',
	'waves.review.waveSequence.multiple.summary': (values, locale) =>
		locale === 'hr-HR'
			? `Postoje ${formatCount(locale, numberValue(values, 'count'), 'measurement')}`
			: `${formatCount(locale, numberValue(values, 'count'), 'measurement')} exist`,
	'waves.review.waveSequence.multiple.detail':
		'Review the latest two waves below. Add another wave only when a new collection round is actually planned.',
	'waves.review.comparisonType.label': 'Comparison type',
	'waves.review.comparisonType.noComparison.summary': 'No comparison yet',
	'waves.review.comparisonType.noComparison.detail':
		'A comparison needs at least two waves with responses.',
	'waves.review.comparisonType.sameRespondent.summary': 'Same-respondent linked change',
	'waves.review.comparisonType.sameRespondent.detail':
		'These waves have repeat-participation linking, scoring compatibility, and disclosure-visible comparison output.',
	'waves.review.comparisonType.linkedNeedsChecks.summary': 'Linked comparison needs checks',
	'waves.review.comparisonType.linkedNeedsChecks.detail':
		'The study has repeat-participation waves, but linked pairs, scoring compatibility, or disclosure output still need confirmation.',
	'waves.review.comparisonType.groupTrend.summary': 'Group trend only',
	'waves.review.comparisonType.groupTrend.detail':
		'These waves can show aggregate movement between rounds, but not individual respondent change.',
	'waves.review.comparisonType.collectTwo.detail':
		'Collect responses in at least two waves before reviewing change over time.',
	'waves.review.dataReadiness.label': 'Data readiness',
	'waves.review.dataReadiness.linkedReady.summary': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'linkedPairs'), 'linkedPair')}, ${formatCount(
			locale,
			numberValue(values, 'visibleScores'),
			'comparisonScore'
		)}`,
	'waves.review.dataReadiness.linkedReady.detail':
		'The linked comparison is visible after disclosure and scoring checks. Suppressed scores stay hidden.',
	'waves.review.dataReadiness.followUpFirst.summary': 'Collect a follow-up wave first',
	'waves.review.dataReadiness.followUpFirst.detail':
		'One wave can be reviewed in Results, but it cannot show change over time.',
	'waves.review.dataReadiness.groupTrendReady.summary': (values, locale) =>
		locale === 'hr-HR'
			? `${formatCount(locale, numberValue(values, 'firstScores'), 'score')} u prvom mjerenju, ${formatCount(
					locale,
					numberValue(values, 'secondScores'),
					'score'
				)} u drugom mjerenju`
			: `${formatCount(locale, numberValue(values, 'firstScores'), 'score')} first-wave, ${formatCount(
					locale,
					numberValue(values, 'secondScores'),
					'score'
				)} second-wave`,
	'waves.review.dataReadiness.groupTrendPending.summary':
		'Finish score output before reading the trend',
	'waves.review.dataReadiness.groupTrend.detail':
		'Use Results for the actual score tables and exports before making claims from this trend.',
	'waves.review.dataReadiness.comparisonNotReady.summary': 'Comparison output is not ready',
	'waves.review.dataReadiness.comparisonNotReady.detail':
		'Run the checks below to see whether linked pairs, scoring, or disclosure are blocking review.',
	'waves.review.claimBoundary.label': 'Claim boundary',
	'waves.review.claimBoundary.sameReady.summary':
		'Disclosure-gated custom-study comparison',
	'waves.review.claimBoundary.sameReady.detail':
		'Describe this as change in this tenant-provided custom study, not as an official benchmark or clinical threshold.',
	'waves.review.claimBoundary.groupTrend.summary':
		'Do not call this same-respondent change',
	'waves.review.claimBoundary.groupTrend.detail':
		'Use group-level wording such as aggregate trend between waves unless repeat-participation linking is ready.',
	'waves.review.claimBoundary.oneWave.summary': 'Current results are wave-level only',
	'waves.review.claimBoundary.oneWave.detail':
		'Review Wave 1 on its own. Do not describe movement until a follow-up wave exists.',
	'waves.review.claimBoundary.noClaim.summary': 'No change claim available',
	'waves.review.claimBoundary.noClaim.detail':
		'Create and collect waves before writing any change-over-time interpretation.',

	'waves.method.scoringRules.label': 'Scoring rules',
	'waves.method.comparisonMethod.label': 'Comparison method',
	'waves.method.outputs.label': 'Compared outputs',
	'waves.method.missingness.label': 'Missing answers',
	'waves.method.interpretationBoundary.label': 'Interpretation boundary',
	'waves.method.rule.notConfigured': 'not configured',
	'waves.method.scoringRules.twoWavesNeeded.summary': 'Two waves needed',
	'waves.method.scoringRules.twoWavesNeeded.detail':
		'Select or create two waves before comparing scoring rules.',
	'waves.method.scoringRules.sameRule.summary': (values, locale) =>
		locale === 'hr-HR'
			? `Mjerenje 1 i Mjerenje 2 koriste ${textValue(values, 'rule')}`
			: `Wave 1 and Wave 2 use ${textValue(values, 'rule')}`,
	'waves.method.scoringRules.sameRule.detail':
		'The selected waves use the same scoring rule key and version.',
	'waves.method.scoringRules.different.summary': (values, locale) =>
		locale === 'hr-HR'
			? `Mjerenje 1 koristi ${textValue(values, 'baselineRule')}; Mjerenje 2 koristi ${textValue(
					values,
					'comparisonRule'
				)}`
			: `Wave 1 uses ${textValue(values, 'baselineRule')}; Wave 2 uses ${textValue(
					values,
					'comparisonRule'
				)}`,
	'waves.method.scoringRules.different.detail':
		'Review compatibility before describing score movement between waves.',
	'waves.method.comparisonMethod.waveOnly.summary': 'Wave-level review only',
	'waves.method.comparisonMethod.waveOnly.detail':
		'Same-respondent change needs repeat-participation waves. Anonymous unlinked waves support aggregate group trend only.',
	'waves.method.comparisonMethod.linked.summary': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'linkedPairs'), 'linkedPair')}, ${formatCount(
			locale,
			numberValue(values, 'visibleScores'),
			'comparisonScore'
		)}`,
	'waves.method.comparisonMethod.linked.detail':
		'Linked change uses repeat-participation trajectories after scoring compatibility and disclosure checks.',
	'waves.method.outputs.noRepeated.summary': 'No linked outputs yet',
	'waves.method.outputs.noRepeated.detail':
		'Compared output names appear after repeated waves are ready for linked change.',
	'waves.method.outputs.ready.summary': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'comparedOutput')}: ${textValue(
			values,
			'outputs'
		)}`,
	'waves.method.outputs.ready.detail':
		'These output codes come from the linked-change proof. Keep the scoring plan with any interpretation.',
	'waves.method.outputs.pending.summary':
		'Compared output names available after reviewing linked change',
	'waves.method.outputs.blocked.summary': 'No visible compared outputs yet',
	'waves.method.outputs.review.detail':
		'Run Review linked change to load the compared score output rows.',
	'waves.method.missingness.noRepeated.summary': 'No linked missingness yet',
	'waves.method.missingness.noRepeated.detail':
		'Missing-answer comparison metadata appears after repeated waves are ready.',
	'waves.method.missingness.pending.summary':
		'Missing-answer metadata available after reviewing linked change',
	'waves.method.missingness.pending.detail':
		'Run Review linked change to load valid/expected answer contribution counts.',
	'waves.method.missingness.complete.summary': 'No missing-score input gap in comparison preview',
	'waves.method.missingness.complete.detail':
		'The compared outputs reported complete valid/expected answer contribution counts.',
	'waves.method.missingness.incomplete.summary':
		'Some compared score inputs were incomplete',
	'waves.method.missingness.incomplete.baseline': (values, locale) =>
		locale === 'hr-HR'
			? `${textValue(values, 'dimension')} u početnom mjerenju koristi ${formatNumber(
					locale,
					numberValue(values, 'valid')
				)} od ${formatNumber(locale, numberValue(values, 'expected'))} očekivanih doprinosa odgovora`
			: `${textValue(values, 'dimension')} baseline used ${formatNumber(
					locale,
					numberValue(values, 'valid')
				)} of ${formatNumber(locale, numberValue(values, 'expected'))} expected answer contributions`,
	'waves.method.missingness.incomplete.followUp': (values, locale) =>
		locale === 'hr-HR'
			? `${textValue(values, 'dimension')} u usporednom mjerenju koristi ${formatNumber(
					locale,
					numberValue(values, 'valid')
				)} od ${formatNumber(locale, numberValue(values, 'expected'))} očekivanih doprinosa odgovora`
			: `${textValue(values, 'dimension')} follow-up used ${formatNumber(
					locale,
					numberValue(values, 'valid')
				)} of ${formatNumber(locale, numberValue(values, 'expected'))} expected answer contributions`,
	'waves.method.interpretation.noWave.summary': 'No wave selected',
	'waves.method.interpretation.noWave.detail':
		'Create a wave before reviewing interpretation boundaries.',
	'waves.method.interpretation.waveOnly.summary': 'Wave-level results only',
	'waves.method.interpretation.waveOnly.detail':
		'Do not describe change until a follow-up wave exists.',
	'waves.method.interpretation.ready.summary':
		'Interpretation reviewed for this comparison',
	'waves.method.interpretation.ready.detail':
		'Keep disclosure and method notes with any wave-comparison export or report.',
	'waves.method.interpretation.custom.summary': 'Custom-study change, not a benchmark',
	'waves.method.interpretation.custom.detail':
		'Describe this as change in this tenant-defined study only. Do not present it as an official benchmark, norm, clinical threshold, or externally validated claim.',

	'setup.status.notEditable': 'not editable',
	'setup.status.draft': 'draft',
	'setup.status.scheduled': 'scheduled',
	'setup.status.live': 'live',
	'setup.status.closed': 'closed',
	'setup.launchState.savedSelections': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'selectionCount'), 'selection')} saved, ${formatCount(
			locale,
			numberValue(values, 'pairCount'),
			'invitationPair'
		)} ready.`,
	'setup.launchPlan.waveDraftReady': (values) =>
		`${textValue(values, 'waveName')} is the draft wave for this study.`,
	'setup.launchPlan.waveWillBeCreated': (values) =>
		`${textValue(values, 'waveName')} will be created when you save this step.`,
	'setup.launchPlan.savedRecipientDetail': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'selectionCount'), 'selection')} saved with ${formatCount(
			locale,
			numberValue(values, 'pairCount'),
			'invitationPair'
		)}.`,
	'setup.designMap.questionnaireSaved': (values, locale) =>
		`${textValue(values, 'name')} is saved with ${formatCount(
			locale,
			numberValue(values, 'questionCount'),
			'question'
		)}.`,
	'setup.designMap.resultsReady': (values) =>
		`Result outputs are saved as ${textValue(values, 'ruleKey')}.`,
	'setup.designMap.draftWaveNeedsReadiness': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'measurement')} ${
			numberValue(values, 'count') === 1 ? 'is' : 'are'
		} prepared as draft; launch readiness still needs attention.`,
	'setup.designMap.waveReady': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'measurement')} ${
			numberValue(values, 'count') === 1 ? 'is' : 'are'
		} ready for Collection.`,
	'setup.designMap.liveWave': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'measurement')} ${
			numberValue(values, 'count') === 1 ? 'is' : 'are'
		} collecting responses.`,
	'setup.designMap.closedWave': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'measurement')} ${
			numberValue(values, 'count') === 1 ? 'has' : 'have'
		} closed data for Results review.`,
	'setup.waveContext.prepareForCollection': (values) =>
		`Prepare ${textValue(values, 'waveName')} for collection`,
	'setup.waveContext.followUpDraftSummary': (values) =>
		`${textValue(values, 'waveName')} is a draft follow-up wave. Use it only when the next collection round is intentional.`,
	'setup.waveContext.closedOneWaveSummary': (values) =>
		`${textValue(values, 'previousWaveName')} is already ${textValue(
			values,
			'previousWaveStatus'
		)}. Create ${textValue(values, 'nextWaveName')} only when the next collection round is intentional.`,
	'setup.waveContext.multipleWaveSummary': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'existingWaveCount'), 'measurement')} already exist. Create ${textValue(
			values,
			'nextWaveName'
		)} only after the current wave results have been reviewed.`,
	'setup.waveContext.recipientBelongsUntilLaunch': (values) =>
		`Recipient selection belongs to ${textValue(values, 'waveName')} until this wave is launched.`,
	'setup.waveContext.reviewBeforePreparing': (values) =>
		`Review ${textValue(values, 'previousWaveName')} before preparing ${textValue(
			values,
			'nextWaveName'
		)}`,
	'setup.waveContext.reviewExistingBeforePreparing': (values) =>
		`Review existing waves before preparing ${textValue(values, 'nextWaveName')}`,
	'setup.waveContext.openResultsBeforeCreating': (values) =>
		`Open Results to review or export ${textValue(values, 'reviewTarget')} before creating ${textValue(
			values,
			'nextWaveName'
		)}.`,
	'setup.waveContext.createOnlyWhenIntentional': (values) =>
		`Create ${textValue(values, 'nextWaveName')} only when the next collection round is intentional.`,
	'setup.waveContext.recipientBelongsToNewDraft': (values) =>
		`Recipient selection in this step will belong to the new draft wave, not to ${textValue(
			values,
			'previousLabel'
		)}.`
} satisfies Record<string, AppMessageTemplate>;

export type AppMessageId = keyof typeof enMessages;
type AppMessageCatalog = Record<AppMessageId, AppMessageTemplate>;

const hrMessages: AppMessageCatalog = {
	...enMessages,
	'results.packet.responses.label': 'Odgovori',
	'results.packet.scores.label': 'Rezultati',
	'results.packet.exportFiles.label': 'Datoteke izvoza',
	'results.packet.useStatus.label': 'Status korištenja',
	'results.packet.rawResponses.detail':
		'Postoje predani odgovori. Mogu se izvesti za internu analizu nakon izrade datoteke izvoza.',
	'results.packet.scoredResults.detail':
		'Izračunati rezultati vidljivi su za interni pregled. Uz svaku izvezenu analizu zadržite značenje rezultata i bilješke o metodi.',
	'results.packet.responseDatasetReady.summary': 'Skup odgovora je spreman',
	'results.packet.responseDatasetReady.detail':
		'Ovaj CSV i knjigu kodova koristite za analizu. Uz datoteku zadržite bilješke o metodi i tumačenju.',
	'results.packet.internalReviewOnly.summary': 'Samo interni pregled',
	'results.packet.internalReviewOnly.detail':
		'Ove rezultate koristite unutar radnog prostora dok izvoz, tumačenje ili status prikupljanja još trebaju pregled.',
	'results.packet.noWaveSelected.summary': 'Nije odabrano mjerenje',
	'results.packet.noWaveSelected.detail': 'Odaberite mjerenje prije odluke o korištenju rezultata.',
	'results.packet.noResultData.summary': 'Još nema podataka rezultata',
	'results.packet.noResultData.detail': 'Prikupite odgovore prije korištenja ili izvoza rezultata.',
	'results.packet.rawResponsesOnly.summary': 'Samo sirovi odgovori',
	'results.packet.rawResponsesOnly.detail':
		'Još nemojte predstavljati izračunate rezultate. Sirove odgovore koristite interno ili popravite bodovanje, pravila nedostajućih odgovora i pravila prikaza.',
	'results.packet.controlledSharing.summary': 'Spremno za kontrolirano dijeljenje',
	'results.packet.controlledSharing.detail':
		'Skup odgovora, vidljivi rezultati, pregledano tumačenje i zatvoreno prikupljanje su spremni. Uz sve što dijelite zadržite pravila prikaza i bilješke o metodi studije.',

	'results.method.outputs.label': 'Izlazi rezultata',
	'results.method.coverage.label': 'Pokrivenost odgovora',
	'results.method.directionScale.label': 'Smjer i ljestvica',
	'results.method.missingAnswers.label': 'Nedostajući odgovori',
	'results.method.interpretationBoundary.label': 'Granica tumačenja',
	'results.method.outputs.pending.summary': 'Nazivi izlaza dostupni su nakon pregleda rezultata',
	'results.method.outputs.pending.detail':
		'Upotrijebite Pregled rezultata za učitavanje redaka izlaza rezultata. Sustav ne zaključuje značenje rezultata iz skrivenih postavki.',
	'results.method.coverage.allScored.detail':
		'Svi predani odgovori imaju uspješno izračunane rezultate.',
	'results.method.directionScale.pending.summary':
		'Smjer i vrsta ljestvice trebaju kontekst iz postavki',
	'results.method.directionScale.pending.detail':
		'Rezultati mogu prikazati izlazne retke i nedostajuće vrijednosti, ali pokrivenost pitanja, obrnuto bodovanje i vrsta ljestvice još trebaju plan rezultata iz Postavki. Nemojte zaključivati bolje/lošije značenje samo iz kodova izlaza.',
	'results.method.missingAnswers.pending.summary':
		'Metapodaci o nedostajućim odgovorima dostupni su nakon pregleda rezultata',
	'results.method.missingAnswers.pending.detail':
		'Upotrijebite Pregled rezultata za učitavanje broja važećih i očekivanih doprinosa odgovora gdje su dostupni.',
	'results.method.missingAnswers.incomplete.summary': 'Neki ulazi rezultata nisu potpuni',
	'results.method.interpretation.custom.summary':
		'Tumačenje prilagođene studije, bez vanjske validacije',
	'results.method.interpretation.custom.detail':
		'Ove rezultate koristite kao izračune prilagođene studije ovog radnog prostora. Nemojte ih predstavljati kao službene norme, usporedne vrijednosti, kliničke pragove ili vanjski validirane tvrdnje.',

	'results.export.filePurpose.label': 'Namjena datoteke',
	'results.export.rowShape.label': 'Oblik redaka',
	'results.export.waveFields.label': 'Polja mjerenja',
	'results.export.trajectoryKeys.label': 'Ključevi praćenja',
	'results.export.variablesValues.label': 'Varijable i vrijednosti',
	'results.export.missingness.label': 'Nedostajuće vrijednosti',
	'results.export.pending.filePurpose.summary': 'Pregledajte datoteku izvoza za provjeru sadržaja',
	'results.export.pending.rowShape.summary': 'Oblik redaka dostupan je nakon pregleda datoteke',
	'results.export.pending.waveFields.summary': 'Polja mjerenja dostupna su nakon pregleda datoteke',
	'results.export.pending.trajectoryKeys.summary':
		'Pravila ključeva praćenja dostupna su nakon pregleda datoteke',
	'results.export.pending.variablesValues.summary':
		'Varijable i vrijednosti dostupne su nakon pregleda datoteke',
	'results.export.pending.missingness.summary':
		'Pravila nedostajućih vrijednosti dostupna su nakon pregleda datoteke',
	'results.export.pending.scoreOutputs.summary':
		'Izlazi rezultata dostupni su nakon pregleda datoteke',
	'results.export.responseDataset.summary': 'CSV skup odgovora i knjiga kodova',
	'results.export.responseDataset.detail':
		'Koristite ovo za analizu na razini redaka. Uz datoteku zadržite bilješke o metodi, pravilima prikaza i tumačenju.',
	'results.export.reportSummary.summary': 'CSV sažetka izvještaja, ne podaci odgovora po retku',
	'results.export.reportSummary.detail':
		'Koristite ovo za interni agregirani pregled. Izradite skup odgovora kada trebate retke predanih odgovora.',
	'results.export.unknownFile.detail':
		'Pregledajte ovu vrstu datoteke prije korištenja izvan radnog prostora.',
	'results.export.rowShape.responseRows.detail':
		'Svaki redak predstavlja jednu predanu sesiju odgovora u ovom izvozu studije.',
	'results.export.rowShape.scoreRows.detail':
		'Svaki redak sažima jedan izlaz rezultata za odabrano mjerenje, a ne jednog ispitanika.',
	'results.export.rowShape.generic.detail':
		'Pregledajte knjigu kodova prije tumačenja značenja redaka.',
	'results.export.waveFields.included.summary': 'Polja mjerenja su uključena',
	'results.export.waveFields.reportSummary.summary':
		'Uključena su polja životnog ciklusa odabranog mjerenja',
	'results.export.waveFields.reportSummary.detail':
		'Datoteka sažetka izvještaja opisuje odabrano mjerenje i njegovu finalnost, ne svako mjerenje u studiji.',
	'results.export.waveFields.notDetected.summary': 'Polja mjerenja nisu otkrivena',
	'results.export.waveFields.grouping.detail':
		'Pregledajte popis stupaca u knjizi kodova prije grupiranja po mjerenju.',
	'results.export.trajectoryKeys.reportSummary.summary':
		'Nema ključeva praćenja u izvozu sažetka izvještaja',
	'results.export.trajectoryKeys.reportSummary.detail':
		'Izvozi sažetka izvještaja su agregirane datoteke i ne uključuju retke praćenja ispitanika.',
	'results.export.trajectoryKeys.included.summary':
		'Uključeni su lokalni ključevi praćenja za ovu datoteku',
	'results.export.trajectoryKeys.noColumn.summary':
		'Stupac ključa praćenja nije otkriven',
	'results.export.trajectoryKeys.none.summary': 'Nema ključeva praćenja',
	'results.export.trajectoryKeys.usage.detail':
		'Polja praćenja koristite samo kada postoji anonimno longitudinalno povezivanje i kada pravila prikaza to dopuštaju.',
	'results.export.variables.responseDataset.detail':
		'Stupci pitanja uključuju metapodatke knjige kodova kao što su vrsta pitanja, kodovi nedostajućih vrijednosti, sidrišta ljestvice, oznake vrijednosti i ograničenja odgovora kada su dostupni.',
	'results.export.variables.reportSummary.detail':
		'Stupci opisuju agregirani izlaz rezultata, pravila prikaza, finalnost, metapodatke rezultata i polja tumačenja definirana u radnom prostoru.',
	'results.export.variables.codebook.detail':
		'Pregledajte knjigu kodova prije korištenja vrijednosti stupaca.',
	'results.export.missingness.codesDocumented.summary':
		'Kodovi nedostajućih odgovora su dokumentirani',
	'results.export.missingness.codesNotDetected.summary':
		'Kodovi nedostajućih odgovora nisu otkriveni',
	'results.export.missingness.responseDataset.detail':
		'Prije tretiranja praznih vrijednosti kao stvarnih odgovora koristite polja postupanja s nedostajućim vrijednostima i kodove nedostajućih odgovora iz knjige kodova.',
	'results.export.missingness.scoreFields.summary':
		'Uključena su polja nedostajućih vrijednosti rezultata',
	'results.export.missingness.scoreFieldsNotDetected.summary':
		'Polja nedostajućih vrijednosti rezultata nisu otkrivena',
	'results.export.missingness.reportSummary.detail':
		'Nedostajuće vrijednosti u sažetku izvještaja opisuju važeće/očekivane doprinose rezultatu, a ne preskočene odgovore na razini ispitanika.',
	'results.export.missingness.fields.summary':
		'Uključena su polja nedostajućih vrijednosti',
	'results.export.missingness.fieldsNotDetected.summary':
		'Polja nedostajućih vrijednosti nisu otkrivena',
	'results.export.missingness.review.detail':
		'Pregledajte polja nedostajućih vrijednosti prije analize.',
	'results.export.scoreOutputs.noMetadata.summary':
		'Nisu otkriveni stupci metapodataka rezultata',
	'results.export.scoreOutputs.responseDataset.detail':
		'Skupovi odgovora uključuju metapodatke rezultata kada rezultati postoje; uz analizu zadržite bilješke o metodi rezultata.',
	'results.export.scoreOutputs.dimensionCode.summary':
		'Izlazi rezultata navedeni su u dimension_code',
	'results.export.scoreOutputs.noColumn.summary': 'Stupac izlaza rezultata nije otkriven',
	'results.export.scoreOutputs.dimensionCode.detail':
		'Koristite dimension_code sa score_count i poljima prikaza za razumijevanje agregiranih redaka rezultata.',
	'results.export.scoreOutputs.notDetected.summary': 'Izlazi rezultata nisu otkriveni',
	'results.export.scoreOutputs.review.detail':
		'Pregledajte polja izlaza rezultata prije tumačenja.'
	,
	'waves.review.waveSequence.label': 'Redoslijed mjerenja',
	'waves.review.waveSequence.noWave.summary': 'Još nema mjerenja',
	'waves.review.waveSequence.noWave.detail':
		'Izradite Mjerenje 1 u Postavljanju, pokrenite ga iz Prikupljanja, zatim se vratite nakon što odgovori stignu.',
	'waves.review.waveSequence.oneWave.summary': 'Postoji samo Mjerenje 1',
	'waves.review.waveSequence.oneWave.detail':
		'Pregledajte Mjerenje 1 u Rezultatima prije odluke treba li Mjerenje 2 za nastavak prikupljanja.',
	'waves.review.waveSequence.multiple.detail':
		'Pregledajte zadnja dva mjerenja u nastavku. Novo mjerenje dodajte samo kada je novi krug prikupljanja stvarno planiran.',
	'waves.review.comparisonType.label': 'Vrsta usporedbe',
	'waves.review.comparisonType.noComparison.summary': 'Još nema usporedbe',
	'waves.review.comparisonType.noComparison.detail':
		'Usporedba treba barem dva mjerenja s odgovorima.',
	'waves.review.comparisonType.sameRespondent.summary': 'Povezana promjena istih sudionika',
	'waves.review.comparisonType.sameRespondent.detail':
		'Ova mjerenja imaju ponovljeno sudjelovanje, kompatibilno bodovanje i vidljiv izlaz usporedbe nakon pravila prikaza.',
	'waves.review.comparisonType.linkedNeedsChecks.summary': 'Povezana usporedba treba provjere',
	'waves.review.comparisonType.linkedNeedsChecks.detail':
		'Studija ima mjerenja s ponovljenim sudjelovanjem, ali povezani parovi, kompatibilnost bodovanja ili vidljivost prikaza još trebaju potvrdu.',
	'waves.review.comparisonType.groupTrend.summary': 'Samo grupni trend',
	'waves.review.comparisonType.groupTrend.detail':
		'Ova mjerenja mogu prikazati agregirani pomak između krugova, ali ne promjenu pojedinačnih sudionika.',
	'waves.review.comparisonType.collectTwo.detail':
		'Prikupite odgovore u barem dva mjerenja prije pregleda promjene kroz vrijeme.',
	'waves.review.dataReadiness.label': 'Spremnost podataka',
	'waves.review.dataReadiness.linkedReady.detail':
		'Povezana usporedba vidljiva je nakon provjera prikaza i bodovanja. Skriveni rezultati ostaju skriveni.',
	'waves.review.dataReadiness.followUpFirst.summary': 'Prvo prikupite sljedeće mjerenje',
	'waves.review.dataReadiness.followUpFirst.detail':
		'Jedno mjerenje može se pregledati u Rezultatima, ali ne može prikazati promjenu kroz vrijeme.',
	'waves.review.dataReadiness.groupTrendPending.summary':
		'Dovršite izlaze rezultata prije čitanja trenda',
	'waves.review.dataReadiness.groupTrend.detail':
		'Koristite Rezultate za stvarne tablice rezultata i izvoze prije tvrdnji iz ovog trenda.',
	'waves.review.dataReadiness.comparisonNotReady.summary': 'Izlaz usporedbe nije spreman',
	'waves.review.dataReadiness.comparisonNotReady.detail':
		'Pokrenite provjere u nastavku kako biste vidjeli blokiraju li pregled povezani parovi, bodovanje ili pravila prikaza.',
	'waves.review.claimBoundary.label': 'Granica tvrdnje',
	'waves.review.claimBoundary.sameReady.summary':
		'Usporedba prilagođene studije uz pravila prikaza',
	'waves.review.claimBoundary.sameReady.detail':
		'Opišite ovo kao promjenu u ovoj prilagođenoj studiji radnog prostora, a ne kao službenu usporednu vrijednost ili klinički prag.',
	'waves.review.claimBoundary.groupTrend.summary':
		'Nemojte ovo zvati promjenom istih sudionika',
	'waves.review.claimBoundary.groupTrend.detail':
		'Koristite formulacije na razini grupe, npr. agregirani trend između mjerenja, osim ako je ponovljeno sudjelovanje spremno.',
	'waves.review.claimBoundary.oneWave.summary': 'Trenutni rezultati su samo za jedno mjerenje',
	'waves.review.claimBoundary.oneWave.detail':
		'Pregledajte Mjerenje 1 zasebno. Ne opisujte pomak dok ne postoji sljedeće mjerenje.',
	'waves.review.claimBoundary.noClaim.summary': 'Nema dostupne tvrdnje o promjeni',
	'waves.review.claimBoundary.noClaim.detail':
		'Izradite i prikupite mjerenja prije pisanja tumačenja promjene kroz vrijeme.',
	'waves.method.scoringRules.label': 'Pravila rezultata',
	'waves.method.comparisonMethod.label': 'Metoda usporedbe',
	'waves.method.outputs.label': 'Uspoređeni izlazi',
	'waves.method.missingness.label': 'Nedostajući odgovori',
	'waves.method.interpretationBoundary.label': 'Granica tumačenja',
	'waves.method.rule.notConfigured': 'nije postavljeno',
	'waves.method.scoringRules.twoWavesNeeded.summary': 'Potrebna su dva mjerenja',
	'waves.method.scoringRules.twoWavesNeeded.detail':
		'Odaberite ili izradite dva mjerenja prije usporedbe pravila rezultata.',
	'waves.method.scoringRules.sameRule.detail':
		'Odabrana mjerenja koriste isti ključ i verziju pravila rezultata.',
	'waves.method.scoringRules.different.detail':
		'Pregledajte kompatibilnost prije opisivanja pomaka rezultata između mjerenja.',
	'waves.method.comparisonMethod.waveOnly.summary': 'Pregled samo na razini mjerenja',
	'waves.method.comparisonMethod.waveOnly.detail':
		'Promjena istih sudionika treba mjerenja s ponovljenim sudjelovanjem. Anonimna nepovezana mjerenja podržavaju samo agregirani grupni trend.',
	'waves.method.comparisonMethod.linked.detail':
		'Povezana promjena koristi putanje ponovljenog sudjelovanja nakon provjera kompatibilnosti bodovanja i pravila prikaza.',
	'waves.method.outputs.noRepeated.summary': 'Još nema povezanih izlaza',
	'waves.method.outputs.noRepeated.detail':
		'Nazivi uspoređenih izlaza pojavljuju se nakon što su ponovljena mjerenja spremna za povezanu promjenu.',
	'waves.method.outputs.ready.detail':
		'Ovi kodovi izlaza dolaze iz provjere povezane promjene. Uz svako tumačenje zadržite plan rezultata.',
	'waves.method.outputs.pending.summary':
		'Nazivi uspoređenih izlaza dostupni su nakon pregleda povezane promjene',
	'waves.method.outputs.blocked.summary': 'Još nema vidljivih uspoređenih izlaza',
	'waves.method.outputs.review.detail':
		'Pokrenite Pregled povezane promjene za učitavanje redaka uspoređenih izlaza rezultata.',
	'waves.method.missingness.noRepeated.summary': 'Još nema povezanih nedostajućih vrijednosti',
	'waves.method.missingness.noRepeated.detail':
		'Metapodaci usporedbe nedostajućih odgovora pojavljuju se nakon što su ponovljena mjerenja spremna.',
	'waves.method.missingness.pending.summary':
		'Metapodaci nedostajućih odgovora dostupni su nakon pregleda povezane promjene',
	'waves.method.missingness.pending.detail':
		'Pokrenite Pregled povezane promjene za učitavanje broja važećih i očekivanih doprinosa odgovora.',
	'waves.method.missingness.complete.summary':
		'Nema praznine u ulazima rezultata u pregledu usporedbe',
	'waves.method.missingness.complete.detail':
		'Uspoređeni izlazi imaju potpune brojeve važećih i očekivanih doprinosa odgovora.',
	'waves.method.missingness.incomplete.summary':
		'Neki uspoređeni ulazi rezultata nisu potpuni',
	'waves.method.interpretation.noWave.summary': 'Nije odabrano mjerenje',
	'waves.method.interpretation.noWave.detail':
		'Izradite mjerenje prije pregleda granica tumačenja.',
	'waves.method.interpretation.waveOnly.summary': 'Rezultati su samo na razini mjerenja',
	'waves.method.interpretation.waveOnly.detail':
		'Ne opisujte promjenu dok ne postoji sljedeće mjerenje.',
	'waves.method.interpretation.ready.summary':
		'Tumačenje je pregledano za ovu usporedbu',
	'waves.method.interpretation.ready.detail':
		'Uz svaki izvoz ili izvještaj usporedbe mjerenja zadržite bilješke o prikazu i metodi.',
	'waves.method.interpretation.custom.summary':
		'Promjena u prilagođenoj studiji, ne usporedna vrijednost',
	'waves.method.interpretation.custom.detail':
		'Opišite ovo samo kao promjenu u ovoj studiji radnog prostora. Nemojte je predstavljati kao službenu usporednu vrijednost, normu, klinički prag ili vanjski validiranu tvrdnju.',
	'setup.status.notEditable': 'nije moguće uređivati',
	'setup.status.draft': 'nacrt',
	'setup.status.scheduled': 'zakazano',
	'setup.status.live': 'u tijeku',
	'setup.status.closed': 'zatvoreno',
	'setup.launchState.savedSelections': (values, locale) =>
		`Spremljeno: ${formatCount(
			locale,
			numberValue(values, 'selectionCount'),
			'selection'
		)}, spremno: ${formatCount(locale, numberValue(values, 'pairCount'), 'invitationPair')}.`,
	'setup.launchPlan.waveDraftReady': (values) =>
		`${textValue(values, 'waveName')} je nacrt mjerenja za ovu studiju.`,
	'setup.launchPlan.waveWillBeCreated': (values) =>
		`${textValue(values, 'waveName')} izradit će se kada spremite ovaj korak.`,
	'setup.launchPlan.savedRecipientDetail': (values, locale) =>
		`Spremljeno: ${formatCount(
			locale,
			numberValue(values, 'selectionCount'),
			'selection'
		)}, s ${formatCount(locale, numberValue(values, 'pairCount'), 'invitationPair')}.`,
	'setup.designMap.questionnaireSaved': (values, locale) =>
		`${textValue(values, 'name')} spremljen je s ${formatCount(
			locale,
			numberValue(values, 'questionCount'),
			'question'
		)}.`,
	'setup.designMap.resultsReady': (values) =>
		`Izlazi rezultata spremljeni su kao ${textValue(values, 'ruleKey')}.`,
	'setup.designMap.draftWaveNeedsReadiness': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'count'),
			'measurement'
		)} u nacrtu je pripremljeno; provjera prije pokretanja još treba pažnju.`,
	'setup.designMap.waveReady': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'measurement')} spremno je za Prikupljanje.`,
	'setup.designMap.liveWave': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'measurement')} prikuplja odgovore.`,
	'setup.designMap.closedWave': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'count'),
			'measurement'
		)} ima zatvorene podatke za pregled Rezultata.`,
	'setup.waveContext.prepareForCollection': (values) =>
		`Pripremite ${textValue(values, 'waveName')} za prikupljanje`,
	'setup.waveContext.followUpDraftSummary': (values) =>
		`${textValue(values, 'waveName')} je nacrt sljedećeg mjerenja. Koristite ga samo kada je sljedeći krug prikupljanja namjeran.`,
	'setup.waveContext.closedOneWaveSummary': (values) =>
		`${textValue(values, 'previousWaveName')} je već ${textValue(
			values,
			'previousWaveStatus'
		)}. Izradite ${textValue(values, 'nextWaveName')} samo kada je sljedeći krug prikupljanja namjeran.`,
	'setup.waveContext.multipleWaveSummary': (values, locale) =>
		`Već postoji ${formatCount(
			locale,
			numberValue(values, 'existingWaveCount'),
			'measurement'
		)}. Izradite ${textValue(
			values,
			'nextWaveName'
		)} tek nakon pregleda rezultata trenutnog mjerenja.`,
	'setup.waveContext.recipientBelongsUntilLaunch': (values) =>
		`Odabir primatelja pripada mjerenju ${textValue(values, 'waveName')} dok se to mjerenje ne pokrene.`,
	'setup.waveContext.reviewBeforePreparing': (values) =>
		`Pregledajte ${textValue(values, 'previousWaveName')} prije pripreme ${textValue(
			values,
			'nextWaveName'
		)}`,
	'setup.waveContext.reviewExistingBeforePreparing': (values) =>
		`Pregledajte postojeća mjerenja prije pripreme ${textValue(values, 'nextWaveName')}`,
	'setup.waveContext.openResultsBeforeCreating': (values) =>
		`Otvorite Rezultate za pregled ili izvoz ${textValue(
			values,
			'reviewTarget'
		)} prije izrade ${textValue(values, 'nextWaveName')}.`,
	'setup.waveContext.createOnlyWhenIntentional': (values) =>
		`Izradite ${textValue(values, 'nextWaveName')} samo kada je sljedeći krug prikupljanja namjeran.`,
	'setup.waveContext.recipientBelongsToNewDraft': (values) =>
		`Odabir primatelja u ovom koraku pripadat će novom nacrtu mjerenja, a ne mjerenju ${textValue(
			values,
			'previousLabel'
		)}.`
};

const messageCatalogs: Record<AppLocale, AppMessageCatalog> = {
	en: enMessages,
	'hr-HR': hrMessages
};

const countNouns: Record<AppLocale, Record<CountNounId, Partial<CountNounForms>>> = {
	en: {
		answerMetadataField: { one: 'answer metadata field', other: 'answer metadata fields' },
		answerVariable: { one: 'answer variable', other: 'answer variables' },
		column: { one: 'column', other: 'columns' },
		comparedOutput: { one: 'compared output', other: 'compared outputs' },
		comparisonScore: { one: 'visible comparison score', other: 'visible comparison scores' },
		invitationPair: { one: 'invitation pair', other: 'invitation pairs' },
		linkedPair: { one: 'linked pair', other: 'linked pairs' },
		measurement: { one: 'wave', other: 'waves' },
		question: { one: 'question', other: 'questions' },
		reportSummaryColumn: { one: 'report-summary column', other: 'report-summary columns' },
		response: { one: 'response', other: 'responses' },
		row: { one: 'row', other: 'rows' },
		score: { one: 'score', other: 'scores' },
		scoreMetadataField: { one: 'score metadata field', other: 'score metadata fields' },
		scoreOutput: { one: 'score output', other: 'score outputs' },
		selection: { one: 'selection', other: 'selections' }
	},
	'hr-HR': {
		answerMetadataField: {
			one: 'metapodatkovno polje odgovora',
			few: 'metapodatkovna polja odgovora',
			other: 'metapodatkovnih polja odgovora'
		},
		answerVariable: {
			one: 'varijabla odgovora',
			few: 'varijable odgovora',
			other: 'varijabli odgovora'
		},
		column: { one: 'stupac', few: 'stupca', other: 'stupaca' },
		comparedOutput: {
			one: 'uspoređeni izlaz rezultata',
			few: 'uspoređena izlaza rezultata',
			other: 'uspoređenih izlaza rezultata'
		},
		comparisonScore: {
			one: 'vidljiv usporedni rezultat',
			few: 'vidljiva usporedna rezultata',
			other: 'vidljivih usporednih rezultata'
		},
		invitationPair: {
			one: 'par pozivnica',
			few: 'para pozivnica',
			other: 'parova pozivnica'
		},
		linkedPair: { one: 'povezani par', few: 'povezana para', other: 'povezanih parova' },
		measurement: { one: 'mjerenje', few: 'mjerenja', other: 'mjerenja' },
		question: { one: 'pitanje', few: 'pitanja', other: 'pitanja' },
		reportSummaryColumn: {
			one: 'stupac sažetka izvještaja',
			few: 'stupca sažetka izvještaja',
			other: 'stupaca sažetka izvještaja'
		},
		response: { one: 'odgovor', few: 'odgovora', other: 'odgovora' },
		row: { one: 'redak', few: 'retka', other: 'redaka' },
		score: { one: 'rezultat', few: 'rezultata', other: 'rezultata' },
		scoreMetadataField: {
			one: 'metapodatkovno polje rezultata',
			few: 'metapodatkovna polja rezultata',
			other: 'metapodatkovnih polja rezultata'
		},
		scoreOutput: { one: 'izlaz rezultata', few: 'izlaza rezultata', other: 'izlaza rezultata' },
		selection: { one: 'odabir', few: 'odabira', other: 'odabira' }
	}
};

const collectedResponsePhrases: Record<AppLocale, Partial<CountNounForms>> = {
	en: { one: 'response collected', other: 'responses collected' },
	'hr-HR': {
		one: 'prikupljen odgovor',
		few: 'prikupljena odgovora',
		other: 'prikupljenih odgovora'
	}
};

const visibleScorePhrases: Record<AppLocale, Partial<CountNounForms>> = {
	en: { one: 'score visible', other: 'scores visible' },
	'hr-HR': {
		one: 'vidljiv rezultat',
		few: 'vidljiva rezultata',
		other: 'vidljivih rezultata'
	}
};

export function appMessage(
	locale: AppLocale,
	id: AppMessageId,
	values: AppMessageValues = {}
): string {
	const template = messageCatalogs[locale][id] ?? enMessages[id];
	return typeof template === 'function' ? template(values, locale) : template;
}

export function appMessageIds(): AppMessageId[] {
	return Object.keys(enMessages) as AppMessageId[];
}

export function missingAppMessageIds(locale: AppLocale): AppMessageId[] {
	return appMessageIds().filter((id) => !(id in messageCatalogs[locale]));
}

export function formatCount(locale: AppLocale, count: number, noun: CountNounId): string {
	return `${formatNumber(locale, count)} ${pluralForm(locale, countNouns[locale][noun], count)}`;
}

function formatNumber(locale: AppLocale, value: number): string {
	return new Intl.NumberFormat(locale).format(value);
}

function pluralForm(locale: AppLocale, forms: Partial<CountNounForms>, value: number): string {
	const category = new Intl.PluralRules(locale).select(value) as PluralCategory;
	return forms[category] ?? forms.other ?? forms.one ?? '';
}

function numberValue(values: AppMessageValues, key: string): number {
	const value = values[key];
	return typeof value === 'number' && Number.isFinite(value) ? value : 0;
}

function textValue(values: AppMessageValues, key: string): string {
	const value = values[key];
	return value == null ? '' : String(value);
}

function localizedCountPhrase(
	locale: AppLocale,
	count: number,
	forms: Record<AppLocale, Partial<CountNounForms>>
): string {
	return `${formatNumber(locale, count)} ${pluralForm(locale, forms[locale], count)}`;
}
