import { describe, expect, it } from 'vitest';
import {
	getProofWorkflowSurface,
	proofWorkflowSurfaces,
	resolveProofWorkflowSeries
} from './proof-workflow';

describe('proof workflow surface metadata', () => {
	it('keeps proof workflow sections mapped to UX01 product surfaces', () => {
		expect(proofWorkflowSurfaces.map((surface) => surface.id)).toEqual([
			'setup',
			'operations',
			'reports',
			'waves'
		]);

		expect(getProofWorkflowSurface('setup')?.sections).toEqual([
			'instrument',
			'template',
			'scoring',
			'campaign',
			'readiness'
		]);
		expect(getProofWorkflowSurface('operations')?.sections).toEqual([
			'launch',
			'open-link',
			'invitations',
			'delivery'
		]);
		expect(getProofWorkflowSurface('reports')?.sections).toEqual([
			'response-lab',
			'score-proof',
			'report-proof',
			'export-artifacts'
		]);
		expect(getProofWorkflowSurface('waves')?.sections).toEqual([
			'two-wave-proof',
			'wave-comparison'
		]);
	});

	it('records the intentional UX01 limits for route workflow state', () => {
		expect(getProofWorkflowSurface('operations')?.requiresPriorSetup).toBe(true);
		expect(getProofWorkflowSurface('reports')?.requiresPriorSetup).toBe(true);
		expect(getProofWorkflowSurface('waves')?.requiresPriorSetup).toBe(true);
		expect(getProofWorkflowSurface('setup')?.requiresPriorSetup).toBe(false);
	});

	it('prefers the selected route series over a locally created proof series', () => {
		expect(
			resolveProofWorkflowSeries(
				{ id: 'route-series-id', name: 'Route pulse' },
				{ id: 'local-series-id' }
			)
		).toEqual({
			id: 'route-series-id',
			name: 'Route pulse',
			source: 'selected'
		});
	});

	it('falls back to a locally created proof series when no route series exists', () => {
		expect(resolveProofWorkflowSeries(null, { id: 'local-series-id' })).toEqual({
			id: 'local-series-id',
			name: null,
			source: 'local'
		});
		expect(resolveProofWorkflowSeries(null, null)).toBeNull();
	});
});
