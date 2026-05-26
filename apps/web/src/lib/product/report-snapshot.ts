import type {
	CampaignSeriesReportsCampaignResponse,
	CampaignSeriesReportsExportArtifactResponse,
	CampaignSeriesReportsWorkspaceResponse
} from '$lib/api/product';
import type { AppLocale } from '$lib/i18n/localization';
import { formatAppNumber } from '$lib/i18n/localization';
import type { ProductReadModelBadgeStatus } from './view-models';

export type SelectedSeriesReportSnapshotLocalState = {
	loadedCampaignId?: string | null;
};

export type SelectedSeriesReportSnapshotState = {
	status: ProductReadModelBadgeStatus;
	available: boolean;
	campaignId: string | null;
	campaignName: string | null;
	badgeLabel: string;
	disabledReason: string | null;
};

export type SelectedSeriesReportDashboardRow = {
	label: string;
	value: string;
	mono?: boolean;
};

export type SelectedSeriesReportDashboardView = {
	title: string;
	status: ProductReadModelBadgeStatus;
	available: boolean;
	badgeLabel: string;
	emptyMessage: string | null;
	readinessRows: SelectedSeriesReportDashboardRow[];
	disclosureRows: SelectedSeriesReportDashboardRow[];
	provenanceRows: SelectedSeriesReportDashboardRow[];
	exportRows: SelectedSeriesReportDashboardRow[];
	artifactRegistry: SelectedSeriesReportArtifactRegistryItem[];
};

export type SelectedSeriesReportArtifactRegistryItem = {
	id: string;
	targetKind: string;
	targetId: string;
	targetLabel: string;
	campaignId: string | null;
	campaignName: string | null;
	title: string;
	badgeStatus: ProductReadModelBadgeStatus;
	badgeLabel: string;
	meta: string[];
	rows: SelectedSeriesReportDashboardRow[];
};

export type SelectedSeriesReportSnapshotCopy = {
	locale: AppLocale;
	notAvailable: string;
	yes: string;
	no: string;
	untitledCampaign: string;
	untitledTarget: string;
	untitledExportFile: string;
	state: {
		notAvailable: string;
		blocked: string;
		previewReady: string;
		previewAvailable: string;
	};
	disabled: {
		noCampaign: string;
		blocked: string;
	};
	dashboard: {
		unavailableTitle: string;
		unavailableMessage: string;
		title: (campaignName: string) => string;
	};
	labels: Record<
		| 'campaigns'
		| 'reportableCampaigns'
		| 'missingPrerequisites'
		| 'selectedCampaign'
		| 'campaignStatus'
		| 'reportStatus'
		| 'interpretation'
		| 'submittedResponses'
		| 'scores'
		| 'disclosure'
		| 'disclosureK'
		| 'visibleScores'
		| 'suppressedScores'
		| 'launchSnapshot'
		| 'latestLaunch'
		| 'scoringRule'
		| 'consentDocument'
		| 'retentionPolicy'
		| 'disclosurePolicy'
		| 'exportFiles'
		| 'latestExportRecord'
		| 'latestExportFile'
		| 'latestExportStatus'
		| 'latestExportCreated'
		| 'latestExportCompleted'
		| 'latestExportStarted'
		| 'latestExportFailed'
		| 'latestExportExpires'
		| 'latestExportDeleted'
		| 'latestExportFailureReason'
		| 'latestExportDownloadable'
		| 'exportRecord'
		| 'studyContext'
		| 'contextType'
		| 'created'
		| 'completed'
		| 'started'
		| 'failed'
		| 'expires'
		| 'deleted'
		| 'failureReason'
		| 'downloadable'
		| 'checksum',
		string
	>;
	codeLabels: Record<string, string>;
	exportFileTypeLabels: Record<string, string>;
	artifactStatusLabels: Record<string, string>;
	rowsUnit: (count: number, formatted: string) => string;
	bytesUnit: (count: number, formatted: string) => string;
	panel: Record<
		| 'reportSnapshotAria'
		| 'snapshotKicker'
		| 'snapshotTitle'
		| 'snapshotDescription'
		| 'dashboardAria'
		| 'dashboardKicker'
		| 'dashboardTitle'
		| 'dashboardDescription'
		| 'readinessAria'
		| 'readinessKicker'
		| 'readinessTitle'
		| 'disclosureAria'
		| 'disclosureKicker'
		| 'disclosureTitle'
		| 'provenanceAria'
		| 'provenanceKicker'
		| 'provenanceTitle'
		| 'exportReadinessAria'
		| 'exportsKicker'
		| 'exportReadinessTitle'
		| 'exportFilesAria'
		| 'exportFilesTitle'
		| 'exportFileAriaPrefix'
		| 'noStoredExportsTitle'
		| 'noStoredExportsMessage'
		| 'loadingSnapshot'
		| 'refreshSnapshot'
		| 'loading'
		| 'ready'
		| 'failed'
		| 'local'
		| 'campaign'
		| 'aggregateReportSnapshotAria'
		| 'reportPreviewKicker'
		| 'previewLabel'
		| 'preliminarySummaryAria'
		| 'preliminarySummaryKicker'
		| 'reportScoreRowsAria'
		| 'reportScoreAriaPrefix'
		| 'meanLabel'
		| 'scoresLabel'
		| 'rangeLabel',
		string
	>;
};

