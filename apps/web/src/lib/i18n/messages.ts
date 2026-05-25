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
	| 'emailInvitation'
	| 'exportFile'
	| 'invitationPair'
	| 'linkedPair'
	| 'measurement'
	| 'openRespondentLink'
	| 'purpose'
	| 'question'
	| 'recipient'
	| 'reportSummaryColumn'
	| 'response'
	| 'row'
	| 'score'
	| 'scoreMetadataField'
	| 'scoreOutput'
	| 'sentEmail'
	| 'selection';

type CountNounForms = Record<PluralCategory | 'other', string>;

const enMessages = {
	'overview.surface.title': 'Study overview',
	'overview.surface.description':
		'Use this overview to prepare, collect, review results, and compare waves for the selected study.',
	'overview.reference.title': 'Study reference',
	'overview.reference.description':
		'Detailed records, governance status, and wave rows for this selected study.',
	'overview.untitledSeries': 'Untitled wave series',
	'overview.untitledStudy': 'Untitled study',
	'overview.subtitle': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'campaignCount'))} ${
			numberValue(values, 'campaignCount') === 1 ? 'campaign' : 'campaigns'
		}, ${formatNumber(
			locale,
			numberValue(values, 'liveCount')
		)} live`,
	'overview.lifecycle.title': 'Study lifecycle',
	'overview.lifecycle.description':
		'Move through this study from preparation to collection, results, and waves.',
	'overview.lifecycle.setup.label': 'Prepare',
	'overview.lifecycle.setup.description':
		'Build the questionnaire, results setup, policies, wave, and launch check.',
	'overview.lifecycle.operations.label': 'Collect',
	'overview.lifecycle.operations.description':
		'Start the wave, share access, send invitations, and monitor submissions.',
	'overview.lifecycle.reports.label': 'Review results',
	'overview.lifecycle.reports.description':
		'Review findings, limitations, and export files after responses are ready.',
	'overview.lifecycle.waves.label': 'Compare waves',
	'overview.lifecycle.waves.description':
		'Create follow-up waves and compare results across collection rounds.',
	'overview.studyModel.title': 'What this study contains',
	'overview.studyModel.description':
		'A study is the project container. A questionnaire source is only starting material; the questionnaire is what respondents answer; waves are collection rounds; results and exports use the saved answers.',
	'overview.studyModel.studyContainer.label': 'Study',
	'overview.studyModel.studyContainer.badge': 'Saved',
	'overview.studyModel.studyContainer.summary': (values) =>
		`${textValue(
			values,
			'studyName'
		)} holds setup, collection waves, results, and exports. It is not a questionnaire or an instrument.`,
	'overview.studyModel.studyContainer.guidance':
		'Open Setup when you need to change what respondents answer or how results are prepared.',
	'overview.studyModel.questionnaireResults.label': 'Questionnaire and result outputs',
	'overview.studyModel.questionnaireResults.summary':
		'The questionnaire defines the questions; result outputs define which answers you score, export, or interpret.',
	'overview.studyModel.questionnaireResults.guidance':
		'Open Setup to finish the source, questionnaire, result outputs, wave, and launch readiness.',
	'overview.studyModel.collectionWaves.label': 'Collection waves',
	'overview.studyModel.collectionWaves.badge.live': 'Live',
	'overview.studyModel.collectionWaves.badge.prepared': 'Prepared',
	'overview.studyModel.collectionWaves.badge.none': 'No waves',
	'overview.studyModel.collectionWaves.summary.none': 'No collection waves exist yet.',
	'overview.studyModel.collectionWaves.summary.existing': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'campaignCount'))} ${
			numberValue(values, 'campaignCount') === 1
				? 'collection wave exists'
				: 'collection waves exist'
		}; ${formatNumber(locale, numberValue(values, 'liveCount'))} ${
			numberValue(values, 'liveCount') === 1 ? 'is' : 'are'
		} live.`,
	'overview.studyModel.collectionWaves.guidance':
		'Create a collection wave in Setup, then use Collect to open access for respondents.',
	'overview.studyModel.evidenceOutputs.label': 'Evidence and comparison',
	'overview.studyModel.evidenceOutputs.badge.ready': 'Evidence ready',
	'overview.studyModel.evidenceOutputs.badge.needsScoring': 'Needs scoring',
	'overview.studyModel.evidenceOutputs.badge.none': 'No evidence yet',
	'overview.studyModel.evidenceOutputs.summary': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'submittedCount'))} submitted responses, ${formatNumber(
			locale,
			numberValue(values, 'scoreCount')
		)} scores, and ${formatNumber(
			locale,
			numberValue(values, 'exportCount')
		)} export files are recorded.`,
	'overview.studyModel.evidenceOutputs.guidance':
		'Use Results for current evidence, Waves for repeated collection rounds, and Exports for analysis handoff.',
	'overview.row.waves': 'Waves',
	'overview.row.liveWaves': 'Live waves',
	'overview.row.submittedResponses': 'Submitted responses',
	'overview.row.scores': 'Scores',
	'overview.row.exportFiles': 'Export files',
	'overview.row.identityMode': 'Identity mode',
	'overview.row.locale': 'Locale',
	'overview.row.created': 'Created',
	'overview.row.updated': 'Updated',
	'overview.row.archived': 'Archived',
	'overview.row.archiveReason': 'Archive reason',
	'overview.governance.consent': 'Consent',
	'overview.governance.retention': 'Retention',
	'overview.governance.disclosure': 'Disclosure',
	'overview.governance.scoring': 'Scoring',
	'overview.badge.ready': 'Ready',
	'overview.badge.pending': 'Pending',
	'overview.badge.blocked': 'Blocked',
	'overview.badge.live': 'Live',
	'overview.badge.notConfigured': 'Not configured',
	'overview.badge.notAvailable': 'Not available',
	'exports.library.surface.title': 'Use exports',
	'exports.library.surface.eyebrow': 'Study support',
	'exports.library.surface.description':
		'Find generated CSV/codebook files by purpose, readiness, source study, and next use.',
	'exports.library.reference.title': 'Export reference',
	'exports.library.reference.description':
		'File metadata, lifecycle timestamps, failure codes, and download availability stay available for audit and troubleshooting.',
	'exports.library.row.exportFiles': 'Export files',
	'exports.library.row.downloadable': 'Downloadable',
	'exports.library.row.failed': 'Failed',
	'exports.library.row.pending': 'Pending',
	'exports.library.row.reportSummaryExports': 'Report summary exports',
	'exports.library.row.responseDatasetExports': 'Response dataset exports',
	'exports.library.row.reportSummaryDownloads': 'Report-summary exports',
	'exports.library.row.responseDatasets': 'Response datasets',
	'exports.library.row.campaignFiles': 'Campaign files',
	'exports.library.row.studyFiles': 'Study files',
	'exports.library.row.studyContext': 'Study context',
	'exports.library.row.fileType': 'File type',
	'exports.library.row.format': 'Format',
	'exports.library.row.dataFinality': 'Data finality',
	'exports.library.row.rows': 'Rows',
	'exports.library.row.size': 'Size',
	'exports.library.row.created': 'Created',
	'exports.library.row.completed': 'Completed',
	'exports.library.row.failure': 'Failure',
	'exports.library.row.download': 'Download',
	'exports.library.readyDownloads.label': 'Downloadable files',
	'exports.library.readyDownloads.badge': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} downloadable`,
	'exports.library.readyDownloads.summary.ready': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'exportFile')} ${
			numberValue(values, 'count') === 1 ? 'is' : 'are'
		} ready to download.`,
	'exports.library.readyDownloads.summary.empty': 'No export files are ready to download yet.',
	'exports.library.readyDownloads.guidance.withDataset':
		'Use response dataset exports for analysis handoff. Use report-summary exports for review packets, client summaries, or codebook checks.',
	'exports.library.readyDownloads.guidance.reportOnly':
		'Report-summary files are downloadable for review packets, client summaries, or codebook checks. No analysis-ready response dataset is available yet.',
	'exports.library.readyDownloads.guidance.empty':
		'Create an export from a study results page after results are available.',
	'exports.library.attention.label': 'Needs attention',
	'exports.library.attention.badge.failed': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} failed`,
	'exports.library.attention.badge.pending': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} pending`,
	'exports.library.attention.badge.ready': 'No attention items',
	'exports.library.attention.summary.failed': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'exportFile')} ${
			numberValue(values, 'count') === 1 ? 'needs' : 'need'
		} attention.`,
	'exports.library.attention.summary.pending': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'exportFile')} ${
			numberValue(values, 'count') === 1 ? 'is' : 'are'
		} still queued or rendering.`,
	'exports.library.attention.summary.ready': 'No failed or pending export files.',
	'exports.library.attention.guidance.failed':
		'Review the failed export file, then recreate it from the source study after the cause is resolved.',
	'exports.library.attention.guidance.pending':
		'Wait for generation to finish before using the export file for handoff.',
	'exports.library.attention.guidance.ready':
		'New export issues will appear here when generation fails or remains pending.',
	'exports.library.purpose.label': 'File purpose',
	'exports.library.purpose.badge.ready': (values, locale) =>
		formatCount(locale, numberValue(values, 'count'), 'purpose'),
	'exports.library.purpose.badge.empty': 'No files',
	'exports.library.purpose.summary.ready': (values) =>
		`Exports cover ${textValue(values, 'purposeLabels')}.`,
	'exports.library.purpose.summary.empty': 'No generated export purposes are available yet.',
	'exports.library.purpose.guidance.ready':
		'Choose report summary exports for result handoff; choose response dataset exports for analysis with the codebook.',
	'exports.library.purpose.guidance.empty':
		'Create report summary or response dataset exports from a study when results are ready.',
	'exports.library.context.label': 'Study context and next use',
	'exports.library.context.badge.ready': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} ${
			numberValue(values, 'count') === 1 ? 'source' : 'sources'
		}`,
	'exports.library.context.badge.empty': 'No sources',
	'exports.library.context.summary.ready': (values) =>
		`Export files are tied to ${textValue(values, 'sourceLabels')}.`,
	'exports.library.context.summary.empty': 'No export files are tied to a study yet.',
	'exports.library.context.guidance.ready':
		'Open the source study or report context when you need to understand how an export file was generated.',
	'exports.library.context.guidance.empty':
		'Generated export files will link back to their study or report context when that context is available.',
	'exports.library.purpose.reportSummary.label': 'Report summary export',
	'exports.library.purpose.reportSummary.nextUse':
		'Use this export for report handoff, summary review, or codebook checks.',
	'exports.library.purpose.responseDataset.label': 'Response dataset export',
	'exports.library.purpose.responseDataset.nextUse':
		'Use this export for response-level analysis with the generated codebook.',
	'exports.library.purpose.other.nextUse':
		'Use this export with its source context and generated codebook.',
	'exports.library.context.campaignSeries': (values) =>
		`Campaign series / ${textValue(values, 'label')}`,
	'exports.library.context.campaign': (values) => `Campaign / ${textValue(values, 'label')}`,
	'exports.library.fileType.reportSummary': 'Report summary CSV and codebook',
	'exports.library.fileType.responseDataset': 'Response dataset CSV and codebook',
	'exports.library.fileFormat.csvCodebook': 'CSV codebook',
	'exports.library.finality.closedWave': 'Closed wave',
	'exports.library.finality.notClosedWave': 'Not tied to a closed wave',
	'exports.library.download.available': 'Available',
	'exports.library.download.notAvailable': 'Not available',
	'readModel.status.preview': 'preview',
	'readModel.status.ready': 'ready',
	'readModel.status.blocked': 'blocked',
	'readModel.status.live': 'live',
	'readModel.status.pending': 'pending',
	'readModel.status.notAvailable': 'not available',
	'readModel.status.notConfigured': 'not configured',
	'readModel.identity.anonymous': 'anonymous',
	'readModel.identity.anonymousLongitudinal': 'anonymous repeat participation',
	'readModel.identity.identified': 'identified',
	'readModel.untitledWaveSeries': 'Untitled wave series',
	'readModel.untitledWave': 'Untitled wave',
	'readModel.subtitle.campaignsLive': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'campaignCount'))} ${
			numberValue(values, 'campaignCount') === 1 ? 'campaign' : 'campaigns'
		}, ${formatNumber(locale, numberValue(values, 'liveCount'))} live`,
	'readModel.row.campaigns': 'Campaigns',
	'readModel.row.liveCampaigns': 'Live campaigns',
	'readModel.row.missingPrerequisites': 'Missing prerequisites',
	'readModel.row.submittedResponses': 'Submitted responses',
	'readModel.surface.setup.label': 'Prepare study',
	'readModel.surface.setup.eyebrow': 'Study preparation',
	'readModel.surface.setup.description':
		'Prepare this study for collection by completing setup tasks and launch-readiness checks.',
	'readModel.surface.setup.referenceTitle': 'Setup reference',
	'readModel.surface.setup.referenceDescription':
		'Detailed setup records, policy status, selected wave fields, and launch-check notes stay here for review.',
	'readModel.surface.setup.emptyTitle': 'No campaigns yet',
	'readModel.surface.setup.emptyMessage': 'Create a wave draft before running launch readiness.',
	'readModel.surface.setup.proofTitle': 'Preparation actions',
	'readModel.surface.setup.proofDescription':
		'Choose a questionnaire source, prepare questionnaire and result outputs, create a wave draft, and check launch readiness.',
	'settings.tenant.untitled': 'Tenant settings',
	'settings.profile.slug': 'Slug',
	'settings.profile.region': 'Region',
	'settings.profile.defaultLocale': 'Default locale',
	'settings.profile.status': 'Status',
	'settings.profile.created': 'Created',
	'settings.profile.updated': 'Updated',
	'settings.metric.studies': 'Campaign series',
	'settings.metric.campaigns': 'Campaigns',
	'settings.metric.liveCampaigns': 'Live campaigns',
	'settings.metric.submittedResponses': 'Submitted responses',
	'settings.metric.subjects': 'Subjects',
	'settings.metric.subjectGroups': 'Subject groups',
	'settings.metric.tenantMembers': 'Tenant members',
	'settings.metric.tenantRoles': 'Tenant roles',
	'settings.metric.exportFiles': 'Export files',
	'instruments.metric.sources': 'Instruments',
	'instruments.metric.launchEligible': 'Launch eligible',
	'instruments.metric.launchBlocked': 'Launch blocked',
	'instruments.status.launchEligible': 'Launch eligible',
	'instruments.status.launchBlocked': 'Launch blocked',
	'instruments.row.rights': 'Rights',
	'instruments.row.validity': 'Validity',
	'session.checking.title': 'Checking access',
	'session.checking.message': 'Loading authenticated workspace access.',
	'session.authenticated.title': 'Signed in',
	'session.authenticated.message': (values) => `Tenant ${textValue(values, 'tenantId')}`,
	'session.unauthenticated.title': 'Sign in required',
	'session.unauthenticated.message': 'Sign in before opening tenant product surfaces.',
	'session.forbidden.title': 'Tenant access unavailable',
	'session.forbidden.message': 'Your session does not have access to this tenant workspace.',
	'session.failed.title': 'Session check failed',
	'session.failed.statusMessage': (values) =>
		`Session check failed with status ${textValue(values, 'status')}.`,
	'session.failed.message': 'Session check failed.',
	'workspace.lifecycle.prepare.label': 'Prepare',
	'workspace.lifecycle.prepare.description': 'Set up the instrument, questions, scoring, and launch rules.',
	'workspace.lifecycle.collect.label': 'Collect',
	'workspace.lifecycle.collect.description': 'Launch the study and track response progress.',
	'workspace.lifecycle.review.label': 'Review',
	'workspace.lifecycle.review.description': 'Inspect coverage, findings, limitations, and comparisons.',
	'workspace.lifecycle.export.label': 'Export',
	'workspace.lifecycle.export.description': 'Use generated CSV and codebook files for analysis.',
	'workspace.command.untitled': 'Workspace action',
	'workspace.command.defaultDescription': 'Open the linked product surface for details.',
	'workspace.command.defaultAction': 'Open',
	'workspace.row.surface': 'Surface',
	'workspace.surface.campaignSeries': 'Campaign series',
	'workspace.surface.directory': 'Directory',
	'workspace.surface.operations': 'Operations',
	'workspace.surface.reports': 'Reports',
	'workspace.surface.setup': 'Setup',
	'workspace.surface.team': 'Team',
	'workspace.surface.waves': 'Waves',
	'workspace.surface.workspace': 'Workspace',
	'workspace.action.reviewSampleResults': 'Review sample results',
	'workspace.action.inspectSampleCollection': 'Inspect sample collection',
	'workspace.action.inspectSampleSetup': 'Inspect sample setup',
	'workspace.action.continueSetup': 'Continue setup',
	'workspace.action.monitorCollection': 'Monitor collection',
	'workspace.action.reviewResults': 'Review results',
	'workspace.action.openStudy': 'Open study',
	'workspace.action.inspectStudy': 'Inspect study',
	'workspace.action.openArchivedStudy': 'Open archived study',
	'portfolio.empty.noMatching.title': 'No matching studies',
	'portfolio.empty.noMatching.message':
		'Adjust search, readiness, or visibility filters to show sample or own studies.',
	'portfolio.empty.noStudies.title': 'No studies yet',
	'portfolio.empty.noStudies.message':
		'Create your study when you have setup access, or add sample studies for learning.',
	'portfolio.filter.allReadiness': 'All readiness',
	'portfolio.filter.notConfigured': 'Not configured',
	'portfolio.filter.pending': 'Pending',
	'portfolio.filter.preview': 'Preview',
	'portfolio.sort.latestActivity': 'Latest activity',
	'portfolio.sort.recentlyUpdated': 'Recently updated',
	'portfolio.sort.recentlyCreated': 'Recently created',
	'portfolio.sort.nameAscending': 'Name A-Z',
	'portfolio.visibility.active': 'Active',
	'portfolio.visibility.archived': 'Archived',
	'portfolio.visibility.all': 'All visibility',
	'portfolio.section.sample.title': 'Sample studies',
	'portfolio.section.sample.description':
		'Read-only examples you can inspect before creating your own study.',
	'portfolio.section.sample.empty': 'No sample studies match this view. Clear filters to inspect examples.',
	'portfolio.section.own.title': 'Your studies',
	'portfolio.section.own.description': 'Editable studies owned by this workspace.',
	'portfolio.section.own.empty':
		'No own studies match this view. Clear filters or create your study when you have setup access.',
	'portfolio.lifecycle.needsSetup.label': 'Needs setup',
	'portfolio.lifecycle.needsSetup.description': 'Studies that need setup before collection is useful.',
	'portfolio.lifecycle.inCollection.label': 'In collection',
	'portfolio.lifecycle.inCollection.description': 'Studies with live collection activity to monitor.',
	'portfolio.lifecycle.resultsReady.label': 'Results ready',
	'portfolio.lifecycle.resultsReady.description': 'Studies with submitted responses ready for review.',
	'portfolio.lifecycle.archived.label': 'Archived',
	'portfolio.lifecycle.archived.description': 'Studies kept for reference after active work ended.',
	'portfolio.lifecycle.open.label': 'Open study',
	'portfolio.lifecycle.open.description': 'Studies available for normal inspection.',
	'portfolio.row.campaigns': 'Campaigns',
	'portfolio.row.liveCampaigns': 'Live campaigns',
	'portfolio.row.submittedResponses': 'Submitted responses',
	'portfolio.row.latestActivity': 'Latest activity',
	'portfolio.row.archived': 'Archived',
	'portfolio.value.notAvailable': 'Not available',
	'portfolio.action.archive': 'Archive',
	'portfolio.action.restore': 'Restore',
	'portfolio.action.duplicateAsStudy': 'Duplicate as study',
	'portfolio.ownership.sample.label': 'Sample study',
	'portfolio.ownership.own.label': 'Your study',
	'portfolio.sampleReadOnly.setup':
		'Setup sample: read-only starter content showing study preparation before launch.',
	'portfolio.sampleReadOnly.blocked':
		'Setup sample: read-only starter content showing blocked preparation before launch.',
	'portfolio.sampleReadOnly.inCollection':
		'Collection sample: read-only starter content showing live or partial response collection.',
	'portfolio.sampleReadOnly.longitudinal':
		'Repeat-participation sample: read-only starter content showing repeated measurements and linked repeat-response review.',
	'portfolio.sampleReadOnly.results':
		'Results sample: read-only starter content showing collected responses, scores, reports, and exports.',
	'portfolio.sampleReadOnly.default':
		'Sample study: read-only starter content you can inspect before duplicating.',
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
	'results.export.trajectoryKeys.label': 'Repeat-response keys',
	'results.export.variablesValues.label': 'Variables and values',
	'results.export.missingness.label': 'Missingness',
	'results.export.scoreOutputs.label': 'Score outputs',
	'results.export.pending.filePurpose.summary': 'Review export file to inspect contents',
	'results.export.pending.rowShape.summary': 'Row shape available after file review',
	'results.export.pending.waveFields.summary': 'Wave fields available after file review',
	'results.export.pending.trajectoryKeys.summary':
		'Repeat-response key policy available after file review',
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
		'No repeat-response keys in report-summary export',
	'results.export.trajectoryKeys.reportSummary.detail':
		'Report-summary exports are aggregate files and do not include respondent repeat-response rows.',
	'results.export.trajectoryKeys.included.summary': 'Artifact-local repeat-response keys included',
	'results.export.trajectoryKeys.policy.detail': (values, locale) =>
		locale === 'hr-HR'
			? `Ključevi praćenja su ${textValue(
					values,
					'policy'
				)} i ne smiju se tretirati kao izvorni kodovi sudionika ili ponovno upotrebljivi identifikatori.`
			: `Repeat-response ids are ${textValue(
					values,
					'policy'
				)} and should not be treated as raw participant codes or reusable identifiers.`,
	'results.export.trajectoryKeys.noColumn.summary': 'No repeat-response key column detected',
	'results.export.trajectoryKeys.none.summary': 'No repeat-response keys',
	'results.export.trajectoryKeys.usage.detail':
		'Use repeat-response fields only when anonymous repeat participation linking is present and disclosure allows it.',
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
		'Linked change uses repeated-response pairs after scoring compatibility and disclosure checks.',
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
		)}.`,

	'operations.status.reportVisibility.notAvailable': 'Not available',
	'operations.status.reportVisibility.reportable': 'reportable',
	'operations.status.reportVisibility.visible': 'visible',
	'operations.status.reportVisibility.blocked': 'blocked',
	'operations.status.reportVisibility.unknownPolicy': 'policy not confirmed',
	'operations.status.submittedTitle': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'submitted'))} submitted`,
	'operations.status.responseActivityDetail': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'started'))} started, ${formatNumber(
			locale,
			numberValue(values, 'drafts')
		)} in progress, ${formatNumber(locale, numberValue(values, 'submitted'))} submitted.`,
	'operations.status.closedHeadline': (values, locale) =>
		`Closed: ${formatNumber(locale, numberValue(values, 'submitted'))} submitted ${
			numberValue(values, 'submitted') === 1 ? 'response' : 'responses'
		}`,
	'operations.status.liveHeadline': (values, locale) =>
		`Live: accepting responses with ${formatNumber(
			locale,
			numberValue(values, 'submitted')
		)} submitted`,
	'operations.access.identifiedDetail': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'openLinkCount'),
			'openRespondentLink'
		)} prepared. Respondents are connected to known subject records for this wave.`,
	'operations.access.inviteOnlyDetail': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'invitationCount'),
			'emailInvitation'
		)} ${numberValue(values, 'invitationCount') === 1 ? 'is' : 'are'} ready for this wave. Only saved recipients receive private access, and ${textValue(
			values,
			'boundary'
		)}`,
	'operations.access.mixedDetail': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'openLinkCount'),
			'openRespondentLink'
		)} and ${formatCount(
			locale,
			numberValue(values, 'invitationCount'),
			'emailInvitation'
		)}. Open-link access is broad; invite-only email access limits entry to saved recipients. ${textValue(
			values,
			'boundary'
		)}`,
	'operations.access.openLinkDetail': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'openLinkCount'),
			'openRespondentLink'
		)} ${numberValue(values, 'openLinkCount') === 1 ? 'is' : 'are'} active. Anyone with the link can enter this wave; use saved invitations when access should be limited.`
	,
	'operations.suppression.headline': (values, locale) => {
		const blockedCount = numberValue(values, 'blockedCount');
		if (blockedCount === 0) {
			return 'No recipients are on the do-not-contact list';
		}
		return `${formatCount(locale, blockedCount, 'recipient')} ${
			blockedCount === 1 ? 'is' : 'are'
		} on the do-not-contact list`;
	},
	'operations.suppression.guidance.blocked':
		'Use another email, remove the recipient, or release the suppression only when you are sure future invitations are appropriate.',
	'operations.suppression.guidance.clear':
		'Recipient list is not blocked by active do-not-contact records.',
	'operations.suppression.reason.recipientUnsubscribed': 'Unsubscribed',
	'operations.suppression.reason.providerBounced': 'Bounced',
	'operations.suppression.reason.providerComplained': 'Spam complaint',
	'operations.suppression.reason.operatorDoNotContact': 'Manually suppressed',
	'operations.suppression.source.respondentInvitationLink': 'Invitation unsubscribe link',
	'operations.suppression.source.providerDeliveryEvent': 'Provider delivery event',
	'operations.suppression.source.tenantOperator': 'Workspace admin',
	'operations.suppression.sourceCreatedAt': (values) =>
		`${textValue(values, 'sourceLabel')} - Added ${textValue(values, 'createdAt')}`,
	'operations.readModel.collectionMonitor.title': 'Response monitor',
	'operations.readModel.scoreCoverage.title': 'Score coverage',
	'operations.readModel.collectionState.label': 'Collection status',
	'operations.readModel.collectionState.emptyLabel': 'Collection state',
	'operations.readModel.collectionState.blockedBadge': 'Blocked',
	'operations.readModel.collectionState.noSelectedSummary':
		'No selected campaign is collecting responses',
	'operations.readModel.collectionState.noSelectedGuidance':
		'Create and launch a campaign before collecting responses.',
	'operations.readModel.collectionState.selectedSummary': (values) =>
		`${textValue(values, 'campaignName')} is ${textValue(values, 'statusLabel')}`,
	'operations.readModel.respondentAccess.label': 'Respondent access',
	'operations.readModel.respondentAccess.badge.ready': 'Access ready',
	'operations.readModel.respondentAccess.badge.pending': 'Preparing access',
	'operations.readModel.respondentAccess.summary': (values) =>
		`${textValue(values, 'openLinkSummary')}, ${textValue(values, 'sentEmailSummary')}`,
	'operations.readModel.respondentAccess.guidance.mixed':
		'Respondents can enter through shared links and sent emails.',
	'operations.readModel.respondentAccess.guidance.openLink':
		'Respondents can enter through shared links.',
	'operations.readModel.respondentAccess.guidance.email':
		'Respondents can enter through sent emails.',
	'operations.readModel.respondentAccess.guidance.none':
		'Prepare respondent access before collecting responses.',
	'operations.readModel.responseProgress.label': 'Response progress',
	'operations.readModel.responseProgress.badge.withSubmissions': (values) =>
		`${numberValue(values, 'submitted')} submitted`,
	'operations.readModel.responseProgress.badge.empty': 'No submissions',
	'operations.readModel.responseProgress.summary': (values) =>
		`${numberValue(values, 'started')} started, ${numberValue(values, 'drafts')} draft, ${numberValue(
			values,
			'submitted'
		)} submitted`,
	'operations.readModel.scoreReadiness.label': 'Score and report readiness',
	'operations.readModel.scoreReadiness.badge.ready': 'Reports ready',
	'operations.readModel.scoreReadiness.badge.empty': 'No submissions',
	'operations.readModel.scoreReadiness.badge.notConfigured': 'Not configured',
	'operations.readModel.scoreReadiness.summary.withSubmissions': (values) =>
		`${numberValue(values, 'scored')} of ${numberValue(
			values,
			'submitted'
		)} submitted responses scored`,
	'operations.readModel.scoreReadiness.summary.empty': 'No submitted responses to score yet',
	'operations.readModel.guidance.collection.readyForAggregateReport':
		'Enough submitted responses exist for aggregate report visibility.',
	'operations.readModel.guidance.collection.unknownDisclosure':
		'Report visibility readiness is unknown because disclosure policy is missing.',
	'operations.readModel.guidance.score.complete':
		'All submitted responses have successful scoring activity.',
	'operations.readModel.guidance.score.partial':
		'Some submitted responses still need scoring activity before score-dependent reports are complete.',
	'operations.readModel.guidance.score.notConfigured':
		'Submitted responses exist, but scoring is not configured for those campaigns.',
	'operations.readModel.guidance.score.noSubmissions':
		'No submitted responses are available for score coverage yet.',
	'operations.readModel.row.selectedWave': 'Selected wave',
	'operations.readModel.row.status': 'Status',
	'operations.readModel.row.collectionStarted': 'Collection started',
	'operations.readModel.row.missingPrerequisites': 'Missing prerequisites',
	'operations.readModel.row.identityMode': 'Identity mode',
	'operations.readModel.row.respondentLinks': 'Respondent links',
	'operations.readModel.row.queuedEmails': 'Queued emails',
	'operations.readModel.row.sentEmails': 'Sent emails',
	'operations.readModel.row.failedEmails': 'Failed emails',
	'operations.readModel.row.suppressedEmails': 'Suppressed emails',
	'operations.readModel.row.latestEmailActivity': 'Latest email activity',
	'operations.readModel.row.startedResponses': 'Started responses',
	'operations.readModel.row.draftResponses': 'Draft responses',
	'operations.readModel.row.submittedResponses': 'Submitted responses',
	'operations.readModel.row.latestStarted': 'Latest started',
	'operations.readModel.row.latestSubmitted': 'Latest submitted',
	'operations.readModel.row.reportVisibility': 'Report visibility',
	'operations.readModel.row.scoreCoverage': 'Score coverage',
	'operations.readModel.row.scoredSubmitted': 'Scored submitted',
	'operations.readModel.row.unscoredSubmitted': 'Unscored submitted',
	'operations.readModel.row.notConfigured': 'Not configured',
	'operations.readModel.row.campaignsWithScoring': 'Campaigns with scoring',
	'operations.readModel.row.campaignsWithoutScoring': 'Campaigns without scoring',
	'operations.readModel.row.latestScoringActivity': 'Latest scoring activity',
	'operations.readModel.value.missing': 'Missing',
	'operations.readModel.status.live': 'live',
	'operations.readModel.status.draft': 'draft',
	'operations.readModel.status.closed': 'closed',
	'operations.readModel.status.complete': 'complete',
	'operations.readModel.status.partial': 'partial',
	'operations.readModel.status.noSubmissions': 'no submissions',
	'operations.readModel.status.notConfigured': 'not configured'
} satisfies Record<string, AppMessageTemplate>;

