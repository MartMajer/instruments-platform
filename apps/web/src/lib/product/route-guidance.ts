export type ProductRouteGuidanceId =
	| 'home'
	| 'studies'
	| 'selected-study'
	| 'setup'
	| 'operations'
	| 'reports'
	| 'waves'
	| 'exports'
	| 'instruments'
	| 'directory'
	| 'team'
	| 'settings';

export type ProductRouteGuidanceContext = {
	isSample?: boolean;
	canManageSetup?: boolean;
	canManageTeam?: boolean;
	isEmpty?: boolean;
};

export type ProductRouteGuidanceView = {
	title: string;
	summary: string;
	inspectFirst: string;
	whenItMatters: string;
	nextMove: string;
	limits?: string;
};

const baseGuidance: Record<ProductRouteGuidanceId, ProductRouteGuidanceView> = {
	home: {
		title: 'Start with study context',
		summary: 'Use this page to choose between read-only sample studies and your own study work.',
		inspectFirst:
			'Inspect the sample studies first, then compare them with your own active studies.',
		whenItMatters:
			'Use this as the first stop after sign-in or when you lose track of the study lifecycle.',
		nextMove: 'Open Studies when you need the full portfolio.'
	},
	studies: {
		title: 'Choose the right study',
		summary: 'Compare sample and own studies by lifecycle state before opening deeper work routes.',
		inspectFirst: 'Inspect Sample studies for examples and Your studies for editable work.',
		whenItMatters: 'Use this when deciding what to prepare, collect, review, archive, or restore.',
		nextMove: 'Open a study, or create your study when you have setup management access.'
	},
	'selected-study': {
		title: 'Orient inside one study',
		summary: 'Use the selected-study overview to understand where this study is in the lifecycle.',
		inspectFirst: 'Inspect the lifecycle map before opening Prepare, Collect, Results, or Waves.',
		whenItMatters: 'Use this after choosing a study or when you need to switch lifecycle phases.',
		nextMove: 'Open the lifecycle phase with the next blocked or ready action.'
	},
	setup: {
		title: 'Prepare before collection',
		summary: 'Review the setup checklist before the study moves into collection.',
		inspectFirst:
			'Inspect blocked checklist items for instrument, scoring, policy, campaign, and launch readiness.',
		whenItMatters: 'Use this before launching collection or when a study is not configured.',
		nextMove: 'Move to collection after launch readiness is clear.'
	},
	operations: {
		title: 'Track collection',
		summary:
			'Follow collection state, respondent access, response progress, and scoring readiness.',
		inspectFirst: 'Inspect respondent access and response progress before operational actions.',
		whenItMatters:
			'Use this while a study is launching, live, partially complete, or ready to close.',
		nextMove: 'Open Review results when submitted responses and scoring are ready.'
	},
	reports: {
		title: 'Read results carefully',
		summary:
			'Start with availability, coverage, limitations, and export next use before reference metadata.',
		inspectFirst:
			'Inspect result availability and coverage before acting on charts or export files.',
		whenItMatters:
			'Use this after collection has submissions or when results look suppressed or preliminary.',
		nextMove: 'Use exports after the result story and limitations are understood.'
	},
	waves: {
		title: 'Advanced longitudinal context',
		summary: 'Use Waves for longitudinal comparison across repeated study waves.',
		inspectFirst:
			'Inspect selected waves, compatibility, linked trajectories, and disclosure limits.',
		whenItMatters: 'Use this only for repeated-wave studies where change over time matters.',
		nextMove: 'For a single-wave study, return to Review results or Exports.'
	},
	exports: {
		title: 'Use generated files',
		summary: 'Find study-support files by readiness, purpose, source context, and next use.',
		inspectFirst: 'Inspect ready downloads and attention items before opening file details.',
		whenItMatters:
			'Use this when analysis handoff, codebook review, or result packets need generated files.',
		nextMove:
			'Open the source study results page when you need to understand how an export file was generated.'
	},
	instruments: {
		title: 'Check launchable instruments',
		summary: 'Review tenant-visible instruments and whether they can be used in study setup.',
		inspectFirst: 'Inspect launch eligibility, rights labels, and validity labels.',
		whenItMatters: 'Use this before creating or preparing a study that needs an instrument.',
		nextMove: 'Open Studies and prepare a study with the right instrument context.'
	},
	directory: {
		title: 'Prepare people and targeting',
		summary:
			'Use Directory when studies need subjects, groups, memberships, or manager relationships.',
		inspectFirst:
			'Inspect subjects, groups, and manager links before launch targeting or hierarchy reporting.',
		whenItMatters:
			'Use this when a study needs identified audiences, groups, or hierarchy context.',
		nextMove: 'Return to Prepare study when directory prerequisites are ready.'
	},
	team: {
		title: 'Review access and roles',
		summary:
			'Use Team to understand who can enter the tenant and what app-owned permissions they have.',
		inspectFirst:
			'Inspect active members, role assignments, identity status, and effective permissions.',
		whenItMatters: 'Use this before adding collaborators or debugging read-only access.',
		nextMove: 'Prepare or change member access when you have team management access.'
	},
	settings: {
		title: 'Inspect workspace profile',
		summary: 'Use Settings for tenant profile, workspace scale, and management destinations.',
		inspectFirst: 'Inspect the tenant profile and workspace counts before using management links.',
		whenItMatters: 'Use this for workspace administration context, not as the main study workflow.',
		nextMove: 'Return to Studies for normal study work.'
	}
};

