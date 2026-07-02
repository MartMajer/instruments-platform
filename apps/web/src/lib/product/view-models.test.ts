import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import type { ComponentProps } from 'svelte';
import type StatusBadge from '../components/StatusBadge.svelte';
import { ApiError } from '../api/client';
import type {
	CampaignSeriesHubResponse,
	CampaignSeriesOperationsWorkspaceResponse,
	CampaignSeriesReportsWorkspaceResponse,
	CampaignSeriesSetupWorkspaceResponse,
	CampaignSeriesWavesWorkspaceResponse,
	CampaignSeriesListResponse,
	ExportArtifactLibraryResponse,
	TenantSettingsWorkspaceResponse,
	WorkspaceOverviewResponse
} from '../api/product';
import type {
	AuthSessionResponse,
	CampaignReportProofResponse,
	CampaignSeriesWaveComparisonProofResponse,
	ExportArtifactDownloadResponse,
	InstrumentSummaryResponse,
	LaunchReadinessResponse,
	ReportProofExportArtifactResponse
} from '../api/setup';
import {
	toCampaignSeriesListView,
	toCampaignSeriesHubView,
	toCampaignSeriesSetupWorkspaceView,
	toCampaignSeriesOperationsWorkspaceView,
	toCampaignSeriesReportsWorkspaceView,
	toExportArtifactView,
	toExportArtifactLibraryView,
	toInstrumentLibraryView,
	toLaunchReadinessView,
	toProductApiErrorMessage,
	toReportProofView,
	toSelectedSeriesSurfaceView,
	toSessionView,
	toCampaignSeriesWavesWorkspaceView,
	toTenantSettingsView,
	toWorkspaceOverviewView,
	toWaveComparisonView
} from './view-models';

type StatusBadgeStatus = ComponentProps<typeof StatusBadge>['status'];

const readModelBadgeStatuses = [
	'archived',
	'proof_only',
	'draft',
	'scheduled',
	'live',
	'closed',
	'cancelled'
] as const satisfies readonly StatusBadgeStatus[];

function expectStatusBadgeStatus(_status: StatusBadgeStatus) {
	void _status;
	return undefined;
}

const mojibakePattern =
	/[\u00c2\u00c3]|\u00c4[\u0080-\u00bf]|\u00c5[\u0080-\u00bf]|\u00e2[\u0080-\u2122]/u;

function collectStrings(value: unknown, path = '$', output: string[] = []) {
	if (typeof value === 'string') {
		output.push(`${path}: ${value}`);
		return output;
	}

	if (Array.isArray(value)) {
		value.forEach((item, index) => collectStrings(item, `${path}[${index}]`, output));
		return output;
	}

	if (value && typeof value === 'object') {
		for (const [key, entryValue] of Object.entries(value)) {
			collectStrings(entryValue, `${path}.${key}`, output);
		}
	}

	return output;
}

