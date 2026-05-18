export type ProofWorkflowSurfaceId = 'setup' | 'operations' | 'reports' | 'waves';

export type ProofWorkflowSection =
	| 'instrument'
	| 'template'
	| 'scoring'
	| 'campaign'
	| 'readiness'
	| 'launch'
	| 'open-link'
	| 'invitations'
	| 'delivery'
	| 'response-lab'
	| 'score-proof'
	| 'report-proof'
	| 'export-artifacts'
	| 'two-wave-proof'
	| 'wave-comparison';

export type ProofWorkflowSurface = {
	id: ProofWorkflowSurfaceId;
	label: string;
	sections: ProofWorkflowSection[];
	requiresPriorSetup: boolean;
};

export type ProofWorkflowSelectedSeries = {
	id: string;
	name: string;
};

export type ProofWorkflowSelectedCampaign = {
	id: string;
	name: string;
	status: string;
	responseIdentityMode: string;
	latestLaunchSnapshotId: string | null;
	latestLaunchAt: string | null;
};

export type ProofWorkflowSeriesResolution =
	| {
			id: string;
			name: string | null;
			source: 'selected' | 'local';
	  }
	| null;

export const proofWorkflowSurfaces: ProofWorkflowSurface[] = [
	{
		id: 'setup',
		label: 'Setup',
		sections: ['instrument', 'template', 'scoring', 'campaign', 'readiness'],
		requiresPriorSetup: false
	},
	{
		id: 'operations',
		label: 'Operations',
		sections: ['launch', 'open-link', 'invitations', 'delivery'],
		requiresPriorSetup: true
	},
	{
		id: 'reports',
		label: 'Reports',
		sections: ['response-lab', 'score-proof', 'report-proof', 'export-artifacts'],
		requiresPriorSetup: true
	},
	{
		id: 'waves',
		label: 'Waves',
		sections: ['two-wave-proof', 'wave-comparison'],
		requiresPriorSetup: true
	}
];

export function getProofWorkflowSurface(id: ProofWorkflowSurfaceId) {
	return proofWorkflowSurfaces.find((surface) => surface.id === id);
}

export function resolveProofWorkflowSeries(
	selectedSeries: ProofWorkflowSelectedSeries | null | undefined,
	localSeries: { id: string } | null | undefined
): ProofWorkflowSeriesResolution {
	if (selectedSeries) {
		return {
			id: selectedSeries.id,
			name: selectedSeries.name,
			source: 'selected'
		};
	}

	if (localSeries) {
		return {
			id: localSeries.id,
			name: null,
			source: 'local'
		};
	}

	return null;
}
