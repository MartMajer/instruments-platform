import { describe, expect, it } from 'vitest';
import {
	createRecipientBuilderState,
	parseSavedRecipientRule,
	recipientBuilderCanPreview,
	recipientBuilderValidation,
	serializeRecipientBuilderRule,
	type RecipientBuilderState
} from './recipient-builder';

const anaId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const boId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
const groupId = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const managersGroupId = 'dddddddd-dddd-4ddd-8ddd-dddddddddddd';

describe('recipient builder', () => {
	it('requires explicit confirmation before targeting every active person', () => {
		const state = createRecipientBuilderState({
			mode: 'all_active_people',
			confirmedAllActivePeople: false
		});

		expect(recipientBuilderCanPreview(state)).toBe(false);
		expect(recipientBuilderValidation(state)).toEqual({
			ok: false,
			reason: 'confirm_all_active_people'
		});

		expect(serializeRecipientBuilderRule({ ...state, confirmedAllActivePeople: true })).toBe(
			JSON.stringify({ kind: 'all_active_people', role: 'self' })
		);
	});

	it('serializes selected people with stable deduped subject ids', () => {
		const state = createRecipientBuilderState({
			mode: 'selected_people',
			selectedSubjectIds: [boId, anaId, boId, '']
		});

		expect(recipientBuilderCanPreview(state)).toBe(true);
		expect(JSON.parse(serializeRecipientBuilderRule(state))).toEqual({
			kind: 'selected_people',
			role: 'self',
			subject_ids: [boId, anaId]
		});
	});

	it('serializes selected groups with group_ids only', () => {
		const state = createRecipientBuilderState({
			mode: 'all_in_group',
			selectedGroupIds: [groupId, groupId]
		});

		expect(JSON.parse(serializeRecipientBuilderRule(state))).toEqual({
			kind: 'all_in_group',
			role: 'group_member',
			group_ids: [groupId]
		});
	});

	it('serializes relationship rules from selected people and groups', () => {
		const managerState = createRecipientBuilderState({
			mode: 'manager_of_target',
			targetSubjectIds: [anaId],
			targetGroupIds: [groupId]
		});
		const reportsState = createRecipientBuilderState({
			mode: 'reports_of_target',
			targetSubjectIds: [boId],
			targetGroupIds: [managersGroupId]
		});

		expect(JSON.parse(serializeRecipientBuilderRule(managerState))).toEqual({
			kind: 'manager_of_target',
			role: 'manager',
			target_subject_ids: [anaId],
			target_group_ids: [groupId]
		});
		expect(JSON.parse(serializeRecipientBuilderRule(reportsState))).toEqual({
			kind: 'reports_of_target',
			role: 'direct_report',
			target_subject_ids: [boId],
			target_group_ids: [managersGroupId]
		});
	});

	it('keeps one-time email recipients as the only email-based rule', () => {
		const state = createRecipientBuilderState({
			mode: 'external_emails',
			externalEmails: ['ada@example.test', 'bo@example.test']
		});

		expect(JSON.parse(serializeRecipientBuilderRule(state))).toEqual({
			kind: 'external_emails',
			role: 'email_recipient',
			emails: ['ada@example.test', 'bo@example.test']
		});
	});

	it('parses saved rule JSON back into builder state for clear summaries', () => {
		const state = parseSavedRecipientRule(
			JSON.stringify({
				kind: 'manager_of_target',
				role: 'manager',
				target_subject_ids: [anaId],
				target_group_ids: [groupId]
			})
		);

		expect(state).toMatchObject<Partial<RecipientBuilderState>>({
			mode: 'manager_of_target',
			targetSubjectIds: [anaId],
			targetGroupIds: [groupId]
		});
	});

	it('treats legacy self rules as a broad saved-audience selector', () => {
		expect(parseSavedRecipientRule(JSON.stringify({ kind: 'self', role: 'self' }))).toMatchObject<
			Partial<RecipientBuilderState>
		>({
			mode: 'all_active_people',
			confirmedAllActivePeople: true
		});
	});
});