describe('product view models', () => {
	it('does not rely on recursive exact-string localization fallback for generated read models', () => {
		const source = readFileSync(new URL('./view-models.ts', import.meta.url), 'utf8');

		expect(source).not.toMatch(/localizeProductReadModel|localizeReadModelString|hrReadModelStrings/);
	});

	it('keeps read-model statuses compatible with status badges', () => {
		expect(readModelBadgeStatuses).toEqual([
			'archived',
			'proof_only',
			'draft',
			'scheduled',
			'live',
			'closed',
			'cancelled'
		]);
	});

	it('maps launch readiness blockers to checklist rows', () => {
		const readiness: LaunchReadinessResponse = {
			campaignId: 'campaign-id',
			ready: false,
			issues: [
				{
					code: 'template.version_missing',
					severity: 'error',
					message: 'Template version is required before launch.'
				},
				{
					code: 'disclosure.policy_missing',
					severity: 'warning',
					message: 'Disclosure policy is not configured.'
				}
			]
		};

		const view = toLaunchReadinessView(readiness);

		expect(view).toMatchObject({
			campaignId: 'campaign-id',
			state: 'blocked',
			statusLabel: 'Blocked'
		});
		expect(view.rows).toEqual([
			{
				code: 'template.version_missing',
				label: 'Questionnaire missing',
				severity: 'error',
				state: 'blocked',
				message: 'Questionnaire is required before launch.'
			},
			{
				code: 'disclosure.policy_missing',
				label: 'Disclosure policy missing',
				severity: 'warning',
				state: 'blocked',
				message: 'Disclosure policy is not configured.'
			}
		]);
	});

	it('maps workspace overview command items, totals, and recent-series cards', () => {
		const view = toWorkspaceOverviewView(sampleWorkspaceOverview);

		expect(view.tenantId).toBe('tenant-id');
		expect(view.commandItems).toEqual([
			{
				id: 'series-id-setup',
				title: 'Finish setup for Quarterly pulse',
				description: 'Consent, retention, disclosure, and scoring setup still need attention.',
				href: '/app/campaign-series/series-id/setup',
				actionLabel: 'Open setup',
				status: 'blocked',
				priority: 20,
				surfaceLabel: 'Prepare',
				rows: [{ label: 'Surface', value: 'Prepare' }]
			}
		]);
		expect(view.lifecycleSteps).toEqual([
			{
				id: 'prepare',
				label: 'Protocol',
				description: 'Compose the instrument, questions, scoring, and launch rules.'
			},
			{
				id: 'collect',
				label: 'Field',
				description: 'Launch the wave and watch responses and coverage build.'
			},
			{
				id: 'review',
				label: 'Evidence',
				description: 'Read compiled findings, limitations, and comparisons.'
			},
			{
				id: 'export',
				label: 'Export',
				description: 'Use generated CSV and codebook files for analysis.'
			}
		]);
		expect(view.sampleStudies).toEqual([
			expect.objectContaining({
				id: 'sample-series-id',
				title: 'Completed sample',
				actionLabel: 'Review sample results',
				actionHref: '/app/campaign-series/sample-series-id/reports',
				ownership: expect.objectContaining({
					label: 'Sample study',
					isSample: true,
					sampleScenario: 'mixed_lifecycle',
					readOnlyReason: 'sample_study'
				})
			})
		]);
		expect(view.ownStudies).toEqual([
			expect.objectContaining({
				id: 'own-series-id',
				title: 'New team study',
				actionLabel: 'Continue preparation',
				actionHref: '/app/campaign-series/own-series-id/setup',
				ownership: expect.objectContaining({
					label: 'Your study',
					isSample: false
				})
			})
		]);
		expect(view.totalRows).toEqual([
			{ label: 'Campaign series', value: '1' },
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '1' },
			{ label: 'Submitted responses', value: '14' },
			{ label: 'Export files', value: '3' }
		]);
		expect(view.recentSeries).toEqual([
			{
				id: 'series-id',
				title: 'Quarterly pulse',
				href: '/app/campaign-series/series-id',
				status: 'proof_only',
				archived: false,
				waveDots: ['done', 'live'],
				archiveActionLabel: 'Archive',
				canMutate: true,
				duplicateAction: null,
				ownership: {
					label: 'Your study',
					badgeStatus: 'neutral',
					isSample: false,
					sampleScenario: null,
					readOnlyReason: null,
					readOnlyMessage: null
				},
				lifecycle: {
					id: 'results_ready',
					label: 'Results ready',
					status: 'ready',
					actionLabel: 'Review results',
					actionHref: '/app/campaign-series/series-id/reports'
				},
				primaryAction: {
					label: 'Review results',
					href: '/app/campaign-series/series-id/reports'
				},
				rows: [
					{ label: 'Campaigns', value: '2' },
					{ label: 'Live campaigns', value: '1' },
					{ label: 'Submitted responses', value: '14' },
					{ label: 'Latest activity', value: '03. 05. 2026. 13:00' }
				]
			}
		]);
		expectStatusBadgeStatus(view.commandItems[0].status);
		expectStatusBadgeStatus(view.recentSeries[0].status);
	});

	it('localizes workspace overview and studies portfolio chrome for Croatian app mode', () => {
		const view = toWorkspaceOverviewView(sampleWorkspaceOverview, 'hr-HR');

		expect(view.lifecycleSteps.map((step) => step.label)).toEqual([
			'Protokol',
			'Teren',
			'Nalazi',
			'Izvoz'
		]);
		expect(view.totalRows.map((row) => row.label)).toEqual([
			'Studije',
			'Mjerenja',
			'Aktivna mjerenja',
			'Predani odgovori',
			'Datoteke izvoza'
		]);
		expect(view.commandItems[0].rows).toContainEqual({ label: 'Površina', value: 'Priprema' });
		expect(view.sampleStudies[0].actionLabel).toBe('Pregledaj ogledne rezultate');
		expect(view.sampleStudies[0].ownership.label).toBe('Ogledna studija');
		expect(view.ownStudies[0].actionLabel).toBe('Nastavi postavljanje');
		expect(view.recentSeries[0].rows).toContainEqual({
			label: 'Zadnja aktivnost',
			value: '03. 05. 2026. 13:00'
		});

		const empty = toCampaignSeriesListView({ items: [] }, {}, 'hr-HR');
		expect(empty.emptyState).toEqual({
			title: 'Još nema studija',
			message:
				'Izradite studiju kada imate pristup postavljanju ili dodajte ogledne studije za učenje.'
		});
		expect(empty.statusOptions.map((option) => option.label)).toEqual([
			'Sva spremnost',
			'Nije postavljeno',
			'Na čekanju',
			'Pregled'
		]);
		expect(empty.studySections.map((section) => section.title)).toEqual([
			'Ogledne studije',
			'Vaše studije'
		]);
	});

	it('maps tenant settings profile, counts, and management links', () => {
		const settings: TenantSettingsWorkspaceResponse = {
			profile: {
				tenantId: 'tenant-id',
				slug: 'algebra-research',
				name: 'Algebra Research',
				region: 'eu',
				defaultLocale: 'hr',
				status: 'active',
				createdAt: '2026-05-01T08:00:00Z',
				updatedAt: '2026-05-11T09:00:00Z'
			},
			counts: {
				campaignSeriesCount: 4,
				campaignCount: 7,
				liveCampaignCount: 2,
				submittedResponseCount: 128,
				subjectCount: 42,
				subjectGroupCount: 6,
				tenantMemberCount: 5,
				tenantRoleCount: 3,
				exportArtifactCount: 9
			},
			reportBranding: {
				organizationLabel: 'Algebra Research',
				reportTitle: 'Monthly workplace risk report',
				brandingSource: 'tenant_profile',
				logoMode: 'none',
				accentColorHex: '#2563eb',
				layoutVariant: 'standard',
				deferredCustomizations: ['logo_upload', 'custom_fonts', 'product_shell_theming']
			},
			managementLinks: [
				{
					id: 'campaign-series',
					label: 'Campaign series',
					description: 'Create and select tenant campaign series.',
					route: '/app/campaign-series'
				},
				{
					id: 'team',
					label: 'Team',
					description: 'Review tenant members and app-owned roles.',
					route: '/app/team'
				},
				{
					id: 'directory',
					label: 'People',
					description: 'Review subjects, groups, and hierarchy.',
					route: '/app/directory'
				}
			]
		};

		const view = toTenantSettingsView(settings);

		expect(view.title).toBe('Algebra Research');
		expect(view.profileRows).toEqual([
			{ label: 'Slug', value: 'algebra-research', mono: true },
			{ label: 'Region', value: 'EU' },
			{ label: 'Default locale', value: 'hr' },
			{ label: 'Status', value: 'Active' },
			{ label: 'Created', value: '01. 05. 2026. 10:00' },
			{ label: 'Updated', value: '11. 05. 2026. 11:00' }
		]);
		expect(view.metricRows).toEqual([
			{ label: 'Campaign series', value: '4' },
			{ label: 'Campaigns', value: '7' },
			{ label: 'Live campaigns', value: '2' },
			{ label: 'Submitted responses', value: '128' },
			{ label: 'Subjects', value: '42' },
			{ label: 'Subject groups', value: '6' },
			{ label: 'Tenant members', value: '5' },
			{ label: 'Tenant roles', value: '3' },
			{ label: 'Export files', value: '9' }
		]);
		expect(view.reportBranding.rows).toEqual([
			{ label: 'Organization label', value: 'Algebra Research' },
			{ label: 'Report title', value: 'Monthly workplace risk report' },
			{ label: 'Branding source', value: 'Tenant profile' },
			{ label: 'Logo mode', value: 'None' },
			{ label: 'Accent color', value: '#2563eb' },
			{ label: 'Layout', value: 'Standard' }
		]);
		expect(view.reportBranding.deferredItems).toEqual([
			'Logo upload',
			'Custom fonts',
			'Product shell theming'
		]);
		expect(view.managementLinks.map((link) => link.href)).toEqual([
			'/app/campaign-series',
			'/app/team',
			'/app/directory'
		]);
	});

	it('localizes settings, instrument library, and session states for Croatian app mode', () => {
		const settings: TenantSettingsWorkspaceResponse = {
			profile: {
				tenantId: 'tenant-id',
				slug: 'algebra-research',
				name: 'Algebra Research',
				region: 'eu',
				defaultLocale: 'hr',
				status: 'active',
				createdAt: '2026-05-01T08:00:00Z',
				updatedAt: '2026-05-11T09:00:00Z'
			},
			counts: {
				campaignSeriesCount: 4,
				campaignCount: 7,
				liveCampaignCount: 2,
				submittedResponseCount: 128,
				subjectCount: 42,
				subjectGroupCount: 6,
				tenantMemberCount: 5,
				tenantRoleCount: 3,
				exportArtifactCount: 9
			},
			reportBranding: {
				organizationLabel: 'Algebra Research',
				reportTitle: 'Monthly workplace risk report',
				brandingSource: 'tenant_profile',
				logoMode: 'none',
				accentColorHex: '#2563eb',
				layoutVariant: 'standard',
				deferredCustomizations: ['logo_upload', 'custom_fonts', 'product_shell_theming']
			},
			managementLinks: []
		};

		const settingsView = toTenantSettingsView(settings, 'hr-HR');
		expect(settingsView.profileRows.map((row) => row.label)).toEqual([
			'Slug',
			'Regija',
			'Zadani jezik',
			'Status',
			'Izrađeno',
			'Ažurirano'
	]);
	expect(settingsView.metricRows.map((row) => row.label)).toContain('Studije');
	expect(settingsView.metricRows.map((row) => row.label)).toContain('Članovi radnog prostora');
	expect(settingsView.reportBranding.rows.map((row) => row.label)).toContain('Oznaka organizacije');
	expect(settingsView.reportBranding.rows.map((row) => row.label)).toContain('Naslov izvještaja');
	expect(settingsView.reportBranding.deferredTitle).toBe('Još nije podesivo');

		const instruments = toInstrumentLibraryView(
			[
				{
					id: 'instrument-id-1',
					code: 'BURNOUT_16',
					version: '1.0.0',
					fullName: 'Tenant burnout pulse',
					rightsStatus: 'tenant_attested',
					validityLabel: 'Tenant-provided validated instrument',
					canStartNewCampaign: true
				}
			],
			'hr-HR'
		);
		expect(instruments.metricRows.map((row) => row.label)).toEqual([
			'Izvori upitnika',
			'Spremno za pokretanje',
			'Blokirano za pokretanje'
		]);
		expect(instruments.cards[0].statusLabel).toBe('Spremno za pokretanje');
		expect(instruments.cards[0].rows).toContainEqual({
			label: 'Prava korištenja',
			value: 'Tenant attested'
		});

		expect(toSessionView({ error: new ApiError('Unauthorized', 401, null) }, 'hr-HR')).toMatchObject(
			{
				state: 'unauthenticated',
				title: 'Potrebna je prijava',
				message: 'Prijavite se prije otvaranja površina radnog prostora.'
			}
		);
		expect(toSessionView({ error: new ApiError('Failure', 500, null) }, 'hr-HR')).toMatchObject({
			state: 'failed',
			title: 'Provjera sesije nije uspjela',
			message: 'Provjera sesije nije uspjela sa statusom 500.'
		});
	});

	it('maps instrument library summary and cards', () => {
		const instruments: InstrumentSummaryResponse[] = [
			{
				id: 'instrument-id-1',
				code: 'BURNOUT_16',
				version: '1.0.0',
				fullName: 'Tenant burnout pulse',
				rightsStatus: 'tenant_attested',
				validityLabel: 'Tenant-provided validated instrument',
				canStartNewCampaign: true
			},
			{
				id: 'instrument-id-2',
				code: 'INTERNAL_DEMO',
				version: '0.1.0',
				fullName: 'Internal demo only',
				rightsStatus: 'unverified_internal_demo',
				validityLabel: 'Not launchable',
				canStartNewCampaign: false
			}
		];

		const view = toInstrumentLibraryView(instruments);

		expect(view.metricRows).toEqual([
			{ label: 'Instruments', value: '2' },
			{ label: 'Launch eligible', value: '1' },
			{ label: 'Launch blocked', value: '1' }
		]);
		expect(view.cards).toEqual([
			{
				id: 'instrument-id-1',
				title: 'Tenant burnout pulse',
				subtitle: 'BURNOUT_16 1.0.0',
				status: 'ready',
				statusLabel: 'Launch eligible',
				rows: [
					{ label: 'Rights', value: 'Tenant attested' },
					{ label: 'Validity', value: 'Tenant-provided validated instrument' }
				]
			},
			{
				id: 'instrument-id-2',
				title: 'Internal demo only',
				subtitle: 'INTERNAL_DEMO 0.1.0',
				status: 'blocked',
				statusLabel: 'Launch blocked',
				rows: [
					{ label: 'Rights', value: 'Unverified internal demo' },
					{ label: 'Validity', value: 'Not launchable' }
				]
			}
		]);
		expectStatusBadgeStatus(view.cards[0].status);
		expectStatusBadgeStatus(view.cards[1].status);
	});

	it('maps empty campaign-series list to an empty state', () => {
		const view = toCampaignSeriesListView({ items: [] });

		expect(view.items).toEqual([]);
		expect(view.emptyState).toEqual({
			title: 'No studies yet',
			message: 'Create your study when you have setup access, or add sample studies for learning.'
		});
	});

	it('maps filtered empty campaign-series list to a matching empty state', () => {
		const view = toCampaignSeriesListView(
			{ items: [] },
			{ search: 'gamma', status: 'proof_only', sort: 'name_asc', visibility: 'archived' }
		);

		expect(view.items).toEqual([]);
		expect(view.filtersActive).toBe(true);
		expect(view.emptyState).toEqual({
			title: 'No matching studies',
			message: 'Adjust search, readiness, or visibility filters to show sample or own studies.'
		});
	});

	it('exposes campaign-series portfolio filter and sort options', () => {
		const view = toCampaignSeriesListView(sampleCampaignSeriesList);

		expect(view.filtersActive).toBe(false);
		expect(view.statusOptions).toEqual([
			{ value: 'all', label: 'All readiness' },
			{ value: 'not_configured', label: 'Not configured' },
			{ value: 'pending', label: 'Pending' },
			{ value: 'proof_only', label: 'Preview' }
		]);
		expect(view.sortOptions).toEqual([
			{ value: 'activity_desc', label: 'Latest activity' },
			{ value: 'updated_desc', label: 'Recently updated' },
			{ value: 'created_desc', label: 'Recently created' },
			{ value: 'name_asc', label: 'Name A-Z' }
		]);
		expect(view.visibilityOptions).toEqual([
			{ value: 'active', label: 'Active' },
			{ value: 'archived', label: 'Archived' },
			{ value: 'all', label: 'All visibility' }
		]);
	});

	it('maps campaign-series list items to link-ready rows', () => {
		const view = toCampaignSeriesListView(sampleCampaignSeriesList);

		expect(view.emptyState).toBeNull();
		expect(view.items).toEqual([
			{
				id: 'series-id',
				title: 'Quarterly pulse',
				href: '/app/campaign-series/series-id',
				status: 'pending',
				archived: false,
				waveDots: ['done', 'live'],
				archiveActionLabel: 'Archive',
				canMutate: true,
				duplicateAction: null,
				ownership: {
					label: 'Your study',
					badgeStatus: 'neutral',
					isSample: false,
					sampleScenario: null,
					readOnlyReason: null,
					readOnlyMessage: null
				},
				lifecycle: {
					id: 'results_ready',
					label: 'Results ready',
					status: 'ready',
					actionLabel: 'Review results',
					actionHref: '/app/campaign-series/series-id/reports'
				},
				primaryAction: {
					label: 'Review results',
					href: '/app/campaign-series/series-id/reports'
				},
				rows: [
					{ label: 'Campaigns', value: '2' },
					{ label: 'Live campaigns', value: '1' },
					{ label: 'Submitted responses', value: '14' },
					{ label: 'Latest activity', value: '03. 05. 2026. 13:00' }
				]
			}
		]);
		expectStatusBadgeStatus(view.items[0].status);
	});

	it('localizes major generated read-model labels for Croatian app mode', () => {
		const listView = toCampaignSeriesListView(sampleCampaignSeriesList, {}, 'hr-HR');
		const hubView = toCampaignSeriesHubView(sampleCampaignSeriesHub, 'hr-HR');
		const setupView = toCampaignSeriesSetupWorkspaceView(sampleSetupWorkspace, 'hr-HR');

		expect(listView.statusOptions).toEqual([
			{ value: 'all', label: 'Sva spremnost' },
			{ value: 'not_configured', label: 'Nije postavljeno' },
			{ value: 'pending', label: 'Na čekanju' },
			{ value: 'proof_only', label: 'Pregled' }
		]);
		expect(listView.items[0].rows).toContainEqual({ label: 'Mjerenja', value: '2' });
		expect(listView.items[0].ownership.label).toBe('Vaša studija');
		expect(listView.items[0].archiveActionLabel).toBe('Arhiviraj');
		expect(listView.items[0].lifecycle.label).toBe('Rezultati spremni');

		expect(hubView.surfaceTitle).toBe('Pregled studije');
		expect(hubView.rows[0].label).toBe('Izrađeno');
		expect(hubView.studyModel.title).toBe('Pregled studije');
		expect(hubView.studyModel.items[0].label).toBe('Sažetak studije');
		expect(hubView.studyModel.items[0].summary).toContain('Svrha studije');
		expect(hubView.studyModel.items[1].label).toBe('Trenutno stanje');
		expect(hubView.studyModel.items[1].summary).toContain('Krugovi prikupljanja');
		expect(hubView.governanceRows[0]).toEqual({
			label: 'Pristanak',
			value: 'pregled',
			status: 'proof_only'
		});
		expect(hubView.lifecycleMap.title).toBe('Životni ciklus studije');
	expect(hubView.campaignRows[0].rows).toContainEqual({
		label: 'Način identiteta',
		value: 'anonimno s ponovljenim sudjelovanjem'
	});

	expect(setupView.surfaceLabel).toBe('Priprema studije');
	expect(setupView.summaryRows).toContainEqual({
		label: 'Nedostajući preduvjeti',
		value: '1'
	});
	});

	it('does not emit mojibake in Croatian generated read-model text', () => {
		const views = [
			toCampaignSeriesListView(sampleCampaignSeriesList, {}, 'hr-HR'),
			toCampaignSeriesHubView(sampleCampaignSeriesHub, 'hr-HR'),
			toCampaignSeriesSetupWorkspaceView(sampleSetupWorkspace, 'hr-HR'),
			toCampaignSeriesOperationsWorkspaceView(sampleOperationsWorkspace, 'hr-HR'),
			toCampaignSeriesReportsWorkspaceView(sampleReportsWorkspace, 'hr-HR'),
			toCampaignSeriesWavesWorkspaceView(sampleWavesWorkspace, 'hr-HR')
		];
		const generatedStrings = views.flatMap((view, index) => collectStrings(view, `view${index}`));

		expect(generatedStrings.filter((entry) => mojibakePattern.test(entry))).toEqual([]);
	});

	it('does not leak old English read-model chrome into Croatian workspace views', () => {
		const views = [
			toCampaignSeriesHubView(sampleCampaignSeriesHub, 'hr-HR'),
			toCampaignSeriesSetupWorkspaceView(sampleSetupWorkspace, 'hr-HR'),
			toCampaignSeriesOperationsWorkspaceView(sampleOperationsWorkspace, 'hr-HR'),
			toCampaignSeriesReportsWorkspaceView(sampleReportsWorkspace, 'hr-HR'),
			toCampaignSeriesWavesWorkspaceView(sampleWavesWorkspace, 'hr-HR')
		];
		const generatedText = views
			.flatMap((view, index) => collectStrings(view, `view${index}`))
			.join('\n');

		for (const forbidden of [
			'Prepare study',
			'Study preparation',
			'Prepare reference',
			'Collect responses',
			'Study collection',
			'Collection reference',
			'Review results',
			'Study results',
			'Results reference',
			'Compare rounds',
			'Repeated-round comparison',
			'Identity mode',
			'Submitted responses',
			'Missing prerequisites',
			'Result availability',
			'Coverage and visibility',
			'Limitations and finality',
			'Export next use',
			'Repeat-participation waves',
			'Linked repeat responses',
			'Visible comparisons'
		]) {
			expect(generatedText).not.toContain(forbidden);
		}
	});

	it('maps archived campaign-series list items to restore-ready cards', () => {
		const view = toCampaignSeriesListView({
			items: [
				{
					...sampleCampaignSeriesList.items[0],
					archived: true,
					archivedAt: '2026-05-11T13:15:00Z',
					archivedByUserId: 'actor-id',
					archiveReason: 'Completed pilot'
				}
			]
		});

		expect(view.items[0]).toEqual({
			id: 'series-id',
			title: 'Quarterly pulse',
			href: '/app/campaign-series/series-id',
			status: 'archived',
			archived: true,
			waveDots: ['done', 'live'],
			archiveActionLabel: 'Restore',
			canMutate: true,
			duplicateAction: null,
			ownership: {
				label: 'Your study',
				badgeStatus: 'neutral',
				isSample: false,
				sampleScenario: null,
				readOnlyReason: null,
				readOnlyMessage: null
			},
			lifecycle: {
				id: 'archived',
				label: 'Archived',
				status: 'archived',
				actionLabel: 'Open archived study',
				actionHref: '/app/campaign-series/series-id'
			},
			primaryAction: {
				label: 'Open archived study',
				href: '/app/campaign-series/series-id'
			},
			rows: [
				{ label: 'Campaigns', value: '2' },
				{ label: 'Live campaigns', value: '1' },
				{ label: 'Submitted responses', value: '14' },
				{ label: 'Latest activity', value: '03. 05. 2026. 13:00' },
				{ label: 'Archived', value: '2026-05-11T13:15:00Z' }
			]
		});
		expectStatusBadgeStatus(view.items[0].status);
	});

	it('maps sample campaign-series list items to read-only ownership labels', () => {
		const view = toCampaignSeriesListView({
			items: [
				{
					...sampleCampaignSeriesList.items[0],
					studyKind: 'sample',
					isSample: true,
					sampleScenario: 'mixed_lifecycle',
					readOnlyReason: 'sample_study'
				}
			]
		});

		expect(view.items[0].canMutate).toBe(false);
		expect(view.items[0].ownership).toEqual({
			label: 'Sample study',
			badgeStatus: 'demo',
			isSample: true,
			sampleScenario: 'mixed_lifecycle',
			readOnlyReason: 'sample_study',
			readOnlyMessage:
				'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
		});
		expectStatusBadgeStatus(view.items[0].ownership.badgeStatus);
	});

	it('maps sample scenarios to product-facing starter state messages', () => {
		const scenarios = [
			{
				scenario: 'setup',
				message:
					'Preparation sample: read-only starter content showing study preparation before launch.'
			},
			{
				scenario: 'blocked',
				message:
					'Preparation sample: read-only starter content showing blocked preparation before launch.'
			},
			{
				scenario: 'in_collection',
				message:
					'Collection sample: read-only starter content showing live or partial response collection.'
			},
			{
				scenario: 'mixed_lifecycle',
				message:
					'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
			},
			{
				scenario: 'completed',
				message:
					'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
			},
			{
				scenario: 'longitudinal',
				message:
					'Repeat-participation sample: read-only starter content showing repeated measurements and linked repeat-response review.'
			}
		] as const;

		for (const { scenario, message } of scenarios) {
			const view = toCampaignSeriesListView({
				items: [
					{
						...sampleCampaignSeriesList.items[0],
						studyKind: 'sample',
						isSample: true,
						sampleScenario: scenario,
						readOnlyReason: 'sample_study'
					}
				]
			});

			expect(view.items[0].ownership.readOnlyMessage).toBe(message);
		}
	});

	it('exposes a separate duplicate action for sample campaign-series cards', () => {
		const view = toCampaignSeriesListView({
			items: [
				{
					...sampleCampaignSeriesList.items[0],
					name: 'Starter sample',
					studyKind: 'sample',
					isSample: true,
					sampleScenario: 'mixed_lifecycle',
					readOnlyReason: 'sample_study'
				},
				{
					...sampleCampaignSeriesList.items[0],
					id: 'own-series-id',
					name: 'Own study'
				}
			]
		});

		expect(view.items[0].canMutate).toBe(false);
		expect(view.items[0].duplicateAction).toEqual({
			label: 'Duplicate as study',
			defaultName: 'Copy of Starter sample'
		});
		expect(view.items[1].duplicateAction).toBeNull();
	});

	it('groups campaign-series list items by sample or own study and lifecycle', () => {
		const view = toCampaignSeriesListView({
			items: [
				{
					...sampleCampaignSeriesList.items[0],
					studyKind: 'sample',
					isSample: true,
					sampleScenario: 'completed',
					readOnlyReason: 'sample_study',
					id: 'sample-results',
					name: 'Completed sample',
					liveCampaignCount: 0,
					submittedResponseCount: 28
				},
				{
					...sampleCampaignSeriesList.items[0],
					id: 'own-live',
					name: 'Live rollout',
					liveCampaignCount: 2,
					submittedResponseCount: 0
				},
				{
					...sampleCampaignSeriesList.items[0],
					id: 'own-setup',
					name: 'Setup draft',
					campaignCount: 0,
					liveCampaignCount: 0,
					submittedResponseCount: 0,
					readinessStatus: 'not_configured'
				},
				{
					...sampleCampaignSeriesList.items[0],
					id: 'own-archived',
					name: 'Archived pilot',
					archived: true,
					archivedAt: '2026-05-11T13:15:00Z',
					archivedByUserId: 'actor-id',
					archiveReason: 'Completed pilot'
				}
			]
		});

		expect(view.studySections.map((section) => section.title)).toEqual([
			'Sample studies',
			'Your studies'
		]);
		expect(view.studySections.map((section) => section.emptyState)).toEqual([
			'No sample studies match this view. Clear filters to inspect examples.',
			'No own studies match this view. Clear filters or create your study when you have setup access.'
		]);
		expect(view.studySections[0].groups).toEqual([
			expect.objectContaining({
				label: 'Results ready',
				items: [
					expect.objectContaining({
						id: 'sample-results',
						primaryAction: {
							label: 'Review sample results',
							href: '/app/campaign-series/sample-results/reports'
						}
					})
				]
			})
		]);
		expect(view.studySections[1].groups.map((group) => group.label)).toEqual([
			'Needs setup',
			'In collection',
			'Archived'
		]);
		expect(view.studySections[1].groups.flatMap((group) => group.items)).toEqual([
			expect.objectContaining({
				id: 'own-setup',
				primaryAction: {
					label: 'Continue preparation',
					href: '/app/campaign-series/own-setup/setup'
				}
			}),
			expect.objectContaining({
				id: 'own-live',
				primaryAction: {
					label: 'Monitor collection',
					href: '/app/campaign-series/own-live/operations'
				}
			}),
			expect.objectContaining({
				id: 'own-archived',
				primaryAction: {
					label: 'Open archived study',
					href: '/app/campaign-series/own-archived'
				}
			})
		]);
	});

	it('maps campaign-series hub response to title, totals, governance, and campaigns', () => {
		const view = toCampaignSeriesHubView(sampleCampaignSeriesHub);

		expect(view.title).toBe('Quarterly burnout pulse');
		expect(view.subtitle).toBe('2 campaigns, 1 live');
		expect(view.ownership).toEqual({
			label: 'Your study',
			badgeStatus: 'neutral',
			isSample: false,
			sampleScenario: null,
			readOnlyReason: null,
			readOnlyMessage: null
		});
		expect(view.canMutate).toBe(true);
		expect(view.rows).toEqual([
			{ label: 'Created', value: '01. 05. 2026. 10:00' },
			{ label: 'Updated', value: '02. 05. 2026. 11:00' }
		]);
		expect(view.archiveState).toEqual({
			archived: false,
			status: 'ready',
			label: 'Active',
			archivedAt: null,
			reason: null
		});
		expect(view.totalRows).toEqual([
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '1' },
			{ label: 'Submitted responses', value: '31' },
			{ label: 'Scores', value: '28' },
			{ label: 'Export files', value: '3' }
		]);
		expect(view.governanceRows).toEqual([
			{ label: 'Consent', value: 'preview', status: 'proof_only' },
			{ label: 'Retention', value: 'preview', status: 'proof_only' },
			{ label: 'Disclosure', value: 'pending', status: 'pending' },
			{ label: 'Scoring', value: 'not configured', status: 'not_configured' }
		]);
		for (const row of view.governanceRows) {
			expectStatusBadgeStatus(row.status);
		}
		expect(view.lifecycleItems).toEqual([
			{
				id: 'setup',
				label: 'Prepare',
				status: 'ready',
				guidance: 'Governance prerequisites are configured for this series.',
				route: 'setup',
				actionLabel: 'Review setup'
			},
			{
				id: 'operations',
				label: 'Operations',
				status: 'pending',
				guidance: 'Collection is live, but no submitted responses are available yet.',
				route: 'operations',
				actionLabel: 'Open operations'
			}
		]);
		for (const item of view.lifecycleItems) {
			expectStatusBadgeStatus(item.status);
		}
		expect(view.campaignRows).toEqual([
			{
				id: 'campaign-id',
				title: 'Wave 1',
				status: 'live',
				rows: [
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Submitted responses', value: '31' },
					{ label: 'Scores', value: '28' },
					{ label: 'Export files', value: '3' }
				]
			}
		]);
		expectStatusBadgeStatus(view.campaignRows[0].status);
	});

	it('maps selected-series hub lifecycle into a study overview map', () => {
		const view = toCampaignSeriesHubView({
			...sampleCampaignSeriesHub,
			lifecycle: [
				...sampleCampaignSeriesHub.lifecycle,
				{
					id: 'reports',
					label: 'Reports',
					status: 'ready',
					guidance: 'Submitted responses are available for aggregate review.',
					route: 'reports',
					actionLabel: 'Open reports'
				},
				{
					id: 'waves',
					label: 'Rounds',
					status: 'pending',
					guidance: 'A second wave can be compared when linked responses exist.',
					route: 'waves',
					actionLabel: 'Open waves'
				}
			]
		});

		expect(view.surfaceTitle).toBe('Study overview');
		expect(view.referenceTitle).toBe('Study reference');
		expect(view.studyModel).toEqual({
			title: 'Study overview',
			description:
				'A short orientation for the selected study. Use Prepare, Collect, and Results for the main path; use Rounds only when repeated measurement applies.',
			items: [
				{
					id: 'study_brief',
					label: 'Study context',
					status: 'pending',
					badgeLabel: 'Needs brief',
					summary: 'The study purpose has not been captured yet.',
					guidance:
						'Add purpose, audience, design, intended use, and interpretation limits before sharing results.',
					detailRows: [
						{ label: 'Audience', value: 'Not set' },
						{ label: 'Design', value: 'Not set' },
						{ label: 'Intended use', value: 'Not set' }
					]
				},
				{
					id: 'current_state',
					label: 'Current state',
					status: 'live',
					badgeLabel: 'Collection live',
					summary: 'Collection rounds: 2; collected responses: 31; export files: 3.',
					guidance:
						'Collection is open. Use Collection to monitor response progress, then Results when the round closes.',
					detailRows: [
						{ label: 'Collection rounds', value: '2' },
						{ label: 'Collected responses', value: '31' },
						{ label: 'Export files', value: '3' }
					]
				},
				{
					id: 'next_action',
					label: 'Recommended next move',
					status: 'ready',
					badgeLabel: 'Review results',
					summary: 'Open Results to review charts, evidence, exports, and interpretation limits.',
					guidance:
						'Results is where you inspect evidence. Use Rounds only when you need change-over-time comparison.',
					detailRows: []
				}
			]
		});

		expect(view.lifecycleMap.items).toEqual([
			{
				id: 'setup',
				label: 'Prepare',
				status: 'ready',
				description: 'Prepare the questionnaire, result outputs, policies, collection round, and launch check.',
				guidance: 'Governance prerequisites are configured for this series.',
				route: 'setup',
				href: '/app/campaign-series/series-id/setup',
				actionLabel: 'Review setup'
			},
			{
				id: 'operations',
				label: 'Collect',
				status: 'pending',
				description: 'Start the wave, share access, send invitations, and monitor submissions.',
				guidance: 'Collection is live, but no submitted responses are available yet.',
				route: 'operations',
				href: '/app/campaign-series/series-id/operations',
				actionLabel: 'Open operations'
			},
			{
				id: 'reports',
				label: 'Review results',
				status: 'ready',
				description: 'Review findings, limitations, and export files after responses are ready.',
				guidance: 'Submitted responses are available for aggregate review.',
				route: 'reports',
				href: '/app/campaign-series/series-id/reports',
				actionLabel: 'Open reports'
			},
			{
				id: 'waves',
				label: 'Compare rounds',
				status: 'pending',
				description: 'Use only for repeated measurement rounds; keep the main path in Results and Exports.',
				guidance: 'A second wave can be compared when linked responses exist.',
				route: 'waves',
				href: '/app/campaign-series/series-id/waves',
				actionLabel: 'Open waves'
			}
		]);
		expect(view.lifecycleMap.items[2].description).toContain('export files');
		for (const item of view.studyModel.items) {
			expectStatusBadgeStatus(item.status);
			for (const row of item.detailRows) {
				if (row.status) {
					expectStatusBadgeStatus(row.status);
				}
			}
		}
		for (const item of view.lifecycleMap.items) {
			expectStatusBadgeStatus(item.status);
		}
	});

	it('localizes selected-series overview read-model copy for Croatian app mode', () => {
		const view = toCampaignSeriesHubView(sampleCampaignSeriesHub, 'hr-HR');

		expect(view.surfaceTitle).toBe('Pregled studije');
		expect(view.referenceTitle).toBe('Referenca studije');
		expect(view.studyModel.title).toBe('Pregled studije');
		expect(view.studyModel.description).toContain('Kratak orijentir');
		expect(view.studyModel.items[0]).toMatchObject({
			label: 'Sažetak studije',
			badgeLabel: 'Treba opis',
			summary: 'Svrha studije još nije zabilježena.'
		});
		expect(view.studyModel.items[1]).toMatchObject({
			label: 'Trenutno stanje',
			badgeLabel: 'Prikupljanje traje',
			summary: 'Krugovi prikupljanja: 2; prikupljeni odgovori: 31; izvozne datoteke: 3.'
		});
		expect(view.studyModel.items[2]).toMatchObject({
			label: 'Preporučeni sljedeći korak',
			badgeLabel: 'Pregledajte rezultate',
			summary: 'Otvorite Rezultate za grafikone, nalaze, izvoze i granice tumačenja.'
		});
		expect(view.lifecycleMap).toMatchObject({
			title: 'Životni ciklus studije',
			description: 'Prođite kroz studiju od pripreme do prikupljanja, rezultata i usporedbe mjerenja.'
		});
		expect(view.lifecycleMap.items[0]).toMatchObject({
			label: 'Priprema',
			description: 'Izradite upitnik, pripremu rezultata, pravila, mjerenje i provjeru pokretanja.'
		});
	});

	it('maps archived campaign-series hub response to archive state rows', () => {
		const view = toCampaignSeriesHubView({
			...sampleCampaignSeriesHub,
			archived: true,
			archivedAt: '2026-05-11T13:15:00Z',
			archivedByUserId: 'actor-id',
			archiveReason: 'Completed pilot'
		});

		expect(view.archiveState).toEqual({
			archived: true,
			status: 'archived',
			label: 'Archived',
			archivedAt: '2026-05-11T13:15:00Z',
			reason: 'Completed pilot'
		});
		expect(view.rows).toContainEqual({ label: 'Archived', value: '11. 05. 2026. 15:15' });
		expect(view.rows).toContainEqual({ label: 'Archive reason', value: 'Completed pilot' });
		expectStatusBadgeStatus(view.archiveState.status);
	});

	it('maps sample campaign-series hub responses to read-only ownership state', () => {
		const view = toCampaignSeriesHubView({
			...sampleCampaignSeriesHub,
			studyKind: 'sample',
			isSample: true,
			sampleScenario: 'completed',
			readOnlyReason: 'sample_study'
		});

		expect(view.ownership).toEqual({
			label: 'Sample study',
			badgeStatus: 'demo',
			isSample: true,
			sampleScenario: 'completed',
			readOnlyReason: 'sample_study',
			readOnlyMessage:
				'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
		});
		expect(view.canMutate).toBe(false);
		expectStatusBadgeStatus(view.ownership.badgeStatus);
	});

	it('maps product API errors to display messages', () => {
		expect(
			toProductApiErrorMessage(
				new ApiError('Not found', 404, { detail: 'Campaign series was not found.' }),
				'Fallback message.'
			)
		).toBe('Campaign series was not found.');
		expect(toProductApiErrorMessage(new ApiError('Failure', 500, null), 'Fallback message.')).toBe(
			'API request failed with status 500.'
		);
		expect(toProductApiErrorMessage(new Error('network'), 'Fallback message.')).toBe(
			'Fallback message.'
		);
	});

	it('maps setup selected-series surface from hub context', () => {
		const view = toSelectedSeriesSurfaceView(sampleCampaignSeriesHub, 'setup');

		expect(view.title).toBe('Quarterly burnout pulse');
		expect(view.surfaceLabel).toBe('Prepare study');
		expect(view.summaryRows).toEqual([
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '1' }
		]);
		expect(view.governanceRows).toEqual([
			{ label: 'Consent', value: 'preview', status: 'proof_only' },
			{ label: 'Retention', value: 'preview', status: 'proof_only' },
			{ label: 'Disclosure', value: 'pending', status: 'pending' },
			{ label: 'Scoring', value: 'not configured', status: 'not_configured' }
		]);
		expect(view.emptyState).toBeNull();
	});

	it('maps setup workspace state to readiness, policies, and selected campaign rows', () => {
		const view = toCampaignSeriesSetupWorkspaceView(sampleSetupWorkspace);

		expect(view.title).toBe('Quarterly burnout pulse');
		expect(view.subtitle).toBe('1 campaign, 0 live');
		expect(view.summaryRows).toEqual([
			{ label: 'Campaigns', value: '1' },
			{ label: 'Live campaigns', value: '0' },
			{ label: 'Missing prerequisites', value: '1' }
		]);
		expect(view.readiness).toEqual({
			label: 'blocked',
			status: 'blocked',
			badgeLabel: 'Blocked'
		});
		expect(view.policyRows).toEqual([
			{
				label: 'Consent',
				value: 'Configured',
				status: 'ready',
				badgeLabel: 'Configured',
				details: [
					{ label: 'Title', value: 'Default participant disclosure' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Required grants', value: '2' },
					{ label: 'Optional grants', value: '0' },
					{ label: 'Published', value: '2026-05-06' }
				]
			},
			{
				label: 'Retention',
				value: 'Configured',
				status: 'ready',
				badgeLabel: 'Configured',
				details: [
					{ label: 'Retain for', value: '1 year' },
					{ label: 'Starts from', value: 'response submitted at' },
					{ label: 'Action after retention', value: 'anonymize' },
					{ label: 'Next review', value: '2027-05-06' }
				]
			},
			{
				label: 'Disclosure',
				value: 'Not configured',
				status: 'not_configured',
				badgeLabel: 'Not configured',
				details: []
			}
		]);
		expect(view.selectedCampaignRows).toEqual([
			{ label: 'Selected wave', value: 'Wave 1' },
			{ label: 'Status', value: 'draft' },
			{ label: 'Identity mode', value: 'anonymous repeat participation' },
			{ label: 'Locale', value: 'en' },
			{ label: 'Latest launch', value: 'Not available' }
		]);
		expect(view.templateRows).toEqual([
			{ label: 'Template', value: 'Tenant burnout pulse template' },
			{ label: 'Status', value: 'draft' },
			{ label: 'Locale', value: 'en' },
			{ label: 'Questions', value: '5' }
		]);
		expect(view.scoringRows).toEqual([
			{ label: 'Rule', value: 'burnout.total', mono: true },
			{ label: 'Status', value: 'draft' },
			{ label: 'Source', value: 'template version' }
		]);
		expect(view.missingPrerequisiteRows).toEqual([
			{
				code: 'disclosure_policy.missing',
				label: 'Disclosure policy',
				message: 'Add a disclosure policy for this series.',
				severity: 'blocking',
				status: 'blocked'
			}
		]);
		expect(view.emptyState).toBeNull();
		expectStatusBadgeStatus(view.readiness.status);
		expectStatusBadgeStatus(view.policyRows[0].status);
		expectStatusBadgeStatus(view.missingPrerequisiteRows[0].status);
	});

	it('maps setup workspace into a guided study preparation checklist', () => {
		const view = toCampaignSeriesSetupWorkspaceView(sampleSetupWorkspace);

		expect(view.surfaceLabel).toBe('Prepare study');
		expect(view.surfaceEyebrow).toBe('Study preparation');
		expect(view.surfaceDescription).toBe(
			'Prepare this study for collection by completing setup tasks and launch-readiness checks.'
		);
		expect(view.referenceTitle).toBe('Prepare reference');
		expect(view.referenceDescription).toBe(
			'Detailed setup records, policy status, selected wave fields, and launch-check notes stay here for review.'
		);
		expect(view.preparationChecklist).toEqual([
			{
				id: 'instrument_template',
				label: 'Instrument and template',
				status: 'ready',
				badgeLabel: 'Ready',
				summary: 'Tenant burnout pulse template / draft / 5 questions',
				guidance: 'Questionnaire is available for wave drafts.',
				detailRows: [
					{ label: 'Template', value: 'Tenant burnout pulse template' },
					{ label: 'Questions', value: '5' }
				]
			},
			{
				id: 'scoring',
				label: 'Scoring',
				status: 'ready',
				badgeLabel: 'Ready',
				summary: 'burnout.total / draft',
				guidance: 'Result outputs are available for launch-readiness checks.',
				detailRows: [
					{ label: 'Rule', value: 'burnout.total', mono: true }
				]
			},
			{
				id: 'policies',
				label: 'Policies',
				status: 'blocked',
				badgeLabel: 'Blocked',
				summary: '2 of 3 policies configured',
				guidance: 'Disclosure policy: Add a disclosure policy for this series.',
				detailRows: [
					{ label: 'Consent', value: 'Configured' },
					{ label: 'Retention', value: 'Configured' },
					{ label: 'Disclosure', value: 'Not configured' }
				]
			},
			{
				id: 'campaign',
				label: 'Wave draft',
				status: 'ready',
				badgeLabel: 'Ready',
				summary: 'Wave 1 / draft / anonymous repeat participation',
				guidance: 'Wave draft is ready for recipient setup and launch checks.',
				detailRows: [
					{ label: 'Selected wave', value: 'Wave 1' },
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Locale', value: 'en' }
				]
			},
			{
				id: 'launch_readiness',
				label: 'Launch readiness',
				status: 'blocked',
				badgeLabel: 'Blocked',
				summary: 'Launch readiness is blocked',
				guidance: 'Disclosure policy: Add a disclosure policy for this series.',
				detailRows: [
					{ label: 'Readiness', value: 'blocked' },
					{ label: 'Missing prerequisites', value: '1' }
				]
			}
		]);

		for (const item of view.preparationChecklist) {
			expectStatusBadgeStatus(item.status);
		}
	});

	it('maps operations selected-series surface with campaign state rows', () => {
		const view = toSelectedSeriesSurfaceView(sampleCampaignSeriesHub, 'operations');

		expect(view.surfaceLabel).toBe('Collect responses');
		expect(view.summaryRows).toEqual([
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '1' },
			{ label: 'Submitted responses', value: '31' }
		]);
		expect(view.campaignRows).toEqual([
			{
				id: 'campaign-id',
				title: 'Wave 1',
				status: 'live',
				rows: [
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Submitted responses', value: '31' },
					{ label: 'Latest launch', value: '05. 05. 2026. 10:30' }
				]
			}
		]);
	});

	it('maps operations workspace state to summary, selected campaign, prerequisites, and campaign rows', () => {
		const view = toCampaignSeriesOperationsWorkspaceView(sampleOperationsWorkspace);

		expect(view.title).toBe('Quarterly burnout pulse');
		expect(view.subtitle).toBe('2 campaigns, 1 live');
		expect(view.surfaceLabel).toBe('Collect responses');
		expect(view.summaryRows).toEqual([
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '1' },
			{ label: 'Respondent links', value: '1' },
			{ label: 'Queued emails', value: '1' },
			{ label: 'Sent emails', value: '8' },
			{ label: 'Failed emails', value: '1' },
			{ label: 'Suppressed emails', value: '0' },
			{ label: 'Started responses', value: '36' },
			{ label: 'Draft responses', value: '5' },
			{ label: 'Submitted responses', value: '31' },
			{ label: 'Latest response activity', value: '05. 05. 2026. 12:15' },
			{ label: 'Missing prerequisites', value: '1' }
		]);
		expect(view.collectionMonitor).toEqual({
			title: 'Response monitor',
			status: 'has_submissions',
			reportVisibilityStatus: 'ready_for_aggregate_report',
			guidance: 'Enough submitted responses exist for aggregate report visibility.',
			summaryRows: [
				{ label: 'Started responses', value: '36' },
				{ label: 'Draft responses', value: '5' },
				{ label: 'Submitted responses', value: '31' },
				{ label: 'Latest started', value: '05. 05. 2026. 12:15' },
				{ label: 'Latest submitted', value: '05. 05. 2026. 12:10' }
			]
		});
		expect(view.scoreCoverageMonitor).toEqual({
			title: 'Score coverage',
			status: 'complete',
			guidance: 'All submitted responses have successful scoring activity.',
			summaryRows: [
				{ label: 'Submitted responses', value: '31' },
				{ label: 'Scored submitted', value: '31' },
				{ label: 'Unscored submitted', value: '0' },
				{ label: 'Not configured', value: '0' },
				{ label: 'Campaigns with scoring', value: '1' },
				{ label: 'Campaigns without scoring', value: '1' },
				{ label: 'Latest scoring activity', value: '05. 05. 2026. 12:20' }
			]
		});
		expect(view.selectedCampaignRows).toEqual([
			{ label: 'Selected wave', value: 'Wave 1' },
			{ label: 'Status', value: 'live' },
			{ label: 'Identity mode', value: 'anonymous repeat participation' },
			{ label: 'Locale', value: 'en' },
			{ label: 'Collection started', value: '05. 05. 2026. 10:30' },
			{ label: 'Closed', value: 'Not available' },
			{ label: 'Close reason', value: 'Not available' },
			{ label: 'Started responses', value: '36' },
			{ label: 'Draft responses', value: '5' },
			{ label: 'Submitted responses', value: '31' },
			{ label: 'Latest response activity', value: '05. 05. 2026. 12:15' },
			{ label: 'Collection status', value: 'has submissions' },
			{ label: 'Report visibility', value: 'ready for aggregate report' },
			{ label: 'Score coverage', value: 'complete' },
			{ label: 'Scored submitted', value: '31' },
			{ label: 'Unscored submitted', value: '0' },
			{ label: 'Not configured submitted', value: '0' },
			{ label: 'Latest scoring activity', value: '05. 05. 2026. 12:20' },
			{ label: 'Respondent links', value: '1' },
			{ label: 'Sent emails', value: '8' },
			{ label: 'Latest email activity', value: '05. 05. 2026. 11:30' }
		]);
		expect(view.launchSnapshotRows).toEqual([
			{ label: 'Frozen identity mode', value: 'anonymous repeat participation' },
			{ label: 'Frozen locale', value: 'en' },
			{ label: 'Template questions', value: '5' },
			{ label: 'Launched at', value: '05. 05. 2026. 10:30' }
		]);
		expect(view.missingPrerequisiteRows).toEqual([
			{
				code: 'public_entry.missing',
				label: 'Public entry',
				message: 'Create an open-link entry point before collecting anonymous responses.',
				severity: 'blocking',
				status: 'blocked'
			}
		]);
		expect(view.campaignRows).toEqual([
			{
				id: 'campaign-id',
				title: 'Wave 1',
				status: 'live',
				rows: [
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Collection started', value: '05. 05. 2026. 10:30' },
					{ label: 'Closed', value: 'Not available' },
					{ label: 'Respondent links', value: '1' },
					{ label: 'Queued emails', value: '1' },
					{ label: 'Sent emails', value: '8' },
					{ label: 'Failed emails', value: '1' },
					{ label: 'Suppressed emails', value: '0' },
					{ label: 'Latest email activity', value: '05. 05. 2026. 11:30' },
					{ label: 'Started responses', value: '36' },
					{ label: 'Draft responses', value: '5' },
					{ label: 'Submitted responses', value: '31' },
					{ label: 'Latest response activity', value: '05. 05. 2026. 12:15' },
					{ label: 'Score coverage', value: 'complete' },
					{ label: 'Unscored submitted', value: '0' }
				]
			},
			{
				id: 'campaign-draft-id',
				title: 'Draft wave',
				status: 'draft',
				rows: [
					{ label: 'Identity mode', value: 'anonymous' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Collection started', value: 'Not available' },
					{ label: 'Closed', value: 'Not available' },
					{ label: 'Respondent links', value: '0' },
					{ label: 'Queued emails', value: '0' },
					{ label: 'Sent emails', value: '0' },
					{ label: 'Failed emails', value: '0' },
					{ label: 'Suppressed emails', value: '0' },
					{ label: 'Latest email activity', value: 'Not available' },
					{ label: 'Started responses', value: '0' },
					{ label: 'Draft responses', value: '0' },
					{ label: 'Submitted responses', value: '0' },
					{ label: 'Latest response activity', value: 'Not available' },
					{ label: 'Score coverage', value: 'no submissions' },
					{ label: 'Unscored submitted', value: '0' }
				]
			}
		]);
		expect(view.emptyState).toBeNull();
		expectStatusBadgeStatus(view.campaignRows[0].status);
		expectStatusBadgeStatus(view.missingPrerequisiteRows[0].status);
	});

	it('frames operations workspace as collection progress and respondent access', () => {
		const view = toCampaignSeriesOperationsWorkspaceView(sampleOperationsWorkspace);

		expect(view.surfaceLabel).toBe('Collect responses');
		expect(view.surfaceEyebrow).toBe('Study collection');
		expect(view.surfaceDescription).toBe(
			'Start the selected wave, share respondent access, monitor submissions, and close collection when finished.'
		);
		expect(view.referenceTitle).toBe('Collection reference');
		expect(view.referenceDescription).toBe(
			'Launch records, prerequisite checks, and selected wave details stay here for review.'
		);
		expect(view.proofActionTitle).toBe('Collection actions');
		expect(view.collectionOverview).toEqual([
			{
				id: 'collection_state',
				label: 'Collection status',
				status: 'live',
				badgeLabel: 'Live',
				summary: 'Wave 1 is live',
				guidance: 'Enough submitted responses exist for aggregate report visibility.',
				detailRows: [
					{ label: 'Selected wave', value: 'Wave 1' },
					{ label: 'Status', value: 'live' },
					{ label: 'Collection started', value: '05. 05. 2026. 10:30' },
					{ label: 'Missing prerequisites', value: '1' }
				]
			},
			{
				id: 'respondent_access',
				label: 'Respondent access',
				status: 'ready',
				badgeLabel: 'Access ready',
				summary: '1 respondent link, 8 sent emails',
				guidance: 'Respondents can enter through shared links and sent emails.',
				detailRows: [
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Respondent links', value: '1' },
					{ label: 'Queued emails', value: '1' },
					{ label: 'Sent emails', value: '8' },
					{ label: 'Failed emails', value: '1' },
					{ label: 'Suppressed emails', value: '0' },
					{ label: 'Latest email activity', value: '05. 05. 2026. 11:30' }
				]
			},
			{
				id: 'response_progress',
				label: 'Response progress',
				status: 'ready',
				badgeLabel: '31 submitted',
				summary: '36 started, 5 draft, 31 submitted',
				guidance: 'Enough submitted responses exist for aggregate report visibility.',
				detailRows: [
					{ label: 'Started responses', value: '36' },
					{ label: 'Draft responses', value: '5' },
					{ label: 'Submitted responses', value: '31' },
					{ label: 'Latest started', value: '05. 05. 2026. 12:15' },
					{ label: 'Latest submitted', value: '05. 05. 2026. 12:10' }
				]
			},
			{
				id: 'score_readiness',
				label: 'Score and report readiness',
				status: 'ready',
				badgeLabel: 'Reports ready',
				summary: '31 of 31 submitted responses scored',
				guidance: 'All submitted responses have successful scoring activity.',
				detailRows: [
					{ label: 'Report visibility', value: 'ready for aggregate report' },
					{ label: 'Score coverage', value: 'complete' },
					{ label: 'Scored submitted', value: '31' },
					{ label: 'Unscored submitted', value: '0' },
					{ label: 'Not configured', value: '0' },
					{ label: 'Latest scoring activity', value: '05. 05. 2026. 12:20' }
				]
			}
		]);

		for (const item of view.collectionOverview) {
			expectStatusBadgeStatus(item.status);
		}
	});

	it('localizes operations collection read-model cards for Croatian app mode', () => {
		const view = toCampaignSeriesOperationsWorkspaceView(sampleOperationsWorkspace, 'hr-HR');

		expect(view.collectionMonitor.title).toBe('Praćenje odgovora');
		expect(view.collectionMonitor.guidance).toBe(
			'Ima dovoljno predanih odgovora za skupni prikaz rezultata.'
		);
		expect(view.collectionMonitor.summaryRows).toEqual([
			{ label: 'Započeti odgovori', value: '36' },
			{ label: 'Odgovori u tijeku', value: '5' },
	{ label: 'Predani odgovori', value: '31' },
	{ label: 'Zadnje započeto', value: '05. 05. 2026. 12:15' },
	{ label: 'Zadnje predano', value: '05. 05. 2026. 12:10' }
]);

		expect(view.collectionOverview[0]).toMatchObject({
			id: 'collection_state',
			label: 'Status prikupljanja',
			badgeLabel: 'Aktivno',
			summary: 'Wave 1: prikupljanje je aktivno.',
			guidance: 'Ima dovoljno predanih odgovora za skupni prikaz rezultata.'
		});
		expect(view.collectionOverview[1]).toMatchObject({
			id: 'respondent_access',
			label: 'Pristup ispitanika',
			badgeLabel: 'Pristup je spreman',
			summary: '1 otvorena poveznica za sudionike, 8 poslanih e-poruka',
			guidance: 'Ispitanici mogu pristupiti preko podijeljene poveznice i poslanih e-poruka.'
		});
		expect(view.collectionOverview[2]).toMatchObject({
		id: 'response_progress',
		label: 'Napredak odgovora',
		badgeLabel: 'Predano: 31 odgovor',
		summary: 'Započeto: 36; u tijeku: 5; predano: 31.',
		guidance: 'Ima dovoljno predanih odgovora za skupni prikaz rezultata.'
	});
		expect(view.collectionOverview[3]).toMatchObject({
			id: 'score_readiness',
			label: 'Spremnost rezultata i izvještaja',
			badgeLabel: 'Izvještaji su spremni',
			summary: 'Bodovano je 31 od 31 predanih odgovora.',
			guidance: 'Svi predani odgovori imaju uspješno bodovanje.'
		});

		expect(view.scoreCoverageMonitor.title).toBe('Pokrivenost bodovanja');
		expect(view.scoreCoverageMonitor.guidance).toBe(
			'Svi predani odgovori imaju uspješno bodovanje.'
		);
		expect(view.scoreCoverageMonitor.summaryRows).toContainEqual({
			label: 'Zadnja aktivnost bodovanja',
			value: '05. 05. 2026. 12:20'
		});
	});

	it('maps not-configured operations score coverage without treating it as a failure', () => {
		const view = toCampaignSeriesOperationsWorkspaceView({
			...sampleOperationsWorkspace,
			scoreCoverage: {
				submittedResponseCount: 2,
				scoredSubmittedResponseCount: 0,
				unscoredSubmittedResponseCount: 0,
				notConfiguredSubmittedResponseCount: 2,
				campaignsWithScoringRuleCount: 0,
				campaignsWithoutScoringRuleCount: 1,
				latestScoringActivityAt: null,
				status: 'not_configured',
				guidance: 'Submitted responses exist, but scoring is not configured for those campaigns.'
			}
		});

		expect(view.scoreCoverageMonitor.status).toBe('not_configured');
		expect(view.scoreCoverageMonitor.guidance).toBe(
			'Submitted responses exist, but scoring is not configured for those campaigns.'
		);
		expect(view.scoreCoverageMonitor.summaryRows).toContainEqual({
			label: 'Not configured',
			value: '2'
		});
	});

	it('maps empty operations workspace state to an empty state', () => {
		const view = toCampaignSeriesOperationsWorkspaceView({
			...sampleOperationsWorkspace,
			summary: {
				campaignCount: 0,
				liveCampaignCount: 0,
				openLinkAssignmentCount: 0,
				queuedInvitationCount: 0,
				sentInvitationCount: 0,
				failedInvitationCount: 0,
				deliveryAttemptCount: 0,
				startedResponseCount: 0,
				draftResponseCount: 0,
				submittedResponseCount: 0,
				latestResponseStartedAt: null,
				latestResponseSubmittedAt: null,
				collectionStatus: 'not_started',
				reportVisibilityStatus: 'unknown_policy',
				collectionGuidance: 'Share the public link or send invitations.',
				missingPrerequisiteCount: 5
			},
			selectedCampaign: null,
			campaigns: []
		});

		expect(view.selectedCampaignRows).toEqual([]);
		expect(view.campaignRows).toEqual([]);
		expect(view.emptyState).toEqual({
			title: 'No collection wave yet',
			message: 'Create a wave draft in Setup, then start collection here.'
		});
		expect(view.collectionOverview).toContainEqual({
			id: 'collection_state',
			label: 'Collection state',
			status: 'blocked',
			badgeLabel: 'Blocked',
			summary: 'No selected campaign is collecting responses',
			guidance: 'Create and launch a campaign before collecting responses.',
			detailRows: [
				{ label: 'Selected wave', value: 'Missing' },
				{ label: 'Status', value: 'not started' },
				{ label: 'Collection started', value: 'Not available' },
				{ label: 'Missing prerequisites', value: '5' }
			]
		});
	});

	it('keeps sample operations workspace read-only while exposing collection progress', () => {
		const view = toCampaignSeriesOperationsWorkspaceView({
			...sampleOperationsWorkspace,
			series: {
				...sampleOperationsWorkspace.series,
				studyKind: 'sample',
				isSample: true,
				sampleScenario: 'mixed_lifecycle',
				readOnlyReason: 'sample_study'
			}
		});

		expect(view.ownership).toEqual({
			label: 'Sample study',
			badgeStatus: 'demo',
			isSample: true,
			sampleScenario: 'mixed_lifecycle',
			readOnlyReason: 'sample_study',
			readOnlyMessage:
				'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
		});
		expect(view.canMutate).toBe(false);
		expect(view.readOnlyMessage).toBe(
			'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
		);
		expect(view.collectionOverview.map((item) => item.id)).toEqual([
			'collection_state',
			'respondent_access',
			'response_progress',
			'score_readiness'
		]);
	});

	it('maps reports workspace state to summary, selected campaign, provenance, prerequisites, and campaign rows', () => {
		const view = toCampaignSeriesReportsWorkspaceView(sampleReportsWorkspace);

		expect(view.title).toBe('Quarterly burnout pulse');
		expect(view.subtitle).toBe('2 campaigns, 1 live');
		expect(view.surfaceLabel).toBe('Review results');
		expect(view.surfaceEyebrow).toBe('Study results');
		expect(view.surfaceDescription).toBe(
			'Review result availability, coverage, limitations, and export next use for the selected campaign.'
		);
		expect(view.referenceTitle).toBe('Results reference');
		expect(view.referenceDescription).toBe(
			'Selected wave details, limitations, prerequisite checks, and export records stay here for review.'
		);
		expect(view.resultsOverview).toEqual([
			{
				id: 'result_availability',
				label: 'Result availability',
				status: 'proof_only',
				badgeLabel: 'Preview',
				summary: 'Wave 1 has preview results from 31 submitted responses.',
				guidance:
					'Use this as a preview of current findings until scoring coverage, disclosure, and finality are complete.',
				detailRows: [
					{ label: 'Selected wave', value: 'Wave 1' },
					{ label: 'Report status', value: 'preview' },
					{ label: 'Reportable campaigns', value: '1' },
					{ label: 'Submitted responses', value: '31' },
					{ label: 'Missing prerequisites', value: '1' }
				]
			},
			{
				id: 'coverage_visibility',
				label: 'Coverage and visibility',
				status: 'pending',
				badgeLabel: '28 of 31 scored',
				summary: '25 visible scores, 3 suppressed scores, 3 submitted responses still unscored.',
				guidance: 'Review visible score coverage before treating the result set as complete.',
				detailRows: [
					{ label: 'Submitted responses', value: '31' },
					{ label: 'Scored submitted', value: '28' },
					{ label: 'Unscored submitted', value: '3' },
					{ label: 'Visible scores', value: '25' },
					{ label: 'Suppressed scores', value: '3' },
					{ label: 'Disclosure', value: 'visible' },
					{ label: 'Disclosure k', value: '5' }
				]
			},
			{
				id: 'limitations_finality',
				label: 'Limitations and finality',
				status: 'live',
				badgeLabel: 'Live data',
				summary:
					'Results are from a live campaign with not validated interpretation and no closed-wave finality yet.',
				guidance:
					'Label this as preliminary until the wave is closed and interpretation posture is reviewed.',
				detailRows: [
					{ label: 'Campaign status', value: 'live' },
					{ label: 'Data finality', value: 'preliminary live' },
					{ label: 'Preliminary live reports', value: '1' },
					{ label: 'Closed-wave reports', value: '0' },
					{ label: 'Interpretation', value: 'not validated interpretation' }
				]
			},
			{
				id: 'export_next_use',
				label: 'Export next use',
				status: 'ready',
				badgeLabel: '2 files',
				summary: 'Latest export report-proof.csv is downloadable.',
				guidance:
					'Download the latest export file for handoff, or create a fresh export after results change.',
				detailRows: [
					{ label: 'Export files', value: '2' },
					{ label: 'Latest export file', value: 'report-proof.csv' },
					{ label: 'Latest export status', value: 'succeeded' },
					{ label: 'Latest export downloadable', value: 'Yes' }
				]
			}
		]);
		expect(view.summaryRows).toEqual([
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '1' },
			{ label: 'Reportable campaigns', value: '1' },
			{ label: 'Submitted responses', value: '31' },
			{ label: 'Scores', value: '28' },
			{ label: 'Visible scores', value: '25' },
			{ label: 'Suppressed scores', value: '3' },
			{ label: 'Export files', value: '2' },
			{ label: 'Missing prerequisites', value: '1' }
		]);
		expect(view.selectedCampaignRows).toEqual([
			{ label: 'Selected wave', value: 'Wave 1' },
			{ label: 'Status', value: 'live' },
			{ label: 'Identity mode', value: 'anonymous repeat participation' },
			{ label: 'Locale', value: 'en' },
			{ label: 'Disclosure', value: 'visible' },
			{ label: 'Disclosure k', value: '5' },
			{ label: 'Report status', value: 'preview' },
			{ label: 'Interpretation', value: 'not validated interpretation' },
			{ label: 'Submitted responses', value: '31' },
			{ label: 'Scores', value: '28' },
			{ label: 'Visible scores', value: '25' },
			{ label: 'Suppressed scores', value: '3' },
			{ label: 'Export files', value: '2' }
		]);
		expect(view.provenanceRows).toEqual([
			{ label: 'Launch snapshot', value: 'launch-snapshot-id', mono: true },
			{ label: 'Latest launch', value: '05. 05. 2026. 10:30' },
			{ label: 'Scoring rule', value: 'scoring-rule-id', mono: true },
			{ label: 'Consent document', value: 'consent-id', mono: true },
			{ label: 'Retention policy', value: 'retention-id', mono: true },
			{ label: 'Disclosure policy', value: 'disclosure-id', mono: true },
			{ label: 'Latest export record', value: 'export-artifact-id', mono: true },
			{ label: 'Latest export file', value: 'report-proof.csv' },
			{ label: 'Latest export status', value: 'succeeded' },
			{ label: 'Latest export created', value: '05. 05. 2026. 11:00' },
			{ label: 'Latest export completed', value: '05. 05. 2026. 11:00' },
			{ label: 'Latest export started', value: 'Not available' },
			{ label: 'Latest export failed', value: 'Not available' },
			{ label: 'Latest export expires', value: 'Not available' },
			{ label: 'Latest export deleted', value: 'Not available' },
			{ label: 'Latest export failure reason', value: 'Not available' },
			{ label: 'Latest export downloadable', value: 'Yes' }
		]);
		expect(view.missingPrerequisiteRows).toEqual([
			{
				code: 'export_artifact.missing',
				label: 'Export file',
				message: 'Create a report preview export before handoff.',
				severity: 'advisory',
				status: 'pending'
			}
		]);
		expect(view.campaignRows).toEqual([
			{
				id: 'campaign-id',
				title: 'Wave 1',
				status: 'proof_only',
				rows: [
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Launch snapshot', value: 'launch-snapshot-id', mono: true },
					{ label: 'Latest launch', value: '05. 05. 2026. 10:30' },
					{ label: 'Submitted responses', value: '31' },
					{ label: 'Scores', value: '28' },
					{ label: 'Visible scores', value: '25' },
					{ label: 'Suppressed scores', value: '3' },
					{ label: 'Disclosure', value: 'visible' },
					{ label: 'Report status', value: 'preview' },
					{ label: 'Export files', value: '2' },
					{ label: 'Latest export', value: 'report-proof.csv' }
				]
			},
			{
				id: 'campaign-draft-id',
				title: 'Draft wave',
				status: 'blocked',
				rows: [
					{ label: 'Identity mode', value: 'anonymous' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Launch snapshot', value: 'Not available' },
					{ label: 'Latest launch', value: 'Not available' },
					{ label: 'Submitted responses', value: '0' },
					{ label: 'Scores', value: '0' },
					{ label: 'Visible scores', value: '0' },
					{ label: 'Suppressed scores', value: '0' },
					{ label: 'Disclosure', value: 'not available' },
					{ label: 'Report status', value: 'blocked' },
					{ label: 'Export files', value: '0' },
					{ label: 'Latest export', value: 'Not available' }
				]
			}
		]);
		expect(view.emptyState).toBeNull();
		expectStatusBadgeStatus(view.campaignRows[0].status);
		expectStatusBadgeStatus(view.missingPrerequisiteRows[0].status);
		expect(view.scoreCoverageSignal).toEqual({
			title: 'Score coverage',
			status: 'partial',
			guidance:
				'Some submitted responses still need scoring activity before score-dependent reports are complete.',
			summaryRows: [
				{ label: 'Submitted responses', value: '31' },
				{ label: 'Scored submitted', value: '28' },
				{ label: 'Unscored submitted', value: '3' },
				{ label: 'Not configured', value: '0' },
				{ label: 'Latest scoring activity', value: '05. 05. 2026. 12:20' }
			]
		});
	});

	it('maps empty reports workspace state to an empty state', () => {
		const view = toCampaignSeriesReportsWorkspaceView({
			...sampleReportsWorkspace,
			summary: {
				campaignCount: 0,
				liveCampaignCount: 0,
				reportableCampaignCount: 0,
				submittedResponseCount: 0,
				scoreCount: 0,
				exportArtifactCount: 0,
				visibleScoreCount: 0,
				suppressedScoreCount: 0,
				missingPrerequisiteCount: 5
			},
			selectedCampaign: null,
			campaigns: []
		});

		expect(view.selectedCampaignRows).toEqual([]);
		expect(view.provenanceRows).toEqual([]);
		expect(view.campaignRows).toEqual([]);
		expect(view.emptyState).toEqual({
			title: 'No reportable campaigns yet',
			message: 'Submit responses and compute scores before report previews are available.'
		});
	});

	it('surfaces reports finality and closed-wave lifecycle metadata when present', () => {
		const closedCampaign = {
			...sampleReportsWorkspace.selectedCampaign!,
			status: 'closed',
			closedAt: '2026-05-11T10:15:00Z',
			closedByUserId: 'owner-user-id',
			closeReason: 'Owner closed collection',
			dataFinality: 'closed_wave'
		};
		const view = toCampaignSeriesReportsWorkspaceView({
			...sampleReportsWorkspace,
			summary: {
				...sampleReportsWorkspace.summary,
				liveCampaignCount: 0,
				preliminaryLiveReportCount: 0,
				closedWaveReportCount: 1
			},
			selectedCampaign: closedCampaign,
			campaigns: [closedCampaign]
		});

		expect(view.summaryRows).toContainEqual({ label: 'Preliminary live reports', value: '0' });
		expect(view.summaryRows).toContainEqual({ label: 'Closed-wave reports', value: '1' });
		expect(view.selectedCampaignRows).toContainEqual({
			label: 'Data finality',
			value: 'closed wave'
		});
		expect(view.provenanceRows).toEqual(
			expect.arrayContaining([
					{ label: 'Closed at', value: '11. 05. 2026. 12:15' },
				{ label: 'Closed by', value: 'owner-user-id', mono: true },
				{ label: 'Close reason', value: 'Owner closed collection' }
			])
		);
		expect(view.campaignRows[0].rows).toContainEqual({
			label: 'Data finality',
			value: 'closed wave'
		});
		expect(view.resultsOverview).toContainEqual({
			id: 'limitations_finality',
			label: 'Limitations and finality',
			status: 'closed',
			badgeLabel: 'Closed wave',
			summary:
				'Results are from a closed campaign with not validated interpretation and 1 closed-wave report.',
			guidance:
				'Closed-wave data is more stable, but interpretation labels still need the recorded validation posture.',
			detailRows: [
				{ label: 'Campaign status', value: 'closed' },
				{ label: 'Data finality', value: 'closed wave' },
				{ label: 'Preliminary live reports', value: '0' },
				{ label: 'Closed-wave reports', value: '1' },
				{ label: 'Interpretation', value: 'not validated interpretation' }
			]
		});
	});

	it('maps empty reports workspace state to blocked result overview guidance', () => {
		const view = toCampaignSeriesReportsWorkspaceView({
			...sampleReportsWorkspace,
			summary: {
				campaignCount: 0,
				liveCampaignCount: 0,
				reportableCampaignCount: 0,
				submittedResponseCount: 0,
				scoreCount: 0,
				exportArtifactCount: 0,
				visibleScoreCount: 0,
				suppressedScoreCount: 0,
				missingPrerequisiteCount: 5,
				preliminaryLiveReportCount: 0,
				closedWaveReportCount: 0
			},
			selectedCampaign: null,
			campaigns: [],
			scoreCoverage: null
		});

		expect(view.resultsOverview).toEqual([
			{
				id: 'result_availability',
				label: 'Result availability',
				status: 'blocked',
				badgeLabel: 'Blocked',
				summary: 'No selected campaign has reportable results.',
				guidance: 'Submit responses and compute scores before reviewing findings.',
				detailRows: [
					{ label: 'Selected wave', value: 'Missing' },
					{ label: 'Report status', value: 'blocked' },
					{ label: 'Reportable campaigns', value: '0' },
					{ label: 'Submitted responses', value: '0' },
					{ label: 'Missing prerequisites', value: '5' }
				]
			},
			{
				id: 'coverage_visibility',
				label: 'Coverage and visibility',
				status: 'empty',
				badgeLabel: 'No scores',
				summary: '0 visible scores, 0 suppressed scores, 0 submitted responses still unscored.',
				guidance: 'Collect submitted responses and compute scores before assessing coverage.',
				detailRows: [
					{ label: 'Submitted responses', value: '0' },
					{ label: 'Scored submitted', value: '0' },
					{ label: 'Unscored submitted', value: '0' },
					{ label: 'Visible scores', value: '0' },
					{ label: 'Suppressed scores', value: '0' },
					{ label: 'Disclosure', value: 'not available' },
					{ label: 'Disclosure k', value: 'Not configured' }
				]
			},
			{
				id: 'limitations_finality',
				label: 'Limitations and finality',
				status: 'blocked',
				badgeLabel: 'No finality',
				summary: 'No selected campaign has final report state yet.',
				guidance: 'Launch, collect, score, and close a campaign before relying on report finality.',
				detailRows: [
					{ label: 'Campaign status', value: 'Missing' },
					{ label: 'Data finality', value: 'not reportable' },
					{ label: 'Preliminary live reports', value: '0' },
					{ label: 'Closed-wave reports', value: '0' },
					{ label: 'Interpretation', value: 'not available' }
				]
			},
			{
				id: 'export_next_use',
				label: 'Export next use',
				status: 'blocked',
				badgeLabel: 'No exports',
				summary: 'No report export file is available yet.',
				guidance: 'Create an export after report results become available.',
				detailRows: [
					{ label: 'Export files', value: '0' },
					{ label: 'Latest export file', value: 'Not available' },
					{ label: 'Latest export status', value: 'Not available' },
					{ label: 'Latest export downloadable', value: 'No' }
				]
			}
		]);
		expect(view.missingPrerequisiteRows).toEqual([
			{
				code: 'export_artifact.missing',
				label: 'Export file',
				message: 'Create a report preview export before handoff.',
				severity: 'advisory',
				status: 'pending'
			}
		]);
	});

	it('keeps sample reports workspace read-only while deriving results overview', () => {
		const view = toCampaignSeriesReportsWorkspaceView({
			...sampleReportsWorkspace,
			series: {
				...sampleReportsWorkspace.series,
				studyKind: 'sample',
				isSample: true,
				sampleScenario: 'completed',
				readOnlyReason: 'sample_study'
			}
		});

		expect(view.ownership).toEqual({
			label: 'Sample study',
			badgeStatus: 'demo',
			isSample: true,
			sampleScenario: 'completed',
			readOnlyReason: 'sample_study',
			readOnlyMessage:
				'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
		});
		expect(view.canMutate).toBe(false);
		expect(view.readOnlyMessage).toBe(
			'Results sample: read-only starter content showing collected responses, scores, reports, and exports.'
		);
		expect(view.resultsOverview.map((item) => item.id)).toEqual([
			'result_availability',
			'coverage_visibility',
			'limitations_finality',
			'export_next_use'
		]);
	});

	it('maps waves workspace state to summary, selected waves, comparison, provenance, prerequisites, and wave rows', () => {
		const view = toCampaignSeriesWavesWorkspaceView(sampleWavesWorkspace);

		expect(view.title).toBe('Quarterly burnout pulse');
		expect(view.subtitle).toBe('2 campaigns, 2 live');
		expect(view.surfaceLabel).toBe('Compare rounds');
		expect(view.summaryRows).toEqual([
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '2' },
			{ label: 'Repeat-participation waves', value: '2' },
			{ label: 'Submitted waves', value: '2' },
			{ label: 'Linked repeat responses', value: '6' },
			{ label: 'Complete repeat-response pairs', value: '6' },
			{ label: 'Comparable scores', value: '1' },
			{ label: 'Visible comparisons', value: '1' },
			{ label: 'Suppressed comparisons', value: '0' },
			{ label: 'Blocked comparisons', value: '0' },
			{ label: 'Missing prerequisites', value: '0' }
		]);
		expect(view.selectedWaveRows).toEqual([
			{ label: 'Baseline wave', value: 'Wave 1' },
			{ label: 'Comparison wave', value: 'Wave 2' },
			{ label: 'Comparison status', value: 'preview' },
			{ label: 'Disclosure', value: 'visible' },
			{ label: 'Compatibility', value: 'compatible' },
			{ label: 'Interpretation', value: 'not validated interpretation' },
			{ label: 'Disclosure k', value: '5' },
			{ label: 'Linked pairs', value: '6' },
			{ label: 'Visible scores', value: '1' },
			{ label: 'Suppressed scores', value: '0' },
			{ label: 'Blocked scores', value: '0' }
		]);
		expect(view.provenanceRows).toEqual([
			{ label: 'Baseline launch snapshot', value: 'wave-1-launch-id', mono: true },
			{ label: 'Baseline latest launch', value: '05. 05. 2026. 10:30' },
			{ label: 'Baseline scoring rule', value: 'burnout.total' },
			{ label: 'Baseline disclosure policy', value: 'wave-1-disclosure-id', mono: true },
			{ label: 'Comparison launch snapshot', value: 'wave-2-launch-id', mono: true },
			{ label: 'Comparison latest launch', value: '12. 05. 2026. 10:30' },
			{ label: 'Comparison scoring rule', value: 'burnout.total' },
			{ label: 'Comparison disclosure policy', value: 'wave-2-disclosure-id', mono: true }
		]);
		expect(view.missingPrerequisiteRows).toEqual([]);
		expect(view.campaignRows).toEqual([
			{
				id: 'wave-1-id',
				title: 'Wave 1',
				status: 'live',
				rows: [
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Wave state', value: 'wave' },
					{ label: 'Launch snapshot', value: 'wave-1-launch-id', mono: true },
					{ label: 'Latest launch', value: '05. 05. 2026. 10:30' },
					{ label: 'Scoring rule', value: 'burnout.total' },
					{ label: 'Disclosure k', value: '5' },
					{ label: 'Submitted responses', value: '6' },
					{ label: 'Scores', value: '6' },
					{ label: 'Linked repeat responses', value: '6' }
				]
			},
			{
				id: 'wave-2-id',
				title: 'Wave 2',
				status: 'live',
				rows: [
					{ label: 'Identity mode', value: 'anonymous repeat participation' },
					{ label: 'Locale', value: 'en' },
					{ label: 'Wave state', value: 'wave' },
					{ label: 'Launch snapshot', value: 'wave-2-launch-id', mono: true },
					{ label: 'Latest launch', value: '12. 05. 2026. 10:30' },
					{ label: 'Scoring rule', value: 'burnout.total' },
					{ label: 'Disclosure k', value: '5' },
					{ label: 'Submitted responses', value: '6' },
					{ label: 'Scores', value: '6' },
					{ label: 'Linked repeat responses', value: '6' }
				]
			}
		]);
		expect(view.emptyState).toBeNull();
		expectStatusBadgeStatus(view.campaignRows[0].status);
	});

	it('surfaces waves finality and closed-wave lifecycle metadata when present', () => {
		const baselineWave = {
			...sampleWavesWorkspace.selectedBaselineWave!,
			status: 'closed',
			closedAt: '2026-05-11T10:20:00Z',
			closedByUserId: 'owner-user-id',
			closeReason: 'Baseline complete',
			dataFinality: 'closed_wave'
		};
		const comparisonWave = {
			...sampleWavesWorkspace.selectedComparisonWave!,
			dataFinality: 'preliminary_live'
		};
		const view = toCampaignSeriesWavesWorkspaceView({
			...sampleWavesWorkspace,
			summary: {
				...sampleWavesWorkspace.summary,
				liveCampaignCount: 1,
				preliminaryLiveWaveCount: 1,
				closedWaveCount: 1
			},
			selectedBaselineWave: baselineWave,
			selectedComparisonWave: comparisonWave,
			waves: [baselineWave, comparisonWave]
		});

		expect(view.summaryRows).toContainEqual({ label: 'Preliminary live waves', value: '1' });
		expect(view.summaryRows).toContainEqual({ label: 'Closed waves', value: '1' });
		expect(view.selectedWaveRows).toEqual(
			expect.arrayContaining([
				{ label: 'Baseline finality', value: 'closed wave' },
				{ label: 'Comparison finality', value: 'preliminary live' }
			])
		);
		expect(view.provenanceRows).toEqual(
			expect.arrayContaining([
					{ label: 'Baseline closed at', value: '11. 05. 2026. 12:20' },
				{ label: 'Baseline closed by', value: 'owner-user-id', mono: true },
				{ label: 'Baseline close reason', value: 'Baseline complete' }
			])
		);
		expect(view.campaignRows[0].rows).toContainEqual({
			label: 'Data finality',
			value: 'closed wave'
		});
	});

	it('maps empty waves workspace state to an empty state', () => {
		const view = toCampaignSeriesWavesWorkspaceView({
			...sampleWavesWorkspace,
			summary: {
				campaignCount: 0,
				liveCampaignCount: 0,
				longitudinalWaveCount: 0,
				submittedWaveCount: 0,
				linkedTrajectoryCount: 0,
				completeTrajectoryCount: 0,
				comparableScoreCount: 0,
				visibleComparisonCount: 0,
				suppressedComparisonCount: 0,
				blockedComparisonCount: 0,
				missingPrerequisiteCount: 5
			},
			selectedBaselineWave: null,
			selectedComparisonWave: null,
			waves: []
		});

		expect(view.selectedWaveRows).toEqual([]);
		expect(view.provenanceRows).toEqual([]);
		expect(view.campaignRows).toEqual([]);
		expect(view.emptyState).toEqual({
			title: 'No repeated rounds yet',
			message: 'Create and launch at least two collection rounds before comparing results over time.'
		});
	});

	it('maps reports selected-series surface with score and export counts', () => {
		const view = toSelectedSeriesSurfaceView(sampleCampaignSeriesHub, 'reports');

		expect(view.surfaceLabel).toBe('Review results');
		expect(view.summaryRows).toEqual([
			{ label: 'Submitted responses', value: '31' },
			{ label: 'Scores', value: '28' },
			{ label: 'Export files', value: '3' }
		]);
		expect(view.campaignRows[0].rows).toContainEqual({ label: 'Scores', value: '28' });
		expect(view.campaignRows[0].rows).toContainEqual({ label: 'Export files', value: '3' });
	});

	it('maps waves selected-series surface with wave identity posture', () => {
		const view = toSelectedSeriesSurfaceView(sampleCampaignSeriesHub, 'waves');

		expect(view.surfaceLabel).toBe('Compare rounds');
		expect(view.summaryRows).toEqual([
			{ label: 'Campaigns', value: '2' },
			{ label: 'Live campaigns', value: '1' },
			{ label: 'Submitted responses', value: '31' }
		]);
		expect(view.campaignRows[0].rows).toContainEqual({
			label: 'Identity mode',
			value: 'anonymous repeat participation'
		});
	});

	it('maps empty selected-series child surfaces to route-specific empty state', () => {
		const view = toSelectedSeriesSurfaceView(
			{
				...sampleCampaignSeriesHub,
				totals: {
					campaignCount: 0,
					liveCampaignCount: 0,
					submittedResponseCount: 0,
					scoreCount: 0,
					exportArtifactCount: 0
				},
				campaigns: []
			},
			'waves'
		);

		expect(view.emptyState).toEqual({
			title: 'No repeated rounds yet',
			message: 'Create and launch at least two collection rounds before comparing results over time.'
		});
		expect(view.campaignRows).toEqual([]);
	});

	it('maps report proof disclosure into table-ready score rows', () => {
		const view = toReportProofView(sampleReportProof);

		expect(view.proofStatus).toBe('ready');
		expect(view.provenance).toEqual([
			{ label: 'Launch snapshot', value: 'snapshot-id', mono: true },
			{ label: 'Scoring rule', value: 'scoring-rule-id', mono: true },
			{ label: 'Disclosure k', value: '5' },
			{ label: 'Interpretation', value: 'not_validated_interpretation' }
		]);
		expect(view.summary).toEqual({
			title: 'Preliminary aggregate summary',
			headline: '1 visible score row across up to 8 submitted responses.',
			detail:
				'1 score row is suppressed by disclosure guardrails. Interpretation posture: not validated interpretation.',
			metrics: [
				{ label: 'Visible score rows', value: '1' },
				{ label: 'Suppressed score rows', value: '1' },
				{ label: 'Submitted responses', value: '8' },
				{ label: 'Data finality', value: 'preliminary live' }
			],
			guardrails: [
				'Aggregate only',
				'Disclosure guardrails still apply',
				'Interpretation labels are tenant-attested or not reviewed unless explicitly approved.'
			]
		});
		expect(view.scoreRows).toEqual([
			{
				dimensionCode: 'exhaustion',
				disclosureState: 'visible',
				submittedResponseCount: 8,
				scoreCount: '8',
				scoreMetadata: 'n 7/8 / ok',
				mean: '3.42',
				range: '1.00-5.00',
				interpretationLabel: 'Tenant middle range',
				interpretationMeta: 'tenant attested / tenant defined / not reviewed / not official',
				note: null
			},
			{
				dimensionCode: 'distance',
				disclosureState: 'suppressed',
				submittedResponseCount: 3,
				scoreCount: 'Suppressed',
				scoreMetadata: null,
				mean: 'Suppressed',
				range: 'Suppressed',
				interpretationLabel: null,
				interpretationMeta: null,
				note: 'below_k_min'
			}
		]);
	});

	it('summarizes fully suppressed report proof without exposing score values', () => {
		const view = toReportProofView({
			...sampleReportProof,
			scores: sampleReportProof.scores.map((score) => ({
				...score,
				disclosure: 'suppressed',
				scoreCount: null,
				mean: null,
				min: null,
				max: null,
				suppressionReason: score.suppressionReason ?? 'below_k_min',
				interpretation: null
			}))
		});

		expect(view.summary.headline).toBe('All 2 score rows are suppressed by disclosure guardrails.');
		expect(view.summary.detail).toBe(
			'No visible aggregate score values are shown until disclosure requirements are met. Interpretation posture: not validated interpretation.'
		);
		expect(view.summary.metrics).toContainEqual({ label: 'Submitted responses', value: '8' });
		expect(view.scoreRows.every((row) => row.mean === 'Suppressed')).toBe(true);
	});

	it('surfaces report proof finality provenance when present', () => {
		const view = toReportProofView({
			...sampleReportProof,
			closedAt: '2026-05-11T10:15:00Z',
			dataFinality: 'closed_wave'
		});

		expect(view.provenance).toEqual(
			expect.arrayContaining([
					{ label: 'Closed at', value: '11. 05. 2026. 12:15' },
				{ label: 'Data finality', value: 'closed wave' }
			])
		);
	});

	it('maps export file responses to display summaries', () => {
		const artifact: ReportProofExportArtifactResponse = {
			id: 'artifact-id',
			targetKind: 'campaign',
			targetId: 'campaign-id',
			targetLabel: 'Campaign',
			campaignId: 'campaign-id',
			campaignSeriesId: 'series-id',
			artifactType: 'campaign_report_proof',
			status: 'completed',
			format: 'csv',
			fileName: 'campaign-report-proof.csv',
			contentType: 'text/csv',
			rowCount: 12,
			byteSize: 1280,
			checksumSha256: 'a'.repeat(64),
			createdAt: '2026-05-08T10:00:00Z',
			completedAt: '2026-05-08T10:00:05Z',
			startedAt: null,
			failedAt: null,
			expiresAt: null,
			deletedAt: null,
			failureReasonCode: null,
			canDownload: true,
			csvContent: 'campaign_id,dimension_code',
			codebookJson: '{}'
		};
		const download: ExportArtifactDownloadResponse = {
			artifactId: 'artifact-id',
			fileName: 'campaign-report-proof.csv',
			contentType: 'text/csv',
			byteSize: 1280,
			content: 'campaign_id,dimension_code'
		};

		expect(toExportArtifactView(artifact, download)).toEqual({
			id: 'artifact-id',
			fileName: 'campaign-report-proof.csv',
			status: 'completed',
			format: 'csv',
			rowCount: '12 rows',
			byteSize: '1.3 KB',
			checksum: 'aaaaaaaaaaaa...',
			download: {
				fileName: 'campaign-report-proof.csv',
				contentType: 'text/csv',
				byteSize: '1.3 KB'
			}
		});
	});

	it('maps export file library summary and cards', () => {
		const library: ExportArtifactLibraryResponse = {
			tenantId: 'tenant-id',
			summary: {
				totalCount: 2,
				downloadableCount: 1,
				failedCount: 1,
				pendingCount: 0
			},
			artifacts: [
				{
					id: 'artifact-id-1',
					targetKind: 'campaign',
					targetId: 'campaign-id',
					targetLabel: 'Baseline wave',
					campaignId: 'campaign-id',
					campaignName: 'Baseline wave',
					artifactType: 'report_proof_csv_codebook',
					status: 'succeeded',
					format: 'csv_codebook',
					fileName: 'baseline-report.csv',
					rowCount: 12,
					byteSize: 2048,
					checksumSha256: 'a'.repeat(64),
					createdAt: '2026-05-16T08:00:00Z',
					completedAt: '2026-05-16T08:00:03Z',
					startedAt: '2026-05-16T08:00:00Z',
					failedAt: null,
					expiresAt: null,
					deletedAt: null,
					failureReasonCode: null,
					canDownload: true,
					campaignStatus: 'closed',
					campaignClosedAt: '2026-05-16T09:00:00Z',
					dataFinality: 'closed_wave'
				},
				{
					id: 'artifact-id-2',
					targetKind: 'campaign_series',
					targetId: 'series-id',
					targetLabel: 'Response study',
					campaignId: null,
					campaignName: null,
					artifactType: 'campaign_series_response_csv_codebook',
					status: 'failed',
					format: 'csv_codebook',
					fileName: 'responses.csv',
					rowCount: 0,
					byteSize: 0,
					checksumSha256: null,
					createdAt: '2026-05-16T07:00:00Z',
					completedAt: null,
					startedAt: '2026-05-16T07:00:01Z',
					failedAt: '2026-05-16T07:00:02Z',
					expiresAt: null,
					deletedAt: null,
					failureReasonCode: 'export.failed',
					canDownload: false,
					campaignStatus: null,
					campaignClosedAt: null,
					dataFinality: null
				}
			]
		};

		const view = toExportArtifactLibraryView(library);

		expect(view.surfaceTitle).toBe('Use exports');
		expect(view.surfaceEyebrow).toBe('Study support');
		expect(view.surfaceDescription).toBe(
			'Find generated CSV/codebook files by purpose, readiness, source study, and next use.'
		);
		expect(view.referenceTitle).toBe('Export reference');
		expect(view.referenceDescription).toBe(
			'File metadata, lifecycle timestamps, failure codes, and download availability stay available for audit and troubleshooting.'
		);
		expect(view.exportOverview).toEqual([
			{
				id: 'ready_downloads',
				label: 'Downloadable files',
				status: 'ready',
				badgeLabel: '1 downloadable',
				summary: '1 export file is ready to download.',
				guidance:
					'Use response dataset exports for analysis handoff. Use results matrix exports for aggregate review, group comparison, measurement comparison, or codebook checks.',
				detailRows: [
					{ label: 'Export files', value: '2' },
					{ label: 'Downloadable', value: '1' },
					{ label: 'Results matrix exports', value: '1' },
					{ label: 'Response datasets', value: '1' }
				]
			},
			{
				id: 'attention_needed',
				label: 'Needs attention',
				status: 'failed',
				badgeLabel: '1 failed',
				summary: '1 export file needs attention.',
				guidance:
					'Review the failed export file, then recreate it from the source study after the cause is resolved.',
				detailRows: [
					{ label: 'Failed', value: '1' },
					{ label: 'Pending', value: '0' }
				]
			},
			{
				id: 'artifact_purpose',
				label: 'File purpose',
				status: 'ready',
				badgeLabel: '2 purposes',
				summary: 'Exports cover Results matrix export and Response dataset export.',
				guidance:
					'Choose results matrix exports for aggregate review; choose response dataset exports for analysis with the codebook.',
				detailRows: [
					{ label: 'Results matrix exports', value: '1' },
					{ label: 'Response dataset exports', value: '1' }
				]
			},
			{
				id: 'study_context',
				label: 'Study context and next use',
				status: 'ready',
				badgeLabel: '2 sources',
				summary: 'Export files are tied to Baseline wave and Response study.',
				guidance:
					'Open the source study or report context when you need to understand how an export file was generated.',
				detailRows: [
					{ label: 'Campaign files', value: '1' },
					{ label: 'Study files', value: '1' }
				]
			}
		]);
		expect(view.metricRows).toEqual([
			{ label: 'Export files', value: '2' },
			{ label: 'Downloadable', value: '1' },
			{ label: 'Failed', value: '1' },
			{ label: 'Pending', value: '0' }
		]);
		expect(view.cards[0]).toEqual({
			id: 'artifact-id-1',
			title: 'baseline-report.csv',
			subtitle: 'Baseline wave',
			purposeLabel: 'Results matrix export',
			finalityLabel: 'Closed wave',
			nextUse:
				'Use this export for aggregate results review, group comparison, measurement comparison, or codebook checks.',
			status: 'ready',
			statusLabel: 'Succeeded',
			href: null,
			rows: [
				{ label: 'Study context', value: 'Campaign / Baseline wave' },
				{ label: 'File type', value: 'Results matrix CSV and codebook' },
				{ label: 'Format', value: 'CSV codebook' },
				{ label: 'Data finality', value: 'Closed wave' },
				{ label: 'Rows', value: '12' },
				{ label: 'Size', value: '2.0 KB' },
				{ label: 'Created', value: '16. 05. 2026. 10:00' },
				{ label: 'Completed', value: '16. 05. 2026. 10:00' },
				{ label: 'Download', value: 'Available' }
			]
		});
		expect(view.cards[1]).toMatchObject({
			purposeLabel: 'Response dataset export',
			finalityLabel: 'Not tied to a closed wave',
			nextUse: 'Use this export for response-level analysis with the generated codebook.'
		});
		expect(view.cards[1].status).toBe('failed');
		expect(view.cards[1].statusLabel).toBe('Failed');
		expect(view.cards[1].href).toBe('/app/campaign-series/series-id/reports');
		expect(view.cards[1].rows).toContainEqual({ label: 'Failure', value: 'export.failed' });
		expectStatusBadgeStatus(view.cards[0].status);
		expectStatusBadgeStatus(view.cards[1].status);
	});

	it('maps empty export file library to purpose-first blocked guidance', () => {
		const view = toExportArtifactLibraryView({
			tenantId: 'tenant-id',
			summary: {
				totalCount: 0,
				downloadableCount: 0,
				failedCount: 0,
				pendingCount: 0
			},
			artifacts: []
		});

		expect(view.exportOverview).toEqual([
			{
				id: 'ready_downloads',
				label: 'Downloadable files',
				status: 'empty',
				badgeLabel: '0 downloadable',
				summary: 'No export files are ready to download yet.',
				guidance: 'Create an export from a study results page after results are available.',
				detailRows: [
					{ label: 'Export files', value: '0' },
					{ label: 'Downloadable', value: '0' },
					{ label: 'Results matrix exports', value: '0' },
					{ label: 'Response datasets', value: '0' }
				]
			},
			{
				id: 'attention_needed',
				label: 'Needs attention',
				status: 'ready',
				badgeLabel: 'No attention items',
				summary: 'No failed or pending export files.',
				guidance: 'New export issues will appear here when generation fails or remains pending.',
				detailRows: [
					{ label: 'Failed', value: '0' },
					{ label: 'Pending', value: '0' }
				]
			},
			{
				id: 'artifact_purpose',
				label: 'File purpose',
				status: 'empty',
				badgeLabel: 'No files',
				summary: 'No generated export purposes are available yet.',
				guidance:
					'Create results matrix or response dataset exports from a study when results are ready.',
				detailRows: [
					{ label: 'Results matrix exports', value: '0' },
					{ label: 'Response dataset exports', value: '0' }
				]
			},
			{
				id: 'study_context',
				label: 'Study context and next use',
				status: 'empty',
				badgeLabel: 'No sources',
				summary: 'No export files are tied to a study yet.',
				guidance:
					'Generated export files will link back to their study or report context when that context is available.',
				detailRows: [
					{ label: 'Campaign files', value: '0' },
					{ label: 'Study files', value: '0' }
				]
			}
		]);
		expect(view.cards).toEqual([]);
	});

	it('maps wave comparison response to baseline comparison and delta rows', () => {
		const view = toWaveComparisonView(sampleWaveComparison);

		expect(view.summaryRows).toEqual([
			{ label: 'Baseline wave', value: 'Wave 1' },
			{ label: 'Comparison wave', value: 'Wave 2' },
			{ label: 'Disclosure k', value: '5' },
			{ label: 'Interpretation', value: 'not_validated_interpretation' }
		]);
		expect(view.scoreRows).toEqual([
			{
				dimensionCode: 'exhaustion',
				disclosureState: 'visible',
				compatibilityStatus: 'compatible',
				baselineMean: '3.90',
				comparisonMean: '4.20',
				aggregateDelta: '+0.30',
				pairedDeltaMean: '+0.25',
				linkedPairCount: 6,
				baselineScoreMetadata: 'n 7/8 / ok',
				comparisonScoreMetadata: 'n 8/8 / ok',
				baselineInterpretationLabel: 'Tenant higher range',
				comparisonInterpretationLabel: 'Tenant higher range',
				interpretationMeta: 'tenant attested / tenant defined / not reviewed / not official',
				note: null
			},
			{
				dimensionCode: 'distance',
				disclosureState: 'suppressed',
				compatibilityStatus: 'incompatible_scoring',
				baselineMean: 'Suppressed',
				comparisonMean: 'Suppressed',
				aggregateDelta: 'Suppressed',
				pairedDeltaMean: 'Suppressed',
				linkedPairCount: 2,
				baselineScoreMetadata: null,
				comparisonScoreMetadata: null,
				baselineInterpretationLabel: null,
				comparisonInterpretationLabel: null,
				interpretationMeta: null,
				note: 'Scoring rule changed'
			}
		]);
	});

	it('localizes export file library summaries and cards for Croatian app mode', () => {
		const library: ExportArtifactLibraryResponse = {
			tenantId: 'tenant-id',
			summary: {
				totalCount: 2,
				downloadableCount: 2,
				failedCount: 0,
				pendingCount: 0
			},
			artifacts: [
				{
					id: 'artifact-id-1',
					targetKind: 'campaign',
					targetId: 'campaign-id',
					targetLabel: 'Wave 1',
					campaignId: 'campaign-id',
					campaignName: 'Wave 1',
					artifactType: 'report_proof_csv_codebook',
					status: 'succeeded',
					format: 'csv_codebook',
					fileName: 'wave-1-report.csv',
					rowCount: 1,
					byteSize: 1500,
					checksumSha256: 'a'.repeat(64),
					createdAt: '2026-05-22T18:56:00Z',
					completedAt: '2026-05-22T18:56:02Z',
					startedAt: '2026-05-22T18:56:00Z',
					failedAt: null,
					expiresAt: null,
					deletedAt: null,
					failureReasonCode: null,
					canDownload: true,
					campaignStatus: 'closed',
					campaignClosedAt: '2026-05-22T19:00:00Z',
					dataFinality: 'closed_wave'
				},
				{
					id: 'artifact-id-2',
					targetKind: 'campaign_series',
					targetId: 'series-id',
					targetLabel: 'AA',
					campaignId: null,
					campaignName: null,
					artifactType: 'campaign_series_response_csv_codebook',
					status: 'succeeded',
					format: 'csv_codebook',
					fileName: 'aa-responses.csv',
					rowCount: 25,
					byteSize: 15200,
					checksumSha256: 'b'.repeat(64),
					createdAt: '2026-05-22T18:55:00Z',
					completedAt: '2026-05-22T18:55:02Z',
					startedAt: '2026-05-22T18:55:00Z',
					failedAt: null,
					expiresAt: null,
					deletedAt: null,
					failureReasonCode: null,
					canDownload: true,
					campaignStatus: null,
					campaignClosedAt: null,
					dataFinality: null
				}
			]
		};

		const view = toExportArtifactLibraryView(library, 'hr-HR');

		expect(view.exportOverview[0].summary).toBe(
			'2 izvozne datoteke spremno je za preuzimanje.'
		);
		expect(view.exportOverview[0].guidance).toContain(
			'Koristite izvoze skupa podataka odgovora za analizu.'
		);
		expect(view.exportOverview[1].summary).toBe(
			'Nema neuspjelih izvoznih datoteka ni datoteka na čekanju.'
		);
		expect(view.exportOverview[2].summary).toBe(
			'Izvozi pokrivaju izvoz matrice rezultata i izvoz skupa podataka odgovora.'
		);
		expect(view.exportOverview[3].summary).toBe('Izvozne datoteke povezane su s Wave 1 i AA.');
		expect(view.cards[0].nextUse).toBe(
			'Koristite ovaj izvoz za agregirani pregled rezultata, usporedbu grupa, usporedbu mjerenja ili provjere opisa podataka.'
		);
		expect(view.cards[0].rows).toContainEqual({ label: 'Kontekst studije', value: 'Mjerenje / Wave 1' });
	expect(view.cards[1].nextUse).toBe(
		'Koristite ovaj izvoz za analizu na razini odgovora s izrađenim opisom podataka.'
	);
		expect(view.cards[1].rows).toContainEqual({ label: 'Kontekst studije', value: 'Studija / AA' });
	});

	it('maps session success and API errors to route-friendly states', () => {
		const session: AuthSessionResponse = {
			userId: 'user-id',
			tenantId: 'tenant-id',
			permissions: ['setup.manage']
		};

		expect(toSessionView({ session })).toEqual({
			state: 'authenticated',
			title: 'Signed in',
			message: 'Tenant tenant-id',
			tenantId: 'tenant-id',
			userId: 'user-id'
		});
		expect(toSessionView({ error: new ApiError('Unauthorized', 401, null) })).toMatchObject({
			state: 'unauthenticated',
			title: 'Sign in required'
		});
		expect(toSessionView({ error: new ApiError('Forbidden', 403, null) })).toMatchObject({
			state: 'forbidden',
			title: 'Tenant access unavailable'
		});
		expect(toSessionView({ error: new ApiError('Failure', 500, null) })).toMatchObject({
			state: 'failed',
			title: 'Session check failed',
			message: 'Session check failed with status 500.'
		});
	});
});