const reportSnapshotCopies: Record<AppLocale, SelectedSeriesReportSnapshotCopy> = {
	en: {
		locale: 'en',
		notAvailable: 'Not available',
		yes: 'Yes',
		no: 'No',
		untitledCampaign: 'Untitled campaign',
		untitledTarget: 'Untitled target',
		untitledExportFile: 'Untitled export file',
		state: {
			notAvailable: 'Not available',
			blocked: 'Blocked',
			previewReady: 'Preview ready',
			previewAvailable: 'Preview available'
		},
		disabled: {
			noCampaign: 'Create or select a campaign before loading the report snapshot.',
			blocked: 'Resolve report prerequisites before loading the report snapshot.'
		},
		dashboard: {
			unavailableTitle: 'Report dashboard unavailable',
			unavailableMessage: 'Create or select a campaign before reviewing the report dashboard.',
			title: (campaignName) => `${campaignName} report dashboard`
		},
		labels: {
			campaigns: 'Campaigns',
			reportableCampaigns: 'Reportable campaigns',
			missingPrerequisites: 'Missing prerequisites',
			selectedCampaign: 'Selected campaign',
			campaignStatus: 'Campaign status',
			reportStatus: 'Report status',
			interpretation: 'Interpretation',
			submittedResponses: 'Submitted responses',
			scores: 'Scores',
			disclosure: 'Disclosure',
			disclosureK: 'Disclosure k',
			visibleScores: 'Visible scores',
			suppressedScores: 'Suppressed scores',
			launchSnapshot: 'Launch snapshot',
			latestLaunch: 'Latest launch',
			scoringRule: 'Scoring rule',
			consentDocument: 'Consent document',
			retentionPolicy: 'Retention policy',
			disclosurePolicy: 'Disclosure policy',
			exportFiles: 'Export files',
			latestExportRecord: 'Latest export record',
			latestExportFile: 'Latest export file',
			latestExportStatus: 'Latest export status',
			latestExportCreated: 'Latest export created',
			latestExportCompleted: 'Latest export completed',
			latestExportStarted: 'Latest export started',
			latestExportFailed: 'Latest export failed',
			latestExportExpires: 'Latest export expires',
			latestExportDeleted: 'Latest export deleted',
			latestExportFailureReason: 'Latest export failure reason',
			latestExportDownloadable: 'Latest export downloadable',
			exportRecord: 'Export record',
			studyContext: 'Study context',
			contextType: 'Context type',
			created: 'Created',
			completed: 'Completed',
			started: 'Started',
			failed: 'Failed',
			expires: 'Expires',
			deleted: 'Deleted',
			failureReason: 'Failure reason',
			downloadable: 'Downloadable',
			checksum: 'Checksum'
		},
		codeLabels: {
			proof_only: 'preview'
		},
		exportFileTypeLabels: {
			report_proof_csv_codebook: 'results matrix CSV and codebook',
			campaign_series_results_matrix_csv_codebook: 'results matrix CSV and codebook',
			campaign_series_response_csv_codebook: 'response dataset CSV and codebook'
		},
		artifactStatusLabels: {
			succeeded: 'succeeded',
			completed: 'completed',
			failed: 'failed',
			pending: 'pending'
		},
		rowsUnit: (_count, formatted) => `${formatted} rows`,
		bytesUnit: (_count, formatted) => `${formatted} bytes`,
		panel: {
			reportSnapshotAria: 'Report snapshot',
			snapshotKicker: 'Report snapshot',
			snapshotTitle: 'Selected-series report snapshot',
			snapshotDescription: 'Aggregate preview for the selected report campaign.',
			dashboardAria: 'Report dashboard',
			dashboardKicker: 'Report dashboard',
			dashboardTitle: 'Selected-series report dashboard',
			dashboardDescription: 'Latest report preview for the selected campaign.',
			readinessAria: 'Report readiness',
			readinessKicker: 'Readiness',
			readinessTitle: 'Report readiness',
			disclosureAria: 'Disclosure guardrails',
			disclosureKicker: 'Disclosure',
			disclosureTitle: 'Disclosure guardrails',
			provenanceAria: 'Report provenance',
			provenanceKicker: 'Provenance',
			provenanceTitle: 'Report provenance',
			exportReadinessAria: 'Export readiness',
			exportsKicker: 'Exports',
			exportReadinessTitle: 'Export readiness',
			exportFilesAria: 'Export files',
			exportFilesTitle: 'Export files',
			exportFileAriaPrefix: 'Export file',
			noStoredExportsTitle: 'No stored export files yet',
			noStoredExportsMessage: 'Create an export file before downloading.',
			loadingSnapshot: 'Loading snapshot',
			refreshSnapshot: 'Refresh report snapshot',
			loading: 'Loading',
			ready: 'Ready',
			failed: 'Failed',
			local: 'Local',
			campaign: 'Campaign',
			aggregateReportSnapshotAria: 'Aggregate report snapshot',
			reportPreviewKicker: 'Report preview',
			previewLabel: 'Preview',
			preliminarySummaryAria: 'Preliminary results summary',
			preliminarySummaryKicker: 'Preliminary summary',
			reportScoreRowsAria: 'Report score rows',
			reportScoreAriaPrefix: 'Report score',
			meanLabel: 'mean',
			scoresLabel: 'scores',
			rangeLabel: 'range'
		}
	},
	'hr-HR': {
		locale: 'hr-HR',
		notAvailable: 'Nije dostupno',
		yes: 'Da',
		no: 'Ne',
		untitledCampaign: 'Neimenovano mjerenje',
		untitledTarget: 'Neimenovani zapis',
		untitledExportFile: 'Neimenovana izvozna datoteka',
		state: {
			notAvailable: 'Nije dostupno',
			blocked: 'Blokirano',
			previewReady: 'Pregled spreman',
			previewAvailable: 'Pregled dostupan'
		},
		disabled: {
			noCampaign: 'Izradite ili odaberite mjerenje prije učitavanja pregleda izvještaja.',
			blocked: 'Riješite preduvjete izvještaja prije učitavanja pregleda izvještaja.'
		},
		dashboard: {
			unavailableTitle: 'Nadzorna ploča izvještaja nije dostupna',
			unavailableMessage: 'Izradite ili odaberite mjerenje prije pregleda nadzorne ploče izvještaja.',
			title: (campaignName) => `Nadzorna ploča izvještaja za ${campaignName}`
		},
		labels: {
			campaigns: 'Mjerenja',
			reportableCampaigns: 'Mjerenja spremni za izvještaj',
			missingPrerequisites: 'Nedostajući preduvjeti',
			selectedCampaign: 'Odabrano mjerenje',
			campaignStatus: 'Status mjerenja',
			reportStatus: 'Status izvještaja',
			interpretation: 'Tumačenje',
			submittedResponses: 'Predani odgovori',
			scores: 'Rezultati',
			disclosure: 'Prikaz rezultata',
			disclosureK: 'Prag prikaza',
			visibleScores: 'Vidljivi rezultati',
			suppressedScores: 'Skriveni rezultati',
			launchSnapshot: 'Zapis pokretanja',
			latestLaunch: 'Zadnje pokretanje',
			scoringRule: 'Pravilo bodovanja',
			consentDocument: 'Dokument pristanka',
			retentionPolicy: 'Pravilo zadržavanja',
			disclosurePolicy: 'Pravilo prikaza rezultata',
			exportFiles: 'Izvozne datoteke',
			latestExportRecord: 'Zadnji zapis izvoza',
			latestExportFile: 'Zadnja izvozna datoteka',
			latestExportStatus: 'Status zadnjeg izvoza',
			latestExportCreated: 'Zadnji izvoz izrađen',
			latestExportCompleted: 'Zadnji izvoz dovršen',
			latestExportStarted: 'Zadnji izvoz pokrenut',
			latestExportFailed: 'Zadnji izvoz neuspješan',
			latestExportExpires: 'Zadnji izvoz istječe',
			latestExportDeleted: 'Zadnji izvoz obrisan',
			latestExportFailureReason: 'Razlog neuspjeha zadnjeg izvoza',
			latestExportDownloadable: 'Zadnji izvoz dostupan za preuzimanje',
			exportRecord: 'Zapis izvoza',
			studyContext: 'Kontekst studije',
			contextType: 'Vrsta konteksta',
			created: 'Izrađeno',
			completed: 'Dovršeno',
			started: 'Pokrenuto',
			failed: 'Neuspjelo',
			expires: 'Istječe',
			deleted: 'Obrisano',
			failureReason: 'Razlog neuspjeha',
			downloadable: 'Dostupno za preuzimanje',
			checksum: 'Kontrolni zbroj'
		},
		codeLabels: {
			live: 'u tijeku',
			proof_only: 'pregled',
			not_validated_interpretation: 'tumačenje nije potvrđeno',
			visible: 'vidljivo',
			not_available: 'nije dostupno',
			campaign_series: 'studija',
			campaign: 'mjerenje',
			csv_codebook: 'CSV opis podataka',
			succeeded: 'uspjelo',
			failed: 'neuspjelo'
		},
		exportFileTypeLabels: {
			report_proof_csv_codebook: 'CSV i opis podataka matrice rezultata',
			campaign_series_results_matrix_csv_codebook: 'CSV i opis podataka matrice rezultata',
			campaign_series_response_csv_codebook: 'CSV i opis podataka s odgovorima'
		},
		artifactStatusLabels: {
			succeeded: 'Uspjelo',
			completed: 'Dovršeno',
			failed: 'Neuspjelo',
			pending: 'Na čekanju'
		},
		rowsUnit: (count, formatted) =>
			`${formatted} ${count === 1 ? 'redak' : count > 1 && count < 5 ? 'retka' : 'redaka'}`,
		bytesUnit: (_count, formatted) => `${formatted} bajtova`,
		panel: {
			reportSnapshotAria: 'Pregled izvještaja',
			snapshotKicker: 'Pregled izvještaja',
			snapshotTitle: 'Pregled izvještaja odabrane studije',
			snapshotDescription: 'Sažeti pregled za odabrano mjerenje izvještaja.',
			dashboardAria: 'Nadzorna ploča izvještaja',
			dashboardKicker: 'Nadzorna ploča izvještaja',
			dashboardTitle: 'Nadzorna ploča izvještaja odabrane studije',
			dashboardDescription: 'Zadnji pregled izvještaja za odabrano mjerenje.',
			readinessAria: 'Spremnost izvještaja',
			readinessKicker: 'Spremnost',
			readinessTitle: 'Spremnost izvještaja',
			disclosureAria: 'Pravila prikaza rezultata',
			disclosureKicker: 'Prikaz rezultata',
			disclosureTitle: 'Pravila prikaza rezultata',
			provenanceAria: 'Podrijetlo izvještaja',
			provenanceKicker: 'Podrijetlo',
			provenanceTitle: 'Podrijetlo izvještaja',
			exportReadinessAria: 'Spremnost izvoza',
			exportsKicker: 'Izvozi',
			exportReadinessTitle: 'Spremnost izvoza',
			exportFilesAria: 'Izvozne datoteke',
			exportFilesTitle: 'Izvozne datoteke',
			exportFileAriaPrefix: 'Izvozna datoteka',
			noStoredExportsTitle: 'Još nema spremljenih izvoznih datoteka',
			noStoredExportsMessage: 'Izradite izvoznu datoteku prije preuzimanja.',
			loadingSnapshot: 'Učitavanje pregleda',
			refreshSnapshot: 'Osvježi pregled izvještaja',
			loading: 'Učitavanje',
			ready: 'Spremno',
			failed: 'Neuspjelo',
			local: 'Lokalno',
			campaign: 'Mjerenje',
			aggregateReportSnapshotAria: 'Sažeti pregled izvještaja',
			reportPreviewKicker: 'Pregled izvještaja',
			previewLabel: 'Pregled',
			preliminarySummaryAria: 'Preliminarni pregled rezultata',
			preliminarySummaryKicker: 'Preliminarni pregled',
			reportScoreRowsAria: 'Redci rezultata izvještaja',
			reportScoreAriaPrefix: 'Rezultat izvještaja',
			meanLabel: 'prosjek',
			scoresLabel: 'rezultati',
			rangeLabel: 'raspon'
		}
	}
};

