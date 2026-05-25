import { describe, expect, it } from 'vitest';
import { toProductRouteGuidance, type ProductRouteGuidanceId } from './route-guidance';

const routeIds: ProductRouteGuidanceId[] = [
	'home',
	'studies',
	'selected-study',
	'setup',
	'operations',
	'reports',
	'waves',
	'exports',
	'instruments',
	'directory',
	'team',
	'settings'
];

describe('product route guidance', () => {
	it('returns complete route guidance for every self-serve route', () => {
		for (const routeId of routeIds) {
			const guidance = toProductRouteGuidance(routeId);

			expect(guidance.title.trim(), routeId).not.toBe('');
			expect(guidance.summary.trim(), routeId).not.toBe('');
			expect(guidance.inspectFirst.trim(), routeId).not.toBe('');
			expect(guidance.whenItMatters.trim(), routeId).not.toBe('');
			expect(guidance.nextMove.trim(), routeId).not.toBe('');
		}
	});

	it('adds sample read-only limits and duplicate-to-edit guidance for sample study routes', () => {
		const guidance = toProductRouteGuidance('setup', {
			isSample: true,
			canManageSetup: true
		});

		expect(guidance.limits).toContain('Sample studies are read-only examples');
		expect(guidance.nextMove).toContain('Duplicate');
	});

	it('keeps own-study guidance editable when the user can manage setup', () => {
		const guidance = toProductRouteGuidance('setup', {
			isSample: false,
			canManageSetup: true
		});

		expect(guidance.limits).toBeUndefined();
		expect(guidance.nextMove).toContain('Move to collection');
	});

	it('adds setup-management role limits for read-only setup users', () => {
		const guidance = toProductRouteGuidance('studies', {
			isSample: false,
			canManageSetup: false
		});

		expect(guidance.limits).toContain('setup management access');
		expect(guidance.nextMove).toContain('inspect');
	});

	it('marks waves as advanced longitudinal context', () => {
		const guidance = toProductRouteGuidance('waves');

		expect(guidance.title).toContain('Advanced');
		expect(guidance.summary).toContain('longitudinal');
		expect(guidance.whenItMatters).toContain('repeated');
		expect(guidance.nextMove).toContain('single-wave');
	});

	it('adapts guidance for empty study and export states', () => {
		const studiesGuidance = toProductRouteGuidance('studies', { isEmpty: true });
		const exportsGuidance = toProductRouteGuidance('exports', { isEmpty: true });

		expect(studiesGuidance.inspectFirst).toContain('No studies are visible');
		expect(studiesGuidance.nextMove).toContain('Create your study');
		expect(exportsGuidance.inspectFirst).toContain('No export files exist yet');
		expect(exportsGuidance.nextMove).toContain('results page');
	});

	it('adds team-management role limits for read-only team users', () => {
		const guidance = toProductRouteGuidance('team', {
			canManageTeam: false
		});

		expect(guidance.limits).toContain('team management access');
		expect(guidance.nextMove).toContain('inspect');
	});

	it('localizes route guidance chrome and body copy for Croatian app mode', () => {
		const guidance = toProductRouteGuidance(
			'setup',
			{
				isSample: true,
				canManageSetup: false
			},
			'hr-HR'
		);

		expect(guidance.ariaLabel).toBe('Smjernice rute');
		expect(guidance.kicker).toBe('Smjernice rute');
		expect(guidance.inspectFirstLabel).toBe('Prvo pregledajte');
		expect(guidance.whenItMattersLabel).toBe('Kada je važno');
		expect(guidance.nextMoveLabel).toBe('Sljedeći korak');
		expect(guidance.limitsLabel).toBe('Ograničenja');
		expect(guidance.title).toBe('Pripremite prije prikupljanja');
		expect(guidance.limits).toContain('Ogledne studije su primjeri samo za čitanje');
		expect(guidance.nextMove).toContain('Duplicirajte ovaj ogledni primjer');
		expect(`${guidance.title} ${guidance.summary} ${guidance.nextMove}`).not.toMatch(
			/Prepare study|Route guidance|Duplicate this sample/
		);
	});
});