const ownSeriesMetadata = {
	studyKind: 'own',
	isSample: false,
	sampleScenario: null,
	readOnlyReason: null
} as const;

const sampleWorkspaceOverview: WorkspaceOverviewResponse = {
	tenantId: 'tenant-id',
	totals: {
		campaignSeriesCount: 1,
		campaignCount: 2,
		liveCampaignCount: 1,
		submittedResponseCount: 14,
		exportArtifactCount: 3
	},
	commandCenter: {
		items: [
			{
				id: 'series-id-setup',
				title: 'Finish setup for Quarterly pulse',
				description: 'Consent, retention, disclosure, and scoring setup still need attention.',
				state: 'blocked',
				surface: 'setup',
				route: '/app/campaign-series/series-id/setup',
				actionLabel: 'Open setup',
				priority: 20,
				campaignSeriesId: 'series-id',
				campaignId: null,
				requiredPermission: 'setup.manage'
			}
		]
	},
	studyCollections: {
		sampleStudies: [
			{
				studyKind: 'sample',
				isSample: true,
				sampleScenario: 'mixed_lifecycle',
				readOnlyReason: 'sample_study',
				id: 'sample-series-id',
				name: 'Completed sample',
				createdAt: '2026-05-01T08:00:00Z',
				updatedAt: '2026-05-02T09:00:00Z',
				campaignCount: 2,
				liveCampaignCount: 0,
				submittedResponseCount: 28,
				latestLaunchAt: '2026-05-02T10:00:00Z',
				latestSubmissionAt: '2026-05-03T11:00:00Z',
				readinessStatus: 'proof_only',
				archived: false,
				archivedAt: null,
				archivedByUserId: null,
				archiveReason: null
			}
		],
		ownStudies: [
			{
				...ownSeriesMetadata,
				id: 'own-series-id',
				name: 'New team study',
				createdAt: '2026-05-04T08:00:00Z',
				updatedAt: '2026-05-05T09:00:00Z',
				campaignCount: 0,
				liveCampaignCount: 0,
				submittedResponseCount: 0,
				latestLaunchAt: null,
				latestSubmissionAt: null,
				readinessStatus: 'not_configured',
				archived: false,
				archivedAt: null,
				archivedByUserId: null,
				archiveReason: null
			}
		]
	},
	recentSeries: [
		{
			...ownSeriesMetadata,
			id: 'series-id',
			name: 'Quarterly pulse',
			createdAt: '2026-05-01T08:00:00Z',
			updatedAt: '2026-05-02T09:00:00Z',
			campaignCount: 2,
			liveCampaignCount: 1,
			submittedResponseCount: 14,
			latestLaunchAt: '2026-05-02T10:00:00Z',
			latestSubmissionAt: '2026-05-03T11:00:00Z',
			readinessStatus: 'proof_only',
			archived: false,
			archivedAt: null,
			archivedByUserId: null,
			archiveReason: null
		}
	]
};