export function selectedSeriesReportSnapshotCopy(
	locale: AppLocale = 'en'
): SelectedSeriesReportSnapshotCopy {
	return reportSnapshotCopies[locale];
}

export function toSelectedSeriesReportSnapshotState(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportSnapshotLocalState = {},
	copy: SelectedSeriesReportSnapshotCopy = selectedSeriesReportSnapshotCopy()
): SelectedSeriesReportSnapshotState {
	const selectedCampaign = workspace.selectedCampaign;

	if (!selectedCampaign) {
		return {
			status: 'not_available',
			available: false,
			campaignId: null,
			campaignName: null,
			badgeLabel: copy.state.notAvailable,
			disabledReason: copy.disabled.noCampaign
		};
	}

	const reportable = selectedCampaign.reportStatus === 'proof_only';

	if (!reportable) {
		return {
			status: 'blocked',
			available: false,
			campaignId: selectedCampaign.id,
			campaignName: selectedCampaign.name,
			badgeLabel: copy.state.blocked,
			disabledReason: copy.disabled.blocked
		};
	}

	const loadedForSelectedCampaign = localState.loadedCampaignId === selectedCampaign.id;

	return {
		status: loadedForSelectedCampaign ? 'ready' : 'pending',
		available: true,
		campaignId: selectedCampaign.id,
		campaignName: selectedCampaign.name,
		badgeLabel: loadedForSelectedCampaign
			? copy.state.previewReady
			: copy.state.previewAvailable,
		disabledReason: null
	};
}

