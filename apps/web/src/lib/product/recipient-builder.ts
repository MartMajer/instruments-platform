export type RecipientBuilderMode =
	| 'all_active_people'
	| 'selected_people'
	| 'all_in_group'
	| 'manager_of_target'
	| 'reports_of_target'
	| 'external_emails';

export type RecipientBuilderValidationReason =
	| 'confirm_all_active_people'
	| 'select_people'
	| 'select_groups'
	| 'select_targets'
	| 'add_external_emails';

export type RecipientBuilderState = {
	mode: RecipientBuilderMode;
	confirmedAllActivePeople: boolean;
	selectedSubjectIds: string[];
	selectedGroupIds: string[];
	targetSubjectIds: string[];
	targetGroupIds: string[];
	externalEmails: string[];
};

export type RecipientBuilderValidation =
	| { ok: true }
	| { ok: false; reason: RecipientBuilderValidationReason };

type RecipientRuleDocument = {
	kind?: string;
	role?: string;
	subject_ids?: unknown;
	group_ids?: unknown;
	group_id?: unknown;
	target_subject_ids?: unknown;
	target_subject_id?: unknown;
	target_group_ids?: unknown;
	emails?: unknown;
};

export function createRecipientBuilderState(
	overrides: Partial<RecipientBuilderState> = {}
): RecipientBuilderState {
	return {
		mode: overrides.mode ?? 'all_in_group',
		confirmedAllActivePeople: overrides.confirmedAllActivePeople ?? false,
		selectedSubjectIds: normalizeIdList(overrides.selectedSubjectIds ?? []),
		selectedGroupIds: normalizeIdList(overrides.selectedGroupIds ?? []),
		targetSubjectIds: normalizeIdList(overrides.targetSubjectIds ?? []),
		targetGroupIds: normalizeIdList(overrides.targetGroupIds ?? []),
		externalEmails: normalizeStringList(overrides.externalEmails ?? [])
	};
}

export function recipientBuilderValidation(
	state: RecipientBuilderState
): RecipientBuilderValidation {
	if (state.mode === 'all_active_people') {
		return state.confirmedAllActivePeople
			? { ok: true }
			: { ok: false, reason: 'confirm_all_active_people' };
	}

	if (state.mode === 'selected_people') {
		return normalizeIdList(state.selectedSubjectIds).length > 0
			? { ok: true }
			: { ok: false, reason: 'select_people' };
	}

	if (state.mode === 'all_in_group') {
		return normalizeIdList(state.selectedGroupIds).length > 0
			? { ok: true }
			: { ok: false, reason: 'select_groups' };
	}

	if (state.mode === 'manager_of_target' || state.mode === 'reports_of_target') {
		return normalizeIdList(state.targetSubjectIds).length > 0 ||
			normalizeIdList(state.targetGroupIds).length > 0
			? { ok: true }
			: { ok: false, reason: 'select_targets' };
	}

	return normalizeStringList(state.externalEmails).length > 0
		? { ok: true }
		: { ok: false, reason: 'add_external_emails' };
}

export function recipientBuilderCanPreview(state: RecipientBuilderState): boolean {
	return recipientBuilderValidation(state).ok;
}

export function serializeRecipientBuilderRule(state: RecipientBuilderState): string {
	const validation = recipientBuilderValidation(state);
	if (!validation.ok) {
		throw new Error(validation.reason);
	}

	const rule: Record<string, unknown> = {
		kind: state.mode,
		role: defaultRecipientRuleRole(state.mode)
	};

	if (state.mode === 'selected_people') {
		rule.subject_ids = normalizeIdList(state.selectedSubjectIds);
	}

	if (state.mode === 'all_in_group') {
		rule.group_ids = normalizeIdList(state.selectedGroupIds);
	}

	if (state.mode === 'manager_of_target' || state.mode === 'reports_of_target') {
		const subjectIds = normalizeIdList(state.targetSubjectIds);
		const groupIds = normalizeIdList(state.targetGroupIds);
		if (subjectIds.length > 0) {
			rule.target_subject_ids = subjectIds;
		}

		if (groupIds.length > 0) {
			rule.target_group_ids = groupIds;
		}
	}

	if (state.mode === 'external_emails') {
		rule.emails = normalizeStringList(state.externalEmails);
	}

	return JSON.stringify(rule);
}

export function parseSavedRecipientRule(ruleJson: string): RecipientBuilderState {
	try {
		const parsed = JSON.parse(ruleJson) as RecipientRuleDocument;
		const mode = normalizeRecipientBuilderMode(parsed.kind);
		return createRecipientBuilderState({
			mode,
			confirmedAllActivePeople: mode === 'all_active_people',
			selectedSubjectIds: mode === 'selected_people' ? parseIdArray(parsed.subject_ids) : [],
			selectedGroupIds:
				mode === 'all_in_group'
					? mergeIdArrays(parseIdArray(parsed.group_id), parseIdArray(parsed.group_ids))
					: [],
			targetSubjectIds:
				mode === 'manager_of_target' || mode === 'reports_of_target'
					? mergeIdArrays(
							parseIdArray(parsed.target_subject_id),
							parseIdArray(parsed.target_subject_ids)
						)
					: [],
			targetGroupIds:
				mode === 'manager_of_target' || mode === 'reports_of_target'
					? parseIdArray(parsed.target_group_ids)
					: [],
			externalEmails: mode === 'external_emails' ? parseStringArray(parsed.emails) : []
		});
	} catch {
		return createRecipientBuilderState({ mode: 'selected_people' });
	}
}

export function defaultRecipientRuleRole(mode: RecipientBuilderMode): string {
	if (mode === 'all_in_group') {
		return 'group_member';
	}

	if (mode === 'manager_of_target') {
		return 'manager';
	}

	if (mode === 'reports_of_target') {
		return 'direct_report';
	}

	if (mode === 'external_emails') {
		return 'email_recipient';
	}

	return 'self';
}

export function normalizeRecipientBuilderMode(value: unknown): RecipientBuilderMode {
	if (
		value === 'all_active_people' ||
		value === 'selected_people' ||
		value === 'all_in_group' ||
		value === 'manager_of_target' ||
		value === 'reports_of_target' ||
		value === 'external_emails'
	) {
		return value;
	}

	if (value === 'self') {
		return 'all_active_people';
	}

	return 'all_in_group';
}

function parseIdArray(value: unknown): string[] {
	if (typeof value === 'string') {
		return normalizeIdList([value]);
	}

	if (!Array.isArray(value)) {
		return [];
	}

	return normalizeIdList(value.filter((item): item is string => typeof item === 'string'));
}

function parseStringArray(value: unknown): string[] {
	return Array.isArray(value)
		? normalizeStringList(value.filter((item): item is string => typeof item === 'string'))
		: [];
}

function mergeIdArrays(...arrays: string[][]): string[] {
	return normalizeIdList(arrays.flat());
}

function normalizeIdList(values: readonly string[]): string[] {
	return normalizeStringList(values);
}

function normalizeStringList(values: readonly string[]): string[] {
	const seen = new Set<string>();
	const normalized: string[] = [];
	for (const value of values) {
		const item = value.trim();
		if (!item || seen.has(item)) {
			continue;
		}

		seen.add(item);
		normalized.push(item);
	}

	return normalized;
}