const sampleCampaignSeriesList: CampaignSeriesListResponse = {
	items: [
		{
			...ownSeriesMetadata,
			id: 'series-id',
			name: 'Quarterly pulse',
			createdAt: '2026-05-01T08:00:00Z',
			updatedAt: '2026-05-02T09:00:00Z',
			campaignCount: 2,
			liveCampaignCount: 1,
			submittedResponseCount: 14,
			latestLaunchAt: '2026-05-02T10:00:00Z',
			latestSubmissionAt: '2026-05-03T11:00:00Z',
			readinessStatus: 'pending',
			archived: false,
			archivedAt: null,
			archivedByUserId: null,
			archiveReason: null
		}
	]
};

const sampleCampaignSeriesHub: CampaignSeriesHubResponse = {
	...ownSeriesMetadata,
	id: 'series-id',
	name: 'Quarterly burnout pulse',
	createdAt: '2026-05-01T08:00:00Z',
	updatedAt: '2026-05-02T09:00:00Z',
	totals: {
		campaignCount: 2,
		liveCampaignCount: 1,
		submittedResponseCount: 31,
		scoreCount: 28,
		exportArtifactCount: 3
	},
	governance: {
		consentStatus: 'proof_only',
		retentionStatus: 'proof_only',
		disclosureStatus: 'pending',
		scoringStatus: 'not_configured'
	},
	lifecycle: [
		{
			id: 'setup',
			label: 'Prepare',
			status: 'ready',
			guidance: 'Governance prerequisites are configured for this series.',
			route: 'setup',
			actionLabel: 'Review setup'
		},
		{
			id: 'operations',
			label: 'Operations',
			status: 'pending',
			guidance: 'Collection is live, but no submitted responses are available yet.',
			route: 'operations',
			actionLabel: 'Open operations'
		}
	],
	campaigns: [
		{
			id: 'campaign-id',
			name: 'Wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			startAt: '2026-05-05T08:00:00Z',
			endAt: '2026-05-15T18:00:00Z',
			latestLaunchAt: '2026-05-05T08:30:00Z',
			submittedResponseCount: 31,
			scoreCount: 28,
			exportArtifactCount: 3
		}
	],
	archived: false,
	archivedAt: null,
	archivedByUserId: null,
	archiveReason: null
};