export function toSelectedSeriesReportDashboardView(
	workspace: CampaignSeriesReportsWorkspaceResponse,
	localState: SelectedSeriesReportSnapshotLocalState = {},
	copy: SelectedSeriesReportSnapshotCopy = selectedSeriesReportSnapshotCopy()
): SelectedSeriesReportDashboardView {
	const snapshotState = toSelectedSeriesReportSnapshotState(workspace, localState, copy);
	const selectedCampaign = workspace.selectedCampaign;

	if (!selectedCampaign) {
		return {
			title: copy.dashboard.unavailableTitle,
			status: snapshotState.status,
			available: false,
			badgeLabel: snapshotState.badgeLabel,
			emptyMessage: copy.dashboard.unavailableMessage,
			readinessRows: [
				{ label: copy.labels.campaigns, value: formatCount(workspace.summary.campaignCount, copy) },
				{
					label: copy.labels.reportableCampaigns,
					value: formatCount(workspace.summary.reportableCampaignCount, copy)
				},
				{
					label: copy.labels.missingPrerequisites,
					value: formatCount(workspace.summary.missingPrerequisiteCount, copy)
				}
			],
			disclosureRows: [],
			provenanceRows: [],
			exportRows: [],
			artifactRegistry: []
		};
	}

	return {
		title: copy.dashboard.title(selectedCampaign.name.trim() || copy.untitledCampaign),
		status: snapshotState.status,
		available: snapshotState.available,
		badgeLabel: snapshotState.badgeLabel,
		emptyMessage: null,
		readinessRows: toReportReadinessRows(selectedCampaign, copy),
		disclosureRows: toReportDisclosureRows(selectedCampaign, copy),
		provenanceRows: toReportProvenanceRows(selectedCampaign, copy),
		exportRows: toReportExportRows(selectedCampaign, copy),
		artifactRegistry: toReportArtifactRegistry(workspace.exportArtifacts, copy)
	};
}

