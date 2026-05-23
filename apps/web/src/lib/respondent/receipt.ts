import type {
	OpenLinkEntryResponse,
	ResponseSessionResponse,
	SaveAnswersResponse,
	SubmitResponseSessionResponse
} from '$lib/api/setup';
import { formatAppDateTime, normalizeAppLocale, type AppLocale } from '$lib/i18n/localization';
import { respondentReceiptCopy } from '$lib/i18n/ui-copy';

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
	const locale = normalizeAppLocale(session.locale || entry.defaultLocale);
	const copy = respondentReceiptCopy(locale);
	const metrics: RespondentReceiptMetric[] = [
		{ label: copy.metrics.study, value: entry.name },
		{ label: copy.metrics.responseMode, value: humanizeResponseMode(entry.responseIdentityMode, locale) },
		{ label: copy.metrics.locale, value: session.locale || entry.defaultLocale },
		{ label: copy.metrics.consentVersion, value: entry.consentDocument.version }
	];

	if (savedAnswers) {
		metrics.push({ label: copy.metrics.answersReceived, value: String(savedAnswers.savedAnswerCount) });
	}

	const guidance = [copy.guidance.close, copy.guidance.noScores, copy.guidance.contact];

	if (entry.requiresParticipantCode) {
		guidance.push(copy.guidance.participantCode);
	}

	return {
		title: copy.title,
		headline: copy.headline(entry.name),
		submittedAt: formatAppDateTime(submitted.submittedAt, locale, { fallback: copy.notAvailable }),
		metrics,
		guidance
	};
}

function humanizeResponseMode(value: string, locale: AppLocale) {
	if (locale === 'hr-HR') {
		if (value === 'anonymous') {
			return 'anonimno';
		}

		if (value === 'anonymous_longitudinal') {
			return 'anonimno longitudinalno';
		}
	}

	return value.replaceAll('_', ' ');
}