const sampleSetupWorkspace: CampaignSeriesSetupWorkspaceResponse = {
	series: {
		...ownSeriesMetadata,
		id: 'series-id',
		name: 'Quarterly burnout pulse',
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-02T09:00:00Z'
	},
	summary: {
		campaignCount: 1,
		liveCampaignCount: 0,
		missingPrerequisiteCount: 1
	},
	selectedCampaign: {
		id: 'campaign-id',
		name: 'Wave 1',
		status: 'draft',
		responseIdentityMode: 'anonymous_longitudinal',
		defaultLocale: 'en',
		templateVersionId: 'template-version-id',
		latestLaunchAt: null
	},
	template: {
		templateId: 'template-id',
		templateVersionId: 'template-version-id',
		templateName: 'Tenant burnout pulse template',
		semver: '1.0.0',
		status: 'draft',
		defaultLocale: 'en',
		instrumentId: null,
		questionCount: 5
	},
	scoring: {
		id: 'scoring-rule-id',
		templateVersionId: 'template-version-id',
		ruleKey: 'burnout.total',
		ruleVersion: '1.0.0',
		status: 'draft',
		source: 'template_version'
	},
	policies: {
		consent: {
			id: 'consent-id',
			version: '1.0.0',
			status: 'configured',
			details: [
				{ label: 'Title', value: 'Default participant disclosure' },
				{ label: 'Locale', value: 'en' },
				{ label: 'Required grants', value: '2' },
				{ label: 'Optional grants', value: '0' },
				{ label: 'Published', value: '2026-05-06' }
			]
		},
		retention: {
			id: 'retention-id',
			version: '1.0.0',
			status: 'configured',
			details: [
				{ label: 'Retain for', value: '1 year' },
				{ label: 'Starts from', value: 'response submitted at' },
				{ label: 'Action after retention', value: 'anonymize' },
				{ label: 'Next review', value: '2027-05-06' }
			]
		},
		disclosure: { id: null, version: null, status: 'not_configured', details: [] }
	},
	readiness: {
		campaignId: 'campaign-id',
		status: 'blocked',
		ready: false
	},
	missingPrerequisites: [
		{
			code: 'disclosure_policy.missing',
			label: 'Disclosure policy',
			message: 'Add a disclosure policy for this series.',
			severity: 'blocking'
		}
	],
	campaigns: [
		{
			id: 'campaign-id',
			name: 'Wave 1',
			status: 'draft',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			templateVersionId: 'template-version-id',
			latestLaunchAt: null
		}
	]
};

