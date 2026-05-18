import { describe, expect, it } from 'vitest';
import {
	getFixtureScenario,
	listFixtureScenarios,
	productFixtureCatalog,
	validateFixtureCatalogSafety
} from './fixtures';

describe('product fixture catalog', () => {
	it('covers the UX01 product surface scenario families', () => {
		expect(productFixtureCatalog.map((group) => group.id)).toEqual([
			'campaign-series',
			'setup',
			'operations',
			'reports',
			'exports',
			'waves',
			'respondent'
		]);

		expect(scenarioIds('campaign-series')).toEqual([
			'empty-list',
			'created-selected-series',
			'one-wave-series',
			'two-wave-series'
		]);
		expect(scenarioIds('setup')).toEqual(['empty', 'launch-ready', 'blocked']);
		expect(scenarioIds('operations')).toEqual([
			'launched-anonymous',
			'launched-anonymous-longitudinal',
			'delivery-failed'
		]);
		expect(scenarioIds('reports')).toEqual([
			'visible-aggregate',
			'suppressed-by-k-min',
			'missing-scores'
		]);
		expect(scenarioIds('exports')).toEqual([
			'created',
			'fetched',
			'downloadable',
			'not-found'
		]);
		expect(scenarioIds('waves')).toEqual([
			'one-wave',
			'two-waves-linked',
			'insufficient-linked-n',
			'incompatible-scoring'
		]);
		expect(scenarioIds('respondent')).toEqual([
			'valid-entry',
			'consent-required',
			'participant-code-required',
			'submit-retry'
		]);
	});

	it('uses campaign-series route links for product scenarios', () => {
		const selected = getFixtureScenario('campaign-series', 'created-selected-series');
		const setupReady = getFixtureScenario('setup', 'launch-ready');
		const wavesLinked = getFixtureScenario('waves', 'two-waves-linked');

		expect(selected?.href).toBe('/app/campaign-series/fixture-series-created');
		expect(setupReady?.href).toBe('/app/campaign-series/fixture-series-ready/setup');
		expect(wavesLinked?.href).toBe('/app/campaign-series/fixture-series-two-wave/waves');
	});

	it('keeps fixture content safe for local/demo use', () => {
		expect(validateFixtureCatalogSafety(productFixtureCatalog)).toEqual([]);
		expect(
			listFixtureScenarios().every((scenario) => scenario.provenance.includes('Demo fixture'))
		).toBe(true);
		expect(
			listFixtureScenarios().every((scenario) => scenario.labels.includes('Demo data'))
		).toBe(true);
	});
});

function scenarioIds(groupId: string) {
	const group = productFixtureCatalog.find((candidate) => candidate.id === groupId);
	return group?.scenarios.map((scenario) => scenario.id) ?? [];
}
