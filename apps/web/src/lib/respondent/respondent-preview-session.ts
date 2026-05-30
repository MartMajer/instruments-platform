import type { RespondentQuestionResponse } from '$lib/api/setup';

export const respondentPreviewSessionVersion = 'validatedscale.respondent-preview.v1';
export const respondentPreviewMaxAgeMs = 6 * 60 * 60 * 1000;

export type RespondentPreviewSession = {
	schemaVersion: typeof respondentPreviewSessionVersion;
	previewId: string;
	seriesId: string;
	seriesName: string;
	questionnaireName: string;
	locale: string;
	createdAt: number;
	questions: RespondentQuestionResponse[];
};

export type CreateRespondentPreviewSessionInput = Omit<RespondentPreviewSession, 'schemaVersion'>;

export type RespondentPreviewReadResult =
	| { status: 'ready'; preview: RespondentPreviewSession }
	| { status: 'missing' | 'invalid' | 'wrong-series' | 'expired' };

type PreviewStorage = Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;

export function respondentPreviewStorageKey(previewId: string): string {
	return `validatedscale.respondent-preview.${previewId}`;
}

export function createRespondentPreviewId(): string {
	if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
		return crypto.randomUUID();
	}

	return `preview-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

export function createRespondentPreviewSession(
	input: CreateRespondentPreviewSessionInput
): RespondentPreviewSession {
	return {
		schemaVersion: respondentPreviewSessionVersion,
		...input
	};
}

export function writeRespondentPreviewSession(
	storage: PreviewStorage,
	preview: RespondentPreviewSession
): void {
	storage.setItem(respondentPreviewStorageKey(preview.previewId), JSON.stringify(preview));
}

export function readRespondentPreviewSession(
	storage: PreviewStorage,
	previewId: string | null | undefined,
	seriesId: string,
	now = Date.now()
): RespondentPreviewReadResult {
	const normalizedPreviewId = previewId?.trim() ?? '';
	if (!normalizedPreviewId) {
		return { status: 'missing' };
	}

	const key = respondentPreviewStorageKey(normalizedPreviewId);
	const raw = storage.getItem(key);
	if (!raw) {
		return { status: 'missing' };
	}

	const preview = parseRespondentPreviewSession(raw);
	if (!preview) {
		storage.removeItem(key);
		return { status: 'invalid' };
	}

	if (preview.seriesId !== seriesId) {
		return { status: 'wrong-series' };
	}

	if (now - preview.createdAt > respondentPreviewMaxAgeMs || now < preview.createdAt - 60_000) {
		storage.removeItem(key);
		return { status: 'expired' };
	}

	return { status: 'ready', preview };
}

function parseRespondentPreviewSession(raw: string): RespondentPreviewSession | null {
	try {
		const parsed = JSON.parse(raw);
		if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
			return null;
		}

		const candidate = parsed as Record<string, unknown>;
		if (
			candidate.schemaVersion !== respondentPreviewSessionVersion ||
			typeof candidate.previewId !== 'string' ||
			typeof candidate.seriesId !== 'string' ||
			typeof candidate.seriesName !== 'string' ||
			typeof candidate.questionnaireName !== 'string' ||
			typeof candidate.locale !== 'string' ||
			typeof candidate.createdAt !== 'number' ||
			!Number.isFinite(candidate.createdAt) ||
			!Array.isArray(candidate.questions) ||
			!candidate.questions.every(isRespondentQuestion)
		) {
			return null;
		}

		return candidate as RespondentPreviewSession;
	} catch {
		return null;
	}
}

function isRespondentQuestion(value: unknown): value is RespondentQuestionResponse {
	if (!value || typeof value !== 'object' || Array.isArray(value)) {
		return false;
	}

	const question = value as Record<string, unknown>;
	return (
		typeof question.id === 'string' &&
		typeof question.ordinal === 'number' &&
		Number.isFinite(question.ordinal) &&
		typeof question.code === 'string' &&
		typeof question.type === 'string' &&
		typeof question.textDefault === 'string' &&
		typeof question.required === 'boolean'
	);
}