const sampleOperationsWorkspace: CampaignSeriesOperationsWorkspaceResponse = {
	series: {
		...ownSeriesMetadata,
		id: 'series-id',
		name: 'Quarterly burnout pulse',
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-02T09:00:00Z'
	},
	summary: {
		campaignCount: 2,
		liveCampaignCount: 1,
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 1,
		sentInvitationCount: 8,
		failedInvitationCount: 1,
		deliveryAttemptCount: 9,
		startedResponseCount: 36,
		draftResponseCount: 5,
		submittedResponseCount: 31,
		latestResponseStartedAt: '2026-05-05T10:15:00Z',
		latestResponseSubmittedAt: '2026-05-05T10:10:00Z',
		collectionStatus: 'has_submissions',
		reportVisibilityStatus: 'ready_for_aggregate_report',
		collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
		missingPrerequisiteCount: 1
	},
	selectedCampaign: {
		id: 'campaign-id',
		name: 'Wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'launch-snapshot-id',
		latestLaunchAt: '2026-05-05T08:30:00Z',
		launchSnapshot: {
			id: 'launch-snapshot-id',
			templateVersionId: 'template-version-id',
			scoringRuleId: 'scoring-rule-id',
			scoringRuleDocumentHash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
			consentDocumentId: 'consent-id',
			retentionPolicyId: 'retention-id',
			disclosurePolicyId: 'disclosure-id',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			templateQuestionCount: 5,
			launchedAt: '2026-05-05T08:30:00Z',
			launchedByUserId: 'owner-user-id'
		},
		startedResponseCount: 36,
		draftResponseCount: 5,
		submittedResponseCount: 31,
		latestResponseStartedAt: '2026-05-05T10:15:00Z',
		latestResponseSubmittedAt: '2026-05-05T10:10:00Z',
		collectionStatus: 'has_submissions',
		reportVisibilityStatus: 'ready_for_aggregate_report',
		collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
		openLinkAssignmentCount: 1,
		queuedInvitationCount: 1,
		sentInvitationCount: 8,
		failedInvitationCount: 1,
		deliveryAttemptCount: 9,
		latestDeliveryAttemptAt: '2026-05-05T09:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoredSubmittedResponseCount: 31,
		unscoredSubmittedResponseCount: 0,
		notConfiguredSubmittedResponseCount: 0,
		latestScoringActivityAt: '2026-05-05T10:20:00Z',
		scoreCoverageStatus: 'complete'
	},
	missingPrerequisites: [
		{
			code: 'public_entry.missing',
			label: 'Public entry',
			message: 'Create an open-link entry point before collecting anonymous responses.',
			severity: 'blocking'
		}
	],
	campaigns: [
		{
			id: 'campaign-id',
			name: 'Wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'launch-snapshot-id',
			latestLaunchAt: '2026-05-05T08:30:00Z',
			launchSnapshot: {
				id: 'launch-snapshot-id',
				templateVersionId: 'template-version-id',
				scoringRuleId: 'scoring-rule-id',
				scoringRuleDocumentHash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
				consentDocumentId: 'consent-id',
				retentionPolicyId: 'retention-id',
				disclosurePolicyId: 'disclosure-id',
				responseIdentityMode: 'anonymous_longitudinal',
				defaultLocale: 'en',
				templateQuestionCount: 5,
				launchedAt: '2026-05-05T08:30:00Z',
				launchedByUserId: 'owner-user-id'
			},
			startedResponseCount: 36,
			draftResponseCount: 5,
			submittedResponseCount: 31,
			latestResponseStartedAt: '2026-05-05T10:15:00Z',
			latestResponseSubmittedAt: '2026-05-05T10:10:00Z',
			collectionStatus: 'has_submissions',
			reportVisibilityStatus: 'ready_for_aggregate_report',
			collectionGuidance: 'Enough submitted responses exist for aggregate report visibility.',
			openLinkAssignmentCount: 1,
			queuedInvitationCount: 1,
			sentInvitationCount: 8,
			failedInvitationCount: 1,
			deliveryAttemptCount: 9,
			latestDeliveryAttemptAt: '2026-05-05T09:30:00Z',
			scoringRuleId: 'scoring-rule-id',
			scoredSubmittedResponseCount: 31,
			unscoredSubmittedResponseCount: 0,
			notConfiguredSubmittedResponseCount: 0,
			latestScoringActivityAt: '2026-05-05T10:20:00Z',
			scoreCoverageStatus: 'complete'
		},
		{
			id: 'campaign-draft-id',
			name: 'Draft wave',
			status: 'draft',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			latestLaunchSnapshotId: null,
			latestLaunchAt: null,
			startedResponseCount: 0,
			draftResponseCount: 0,
			submittedResponseCount: 0,
			latestResponseStartedAt: null,
			latestResponseSubmittedAt: null,
			collectionStatus: 'closed_or_inactive',
			reportVisibilityStatus: 'unknown_policy',
			collectionGuidance:
				'Report visibility readiness is unknown because disclosure policy is missing.',
			openLinkAssignmentCount: 0,
			queuedInvitationCount: 0,
			sentInvitationCount: 0,
			failedInvitationCount: 0,
			deliveryAttemptCount: 0,
			latestDeliveryAttemptAt: null,
			scoringRuleId: null,
			scoredSubmittedResponseCount: 0,
			unscoredSubmittedResponseCount: 0,
			notConfiguredSubmittedResponseCount: 0,
			latestScoringActivityAt: null,
			scoreCoverageStatus: 'no_submissions'
		}
	],
	scoreCoverage: {
		submittedResponseCount: 31,
		scoredSubmittedResponseCount: 31,
		unscoredSubmittedResponseCount: 0,
		notConfiguredSubmittedResponseCount: 0,
		campaignsWithScoringRuleCount: 1,
		campaignsWithoutScoringRuleCount: 1,
		latestScoringActivityAt: '2026-05-05T10:20:00Z',
		status: 'complete',
		guidance: 'All submitted responses have successful scoring activity.'
	}
};