function toReportReadinessRows(
	campaign: CampaignSeriesReportsCampaignResponse,
	copy: SelectedSeriesReportSnapshotCopy
): SelectedSeriesReportDashboardRow[] {
	return [
		{ label: copy.labels.selectedCampaign, value: campaign.name.trim() || copy.untitledCampaign },
		{ label: copy.labels.campaignStatus, value: formatCodeLabel(campaign.status, copy) },
		{ label: copy.labels.reportStatus, value: formatCodeLabel(campaign.reportStatus, copy) },
		{ label: copy.labels.interpretation, value: formatCodeLabel(campaign.interpretationStatus, copy) },
		{ label: copy.labels.submittedResponses, value: formatCount(campaign.submittedResponseCount, copy) },
		{ label: copy.labels.scores, value: formatCount(campaign.scoreCount, copy) }
	];
}

function toReportDisclosureRows(
	campaign: CampaignSeriesReportsCampaignResponse,
	copy: SelectedSeriesReportSnapshotCopy
): SelectedSeriesReportDashboardRow[] {
	return [
		{ label: copy.labels.disclosure, value: formatCodeLabel(campaign.disclosureState, copy) },
		{
			label: copy.labels.disclosureK,
			value: campaign.disclosureKMin === null ? copy.notAvailable : String(campaign.disclosureKMin)
		},
		{ label: copy.labels.visibleScores, value: formatCount(campaign.visibleScoreCount, copy) },
		{ label: copy.labels.suppressedScores, value: formatCount(campaign.suppressedScoreCount, copy) }
	];
}

