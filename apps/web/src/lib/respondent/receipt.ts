import type {
	OpenLinkEntryResponse,
	ResponseSessionResponse,
	SaveAnswersResponse,
	SubmitResponseSessionResponse
} from '$lib/api/setup';

export type RespondentReceiptMetric = {
	label: string;
	value: string;
};

export type RespondentReceiptView = {
	title: string;
	headline: string;
	submittedAt: string;
	metrics: RespondentReceiptMetric[];
	guidance: string[];
};

export function toRespondentReceiptView({
	entry,
	session,
	savedAnswers,
	submitted
}: {
	entry: OpenLinkEntryResponse;
	session: ResponseSessionResponse;
	savedAnswers: SaveAnswersResponse | null;
	submitted: SubmitResponseSessionResponse;
}): RespondentReceiptView {
	const metrics: RespondentReceiptMetric[] = [
		{ label: 'Study', value: entry.name },
		{ label: 'Response mode', value: humanizeValue(entry.responseIdentityMode) },
		{ label: 'Locale', value: session.locale || entry.defaultLocale },
		{ label: 'Consent version', value: entry.consentDocument.version }
	];

	if (savedAnswers) {
		metrics.push({ label: 'Answers received', value: String(savedAnswers.savedAnswerCount) });
	}

	const guidance = [
		'You can close this page.',
		'This page does not show scores or interpretation.',
		'For questions about withdrawal or data use, use the study contact named in the consent information.'
	];

	if (entry.requiresParticipantCode) {
		guidance.push('Keep your participant code. The platform cannot recover it later.');
	}

	return {
		title: 'Response submitted',
		headline: `Your response for ${entry.name} was received.`,
		submittedAt: submitted.submittedAt,
		metrics,
		guidance
	};
}

function humanizeValue(value: string) {
	return value.replaceAll('_', ' ');
}
