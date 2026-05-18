export type ProductFixtureGroupId =
	| 'campaign-series'
	| 'setup'
	| 'operations'
	| 'reports'
	| 'exports'
	| 'waves'
	| 'respondent';

export type ProductFixtureScenario = {
	id: string;
	title: string;
	description: string;
	href: string;
	labels: string[];
	provenance: string;
	data: Record<string, string | number | boolean | null>;
};

export type ProductFixtureGroup = {
	id: ProductFixtureGroupId;
	title: string;
	description: string;
	scenarios: ProductFixtureScenario[];
};

const demoLabels = ['Demo data', 'Proof only'];

export const productFixtureCatalog: ProductFixtureGroup[] = [
	{
		id: 'campaign-series',
		title: 'Campaign series',
		description: 'Series list, selection, and one-wave or two-wave workspace states.',
		scenarios: [
			scenario('empty-list', 'Empty list', 'No campaign series have been created yet.', '/app/campaign-series', {
				seriesCount: 0
			}),
			scenario(
				'created-selected-series',
				'Created selected series',
				'A newly created campaign series is selected through the URL.',
				'/app/campaign-series/fixture-series-created',
				{ seriesCount: 1, selectedSeriesId: 'fixture-series-created' }
			),
			scenario(
				'one-wave-series',
				'One-wave series',
				'A single-wave pulse represented as a campaign series.',
				'/app/campaign-series/fixture-series-one-wave',
				{ waveCount: 1, responseIdentityMode: 'anonymous' }
			),
			scenario(
				'two-wave-series',
				'Two-wave series',
				'A campaign series with baseline and comparison waves.',
				'/app/campaign-series/fixture-series-two-wave',
				{ waveCount: 2, responseIdentityMode: 'anonymous_longitudinal' }
			)
		]
	},
	{
		id: 'setup',
		title: 'Setup',
		description: 'Configuration states for template, scoring, consent, disclosure, and readiness.',
		scenarios: [
			scenario(
				'empty',
				'Empty setup',
				'No instrument, template, scoring rule, or campaign draft exists yet.',
				'/app/campaign-series/fixture-series-empty/setup',
				{ readiness: 'not_started' }
			),
			scenario(
				'launch-ready',
				'Launch ready',
				'Template, scoring, consent, retention, disclosure, and identity mode are ready.',
				'/app/campaign-series/fixture-series-ready/setup',
				{ readiness: 'ready', blockers: 0 }
			),
			scenario(
				'blocked',
				'Blocked setup',
				'Launch readiness reports missing disclosure and consent prerequisites.',
				'/app/campaign-series/fixture-series-blocked/setup',
				{ readiness: 'blocked', blockers: 2 }
			)
		]
	},
	{
		id: 'operations',
		title: 'Operations',
		description: 'Launch, open-link, invitation, and local delivery states.',
		scenarios: [
			scenario(
				'launched-anonymous',
				'Launched anonymous',
				'Anonymous campaign is live and has an open respondent path.',
				'/app/campaign-series/fixture-series-live/operations',
				{ status: 'live', responseIdentityMode: 'anonymous' }
			),
			scenario(
				'launched-anonymous-longitudinal',
				'Launched anonymous longitudinal',
				'Wave is live and requires participant-code entry for linked trajectories.',
				'/app/campaign-series/fixture-series-linked/operations',
				{ status: 'live', responseIdentityMode: 'anonymous_longitudinal' }
			),
			scenario(
				'delivery-failed',
				'Delivery failed',
				'Local delivery has failed invitation sends that need inspection.',
				'/app/campaign-series/fixture-series-delivery/operations',
				{ status: 'live', failedDeliveries: 2 }
			)
		]
	},
	{
		id: 'reports',
		title: 'Reports',
		description: 'Aggregate preview rows with visible and disclosure-suppressed score states.',
		scenarios: [
			scenario(
				'visible-aggregate',
				'Visible aggregate',
				'Aggregate report preview has enough submitted responses for visible scores.',
				'/app/campaign-series/fixture-series-visible/reports',
				{ disclosure: 'visible', submittedResponseCount: 12 }
			),
			scenario(
				'suppressed-by-k-min',
				'Suppressed by k minimum',
				'Aggregate score rows are suppressed by disclosure k minimum.',
				'/app/campaign-series/fixture-series-suppressed/reports',
				{ disclosure: 'suppressed', kMin: 5 }
			),
			scenario(
				'missing-scores',
				'Missing scores',
				'Responses exist but scoring has not produced report rows yet.',
				'/app/campaign-series/fixture-series-missing-scores/reports',
				{ proofStatus: 'pending_scores' }
			)
		]
	},
	{
		id: 'exports',
		title: 'Exports',
		description: 'Governed report artifact lifecycle states inside Reports.',
		scenarios: [
			scenario(
				'created',
				'Created',
				'Aggregate export artifact request has been created.',
				'/app/campaign-series/fixture-series-export-created/reports',
				{ artifactStatus: 'created' }
			),
			scenario(
				'fetched',
				'Fetched',
				'Stored report artifact metadata and codebook are available.',
				'/app/campaign-series/fixture-series-export-fetched/reports',
				{ artifactStatus: 'completed', rowCount: 8 }
			),
			scenario(
				'downloadable',
				'Downloadable',
				'CSV artifact is ready for governed download.',
				'/app/campaign-series/fixture-series-export-downloadable/reports',
				{ artifactStatus: 'completed', format: 'csv' }
			),
			scenario(
				'not-found',
				'Not found',
				'Artifact retrieval returned a missing-artifact state.',
				'/app/campaign-series/fixture-series-export-missing/reports',
				{ artifactStatus: 'not_found' }
			)
		]
	},
	{
		id: 'waves',
		title: 'Waves',
		description: 'One-wave, linked two-wave, insufficient-n, and incompatible scoring states.',
		scenarios: [
			scenario(
				'one-wave',
				'One wave',
				'Only baseline wave exists, so comparison is not yet available.',
				'/app/campaign-series/fixture-series-one-wave/waves',
				{ waveCount: 1, proofStatus: 'not_ready' }
			),
			scenario(
				'two-waves-linked',
				'Two waves linked',
				'Baseline and comparison waves have linked trajectories above disclosure minimum.',
				'/app/campaign-series/fixture-series-two-wave/waves',
				{ waveCount: 2, linkedTrajectoryCount: 6 }
			),
			scenario(
				'insufficient-linked-n',
				'Insufficient linked n',
				'Linked trajectory count is below disclosure minimum for comparison.',
				'/app/campaign-series/fixture-series-low-linked-n/waves',
				{ disclosure: 'suppressed', linkedTrajectoryCount: 2 }
			),
			scenario(
				'incompatible-scoring',
				'Incompatible scoring',
				'Wave scores cannot be compared because scoring rule compatibility failed.',
				'/app/campaign-series/fixture-series-incompatible-scoring/waves',
				{ compatibilityStatus: 'incompatible_scoring' }
			)
		]
	},
	{
		id: 'respondent',
		title: 'Respondent',
		description: 'Public respondent states kept separate from authenticated product routes.',
		scenarios: [
			scenario(
				'valid-entry',
				'Valid entry',
				'Respondent open link loads consent and survey content.',
				'/r/fixture-open-link-valid',
				{ status: 'open' }
			),
			scenario(
				'consent-required',
				'Consent required',
				'Required consent grants must be accepted before session start.',
				'/r/fixture-open-link-consent',
				{ consentRequired: true }
			),
			scenario(
				'participant-code-required',
				'Participant code required',
				'Anonymous longitudinal open link requires participant-code entry.',
				'/r/fixture-open-link-participant-code',
				{ requiresParticipantCode: true }
			),
			scenario(
				'submit-retry',
				'Submit retry',
				'Submitted answers are preserved after a transient submit failure.',
				'/r/fixture-open-link-submit-retry',
				{ submitRetry: true }
			)
		]
	}
];

