import { describe, expect, it } from 'vitest';
import { toSessionProfileView } from './session-profile';
import type { AuthSessionResponse } from '$lib/api/setup';

describe('toSessionProfileView', () => {
	it('uses account email and summarizes management permissions', () => {
		const session: AuthSessionResponse = {
			userId: '22222222-2222-4222-8222-222222222222',
			tenantId: '11111111-1111-4111-8111-111111111111',
			email: 'owner@example.test',
			permissions: ['setup.manage', 'team.manage', 'report.view_series']
		};

		const view = toSessionProfileView(session);

		expect(view.accountLabel).toBe('owner@example.test');
		expect(view.permissionSummary).toBe('Workspace administration and reporting access');
		expect(view.permissionBadges).toEqual(['Setup', 'Team', 'Reports']);
		expect(view.technicalRows).toEqual([]);
	});

	it('falls back to a compact signed-in label without email', () => {
		const session: AuthSessionResponse = {
			userId: '22222222-2222-4222-8222-222222222222',
			tenantId: '11111111-1111-4111-8111-111111111111',
			permissions: []
		};

		const view = toSessionProfileView(session);

		expect(view.accountLabel).toBe('Signed-in platform user');
		expect(view.permissionSummary).toBe('Tenant member access');
		expect(view.permissionBadges).toEqual(['Tenant member']);
	});

	it('summarizes report-only sessions without duplicated access wording', () => {
		const session: AuthSessionResponse = {
			userId: '22222222-2222-4222-8222-222222222222',
			tenantId: '11111111-1111-4111-8111-111111111111',
			email: 'analyst@example.test',
			permissions: ['report.view_series', 'export.read']
		};

		const view = toSessionProfileView(session);

		expect(view.permissionSummary).toBe('Reporting access');
		expect(view.permissionBadges).toEqual(['Reports']);
	});

	it('localizes session permission summaries and badges', () => {
		const session: AuthSessionResponse = {
			userId: '22222222-2222-4222-8222-222222222222',
			tenantId: '11111111-1111-4111-8111-111111111111',
			email: 'owner@example.test',
			permissions: ['setup.manage', 'team.manage', 'report.view_series']
		};

		const view = toSessionProfileView(session, 'hr-HR');

		expect(view.permissionSummary).toBe('Administracija radnog prostora i pristup izvještajima');
		expect(view.permissionBadges).toEqual(['Postavljanje', 'Tim', 'Izvještaji']);
	});
});