const sampleReportsWorkspace: CampaignSeriesReportsWorkspaceResponse = {
	series: {
		...ownSeriesMetadata,
		id: 'series-id',
		name: 'Quarterly burnout pulse',
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-02T09:00:00Z'
	},
	summary: {
		campaignCount: 2,
		liveCampaignCount: 1,
		reportableCampaignCount: 1,
		submittedResponseCount: 31,
		scoreCount: 28,
		exportArtifactCount: 2,
		visibleScoreCount: 25,
		suppressedScoreCount: 3,
		missingPrerequisiteCount: 1
	},
	selectedCampaign: {
		id: 'campaign-id',
		name: 'Wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'launch-snapshot-id',
		latestLaunchAt: '2026-05-05T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		consentDocumentId: 'consent-id',
		retentionPolicyId: 'retention-id',
		disclosurePolicyId: 'disclosure-id',
		submittedResponseCount: 31,
		scoreCount: 28,
		exportArtifactCount: 2,
		visibleScoreCount: 25,
		suppressedScoreCount: 3,
		disclosureState: 'visible',
		disclosureKMin: 5,
		reportStatus: 'proof_only',
		interpretationStatus: 'not_validated_interpretation',
		latestExportArtifactId: 'export-artifact-id',
		latestExportArtifactFileName: 'report-proof.csv',
		latestExportArtifactStatus: 'succeeded',
		latestExportArtifactCreatedAt: '2026-05-05T09:00:00Z',
		latestExportArtifactCompletedAt: '2026-05-05T09:00:03Z',
		latestExportArtifactStartedAt: null,
		latestExportArtifactFailedAt: null,
		latestExportArtifactExpiresAt: null,
		latestExportArtifactDeletedAt: null,
		latestExportArtifactFailureReasonCode: null,
		latestExportArtifactCanDownload: true
	},
	missingPrerequisites: [
		{
			code: 'export_artifact.missing',
			label: 'Export file',
			message: 'Create a report preview export before handoff.',
			severity: 'advisory'
		}
	],
	exportArtifacts: [],
	campaigns: [
		{
			id: 'campaign-id',
			name: 'Wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'launch-snapshot-id',
			latestLaunchAt: '2026-05-05T08:30:00Z',
			scoringRuleId: 'scoring-rule-id',
			consentDocumentId: 'consent-id',
			retentionPolicyId: 'retention-id',
			disclosurePolicyId: 'disclosure-id',
			submittedResponseCount: 31,
			scoreCount: 28,
			exportArtifactCount: 2,
			visibleScoreCount: 25,
			suppressedScoreCount: 3,
			disclosureState: 'visible',
			disclosureKMin: 5,
			reportStatus: 'proof_only',
			interpretationStatus: 'not_validated_interpretation',
			latestExportArtifactId: 'export-artifact-id',
			latestExportArtifactFileName: 'report-proof.csv',
			latestExportArtifactStatus: 'succeeded',
			latestExportArtifactCreatedAt: '2026-05-05T09:00:00Z',
			latestExportArtifactCompletedAt: '2026-05-05T09:00:03Z',
			latestExportArtifactStartedAt: null,
			latestExportArtifactFailedAt: null,
			latestExportArtifactExpiresAt: null,
			latestExportArtifactDeletedAt: null,
			latestExportArtifactFailureReasonCode: null,
			latestExportArtifactCanDownload: true
		},
		{
			id: 'campaign-draft-id',
			name: 'Draft wave',
			status: 'draft',
			responseIdentityMode: 'anonymous',
			defaultLocale: 'en',
			latestLaunchSnapshotId: null,
			latestLaunchAt: null,
			scoringRuleId: null,
			consentDocumentId: null,
			retentionPolicyId: null,
			disclosurePolicyId: null,
			submittedResponseCount: 0,
			scoreCount: 0,
			exportArtifactCount: 0,
			visibleScoreCount: 0,
			suppressedScoreCount: 0,
			disclosureState: 'not_available',
			disclosureKMin: null,
			reportStatus: 'blocked',
			interpretationStatus: 'not_available',
			latestExportArtifactId: null,
			latestExportArtifactFileName: null,
			latestExportArtifactStatus: null,
			latestExportArtifactCreatedAt: null,
			latestExportArtifactCompletedAt: null,
			latestExportArtifactStartedAt: null,
			latestExportArtifactFailedAt: null,
			latestExportArtifactExpiresAt: null,
			latestExportArtifactDeletedAt: null,
			latestExportArtifactFailureReasonCode: null,
			latestExportArtifactCanDownload: false
		}
	],
	scoreCoverage: {
		submittedResponseCount: 31,
		scoredSubmittedResponseCount: 28,
		unscoredSubmittedResponseCount: 3,
		notConfiguredSubmittedResponseCount: 0,
		campaignsWithScoringRuleCount: 1,
		campaignsWithoutScoringRuleCount: 1,
		latestScoringActivityAt: '2026-05-05T10:20:00Z',
		status: 'partial',
		guidance:
			'Some submitted responses still need scoring activity before score-dependent reports are complete.'
	}
};

