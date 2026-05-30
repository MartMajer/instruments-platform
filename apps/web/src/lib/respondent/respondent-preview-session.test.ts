import { describe, expect, test } from 'vitest';
import type { RespondentQuestionResponse } from '$lib/api/setup';
import {
	createRespondentPreviewSession,
	readRespondentPreviewSession,
	respondentPreviewStorageKey,
	writeRespondentPreviewSession
} from './respondent-preview-session';

class MemoryStorage implements Pick<Storage, 'getItem' | 'setItem' | 'removeItem'> {
	private readonly values = new Map<string, string>();

	getItem(key: string): string | null {
		return this.values.get(key) ?? null;
	}

	setItem(key: string, value: string): void {
		this.values.set(key, value);
	}

	removeItem(key: string): void {
		this.values.delete(key);
	}
}

const question: RespondentQuestionResponse = {
	id: 'draft-q01',
	ordinal: 1,
	code: 'q01',
	type: 'likert',
	textDefault: 'Question one',
	required: true,
	scaleCode: 'scale_q01',
	scaleMinValue: 1,
	scaleMaxValue: 5,
	scaleAnchors: JSON.stringify([
		{ value: 1, label: 'Strongly disagree' },
		{ value: 5, label: 'Strongly agree' }
	]),
	payload: '{}'
};

describe('respondent preview session storage', () => {
	test('writes and reads a short-lived preview packet for the selected series', () => {
		const storage = new MemoryStorage();
		const preview = createRespondentPreviewSession({
			previewId: 'preview-1',
			seriesId: 'series-1',
			seriesName: 'Quarterly pulse',
			questionnaireName: 'Draft questionnaire',
			locale: 'en',
			createdAt: 1_000,
			questions: [question]
		});

		writeRespondentPreviewSession(storage, preview);

		expect(storage.getItem(respondentPreviewStorageKey('preview-1'))).not.toBeNull();
		expect(readRespondentPreviewSession(storage, 'preview-1', 'series-1', 1_100)).toEqual({
			status: 'ready',
			preview
		});
	});

	test('rejects wrong-series preview packets', () => {
		const storage = new MemoryStorage();
		writeRespondentPreviewSession(
			storage,
			createRespondentPreviewSession({
				previewId: 'preview-1',
				seriesId: 'series-1',
				seriesName: 'Quarterly pulse',
				questionnaireName: 'Draft questionnaire',
				locale: 'en',
				createdAt: 1_000,
				questions: [question]
			})
		);

		expect(readRespondentPreviewSession(storage, 'preview-1', 'series-2', 1_100)).toEqual({
			status: 'wrong-series'
		});
	});

	test('rejects stale preview packets', () => {
		const storage = new MemoryStorage();
		writeRespondentPreviewSession(
			storage,
			createRespondentPreviewSession({
				previewId: 'preview-1',
				seriesId: 'series-1',
				seriesName: 'Quarterly pulse',
				questionnaireName: 'Draft questionnaire',
				locale: 'en',
				createdAt: 1_000,
				questions: [question]
			})
		);

		expect(
			readRespondentPreviewSession(storage, 'preview-1', 'series-1', 1_000 + 6 * 60 * 60 * 1000 + 1)
		).toEqual({ status: 'expired' });
		expect(storage.getItem(respondentPreviewStorageKey('preview-1'))).toBeNull();
	});
});