export type AppMessageId = keyof typeof enMessages;
type AppMessageCatalog = Record<AppMessageId, AppMessageTemplate>;

const hrMessages: AppMessageCatalog = {
	...enMessages,
	'overview.surface.title': 'Pregled studije',
	'overview.surface.description':
		'Ovaj pregled koristite za pripremu, prikupljanje, pregled rezultata i usporedbu mjerenja u odabranoj studiji.',
	'overview.reference.title': 'Referenca studije',
	'overview.reference.description':
		'Detaljni zapisi, status pravila i redci mjerenja za odabranu studiju.',
	'overview.untitledSeries': 'Studija bez naziva',
	'overview.untitledStudy': 'Studija bez naziva',
	'overview.subtitle': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'campaignCount'), 'measurement')}, aktivno: ${formatNumber(
			locale,
			numberValue(values, 'liveCount')
		)}`,
	'overview.lifecycle.title': 'Životni ciklus studije',
	'overview.lifecycle.description':
		'Prođite kroz studiju od pripreme do prikupljanja, rezultata i usporedbe mjerenja.',
	'overview.lifecycle.setup.label': 'Priprema',
	'overview.lifecycle.setup.description':
		'Izradite upitnik, pripremu rezultata, pravila, mjerenje i provjeru pokretanja.',
	'overview.lifecycle.operations.label': 'Prikupljanje',
	'overview.lifecycle.operations.description':
		'Pokrenite mjerenje, otvorite pristup, pošaljite pozive i pratite odgovore.',
	'overview.lifecycle.reports.label': 'Pregled rezultata',
	'overview.lifecycle.reports.description':
		'Pregledajte nalaze, ograničenja i datoteke izvoza nakon što odgovori budu spremni.',
	'overview.lifecycle.waves.label': 'Usporedba mjerenja',
	'overview.lifecycle.waves.description':
		'Izradite sljedeća mjerenja i usporedite rezultate između krugova prikupljanja.',
	'overview.studyModel.title': 'Što ova studija sadrži',
	'overview.studyModel.description':
		'Studija je projektni spremnik. Izvor upitnika je samo početni materijal; upitnik je ono što ispitanici vide; mjerenja su krugovi prikupljanja; rezultati i izvozi koriste spremljene odgovore.',
	'overview.studyModel.studyContainer.label': 'Studija',
	'overview.studyModel.studyContainer.badge': 'Spremljeno',
	'overview.studyModel.studyContainer.summary': (values) =>
		`${textValue(
			values,
			'studyName'
		)} sadrži postavljanje, mjerenja, rezultate i izvoze. To nije upitnik ni instrument.`,
	'overview.studyModel.studyContainer.guidance':
		'Otvorite Postavljanje kada trebate promijeniti što ispitanici odgovaraju ili kako se rezultati pripremaju.',
	'overview.studyModel.questionnaireResults.label': 'Upitnik i izlazi rezultata',
	'overview.studyModel.questionnaireResults.summary':
		'Upitnik definira pitanja; izlazi rezultata definiraju koje odgovore zbrajate, izvozite ili tumačite.',
	'overview.studyModel.questionnaireResults.guidance':
		'Otvorite Postavljanje za dovršetak ili provjeru izvora, upitnika, izlaza rezultata, mjerenja i spremnosti pokretanja.',
	'overview.studyModel.collectionWaves.label': 'Mjerenja prikupljanja',
	'overview.studyModel.collectionWaves.badge.live': 'Aktivno',
	'overview.studyModel.collectionWaves.badge.prepared': 'Pripremljeno',
	'overview.studyModel.collectionWaves.badge.none': 'Nema mjerenja',
	'overview.studyModel.collectionWaves.summary.none': 'Još nema mjerenja prikupljanja.',
	'overview.studyModel.collectionWaves.summary.existing': (values, locale) =>
		`Mjerenja u studiji: ${formatCount(
			locale,
			numberValue(values, 'campaignCount'),
			'measurement'
		)}; aktivno: ${formatNumber(locale, numberValue(values, 'liveCount'))}.`,
	'overview.studyModel.collectionWaves.guidance':
		'Izradite ili provjerite mjerenje, zatim koristite Prikupljanje za otvaranje pristupa ispitanicima.',
	'overview.studyModel.evidenceOutputs.label': 'Dokazi i usporedba',
	'overview.studyModel.evidenceOutputs.badge.ready': 'Dokazi spremni',
	'overview.studyModel.evidenceOutputs.badge.needsScoring': 'Treba bodovanje',
	'overview.studyModel.evidenceOutputs.badge.none': 'Još nema dokaza',
	'overview.studyModel.evidenceOutputs.summary': (values, locale) =>
		`Zabilježeno: ${formatCount(
			locale,
			numberValue(values, 'submittedCount'),
			'response'
		)}, ${formatCount(locale, numberValue(values, 'scoreCount'), 'score')} i ${formatCount(
			locale,
			numberValue(values, 'exportCount'),
			'exportFile'
		)}.`,
	'overview.studyModel.evidenceOutputs.guidance':
		'Koristite Rezultate za trenutne dokaze, Mjerenja za ponovljene krugove prikupljanja i Izvoze za predaju analize.',
	'overview.row.waves': 'Mjerenja',
	'overview.row.liveWaves': 'Aktivna mjerenja',
	'overview.row.submittedResponses': 'Predani odgovori',
	'overview.row.scores': 'Rezultati',
	'overview.row.exportFiles': 'Datoteke izvoza',
	'overview.row.identityMode': 'Način identiteta',
	'overview.row.locale': 'Jezik',
	'overview.row.created': 'Izrađeno',
	'overview.row.updated': 'Ažurirano',
	'overview.row.archived': 'Arhivirano',
	'overview.row.archiveReason': 'Razlog arhiviranja',
	'overview.governance.consent': 'Pristanak',
	'overview.governance.retention': 'Zadržavanje',
	'overview.governance.disclosure': 'Prikaz rezultata',
	'overview.governance.scoring': 'Bodovanje',
	'overview.badge.ready': 'Spremno',
	'overview.badge.pending': 'Na čekanju',
	'overview.badge.blocked': 'Blokirano',
	'overview.badge.live': 'Aktivno',
	'overview.badge.notConfigured': 'Nije postavljeno',
	'overview.badge.notAvailable': 'Nije dostupno',
	'exports.library.surface.title': 'Datoteke za preuzimanje',
	'exports.library.surface.eyebrow': 'Podrška studiji',
	'exports.library.surface.description':
		'Pronađite izrađene CSV/šifrarnik datoteke prema namjeni, spremnosti, izvornoj studiji i sljedećoj upotrebi.',
	'exports.library.reference.title': 'Referenca izvoza',
	'exports.library.reference.description':
		'Metapodaci datoteke, vremenske oznake, kodovi grešaka i dostupnost preuzimanja ostaju dostupni za audit i rješavanje problema.',
	'exports.library.row.exportFiles': 'Datoteke izvoza',
	'exports.library.row.downloadable': 'Dostupno za preuzimanje',
	'exports.library.row.failed': 'Neuspjelo',
	'exports.library.row.pending': 'Na čekanju',
	'exports.library.row.reportSummaryExports': 'Izvozi sažetka izvještaja',
	'exports.library.row.responseDatasetExports': 'Izvozi skupa podataka odgovora',
	'exports.library.row.reportSummaryDownloads': 'Izvozi sažetka izvještaja',
	'exports.library.row.responseDatasets': 'Skupovi podataka odgovora',
	'exports.library.row.campaignFiles': 'Datoteke mjerenja',
	'exports.library.row.studyFiles': 'Datoteke studije',
	'exports.library.row.studyContext': 'Kontekst studije',
	'exports.library.row.fileType': 'Vrsta datoteke',
	'exports.library.row.format': 'Format',
	'exports.library.row.dataFinality': 'Finalnost podataka',
	'exports.library.row.rows': 'Redci',
	'exports.library.row.size': 'Veličina',
	'exports.library.row.created': 'Izrađeno',
	'exports.library.row.completed': 'Dovršeno',
	'exports.library.row.failure': 'Greška',
	'exports.library.row.download': 'Preuzimanje',
	'exports.library.readyDownloads.label': 'Datoteke za preuzimanje',
	'exports.library.readyDownloads.badge': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} dostupno za preuzimanje`,
	'exports.library.readyDownloads.summary.ready': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'count'),
			'exportFile'
		)} spremno je za preuzimanje.`,
	'exports.library.readyDownloads.summary.empty':
		'Još nema izvoznih datoteka spremnih za preuzimanje.',
	'exports.library.readyDownloads.guidance.withDataset':
		'Koristite izvoze skupa podataka odgovora za analizu. Izvoze sažetka izvještaja koristite za pregledne pakete, sažetke za klijente ili provjere šifrarnika.',
	'exports.library.readyDownloads.guidance.reportOnly':
		'Sažeci izvještaja dostupni su za pregledne pakete, sažetke za klijente ili provjere šifrarnika. Skup odgovora za analizu još nije dostupan.',
	'exports.library.readyDownloads.guidance.empty':
		'Izradite izvoz sa stranice rezultata studije nakon što rezultati budu dostupni.',
	'exports.library.attention.label': 'Treba pažnju',
	'exports.library.attention.badge.failed': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} neuspjelo`,
	'exports.library.attention.badge.pending': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} na čekanju`,
	'exports.library.attention.badge.ready': 'Nema stavki za pažnju',
	'exports.library.attention.summary.failed': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'count'), 'exportFile')} traži pažnju.`,
	'exports.library.attention.summary.pending': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'count'),
			'exportFile'
		)} još je u redu čekanja ili se izrađuje.`,
	'exports.library.attention.summary.ready':
		'Nema neuspjelih izvoznih datoteka ni datoteka na čekanju.',
	'exports.library.attention.guidance.failed':
		'Pregledajte neuspjelu izvoznu datoteku, zatim je ponovno izradite iz izvorne studije nakon uklanjanja uzroka.',
	'exports.library.attention.guidance.pending':
		'Pričekajte završetak izrade prije korištenja izvoza za predaju.',
	'exports.library.attention.guidance.ready':
		'Novi problemi izvoza pojavit će se ovdje kada izrada ne uspije ili ostane na čekanju.',
	'exports.library.purpose.label': 'Namjena datoteke',
	'exports.library.purpose.badge.ready': (values, locale) =>
		formatCount(locale, numberValue(values, 'count'), 'purpose'),
	'exports.library.purpose.badge.empty': 'Nema datoteka',
	'exports.library.purpose.summary.ready': (values) =>
		`Izvozi pokrivaju ${textValue(values, 'purposeLabels')}.`,
	'exports.library.purpose.summary.empty': 'Još nema namjena izrađenih izvoznih datoteka.',
	'exports.library.purpose.guidance.ready':
		'Izvoze sažetka izvještaja odaberite za predaju rezultata; izvoze skupa odgovora za analizu uz šifrarnik.',
	'exports.library.purpose.guidance.empty':
		'Izradite sažetak izvještaja ili skup odgovora iz studije kada rezultati budu spremni.',
	'exports.library.context.label': 'Kontekst studije i sljedeća upotreba',
	'exports.library.context.badge.ready': (values, locale) =>
		`${formatNumber(locale, numberValue(values, 'count'))} izvora`,
	'exports.library.context.badge.empty': 'Nema izvora',
	'exports.library.context.summary.ready': (values) =>
		`Izvozne datoteke povezane su s ${textValue(values, 'sourceLabels')}.`,
	'exports.library.context.summary.empty':
		'Još nema izvoznih datoteka povezanih sa studijom.',
	'exports.library.context.guidance.ready':
		'Otvorite izvornu studiju ili kontekst izvještaja kada trebate razumjeti kako je izvoz izrađen.',
	'exports.library.context.guidance.empty':
		'Izrađene izvozne datoteke povezat će se sa studijom ili kontekstom izvještaja kada taj kontekst bude dostupan.',
	'exports.library.purpose.reportSummary.label': 'Izvoz sažetka izvještaja',
	'exports.library.purpose.reportSummary.nextUse':
		'Koristite ovaj izvoz za predaju izvještaja, pregled sažetka ili provjere šifrarnika.',
	'exports.library.purpose.responseDataset.label': 'Izvoz skupa podataka odgovora',
	'exports.library.purpose.responseDataset.nextUse':
		'Koristite ovaj izvoz za analizu na razini odgovora s generiranim šifrarnikom.',
	'exports.library.purpose.other.nextUse':
		'Ovaj izvoz koristite uz izvorni kontekst i generirani šifrarnik.',
	'exports.library.context.campaignSeries': (values) =>
		`Studija / ${textValue(values, 'label')}`,
	'exports.library.context.campaign': (values) => `Mjerenje / ${textValue(values, 'label')}`,
	'exports.library.fileType.reportSummary': 'CSV i šifrarnik sažetka izvještaja',
	'exports.library.fileType.responseDataset': 'CSV i šifrarnik skupa odgovora',
	'exports.library.fileFormat.csvCodebook': 'CSV šifrarnik',
	'exports.library.finality.closedWave': 'Zatvoreno mjerenje',
	'exports.library.finality.notClosedWave': 'Nije vezano uz zatvoreno mjerenje',
	'exports.library.download.available': 'Dostupno',
	'exports.library.download.notAvailable': 'Nije dostupno',
	'readModel.status.preview': 'pregled',
	'readModel.status.ready': 'spremno',
	'readModel.status.blocked': 'blokirano',
	'readModel.status.live': 'u tijeku',
	'readModel.status.pending': 'na čekanju',
	'readModel.status.notAvailable': 'nije dostupno',
	'readModel.status.notConfigured': 'nije postavljeno',
	'readModel.identity.anonymous': 'anonimno',
	'readModel.identity.anonymousLongitudinal': 'anonimno s ponovljenim sudjelovanjem',
	'readModel.identity.identified': 'identificirano',
	'readModel.untitledWaveSeries': 'Neimenovana studija',
	'readModel.untitledWave': 'Neimenovano mjerenje',
	'readModel.subtitle.campaignsLive': (values, locale) =>
		`${formatCount(locale, numberValue(values, 'campaignCount'), 'measurement')}, ${formatNumber(
			locale,
			numberValue(values, 'liveCount')
		)} aktivno`,
	'readModel.row.campaigns': 'Mjerenja',
	'readModel.row.liveCampaigns': 'Aktivna mjerenja',
	'readModel.row.missingPrerequisites': 'Nedostajući preduvjeti',
	'readModel.row.submittedResponses': 'Predani odgovori',
	'readModel.surface.setup.label': 'Priprema studije',
	'readModel.surface.setup.eyebrow': 'Priprema studije',
	'readModel.surface.setup.description':
		'Pripremite studiju za prikupljanje dovršavanjem postavljanja i provjera spremnosti.',
	'readModel.surface.setup.referenceTitle': 'Referenca postavljanja',
	'readModel.surface.setup.referenceDescription':
		'Detaljni zapisi postavljanja, status pravila, polja odabranog mjerenja i bilješke provjere pokretanja ostaju ovdje za pregled.',
	'readModel.surface.setup.emptyTitle': 'Još nema mjerenja',
	'readModel.surface.setup.emptyMessage': 'Izradite nacrt mjerenja prije provjere spremnosti za pokretanje.',
	'readModel.surface.setup.proofTitle': 'Radnje pripreme',
	'readModel.surface.setup.proofDescription':
		'Odaberite izvor upitnika, pripremite upitnik i izlaze rezultata, izradite nacrt mjerenja i provjerite spremnost pokretanja.',
	'settings.tenant.untitled': 'Postavke radnog prostora',
	'settings.profile.region': 'Regija',
	'settings.profile.defaultLocale': 'Zadani jezik',
	'settings.profile.created': 'Izrađeno',
	'settings.profile.updated': 'Ažurirano',
	'settings.metric.studies': 'Studije',
	'settings.metric.campaigns': 'Mjerenja',
	'settings.metric.liveCampaigns': 'Aktivna mjerenja',
	'settings.metric.submittedResponses': 'Predani odgovori',
	'settings.metric.subjects': 'Osobe',
	'settings.metric.subjectGroups': 'Grupe osoba',
	'settings.metric.tenantMembers': 'Članovi radnog prostora',
	'settings.metric.tenantRoles': 'Uloge radnog prostora',
	'settings.metric.exportFiles': 'Datoteke izvoza',
	'instruments.metric.sources': 'Izvori upitnika',
	'instruments.metric.launchEligible': 'Spremno za pokretanje',
	'instruments.metric.launchBlocked': 'Blokirano za pokretanje',
	'instruments.status.launchEligible': 'Spremno za pokretanje',
	'instruments.status.launchBlocked': 'Blokirano za pokretanje',
	'instruments.row.rights': 'Prava korištenja',
	'instruments.row.validity': 'Status valjanosti',
	'session.checking.title': 'Provjera pristupa',
	'session.checking.message': 'Učitavanje pristupa radnom prostoru.',
	'session.authenticated.title': 'Prijavljeni ste',
	'session.authenticated.message': (values) => `Radni prostor ${textValue(values, 'tenantId')}`,
	'session.unauthenticated.title': 'Potrebna je prijava',
	'session.unauthenticated.message': 'Prijavite se prije otvaranja površina radnog prostora.',
	'session.forbidden.title': 'Pristup radnom prostoru nije dostupan',
	'session.forbidden.message': 'Ova sesija nema pristup odabranom radnom prostoru.',
	'session.failed.title': 'Provjera sesije nije uspjela',
	'session.failed.statusMessage': (values) =>
		`Provjera sesije nije uspjela sa statusom ${textValue(values, 'status')}.`,
	'session.failed.message': 'Provjera sesije nije uspjela.',
	'workspace.lifecycle.prepare.label': 'Priprema',
	'workspace.lifecycle.prepare.description': 'Postavite upitnik, pitanja, rezultate i pravila pokretanja.',
	'workspace.lifecycle.collect.label': 'Prikupljanje',
	'workspace.lifecycle.collect.description': 'Pokrenite studiju i pratite napredak odgovora.',
	'workspace.lifecycle.review.label': 'Pregled',
	'workspace.lifecycle.review.description': 'Pregledajte pokrivenost, nalaze, ograničenja i usporedbe.',
	'workspace.lifecycle.export.label': 'Izvoz',
	'workspace.lifecycle.export.description': 'Koristite izrađene CSV i šifrarnik datoteke za analizu.',
	'workspace.command.untitled': 'Radnja radnog prostora',
	'workspace.command.defaultDescription': 'Otvorite povezanu površinu proizvoda za detalje.',
	'workspace.command.defaultAction': 'Otvori',
	'workspace.row.surface': 'Površina',
	'workspace.surface.campaignSeries': 'Studije',
	'workspace.surface.directory': 'Imenik',
	'workspace.surface.operations': 'Prikupljanje',
	'workspace.surface.reports': 'Rezultati',
	'workspace.surface.setup': 'Postavljanje',
	'workspace.surface.team': 'Tim',
	'workspace.surface.waves': 'Mjerenja',
	'workspace.surface.workspace': 'Radni prostor',
	'workspace.action.reviewSampleResults': 'Pregledaj ogledne rezultate',
	'workspace.action.inspectSampleCollection': 'Pregledaj ogledno prikupljanje',
	'workspace.action.inspectSampleSetup': 'Pregledaj ogledno postavljanje',
	'workspace.action.continueSetup': 'Nastavi postavljanje',
	'workspace.action.monitorCollection': 'Prati prikupljanje',
	'workspace.action.reviewResults': 'Pregledaj rezultate',
	'workspace.action.openStudy': 'Otvori studiju',
	'workspace.action.inspectStudy': 'Pregledaj studiju',
	'workspace.action.openArchivedStudy': 'Otvori arhiviranu studiju',
	'portfolio.empty.noMatching.title': 'Nema odgovarajućih studija',
	'portfolio.empty.noMatching.message':
		'Promijenite pretragu, spremnost ili vidljivost kako biste prikazali ogledne ili vlastite studije.',
	'portfolio.empty.noStudies.title': 'Još nema studija',
	'portfolio.empty.noStudies.message':
		'Izradite studiju kada imate pristup postavljanju ili dodajte ogledne studije za učenje.',
	'portfolio.filter.allReadiness': 'Sva spremnost',
	'portfolio.filter.notConfigured': 'Nije postavljeno',
	'portfolio.filter.pending': 'Na čekanju',
	'portfolio.filter.preview': 'Pregled',
	'portfolio.sort.latestActivity': 'Zadnja aktivnost',
	'portfolio.sort.recentlyUpdated': 'Nedavno ažurirano',
	'portfolio.sort.recentlyCreated': 'Nedavno izrađeno',
	'portfolio.sort.nameAscending': 'Naziv A-Z',
	'portfolio.visibility.active': 'Aktivno',
	'portfolio.visibility.archived': 'Arhivirano',
	'portfolio.visibility.all': 'Sva vidljivost',
	'portfolio.section.sample.title': 'Ogledne studije',
	'portfolio.section.sample.description':
		'Primjeri samo za čitanje koje možete pregledati prije izrade vlastite studije.',
	'portfolio.section.sample.empty':
		'Nijedna ogledna studija ne odgovara ovom prikazu. Očistite filtre za pregled primjera.',
	'portfolio.section.own.title': 'Vaše studije',
	'portfolio.section.own.description': 'Uredivi zapisi studija u vlasništvu ovog radnog prostora.',
	'portfolio.section.own.empty':
		'Nijedna vlastita studija ne odgovara ovom prikazu. Očistite filtre ili izradite studiju kada imate pristup postavljanju.',
	'portfolio.lifecycle.needsSetup.label': 'Treba postavljanje',
	'portfolio.lifecycle.needsSetup.description': 'Studije koje treba postaviti prije smislenog prikupljanja.',
	'portfolio.lifecycle.inCollection.label': 'U prikupljanju',
	'portfolio.lifecycle.inCollection.description': 'Studije s aktivnim prikupljanjem koje treba pratiti.',
	'portfolio.lifecycle.resultsReady.label': 'Rezultati spremni',
	'portfolio.lifecycle.resultsReady.description': 'Studije s predanim odgovorima spremnima za pregled.',
	'portfolio.lifecycle.archived.label': 'Arhivirano',
	'portfolio.lifecycle.archived.description': 'Studije zadržane kao referenca nakon završetka aktivnog rada.',
	'portfolio.lifecycle.open.label': 'Otvori studiju',
	'portfolio.lifecycle.open.description': 'Studije dostupne za uobičajeni pregled.',
	'portfolio.row.campaigns': 'Mjerenja',
	'portfolio.row.liveCampaigns': 'Aktivna mjerenja',
	'portfolio.row.submittedResponses': 'Predani odgovori',
	'portfolio.row.latestActivity': 'Zadnja aktivnost',
	'portfolio.row.archived': 'Arhivirano',
	'portfolio.value.notAvailable': 'Nije dostupno',
	'portfolio.action.archive': 'Arhiviraj',
	'portfolio.action.restore': 'Vrati',
	'portfolio.action.duplicateAsStudy': 'Dupliciraj kao studiju',
	'portfolio.ownership.sample.label': 'Ogledna studija',
	'portfolio.ownership.own.label': 'Vaša studija',
	'portfolio.sampleReadOnly.setup':
		'Ogledno postavljanje: sadržaj samo za čitanje koji prikazuje pripremu studije prije pokretanja.',
	'portfolio.sampleReadOnly.blocked':
		'Ogledno postavljanje: sadržaj samo za čitanje koji prikazuje blokiranu pripremu prije pokretanja.',
	'portfolio.sampleReadOnly.inCollection':
		'Ogledno prikupljanje: sadržaj samo za čitanje koji prikazuje aktivno ili djelomično prikupljanje odgovora.',
	'portfolio.sampleReadOnly.longitudinal':
		'Ogledni primjer ponovljenog sudjelovanja: sadržaj samo za čitanje koji prikazuje ponovljena mjerenja i pregled povezanih ponovljenih odgovora.',
	'portfolio.sampleReadOnly.results':
		'Ogledni rezultati: sadržaj samo za čitanje koji prikazuje prikupljene odgovore, rezultate, izvještaje i izvoze.',
	'portfolio.sampleReadOnly.default':
		'Ogledna studija: sadržaj samo za čitanje koji možete pregledati prije dupliciranja.',
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
		'Polja praćenja koristite samo kada postoji anonimno s ponovljenim sudjelovanjem povezivanje i kada pravila prikaza to dopuštaju.',
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
		'Povezana promjena koristi parove ponovljenih odgovora nakon provjera kompatibilnosti bodovanja i pravila prikaza.',
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
		)}.`,
	'operations.status.reportVisibility.notAvailable': 'Nije dostupno',
	'operations.status.reportVisibility.reportable': 'spremno za izvještaj',
	'operations.status.reportVisibility.visible': 'vidljivo',
	'operations.status.reportVisibility.blocked': 'blokirano',
	'operations.status.reportVisibility.unknownPolicy': 'pravila nisu potvrđena',
	'operations.status.submittedTitle': (values, locale) =>
		`Predano: ${formatCount(locale, numberValue(values, 'submitted'), 'response')}`,
	'operations.status.responseActivityDetail': (values, locale) =>
		`Započeto: ${formatNumber(locale, numberValue(values, 'started'))}; u tijeku: ${formatNumber(
			locale,
			numberValue(values, 'drafts')
		)}; predano: ${formatNumber(locale, numberValue(values, 'submitted'))}.`,
	'operations.status.closedHeadline': (values, locale) =>
		`Zatvoreno; predano: ${formatCount(locale, numberValue(values, 'submitted'), 'response')}`,
	'operations.status.liveHeadline': (values, locale) =>
		`Aktivno: prihvaća odgovore; predano: ${formatCount(
			locale,
			numberValue(values, 'submitted'),
			'response'
		)}`,
	'operations.access.identifiedDetail': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'openLinkCount'),
			'openRespondentLink'
		)} pripremljeno. Sudionici su povezani s poznatim zapisima osoba za ovo mjerenje.`,
	'operations.access.inviteOnlyDetail': (values, locale) =>
		`Za ovo mjerenje spremno je: ${formatCount(
			locale,
			numberValue(values, 'invitationCount'),
			'emailInvitation'
		)}. Samo spremljeni primatelji dobivaju privatni pristup, a ${textValue(
			values,
			'boundary'
		)}`,
	'operations.access.mixedDetail': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'openLinkCount'),
			'openRespondentLink'
		)} i ${formatCount(
			locale,
			numberValue(values, 'invitationCount'),
			'emailInvitation'
		)}. Otvorena poveznica je širok pristup; invite-only e-pošta ograničava ulaz na spremljene primatelje. ${textValue(
			values,
			'boundary'
		)}`,
	'operations.access.openLinkDetail': (values, locale) =>
		`${formatCount(
			locale,
			numberValue(values, 'openLinkCount'),
			'openRespondentLink'
		)} aktivno je. Svatko s poveznicom može ući u ovo mjerenje; koristite spremljene pozive kad pristup treba biti ograničen.`,
	'operations.suppression.headline': (values, locale) => {
		const blockedCount = numberValue(values, 'blockedCount');
		if (blockedCount === 0) {
			return 'Nema primatelja na popisu osoba koje ne treba kontaktirati';
		}
		return `${formatCount(locale, blockedCount, 'recipient')} ${
			blockedCount === 1 ? 'je' : 'su'
		} na popisu osoba koje ne treba kontaktirati`;
	},
	'operations.suppression.guidance.blocked':
		'Koristite drugu adresu e-pošte, uklonite primatelja ili maknite blokadu samo ako ste sigurni da je budući poziv opravdan.',
	'operations.suppression.guidance.clear':
		'Nema aktivnih zabrana kontaktiranja za ove primatelje.',
	'operations.suppression.reason.recipientUnsubscribed': 'Odjavljeno',
	'operations.suppression.reason.providerBounced': 'Odbijena isporuka',
	'operations.suppression.reason.providerComplained': 'Prijava neželjene pošte',
	'operations.suppression.reason.operatorDoNotContact': 'Ručno blokirano',
	'operations.suppression.source.respondentInvitationLink': 'Poveznica za odjavu iz poziva',
	'operations.suppression.source.providerDeliveryEvent': 'Događaj isporuke od pružatelja',
	'operations.suppression.source.tenantOperator': 'Administrator radnog prostora',
	'operations.suppression.sourceCreatedAt': (values) =>
		`${textValue(values, 'sourceLabel')} - dodano ${textValue(values, 'createdAt')}`,
	'operations.readModel.collectionMonitor.title': 'Praćenje odgovora',
	'operations.readModel.scoreCoverage.title': 'Pokrivenost bodovanja',
	'operations.readModel.collectionState.label': 'Status prikupljanja',
	'operations.readModel.collectionState.emptyLabel': 'Stanje prikupljanja',
	'operations.readModel.collectionState.blockedBadge': 'Blokirano',
	'operations.readModel.collectionState.noSelectedSummary':
		'Nema odabranog mjerenja za prikupljanje odgovora',
	'operations.readModel.collectionState.noSelectedGuidance':
		'Izradite i pokrenite mjerenje prije prikupljanja odgovora.',
	'operations.readModel.collectionState.selectedSummary': (values) =>
		`${textValue(values, 'campaignName')}: prikupljanje je ${textValue(
			values,
			'statusLabel'
		)}.`,
	'operations.readModel.respondentAccess.label': 'Pristup ispitanika',
	'operations.readModel.respondentAccess.badge.ready': 'Pristup je spreman',
	'operations.readModel.respondentAccess.badge.pending': 'Priprema pristupa',
	'operations.readModel.respondentAccess.summary': (values) =>
		`${textValue(values, 'openLinkSummary')}, ${textValue(values, 'sentEmailSummary')}`,
	'operations.readModel.respondentAccess.guidance.mixed':
		'Ispitanici mogu pristupiti preko podijeljene poveznice i poslanih e-poruka.',
	'operations.readModel.respondentAccess.guidance.openLink':
		'Ispitanici mogu pristupiti preko podijeljene poveznice.',
	'operations.readModel.respondentAccess.guidance.email':
		'Ispitanici mogu pristupiti preko poslanih e-poruka.',
	'operations.readModel.respondentAccess.guidance.none':
		'Pripremite pristup ispitanika prije prikupljanja odgovora.',
	'operations.readModel.responseProgress.label': 'Napredak odgovora',
	'operations.readModel.responseProgress.badge.withSubmissions': (values, locale) =>
		`Predano: ${formatCount(locale, numberValue(values, 'submitted'), 'response')}`,
	'operations.readModel.responseProgress.badge.empty': 'Nema predaja',
	'operations.readModel.responseProgress.summary': (values, locale) =>
		appMessage(locale, 'operations.status.responseActivityDetail', values),
	'operations.readModel.scoreReadiness.label': 'Spremnost rezultata i izvještaja',
	'operations.readModel.scoreReadiness.badge.ready': 'Izvještaji su spremni',
	'operations.readModel.scoreReadiness.badge.empty': 'Nema predaja',
	'operations.readModel.scoreReadiness.badge.notConfigured': 'Nije konfigurirano',
	'operations.readModel.scoreReadiness.summary.withSubmissions': (values, locale) =>
		`Bodovano je ${formatNumber(locale, numberValue(values, 'scored'))} od ${formatNumber(
			locale,
			numberValue(values, 'submitted')
		)} predanih odgovora.`,
	'operations.readModel.scoreReadiness.summary.empty': 'Nema predanih odgovora za bodovanje.',
	'operations.readModel.guidance.collection.readyForAggregateReport':
		'Ima dovoljno predanih odgovora za skupni prikaz rezultata.',
	'operations.readModel.guidance.collection.unknownDisclosure':
		'Spremnost prikaza rezultata nije poznata jer nedostaju pravila prikaza.',
	'operations.readModel.guidance.score.complete':
		'Svi predani odgovori imaju uspješno bodovanje.',
	'operations.readModel.guidance.score.partial':
		'Neki predani odgovori još trebaju bodovanje prije dovršetka izvještaja koji ovise o rezultatima.',
	'operations.readModel.guidance.score.notConfigured':
		'Predani odgovori postoje, ali bodovanje nije konfigurirano za ta mjerenja.',
	'operations.readModel.guidance.score.noSubmissions':
		'Još nema predanih odgovora za pokrivenost bodovanja.',
	'operations.readModel.row.selectedWave': 'Odabrano mjerenje',
	'operations.readModel.row.status': 'Status',
	'operations.readModel.row.collectionStarted': 'Prikupljanje pokrenuto',
	'operations.readModel.row.missingPrerequisites': 'Nedostajući preduvjeti',
	'operations.readModel.row.identityMode': 'Način identiteta',
	'operations.readModel.row.respondentLinks': 'Poveznice za ispitanike',
	'operations.readModel.row.queuedEmails': 'E-poruke u redu',
	'operations.readModel.row.sentEmails': 'Poslane e-poruke',
	'operations.readModel.row.failedEmails': 'Neuspjele e-poruke',
	'operations.readModel.row.suppressedEmails': 'Blokirane e-poruke',
	'operations.readModel.row.latestEmailActivity': 'Zadnja aktivnost e-pošte',
	'operations.readModel.row.startedResponses': 'Započeti odgovori',
	'operations.readModel.row.draftResponses': 'Odgovori u tijeku',
	'operations.readModel.row.submittedResponses': 'Predani odgovori',
	'operations.readModel.row.latestStarted': 'Zadnje započeto',
	'operations.readModel.row.latestSubmitted': 'Zadnje predano',
	'operations.readModel.row.reportVisibility': 'Prikaz rezultata',
	'operations.readModel.row.scoreCoverage': 'Pokrivenost bodovanja',
	'operations.readModel.row.scoredSubmitted': 'Bodovani predani odgovori',
	'operations.readModel.row.unscoredSubmitted': 'Nebodovani predani odgovori',
	'operations.readModel.row.notConfigured': 'Nije konfigurirano',
	'operations.readModel.row.campaignsWithScoring': 'Mjerenja s bodovanjem',
	'operations.readModel.row.campaignsWithoutScoring': 'Mjerenja bez bodovanja',
	'operations.readModel.row.latestScoringActivity': 'Zadnja aktivnost bodovanja',
	'operations.readModel.value.missing': 'Nedostaje',
	'operations.readModel.status.live': 'aktivno',
	'operations.readModel.status.draft': 'nacrt',
	'operations.readModel.status.closed': 'zatvoreno',
	'operations.readModel.status.complete': 'dovršeno',
	'operations.readModel.status.partial': 'djelomično',
	'operations.readModel.status.noSubmissions': 'nema predaja',
	'operations.readModel.status.notConfigured': 'nije konfigurirano'
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
		emailInvitation: { one: 'saved email invitation', other: 'saved email invitations' },
		exportFile: { one: 'export file', other: 'export files' },
		invitationPair: { one: 'invitation pair', other: 'invitation pairs' },
		linkedPair: { one: 'linked pair', other: 'linked pairs' },
		measurement: { one: 'wave', other: 'waves' },
		openRespondentLink: { one: 'open respondent link', other: 'open respondent links' },
		purpose: { one: 'purpose', other: 'purposes' },
		question: { one: 'question', other: 'questions' },
		recipient: { one: 'recipient', other: 'recipients' },
		reportSummaryColumn: { one: 'report-summary column', other: 'report-summary columns' },
		response: { one: 'response', other: 'responses' },
		row: { one: 'row', other: 'rows' },
		score: { one: 'score', other: 'scores' },
		scoreMetadataField: { one: 'score metadata field', other: 'score metadata fields' },
		scoreOutput: { one: 'score output', other: 'score outputs' },
		sentEmail: { one: 'sent email', other: 'sent emails' },
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
		emailInvitation: {
			one: 'spremljeni poziv e-poštom',
			few: 'spremljena poziva e-poštom',
			other: 'spremljenih poziva e-poštom'
		},
		exportFile: {
			one: 'izvozna datoteka',
			few: 'izvozne datoteke',
			other: 'izvoznih datoteka'
		},
		invitationPair: {
			one: 'par pozivnica',
			few: 'para pozivnica',
			other: 'parova pozivnica'
		},
		linkedPair: { one: 'povezani par', few: 'povezana para', other: 'povezanih parova' },
		measurement: { one: 'mjerenje', few: 'mjerenja', other: 'mjerenja' },
		openRespondentLink: {
			one: 'otvorena poveznica za sudionike',
			few: 'otvorene poveznice za sudionike',
			other: 'otvorenih poveznica za sudionike'
		},
		purpose: { one: 'namjena', few: 'namjene', other: 'namjena' },
		question: { one: 'pitanje', few: 'pitanja', other: 'pitanja' },
		recipient: { one: 'primatelj', few: 'primatelja', other: 'primatelja' },
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
		sentEmail: {
			one: 'poslana e-poruka',
			few: 'poslane e-poruke',
			other: 'poslanih e-poruka'
		},
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