const sampleWavesWorkspace: CampaignSeriesWavesWorkspaceResponse = {
	series: {
		...ownSeriesMetadata,
		id: 'series-id',
		name: 'Quarterly burnout pulse',
		createdAt: '2026-05-01T08:00:00Z',
		updatedAt: '2026-05-02T09:00:00Z'
	},
	summary: {
		campaignCount: 2,
		liveCampaignCount: 2,
		longitudinalWaveCount: 2,
		submittedWaveCount: 2,
		linkedTrajectoryCount: 6,
		completeTrajectoryCount: 6,
		comparableScoreCount: 1,
		visibleComparisonCount: 1,
		suppressedComparisonCount: 0,
		blockedComparisonCount: 0,
		missingPrerequisiteCount: 0
	},
	selectedBaselineWave: {
		id: 'wave-1-id',
		name: 'Wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'wave-1-launch-id',
		latestLaunchAt: '2026-05-05T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		disclosurePolicyId: 'wave-1-disclosure-id',
		disclosureKMin: 5,
		submittedResponseCount: 6,
		scoreCount: 6,
		linkedTrajectoryCount: 6,
		waveState: 'wave'
	},
	selectedComparisonWave: {
		id: 'wave-2-id',
		name: 'Wave 2',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		defaultLocale: 'en',
		latestLaunchSnapshotId: 'wave-2-launch-id',
		latestLaunchAt: '2026-05-12T08:30:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'burnout.total',
		scoringRuleVersion: '1.0.0',
		disclosurePolicyId: 'wave-2-disclosure-id',
		disclosureKMin: 5,
		submittedResponseCount: 6,
		scoreCount: 6,
		linkedTrajectoryCount: 6,
		waveState: 'wave'
	},
	comparison: {
		status: 'proof_only',
		disclosureState: 'visible',
		compatibilityState: 'compatible',
		interpretationStatus: 'not_validated_interpretation',
		disclosureKMin: 5,
		linkedPairCount: 6,
		visibleScoreCount: 1,
		suppressedScoreCount: 0,
		blockedScoreCount: 0
	},
	missingPrerequisites: [],
	waves: [
		{
			id: 'wave-1-id',
			name: 'Wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'wave-1-launch-id',
			latestLaunchAt: '2026-05-05T08:30:00Z',
			scoringRuleId: 'scoring-rule-id',
			scoringRuleKey: 'burnout.total',
			scoringRuleVersion: '1.0.0',
			disclosurePolicyId: 'wave-1-disclosure-id',
			disclosureKMin: 5,
			submittedResponseCount: 6,
			scoreCount: 6,
			linkedTrajectoryCount: 6,
			waveState: 'wave'
		},
		{
			id: 'wave-2-id',
			name: 'Wave 2',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			defaultLocale: 'en',
			latestLaunchSnapshotId: 'wave-2-launch-id',
			latestLaunchAt: '2026-05-12T08:30:00Z',
			scoringRuleId: 'scoring-rule-id',
			scoringRuleKey: 'burnout.total',
			scoringRuleVersion: '1.0.0',
			disclosurePolicyId: 'wave-2-disclosure-id',
			disclosureKMin: 5,
			submittedResponseCount: 6,
			scoreCount: 6,
			linkedTrajectoryCount: 6,
			waveState: 'wave'
		}
	]
};

const sampleReportProof: CampaignReportProofResponse = {
	campaignId: 'campaign-id',
	campaignSeriesId: 'series-id',
	campaignName: 'Wave 1',
	campaignStatus: 'live',
	proofStatus: 'ready',
	interpretationStatus: 'not_validated_interpretation',
	launchSnapshot: {
		id: 'snapshot-id',
		templateVersionId: 'template-version-id',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleDocumentHash: 'a'.repeat(64),
		consentDocumentId: 'consent-id',
		retentionPolicyId: 'retention-id',
		disclosurePolicyId: 'disclosure-id',
		responseIdentityMode: 'anonymous',
		launchedAt: '2026-05-08T09:00:00Z'
	},
	disclosurePolicy: {
		id: 'disclosure-id',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress_small_cells'
	},
	scores: [
		{
			dimensionCode: 'exhaustion',
			disclosure: 'visible',
			submittedResponseCount: 8,
			scoreCount: 8,
			nValidTotal: 7,
			nExpectedTotal: 8,
			missingPolicyStatusSummary: 'ok',
			mean: 3.42,
			min: 1,
			max: 5,
			suppressionReason: null,
			interpretation: {
				status: 'tenant_attested',
				source: 'tenant_defined',
				bandCode: 'middle',
				label: 'Tenant middle range',
				provenance: 'Tenant-defined score bands for this setup; not validated; not official.',
				isValidated: false,
				isOfficial: false
			}
		},
		{
			dimensionCode: 'distance',
			disclosure: 'suppressed',
			submittedResponseCount: 3,
			scoreCount: null,
			nValidTotal: null,
			nExpectedTotal: null,
			missingPolicyStatusSummary: null,
			mean: null,
			min: null,
			max: null,
			suppressionReason: 'below_k_min',
			interpretation: null
		}
	]
};

const sampleWaveComparison: CampaignSeriesWaveComparisonProofResponse = {
	campaignSeriesId: 'series-id',
	proofStatus: 'ready',
	interpretationStatus: 'not_validated_interpretation',
	baselineWave: {
		campaignId: 'wave-1-id',
		name: 'Wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-08T09:00:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'tenant-burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'a'.repeat(64),
		submittedResponseCount: 8
	},
	comparisonWave: {
		campaignId: 'wave-2-id',
		name: 'Wave 2',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-15T09:00:00Z',
		scoringRuleId: 'scoring-rule-id',
		scoringRuleKey: 'tenant-burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'a'.repeat(64),
		submittedResponseCount: 8
	},
	disclosurePolicy: {
		id: 'disclosure-id',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'suppress_small_cells'
	},
	scores: [
		{
			dimensionCode: 'exhaustion',
			compatibilityStatus: 'compatible',
			disclosure: 'visible',
			baselineSubmittedResponseCount: 8,
			comparisonSubmittedResponseCount: 8,
			linkedPairCount: 6,
			baselineScoreCount: 8,
			comparisonScoreCount: 8,
			baselineNValidTotal: 7,
			baselineNExpectedTotal: 8,
			baselineMissingPolicyStatusSummary: 'ok',
			comparisonNValidTotal: 8,
			comparisonNExpectedTotal: 8,
			comparisonMissingPolicyStatusSummary: 'ok',
			baselineMean: 3.9,
			comparisonMean: 4.2,
			aggregateDelta: 0.3,
			pairedDeltaMean: 0.25,
			suppressionReason: null,
			compatibilityReason: null,
			baselineInterpretation: {
				status: 'tenant_attested',
				source: 'tenant_defined',
				bandCode: 'higher',
				label: 'Tenant higher range',
				provenance: 'Tenant-defined score bands for this setup; not validated; not official.',
				isValidated: false,
				isOfficial: false
			},
			comparisonInterpretation: {
				status: 'tenant_attested',
				source: 'tenant_defined',
				bandCode: 'higher',
				label: 'Tenant higher range',
				provenance: 'Tenant-defined score bands for this setup; not validated; not official.',
				isValidated: false,
				isOfficial: false
			}
		},
		{
			dimensionCode: 'distance',
			compatibilityStatus: 'incompatible_scoring',
			disclosure: 'suppressed',
			baselineSubmittedResponseCount: 3,
			comparisonSubmittedResponseCount: 3,
			linkedPairCount: 2,
			baselineScoreCount: null,
			comparisonScoreCount: null,
			baselineNValidTotal: null,
			baselineNExpectedTotal: null,
			baselineMissingPolicyStatusSummary: null,
			comparisonNValidTotal: null,
			comparisonNExpectedTotal: null,
			comparisonMissingPolicyStatusSummary: null,
			baselineMean: null,
			comparisonMean: null,
			aggregateDelta: null,
			pairedDeltaMean: null,
			suppressionReason: 'below_k_min',
			compatibilityReason: 'Scoring rule changed',
			baselineInterpretation: null,
			comparisonInterpretation: null
		}
	]
};
