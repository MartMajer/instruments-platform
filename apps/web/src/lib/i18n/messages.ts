import type { AppLocale } from './localization';

export type AppMessageValues = Record<string, number | string | null | undefined>;

type AppMessageTemplate = string | ((values: AppMessageValues, locale: AppLocale) => string);
type PluralCategory = 'zero' | 'one' | 'two' | 'few' | 'many' | 'other';
type CountNounId =
	| 'answerMetadataField'
	| 'answerVariable'
	| 'column'
	| 'measurement'
	| 'reportSummaryColumn'
	| 'response'
	| 'row'
	| 'score'
	| 'scoreMetadataField'
	| 'scoreOutput';

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
	'results.export.scoreOutputs.review.detail': 'Review score output fields before interpretation.'
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
		measurement: { one: 'wave', other: 'waves' },
		reportSummaryColumn: { one: 'report-summary column', other: 'report-summary columns' },
		response: { one: 'response', other: 'responses' },
		row: { one: 'row', other: 'rows' },
		score: { one: 'score', other: 'scores' },
		scoreMetadataField: { one: 'score metadata field', other: 'score metadata fields' },
		scoreOutput: { one: 'score output', other: 'score outputs' }
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
		measurement: { one: 'mjerenje', few: 'mjerenja', other: 'mjerenja' },
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
		scoreOutput: { one: 'izlaz rezultata', few: 'izlaza rezultata', other: 'izlaza rezultata' }
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
