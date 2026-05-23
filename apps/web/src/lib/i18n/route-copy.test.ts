import { describe, expect, it } from 'vitest';
import { routePageCopy } from './route-copy';

describe('localized route body copy', () => {
	it('provides Croatian public and auth page copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.publicEntry.heroTitle).toContain('istraživačke studije');
		expect(copy.signIn.title).toBe('Prijavite se u svoj radni prostor.');
		expect(copy.register.workspaceName).toBe('Naziv radnog prostora');
	});

	it('provides Croatian workspace route copy', () => {
		const copy = routePageCopy('hr-HR');

		expect(copy.workspaceHome.title).toBe('Početna');
		expect(copy.portfolio.title).toBe('Studije');
		expect(copy.instruments.title).toBe('Instrumenti');
		expect(copy.exports.title).toBe('Preuzimanje datoteka');
		expect(copy.directory.title).toBe('Ljudi i grupe');
		expect(copy.team.title).toBe('Tim');
		expect(copy.settings.title).toBe('Postavke radnog prostora');
	});
});
