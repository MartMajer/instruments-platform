import { afterEach, describe, expect, it } from 'vitest';
import { commandCopy, collectionGuidanceCopy, prerequisiteCopy } from './backend-copy';
import { setLocale } from './locale.svelte';
import type { WorkspaceCommandCenterItemResponse } from '$lib/api/product';

function item(
	overrides: Partial<WorkspaceCommandCenterItemResponse>
): WorkspaceCommandCenterItemResponse {
	return {
		id: 'workspace.review',
		title: 'English title',
		description: 'English description',
		state: 'ready',
		surface: 'campaign_series',
		route: '/app/campaign-series',
		actionLabel: 'Open campaign series',
		priority: 100,
		campaignSeriesId: null,
		campaignId: null,
		requiredPermission: null,
		params: null,
		...overrides
	};
}

afterEach(() => setLocale('en'));

describe('commandCopy', () => {
	it('returns backend text untouched in en', () => {
		const copy = commandCopy(
			item({ id: 'series.0f8fad5bd9cb469fa16540890a29acdf.operations' })
		);
		expect(copy).toEqual({ title: 'English title', description: 'English description' });
	});

	it('recomposes operations items in hr with paukal agreement', () => {
		setLocale('hr');
		const copy = commandCopy(
			item({
				id: 'series.0f8fad5bd9cb469fa16540890a29acdf.operations',
				params: { name: 'Pilot', count: '2' }
			})
		);
		expect(copy.title).toBe('Pratite prikupljanje: Pilot');
		expect(copy.description).toBe('2 aktivna kruga možete pratiti u Prikupljanju.');
	});

	it('uses singular and 5+ forms for team pending links', () => {
		setLocale('hr');
		const one = commandCopy(item({ id: 'team.pending_provider_links', params: { count: '1' } }));
		const five = commandCopy(item({ id: 'team.pending_provider_links', params: { count: '5' } }));
		expect(one.description).toBe('1 član tima još čeka poveznicu s prijavom.');
		expect(five.description).toBe('5 članova tima još čeka poveznicu s prijavom.');
	});

	it('distinguishes the create verb by permission', () => {
		setLocale('hr');
		const manage = commandCopy(
			item({ id: 'campaign_series.create', requiredPermission: 'setup.manage' })
		);
		const view = commandCopy(item({ id: 'campaign_series.create' }));
		expect(manage.title).toBe('Stvorite prvu studiju');
		expect(view.title).toBe('Još nema studija');
	});

	it('falls back to backend text for unknown ids in hr', () => {
		setLocale('hr');
		const copy = commandCopy(item({ id: 'brand.new_kind' }));
		expect(copy).toEqual({ title: 'English title', description: 'English description' });
	});
});

describe('collectionGuidanceCopy', () => {
	it('passes the backend sentence through in en', () => {
		expect(collectionGuidanceCopy('collecting', 'not_ready', 'Backend sentence.')).toBe(
			'Backend sentence.'
		);
	});

	it('composes hr from the status tokens', () => {
		setLocale('hr');
		expect(collectionGuidanceCopy('has_submissions', 'below_disclosure_minimum', 'x')).toBe(
			'Prikupite još odgovora prije nego što se skupne vrijednosti mogu prikazati.'
		);
		expect(collectionGuidanceCopy('closed_or_inactive', 'not_ready', 'x')).toContain(
			'zatvoreno ili neaktivno'
		);
	});
});

describe('prerequisiteCopy', () => {
	it('maps every backend code to product vocabulary with a chapter anchor', () => {
		const consent = prerequisiteCopy('consent_document.missing', 'Add a consent document.');
		expect(consent.anchor).toBe('#policies');
		expect(consent.text).toContain('chapter 04');
	});

	it('keeps the campaign heuristic for unknown campaign-ish codes', () => {
		const hint = prerequisiteCopy('campaign.weird_new', 'Campaign problem.');
		expect(hint.anchor).toBe('#waves');
	});

	it('falls back to the backend message for unknown codes', () => {
		const hint = prerequisiteCopy('totally.unknown', 'Some backend sentence.');
		expect(hint).toEqual({ text: 'Some backend sentence.', anchor: null });
	});
});