function toReportProvenanceRows(
	campaign: CampaignSeriesReportsCampaignResponse,
	copy: SelectedSeriesReportSnapshotCopy
): SelectedSeriesReportDashboardRow[] {
	return [
		idRow(copy.labels.launchSnapshot, campaign.latestLaunchSnapshotId, copy),
		{ label: copy.labels.latestLaunch, value: formatNullableDateTime(campaign.latestLaunchAt, copy) },
		idRow(copy.labels.scoringRule, campaign.scoringRuleId, copy),
		idRow(copy.labels.consentDocument, campaign.consentDocumentId, copy),
		idRow(copy.labels.retentionPolicy, campaign.retentionPolicyId, copy),
		idRow(copy.labels.disclosurePolicy, campaign.disclosurePolicyId, copy)
	];
}

function toReportExportRows(
	campaign: CampaignSeriesReportsCampaignResponse,
	copy: SelectedSeriesReportSnapshotCopy
): SelectedSeriesReportDashboardRow[] {
	return [
		{ label: copy.labels.exportFiles, value: formatCount(campaign.exportArtifactCount, copy) },
		idRow(copy.labels.latestExportRecord, campaign.latestExportArtifactId, copy),
		{
			label: copy.labels.latestExportFile,
			value: campaign.latestExportArtifactFileName ?? copy.notAvailable
		},
		{
			label: copy.labels.latestExportStatus,
			value: campaign.latestExportArtifactStatus
				? formatCodeLabel(campaign.latestExportArtifactStatus, copy)
				: copy.notAvailable
		},
		{
			label: copy.labels.latestExportCreated,
			value: formatNullableDateTime(campaign.latestExportArtifactCreatedAt, copy)
		},
		{
			label: copy.labels.latestExportCompleted,
			value: formatNullableDateTime(campaign.latestExportArtifactCompletedAt, copy)
		},
		{
			label: copy.labels.latestExportStarted,
			value: formatNullableDateTime(campaign.latestExportArtifactStartedAt, copy)
		},
		{
			label: copy.labels.latestExportFailed,
			value: formatNullableDateTime(campaign.latestExportArtifactFailedAt, copy)
		},
		{
			label: copy.labels.latestExportExpires,
			value: formatNullableDateTime(campaign.latestExportArtifactExpiresAt, copy)
		},
		{
			label: copy.labels.latestExportDeleted,
			value: formatNullableDateTime(campaign.latestExportArtifactDeletedAt, copy)
		},
		{
			label: copy.labels.latestExportFailureReason,
			value: campaign.latestExportArtifactFailureReasonCode ?? copy.notAvailable
		},
		{
			label: copy.labels.latestExportDownloadable,
			value: formatBoolean(campaign.latestExportArtifactCanDownload, copy)
		}
	];
}

