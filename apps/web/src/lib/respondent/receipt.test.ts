import { describe, expect, it } from 'vitest';
import type {
	OpenLinkEntryResponse,
	ResponseSessionResponse,
	SaveAnswersResponse,
	SubmitResponseSessionResponse
} from '$lib/api/setup';
import { toRespondentReceiptView } from './receipt';

describe('respondent receipt view', () => {
	it('summarizes an ordinary anonymous submitted response without participant-code guidance', () => {
		const view = toRespondentReceiptView({
			entry: sampleEntry(),
			session: sampleSession({ locale: 'en' }),
			savedAnswers: sampleSavedAnswers(3),
			submitted: sampleSubmitted()
		});

		expect(view.title).toBe('Response submitted');
		expect(view.headline).toBe('Your response for Wave 1 was received.');
		expect(view.submittedAt).toBe('May 7, 2026, 2:05 PM');
		expect(view.metrics).toEqual([
			{ label: 'Study', value: 'Wave 1' },
			{ label: 'Response mode', value: 'anonymous' },
			{ label: 'Locale', value: 'en' },
			{ label: 'Consent version', value: '1.0.0' },
			{ label: 'Answers received', value: '3' }
		]);
		expect(view.guidance).toContain('You can close this page.');
		expect(view.guidance).toContain('This page does not show scores or interpretation.');
		expect(view.guidance).not.toContain('Keep your participant code. The platform cannot recover it later.');
	});

	it('adds participant-code retention guidance for anonymous longitudinal receipts', () => {
		const view = toRespondentReceiptView({
			entry: sampleEntry({
				responseIdentityMode: 'anonymous_longitudinal',
				requiresParticipantCode: true
			}),
			session: sampleSession({ locale: 'hr' }),
			savedAnswers: sampleSavedAnswers(1),
			submitted: sampleSubmitted()
		});

		expect(view.title).toBe('Odgovor je poslan');
		expect(view.headline).toBe('Vaš odgovor za Wave 1 je zaprimljen.');
		expect(view.submittedAt).toBe('07.05.2026. 14:05');
		expect(view.metrics).toContainEqual({
			label: 'Način odgovora',
			value: 'anonimno longitudinalno'
		});
		expect(view.metrics).toContainEqual({ label: 'Jezik', value: 'hr' });
		expect(view.guidance).toContain(
			'Sačuvajte svoj sudionički kod. Platforma ga kasnije ne može vratiti.'
		);
	});

	it('omits answer count when saved-answer state is unavailable', () => {
		const view = toRespondentReceiptView({
			entry: sampleEntry(),
			session: sampleSession({ locale: null }),
			savedAnswers: null,
			submitted: sampleSubmitted()
		});

		expect(view.metrics).not.toContainEqual({ label: 'Answers received', value: '0' });
		expect(view.metrics).toContainEqual({ label: 'Locale', value: 'en' });
	});

	it('includes target context for target-aware identified responses', () => {
		const view = toRespondentReceiptView({
			entry: sampleEntry({
				responseIdentityMode: 'identified',
				assignmentRole: 'manager',
				respondentSubject: {
					id: '018f9d3d-7415-7000-9000-000000000006',
					displayName: 'Miriam Graham',
					email: 'miriam@example.test',
					externalId: null
				},
				targetSubject: {
					id: '018f9d3d-7415-7000-9000-000000000007',
					displayName: 'Adele Vance',
					email: 'adele@example.test',
					externalId: null
				}
			}),
			session: sampleSession({ locale: 'en' }),
			savedAnswers: sampleSavedAnswers(1),
			submitted: sampleSubmitted()
		});

		expect(view.metrics).toContainEqual({ label: 'About', value: 'Adele Vance' });
		expect(view.metrics).toContainEqual({ label: 'Respondent', value: 'Miriam Graham' });
		expect(view.metrics).toContainEqual({ label: 'Relationship', value: 'Manager' });
	});

	it('does not expose raw external subject ids in target-aware receipts', () => {
		const view = toRespondentReceiptView({
			entry: sampleEntry({
				responseIdentityMode: 'identified',
				assignmentRole: 'manager',
				respondentSubject: {
					id: '018f9d3d-7415-7000-9000-000000000006',
					displayName: null,
					email: null,
					externalId: 'msgraph:tenant:respondent'
				},
				targetSubject: {
					id: '018f9d3d-7415-7000-9000-000000000007',
					displayName: null,
					email: null,
					externalId: 'msgraph:tenant:target'
				}
			}),
			session: sampleSession({ locale: 'en' }),
			savedAnswers: sampleSavedAnswers(1),
			submitted: sampleSubmitted()
		});

		expect(view.metrics).not.toContainEqual({ label: 'About', value: 'msgraph:tenant:target' });
		expect(view.metrics).not.toContainEqual({
			label: 'Respondent',
			value: 'msgraph:tenant:respondent'
		});
	});
});

function sampleEntry(
	overrides: Partial<OpenLinkEntryResponse> = {}
): OpenLinkEntryResponse {
	return {
		campaignId: '018f9d3d-7415-7000-9000-000000000001',
		assignmentId: '018f9d3d-7415-7000-9000-000000000002',
		templateVersionId: '018f9d3d-7415-7000-9000-000000000003',
		name: 'Wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous',
		requiresParticipantCode: false,
		defaultLocale: 'en',
		consentDocument: {
			id: '018f9d3d-7415-7000-9000-000000000004',
			locale: 'en',
			version: '1.0.0',
			title: 'Default participant disclosure',
			bodyMarkdown: 'Participant disclosure body.',
			requiredGrants: ['data_processing', 'research_participation'],
			optionalGrants: []
		},
		questions: [],
		...overrides
	};
}

function sampleSession({
	locale
}: {
	locale: string | null;
}): ResponseSessionResponse {
	return {
		id: '018f9d3d-7415-7000-9000-000000000005',
		assignmentId: '018f9d3d-7415-7000-9000-000000000002',
		locale: locale ?? '',
		startedAt: '2026-05-07T12:00:00Z',
		submittedAt: null,
		timeTakenMs: null,
		publicHandle: 'rsh_018f9d3d7415700009000000000000005_abcdefghijklmnopqrstuvwxyz'
	};
}

function sampleSavedAnswers(savedAnswerCount: number): SaveAnswersResponse {
	return {
		sessionId: '018f9d3d-7415-7000-9000-000000000005',
		savedAnswerCount
	};
}

function sampleSubmitted(): SubmitResponseSessionResponse {
	return {
		id: '018f9d3d-7415-7000-9000-000000000005',
		submittedAt: '2026-05-07T12:05:00Z'
	};
}