const sampleAwareRoutes = new Set<ProductRouteGuidanceId>([
	'selected-study',
	'setup',
	'operations',
	'reports',
	'waves'
]);

const setupManagedRoutes = new Set<ProductRouteGuidanceId>([
	'studies',
	'selected-study',
	'setup',
	'operations',
	'reports',
	'waves',
	'directory'
]);

export function toProductRouteGuidance(
	id: ProductRouteGuidanceId,
	context: ProductRouteGuidanceContext = {}
): ProductRouteGuidanceView {
	const guidance = { ...baseGuidance[id] };
	const limits: string[] = [];

	if (context.isEmpty) {
		applyEmptyState(guidance, id);
	}

	if (context.isSample && sampleAwareRoutes.has(id)) {
		limits.push(
			'Sample studies are read-only examples. Duplicate the sample as your own study before changing setup, collection, or reporting state.'
		);
		guidance.nextMove = `Duplicate this sample as your own study before editing. ${guidance.nextMove}`;
	}

	if (context.canManageSetup === false && setupManagedRoutes.has(id)) {
		limits.push(
			id === 'directory'
				? 'Directory setup requires setup management access.'
				: 'Changing studies requires setup management access.'
		);
		guidance.nextMove = toReadOnlySetupNextMove(guidance.nextMove);
	}

	if (context.canManageTeam === false && id === 'team') {
		limits.push('Member preparation and role changes require team management access.');
		guidance.nextMove =
			'Use this route to inspect current access; ask a team manager to change roles.';
	}

	if (limits.length > 0) {
		guidance.limits = limits.join(' ');
	}

	return guidance;
}

function applyEmptyState(guidance: ProductRouteGuidanceView, id: ProductRouteGuidanceId) {
	if (id === 'studies') {
		guidance.inspectFirst = 'No studies are visible in the current portfolio view.';
		guidance.nextMove =
			'Create your study when you have setup access, or adjust filters if studies should exist.';
		return;
	}

	if (id === 'exports') {
		guidance.inspectFirst = 'No export files exist yet.';
		guidance.nextMove = 'Create an export from a study results page after results are available.';
		return;
	}

	if (id === 'home') {
		guidance.inspectFirst = 'No sample or own studies are visible yet.';
		guidance.nextMove = 'Open Studies to create or inspect tenant study work.';
	}
}

function toReadOnlySetupNextMove(currentNextMove: string) {
	return `Use this route to inspect current state; ask a setup manager before changing it. ${currentNextMove}`;
}