function toReportArtifactRegistry(
	artifacts: CampaignSeriesReportsExportArtifactResponse[],
	copy: SelectedSeriesReportSnapshotCopy
): SelectedSeriesReportArtifactRegistryItem[] {
	return [...artifacts]
		.sort((left, right) => {
			const createdOrder = Date.parse(right.createdAt) - Date.parse(left.createdAt);
			return createdOrder === 0 ? left.id.localeCompare(right.id) : createdOrder;
		})
		.map((artifact) => ({
			id: artifact.id,
			targetKind: artifact.targetKind,
			targetId: artifact.targetId,
			targetLabel: artifact.targetLabel.trim() || copy.untitledTarget,
			campaignId: artifact.campaignId,
			campaignName: artifact.campaignName?.trim() || null,
			title: artifact.fileName.trim() || copy.untitledExportFile,
			badgeStatus: toExportArtifactBadgeStatus(artifact.status),
			badgeLabel: copy.artifactStatusLabels[artifact.status] ?? formatCodeLabel(artifact.status, copy),
			meta: [
				formatExportFileTypeLabel(artifact.artifactType, copy),
				formatCodeLabel(artifact.format, copy),
				copy.rowsUnit(artifact.rowCount, formatCount(artifact.rowCount, copy)),
				copy.bytesUnit(artifact.byteSize, formatCount(artifact.byteSize, copy))
			],
			rows: [
				idRow(copy.labels.exportRecord, artifact.id, copy),
				{ label: copy.labels.studyContext, value: artifact.targetLabel.trim() || copy.untitledTarget },
				{ label: copy.labels.contextType, value: formatCodeLabel(artifact.targetKind, copy) },
				{ label: copy.labels.created, value: formatNullableDateTime(artifact.createdAt, copy) },
				{ label: copy.labels.completed, value: formatNullableDateTime(artifact.completedAt, copy) },
				{ label: copy.labels.started, value: formatNullableDateTime(artifact.startedAt, copy) },
				{ label: copy.labels.failed, value: formatNullableDateTime(artifact.failedAt, copy) },
				{ label: copy.labels.expires, value: formatNullableDateTime(artifact.expiresAt, copy) },
				{ label: copy.labels.deleted, value: formatNullableDateTime(artifact.deletedAt, copy) },
				{ label: copy.labels.failureReason, value: artifact.failureReasonCode ?? copy.notAvailable },
				{ label: copy.labels.downloadable, value: formatBoolean(artifact.canDownload, copy) },
				idRow(copy.labels.checksum, artifact.checksumSha256, copy)
			]
		}));
}

function idRow(
	label: string,
	value: string | null,
	copy: SelectedSeriesReportSnapshotCopy
): SelectedSeriesReportDashboardRow {
	return value ? { label, value, mono: true } : { label, value: copy.notAvailable };
}

function formatCount(value: number, copy: SelectedSeriesReportSnapshotCopy) {
	return formatAppNumber(value, copy.locale);
}

function formatBoolean(value: boolean, copy: SelectedSeriesReportSnapshotCopy) {
	return value ? copy.yes : copy.no;
}

function formatCodeLabel(value: string, copy: SelectedSeriesReportSnapshotCopy) {
	return copy.codeLabels[value] ?? value.replaceAll('_', ' ');
}

function formatExportFileTypeLabel(value: string, copy: SelectedSeriesReportSnapshotCopy) {
	return copy.exportFileTypeLabels[value] ?? formatCodeLabel(value, copy);
}

function formatNullableDateTime(
	value: string | null | undefined,
	copy: SelectedSeriesReportSnapshotCopy
) {
	if (!value) {
		return copy.notAvailable;
	}

	const date = new Date(value);
	if (Number.isNaN(date.getTime())) {
		return value;
	}

	return new Intl.DateTimeFormat('hr-HR', {
		day: '2-digit',
		month: '2-digit',
		year: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		hour12: false
	}).format(date);
}

function toExportArtifactBadgeStatus(status: string): ProductReadModelBadgeStatus {
	switch (status) {
		case 'succeeded':
		case 'completed':
			return 'ready';
		case 'failed':
			return 'failed';
		case 'pending':
		case 'queued':
		case 'running':
		case 'started':
			return 'pending';
		default:
			return 'pending';
	}
}