export function listFixtureScenarios() {
	return productFixtureCatalog.flatMap((group) =>
		group.scenarios.map((scenario) => ({ ...scenario, groupId: group.id, groupTitle: group.title }))
	);
}

export function getFixtureScenario(groupId: string, scenarioId: string) {
	return productFixtureCatalog
		.find((group) => group.id === groupId)
		?.scenarios.find((scenario) => scenario.id === scenarioId);
}

export function validateFixtureCatalogSafety(catalog: ProductFixtureGroup[]) {
	const problems: string[] = [];
	const serialized = JSON.stringify(catalog);
	const emailMatches = serialized.match(/[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}/gi) ?? [];
	const nonExampleEmails = emailMatches.filter((email) => !email.toLowerCase().endsWith('@example.test'));

	if (nonExampleEmails.length > 0) {
		problems.push(`Fixture catalog contains non-example.test emails: ${nonExampleEmails.join(', ')}`);
	}

	if (/\bOLBI\b/i.test(serialized)) {
		problems.push('Fixture catalog contains OLBI labels.');
	}

	if (/\b[A-Fa-f0-9]{64}\b/.test(serialized)) {
		problems.push('Fixture catalog contains hash-looking token values.');
	}

	if (/\bparticipant\s*code\s*[:=]\s*["']?[A-Z0-9-]{4,}/i.test(serialized)) {
		problems.push('Fixture catalog contains raw participant code examples.');
	}

	return problems;
}

function scenario(
	id: string,
	title: string,
	description: string,
	href: string,
	data: ProductFixtureScenario['data']
): ProductFixtureScenario {
	return {
		id,
		title,
		description,
		href,
		labels: demoLabels,
		provenance: 'Demo fixture - local design state only',
		data
	};
}
