export interface AutonomousProductApiResponse {
  status: number;
  json: Record<string, unknown> | Array<Record<string, unknown>>;
}

const tenantId = '11111111-1111-4111-8111-111111111111';
const campaignId = '1c6fe0ec-6412-4b03-9b87-7613d9bfe0a2';
const comparisonCampaignId = '6d3271db-494f-401d-af8b-a5c86c9293a8';
const scoringRuleId = '716b2246-70f7-4728-9f44-150bd3b8da7a';
const templateVersionId = '68933bc3-522b-4974-b7a7-04a7eaf52edc';

export const autonomousProductPaths = {
  completedSampleSeriesId: '2f2f819f-f6eb-486a-9e0f-872ac30af3d4',
  setupSampleSeriesId: '019ad5b6-7f00-7000-8a00-000000000101',
  collectionSampleSeriesId: '019ad5b6-7f00-7000-8a00-000000000102',
  longitudinalSampleSeriesId: '019ad5b6-7f00-7000-8a00-000000000103',
  ownStudySeriesId: '6a82f6e0-4712-4c3e-9d20-53715d5c96f3',
  get completedSampleSetup() {
    return `/app/campaign-series/${this.completedSampleSeriesId}/setup`;
  },
  get completedSampleResults() {
    return `/app/campaign-series/${this.completedSampleSeriesId}/reports`;
  },
  get setupSampleSetup() {
    return `/app/campaign-series/${this.setupSampleSeriesId}/setup`;
  },
  get collectionSampleCollect() {
    return `/app/campaign-series/${this.collectionSampleSeriesId}/operations`;
  },
  get longitudinalSampleWaves() {
    return `/app/campaign-series/${this.longitudinalSampleSeriesId}/waves`;
  },
  get ownStudySetup() {
    return `/app/campaign-series/${this.ownStudySeriesId}/setup`;
  },
  exports: '/app/exports',
} as const;

export function resolveAutonomousProductApiResponse(
  method: string,
  pathOrUrl: string
): AutonomousProductApiResponse | undefined {
  const normalizedMethod = method.toUpperCase();
  const path = toApiPath(pathOrUrl);

  if (normalizedMethod !== 'GET') {
    return undefined;
  }

  if (path === '/workspace-overview') {
    return ok(workspaceOverview());
  }

  if (path === '/campaign-series') {
    return ok({
      items: [
        studyListItem(
          autonomousProductPaths.setupSampleSeriesId,
          'Setup readiness sample',
          sampleOwnership('setup-readiness'),
          { campaignCount: 0, submittedResponseCount: 0, readinessStatus: 'not_configured' }
        ),
        studyListItem(
          autonomousProductPaths.collectionSampleSeriesId,
          'Collection in progress sample',
          sampleOwnership('collection-in-progress'),
          { campaignCount: 1, liveCampaignCount: 1, readinessStatus: 'ready' }
        ),
        studyListItem(
          autonomousProductPaths.completedSampleSeriesId,
          'Completed sample',
          sampleOwnership('completed-results'),
          { campaignCount: 2, submittedResponseCount: 128, readinessStatus: 'proof_only' }
        ),
        studyListItem(
          autonomousProductPaths.longitudinalSampleSeriesId,
          'Longitudinal wave sample',
          sampleOwnership('longitudinal-waves'),
          { campaignCount: 2, liveCampaignCount: 2, submittedResponseCount: 12 }
        ),
        studyListItem(autonomousProductPaths.ownStudySeriesId, 'New team study', ownOwnership(), {
          campaignCount: 0,
          submittedResponseCount: 0,
          readinessStatus: 'not_configured',
        }),
      ],
    });
  }

  if (path === '/export-artifacts') {
    return ok(exportArtifactLibrary());
  }

  if (path === '/subjects') {
    return ok(subjectDirectory());
  }

  if (path === '/subject-groups') {
    return ok(subjectGroupList());
  }

  const respondentRulesCampaignId = path.match(/^\/campaigns\/([^/]+)\/respondent-rules$/)?.[1];
  if (respondentRulesCampaignId) {
    return ok({ campaignId: respondentRulesCampaignId, rules: [] });
  }

  const assignmentsCampaignId = path.match(/^\/campaigns\/([^/]+)\/assignments$/)?.[1];
  if (assignmentsCampaignId) {
    return ok({ campaignId: assignmentsCampaignId, assignmentCount: 0, assignments: [] });
  }

  const previewMatch = path.match(
    /^\/campaign-series\/([^/]+)\/campaigns\/([^/]+)\/respondent-rule-preview$/
  );
  if (previewMatch) {
    return ok({
      campaignSeriesId: previewMatch[1],
      campaignId: previewMatch[2],
      ruleKind: 'self',
      role: 'self',
      summary: {
        targetCount: 1,
        respondentCount: 1,
        assignmentPairCount: 1,
        skippedCount: 0,
        warningCount: 0,
        truncated: false,
      },
      rows: [
        {
          ordinal: 1,
          ruleKind: 'self',
          role: 'self',
          target: subjectPreview(),
          respondent: subjectPreview(),
        },
      ],
      warnings: [],
    });
  }

  const seriesPath = path.match(/^\/campaign-series\/([^/]+)(?:\/([^/]+))?$/);
  if (!seriesPath) {
    return undefined;
  }

  const seriesId = decodeURIComponent(seriesPath[1]);
  const suffix = seriesPath[2] ?? '';

  if (!isKnownSeries(seriesId)) {
    return undefined;
  }

  if (!suffix) {
    return ok(campaignSeriesHub(seriesId));
  }

  if (suffix === 'setup-workspace') {
    return ok(setupWorkspace(seriesId));
  }

  if (suffix === 'operations-workspace') {
    return ok(operationsWorkspace(seriesId));
  }

  if (suffix === 'reports-workspace') {
    return ok(reportsWorkspace(seriesId));
  }

  if (suffix === 'reports-widget-manifest') {
    return ok(reportsWidgetManifest(seriesId));
  }

  if (suffix === 'waves-workspace') {
    return ok(wavesWorkspace(seriesId));
  }

  if (suffix === 'two-wave-proof') {
    return ok(twoWaveProof(seriesId));
  }

  if (suffix === 'wave-comparison-proof') {
    return ok(waveComparisonProof(seriesId));
  }

  return undefined;
}

function workspaceOverview() {
  return {
    tenantId,
    totals: {
      campaignSeriesCount: 5,
      campaignCount: 6,
      liveCampaignCount: 2,
      submittedResponseCount: 140,
      exportArtifactCount: 3,
    },
    commandCenter: {
      items: [
        {
          id: 'own-study-setup-command',
          title: 'Finish setup for New team study',
          description: 'Create the questionnaire, scoring rule, first wave, and launch check.',
          state: 'not_configured',
          surface: 'setup',
          route: autonomousProductPaths.ownStudySetup,
          actionLabel: 'Continue setup',
          priority: 10,
          campaignSeriesId: autonomousProductPaths.ownStudySeriesId,
          campaignId: null,
          requiredPermission: 'setup.manage',
        },
        {
          id: 'results-review-command',
          title: 'Review Completed sample results',
          description: 'Use the completed sample to assess result wording and export confidence.',
          state: 'ready',
          surface: 'reports',
          route: autonomousProductPaths.completedSampleResults,
          actionLabel: 'Open results',
          priority: 20,
          campaignSeriesId: autonomousProductPaths.completedSampleSeriesId,
          campaignId,
          requiredPermission: 'export.read',
        },
      ],
    },
    studyCollections: {
      sampleStudies: [
        studyListItem(
          autonomousProductPaths.setupSampleSeriesId,
          'Setup readiness sample',
          sampleOwnership('setup-readiness'),
          { campaignCount: 0, submittedResponseCount: 0, readinessStatus: 'not_configured' }
        ),
        studyListItem(
          autonomousProductPaths.collectionSampleSeriesId,
          'Collection in progress sample',
          sampleOwnership('collection-in-progress'),
          { campaignCount: 1, liveCampaignCount: 1, readinessStatus: 'ready' }
        ),
        studyListItem(
          autonomousProductPaths.completedSampleSeriesId,
          'Completed sample',
          sampleOwnership('completed-results'),
          { campaignCount: 2, submittedResponseCount: 128, readinessStatus: 'proof_only' }
        ),
        studyListItem(
          autonomousProductPaths.longitudinalSampleSeriesId,
          'Longitudinal wave sample',
          sampleOwnership('longitudinal-waves'),
          { campaignCount: 2, liveCampaignCount: 2, submittedResponseCount: 12 }
        ),
      ],
      ownStudies: [
        studyListItem(autonomousProductPaths.ownStudySeriesId, 'New team study', ownOwnership(), {
          campaignCount: 0,
          submittedResponseCount: 0,
          readinessStatus: 'not_configured',
        }),
      ],
    },
    recentSeries: [
      studyListItem(autonomousProductPaths.completedSampleSeriesId, 'Quarterly pulse', ownOwnership(), {
        campaignCount: 2,
        submittedResponseCount: 128,
        readinessStatus: 'proof_only',
      }),
    ],
  };
}

function campaignSeriesHub(seriesId: string) {
  const name = seriesName(seriesId);
  const ownership = ownershipForSeries(seriesId);
  const submittedResponseCount = seriesId === autonomousProductPaths.ownStudySeriesId ? 0 : 128;

  return {
    id: seriesId,
    name,
    ...ownership,
    createdAt: '2026-05-01T08:00:00Z',
    updatedAt: '2026-05-12T09:30:00Z',
    totals: {
      campaignCount: seriesId === autonomousProductPaths.ownStudySeriesId ? 0 : 2,
      liveCampaignCount: seriesId === autonomousProductPaths.ownStudySeriesId ? 0 : 1,
      submittedResponseCount,
      scoreCount: submittedResponseCount > 0 ? 120 : 0,
      exportArtifactCount: submittedResponseCount > 0 ? 2 : 0,
    },
    governance: {
      consentStatus: seriesId === autonomousProductPaths.ownStudySeriesId ? 'not_configured' : 'configured',
      retentionStatus: seriesId === autonomousProductPaths.ownStudySeriesId ? 'not_configured' : 'configured',
      disclosureStatus: seriesId === autonomousProductPaths.ownStudySeriesId ? 'not_configured' : 'configured',
      scoringStatus: seriesId === autonomousProductPaths.ownStudySeriesId ? 'not_configured' : 'configured',
    },
    lifecycle: [
      lifecycleItem('setup', seriesId === autonomousProductPaths.ownStudySeriesId ? 'not_configured' : 'ready'),
      lifecycleItem('operations', submittedResponseCount > 0 ? 'ready' : 'blocked'),
      lifecycleItem('reports', submittedResponseCount > 0 ? 'proof_only' : 'blocked'),
      lifecycleItem(
        'waves',
        seriesId === autonomousProductPaths.longitudinalSampleSeriesId ? 'proof_only' : 'not_available'
      ),
    ],
    campaigns: seriesId === autonomousProductPaths.ownStudySeriesId ? [] : [campaignSummary(seriesId)],
    archived: false,
    archivedAt: null,
    archivedByUserId: null,
    archiveReason: null,
  };
}

function setupWorkspace(seriesId: string) {
  const empty = seriesId === autonomousProductPaths.ownStudySeriesId || seriesId === autonomousProductPaths.setupSampleSeriesId;
  const series = setupSeries(seriesId);

  if (empty) {
    return {
      series,
      summary: { campaignCount: 0, liveCampaignCount: 0, missingPrerequisiteCount: 5 },
      selectedCampaign: null,
      template: null,
      scoring: null,
      policies: {
        consent: { id: null, version: null, status: 'not_configured' },
        retention: { id: null, version: null, status: 'not_configured' },
        disclosure: { id: null, version: null, status: 'not_configured' },
      },
      readiness: { campaignId: null, status: 'not_available', ready: false },
      missingPrerequisites: [
        {
          code: 'instrument.missing',
          label: 'Questionnaire',
          message: 'Create or import the study questionnaire before scoring and launch.',
          severity: 'blocking',
        },
        {
          code: 'campaign.missing',
          label: 'First wave',
          message: 'Create the first collection wave after questionnaire and scoring are ready.',
          severity: 'blocking',
        },
      ],
      campaigns: [],
    };
  }

  return {
    series,
    summary: { campaignCount: 1, liveCampaignCount: 0, missingPrerequisiteCount: 0 },
    selectedCampaign: setupCampaign('draft'),
    template: {
      templateId: '2a642f70-90ca-4aa7-b7c1-84084360a1a9',
      templateVersionId,
      templateName: 'Tenant burnout pulse template',
      semver: '1.0.0',
      status: 'draft',
      defaultLocale: 'en',
      instrumentId: null,
      questionCount: 5,
    },
    scoring: {
      id: scoringRuleId,
      ruleKey: 'burnout.total',
      ruleVersion: '1.0.0',
      status: 'draft',
      source: 'template_version',
    },
    policies: configuredPolicies(),
    readiness: { campaignId, status: 'ready', ready: true },
    missingPrerequisites: [],
    campaigns: [setupCampaign('draft')],
  };
}

function operationsWorkspace(seriesId: string) {
  const empty = seriesId === autonomousProductPaths.ownStudySeriesId || seriesId === autonomousProductPaths.setupSampleSeriesId;
  const submittedResponseCount = empty ? 0 : seriesId === autonomousProductPaths.collectionSampleSeriesId ? 0 : 128;
  const liveCampaignCount = empty ? 0 : 1;

  return {
    series: setupSeries(seriesId),
    summary: {
      campaignCount: empty ? 0 : 1,
      liveCampaignCount,
      openLinkAssignmentCount: empty ? 0 : 1,
      queuedInvitationCount: empty ? 0 : 1,
      sentInvitationCount: empty ? 0 : 8,
      failedInvitationCount: 0,
      deliveryAttemptCount: empty ? 0 : 8,
      startedResponseCount: submittedResponseCount,
      draftResponseCount: 0,
      submittedResponseCount,
      latestResponseStartedAt: submittedResponseCount ? '2026-05-05T10:45:00Z' : null,
      latestResponseSubmittedAt: submittedResponseCount ? '2026-05-05T10:40:00Z' : null,
      collectionStatus: empty ? 'not_available' : submittedResponseCount ? 'has_submissions' : 'live_no_submissions',
      reportVisibilityStatus: submittedResponseCount ? 'ready_for_aggregate_report' : 'collecting',
      collectionGuidance: submittedResponseCount
        ? 'Enough submitted responses exist for aggregate report visibility.'
        : 'Collection is live; share respondent access and watch submissions arrive.',
      missingPrerequisiteCount: empty ? 1 : 0,
    },
    selectedCampaign: empty ? null : operationsCampaign(seriesId, submittedResponseCount),
    missingPrerequisites: empty
      ? [
          {
            code: 'campaign.missing',
            label: 'Campaign wave',
            message: 'Create and launch a campaign wave before collection can start.',
            severity: 'blocking',
          },
        ]
      : [],
    campaigns: empty ? [] : [operationsCampaign(seriesId, submittedResponseCount)],
    scoreCoverage: scoreCoverage(submittedResponseCount),
  };
}

function reportsWorkspace(seriesId: string) {
  const empty = seriesId === autonomousProductPaths.ownStudySeriesId || seriesId === autonomousProductPaths.setupSampleSeriesId;
  const submittedResponseCount = empty ? 0 : 128;
  const selectedCampaign = empty ? null : reportCampaign(seriesId);

  return {
    series: setupSeries(seriesId),
    summary: {
      campaignCount: empty ? 0 : 2,
      liveCampaignCount: empty ? 0 : 1,
      reportableCampaignCount: empty ? 0 : 1,
      submittedResponseCount,
      scoreCount: empty ? 0 : 120,
      exportArtifactCount: empty ? 0 : 2,
      visibleScoreCount: empty ? 0 : 115,
      suppressedScoreCount: empty ? 0 : 5,
      missingPrerequisiteCount: empty ? 2 : 0,
      preliminaryLiveReportCount: empty ? 0 : 1,
      closedWaveReportCount: seriesId === autonomousProductPaths.completedSampleSeriesId ? 1 : 0,
    },
    selectedCampaign,
    missingPrerequisites: empty
      ? [
          {
            code: 'submitted_responses.missing',
            label: 'Submitted responses',
            message: 'Collect responses before aggregate results can be shown.',
            severity: 'blocking',
          },
        ]
      : [],
    exportArtifacts: empty ? [] : [exportArtifact()],
    campaigns: empty ? [] : [reportCampaign(seriesId), reportDraftCampaign()],
    scoreCoverage: scoreCoverage(submittedResponseCount),
  };
}

function reportsWidgetManifest(seriesId: string) {
  const reports = reportsWorkspace(seriesId);
  return {
    campaignSeriesId: seriesId,
    surface: 'reports',
    surfaceVersion: 'reports-widget-manifest/v1',
    layout: { kind: 'dashboard-grid/v1', density: 'standard' },
    widgets: [
      {
        id: 'report-readiness-summary',
        kind: 'report-readiness-summary/v1',
        title: 'Report readiness',
        size: 'half',
        state: reports.selectedCampaign ? 'ready' : 'blocked',
        message: reports.selectedCampaign ? null : 'Collect and score responses before results can be reviewed.',
        data: {
          campaignCount: reports.summary.campaignCount,
          liveCampaignCount: reports.summary.liveCampaignCount,
          reportableCampaignCount: reports.summary.reportableCampaignCount,
          submittedResponseCount: reports.summary.submittedResponseCount,
          scoreCount: reports.summary.scoreCount,
          visibleScoreCount: reports.summary.visibleScoreCount,
          suppressedScoreCount: reports.summary.suppressedScoreCount,
          missingPrerequisiteCount: reports.summary.missingPrerequisiteCount,
          missingPrerequisites: reports.missingPrerequisites,
        },
        dataSource: null,
        actions: [],
      },
      {
        id: 'export-artifact-registry',
        kind: 'export-artifact-registry/v1',
        title: 'Export artifact registry',
        size: 'full',
        state: reports.exportArtifacts.length ? 'ready' : 'empty',
        message: reports.exportArtifacts.length ? null : 'No export file exists yet.',
        data: {
          exportArtifactCount: reports.summary.exportArtifactCount,
          artifacts: reports.exportArtifacts,
        },
        dataSource: null,
        actions: [],
      },
    ],
  };
}

function wavesWorkspace(seriesId: string) {
  const hasComparison = seriesId === autonomousProductPaths.longitudinalSampleSeriesId;
  return {
    series: setupSeries(seriesId),
    summary: {
      campaignCount: hasComparison ? 2 : 1,
      liveCampaignCount: hasComparison ? 2 : 0,
      longitudinalWaveCount: hasComparison ? 2 : 0,
      submittedWaveCount: hasComparison ? 2 : 1,
      linkedTrajectoryCount: hasComparison ? 6 : 0,
      completeTrajectoryCount: hasComparison ? 6 : 0,
      comparableScoreCount: hasComparison ? 1 : 0,
      visibleComparisonCount: hasComparison ? 1 : 0,
      suppressedComparisonCount: 0,
      blockedComparisonCount: hasComparison ? 0 : 1,
      missingPrerequisiteCount: hasComparison ? 0 : 1,
    },
    selectedBaselineWave: hasComparison ? wave(campaignId, 'Pulse wave 1') : null,
    selectedComparisonWave: hasComparison ? wave(comparisonCampaignId, 'Pulse wave 2') : null,
    comparison: {
      status: hasComparison ? 'proof_only' : 'not_available',
      disclosureState: hasComparison ? 'visible' : 'not_available',
      compatibilityState: hasComparison ? 'compatible' : 'not_available',
      interpretationStatus: hasComparison ? 'not_validated_interpretation' : 'not_available',
      disclosureKMin: hasComparison ? 5 : null,
      linkedPairCount: hasComparison ? 6 : 0,
      visibleScoreCount: hasComparison ? 1 : 0,
      suppressedScoreCount: 0,
      blockedScoreCount: hasComparison ? 0 : 1,
    },
    missingPrerequisites: hasComparison
      ? []
      : [
          {
            code: 'waves.missing',
            label: 'Second wave',
            message: 'Launch at least two waves before comparison is available.',
            severity: 'blocking',
          },
        ],
    waves: hasComparison ? [wave(campaignId, 'Pulse wave 1'), wave(comparisonCampaignId, 'Pulse wave 2')] : [],
  };
}

function exportArtifactLibrary() {
  return {
    tenantId,
    summary: { totalCount: 1, downloadableCount: 1, failedCount: 0, pendingCount: 0 },
    artifacts: [exportArtifact()],
  };
}

function subjectDirectory() {
  return {
    tenantId,
    summary: {
      subjectCount: 2,
      groupCount: 1,
      managerRelationshipCount: 0,
    },
    subjects: [
      {
        id: 'subject-1',
        displayName: 'Respondent 1',
        email: null,
        externalId: 'R-001',
        locale: 'en',
        attributes: '{}',
        managerSubjectId: null,
        managerDisplayName: null,
        directReportCount: 0,
        groups: [
          {
            groupId: 'group-1',
            groupType: 'team',
            groupName: 'Research team',
            roleInGroup: 'member',
            validFrom: null,
            validTo: null,
          },
        ],
      },
      {
        id: 'subject-2',
        displayName: 'Respondent 2',
        email: null,
        externalId: 'R-002',
        locale: 'en',
        attributes: '{}',
        managerSubjectId: null,
        managerDisplayName: null,
        directReportCount: 0,
        groups: [
          {
            groupId: 'group-1',
            groupType: 'team',
            groupName: 'Research team',
            roleInGroup: 'member',
            validFrom: null,
            validTo: null,
          },
        ],
      },
    ],
  };
}

function subjectGroupList() {
  return {
    tenantId,
    groups: [
      {
        id: 'group-1',
        type: 'team',
        name: 'Research team',
        parentGroupId: null,
        attributes: '{}',
        memberCount: 2,
      },
    ],
  };
}

function twoWaveProof(seriesId: string) {
  const waves = seriesId === autonomousProductPaths.longitudinalSampleSeriesId
    ? [wave(campaignId, 'Pulse wave 1'), wave(comparisonCampaignId, 'Pulse wave 2')]
    : [];
  return {
    campaignSeriesId: seriesId,
    proofStatus: waves.length === 2 ? 'proof_only' : 'not_available',
    expectedWaveCount: 2,
    launchedWaveCount: waves.length,
    submittedWaveCount: waves.length,
    linkedTrajectoryCount: waves.length === 2 ? 6 : 0,
    completeTrajectoryCount: waves.length === 2 ? 6 : 0,
    waves: waves.map((entry) => ({
      campaignId: entry.id,
      name: entry.name,
      status: entry.status,
      responseIdentityMode: entry.responseIdentityMode,
      submittedResponseCount: entry.submittedResponseCount,
    })),
  };
}

function waveComparisonProof(seriesId: string) {
  const hasComparison = seriesId === autonomousProductPaths.longitudinalSampleSeriesId;
  return {
    campaignSeriesId: seriesId,
    proofStatus: hasComparison ? 'proof_only' : 'not_available',
    interpretationStatus: hasComparison ? 'not_validated_interpretation' : 'not_available',
    baselineWave: hasComparison ? wave(campaignId, 'Pulse wave 1') : null,
    comparisonWave: hasComparison ? wave(comparisonCampaignId, 'Pulse wave 2') : null,
    disclosurePolicy: hasComparison
      ? { id: 'wave-disclosure-policy-id', version: '1.0.0', kMin: 5, suppressionStrategy: 'suppress_small_groups' }
      : null,
    scores: hasComparison
      ? [
          {
            dimensionCode: 'burnout.total',
            compatibilityStatus: 'compatible',
            disclosure: 'visible',
            baselineSubmittedResponseCount: 6,
            comparisonSubmittedResponseCount: 6,
            linkedPairCount: 6,
            baselineScoreCount: 6,
            comparisonScoreCount: 6,
            baselineMean: 2.4,
            comparisonMean: 2.1,
            aggregateDelta: -0.3,
            pairedDeltaMean: -0.2,
            suppressionReason: null,
            compatibilityReason: null,
          },
        ]
      : [],
  };
}

function setupSeries(seriesId: string) {
  return {
    id: seriesId,
    name: seriesName(seriesId),
    ...ownershipForSeries(seriesId),
    createdAt: '2026-05-01T08:00:00Z',
    updatedAt: '2026-05-12T09:30:00Z',
  };
}

function setupCampaign(status: string) {
  return {
    id: campaignId,
    name: 'Pulse wave 1',
    status,
    responseIdentityMode: 'anonymous',
    defaultLocale: 'en',
    templateVersionId,
    latestLaunchAt: status === 'draft' ? null : '2026-05-05T10:15:00Z',
  };
}

function operationsCampaign(seriesId: string, submittedResponseCount: number) {
  return {
    id: campaignId,
    name: seriesId === autonomousProductPaths.collectionSampleSeriesId ? 'Collection wave 1' : 'Pulse wave 1',
    status: 'live',
    responseIdentityMode: 'anonymous',
    defaultLocale: 'en',
    latestLaunchSnapshotId: 'launch-snapshot-id',
    latestLaunchAt: '2026-05-05T10:15:00Z',
    launchSnapshot: {
      id: 'launch-snapshot-id',
      templateVersionId,
      scoringRuleId,
      scoringRuleDocumentHash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
      consentDocumentId: 'consent-policy-id',
      retentionPolicyId: 'retention-policy-id',
      disclosurePolicyId: 'disclosure-policy-id',
      responseIdentityMode: 'anonymous',
      defaultLocale: 'en',
      templateQuestionCount: 8,
      launchedAt: '2026-05-05T10:15:00Z',
      launchedByUserId: null,
    },
    startedResponseCount: submittedResponseCount,
    draftResponseCount: 0,
    submittedResponseCount,
    latestResponseStartedAt: submittedResponseCount ? '2026-05-05T10:45:00Z' : null,
    latestResponseSubmittedAt: submittedResponseCount ? '2026-05-05T10:40:00Z' : null,
    collectionStatus: submittedResponseCount ? 'has_submissions' : 'live_no_submissions',
    reportVisibilityStatus: submittedResponseCount ? 'ready_for_aggregate_report' : 'collecting',
    collectionGuidance: submittedResponseCount
      ? 'Enough submitted responses exist for aggregate report visibility.'
      : 'Collection is live; wait for respondent submissions.',
    openLinkAssignmentCount: 1,
    queuedInvitationCount: submittedResponseCount ? 0 : 1,
    sentInvitationCount: submittedResponseCount ? 8 : 0,
    failedInvitationCount: 0,
    deliveryAttemptCount: submittedResponseCount ? 8 : 0,
    latestDeliveryAttemptAt: submittedResponseCount ? '2026-05-05T10:20:00Z' : null,
    scoringRuleId,
    scoredSubmittedResponseCount: Math.min(submittedResponseCount, 120),
    unscoredSubmittedResponseCount: Math.max(0, submittedResponseCount - 120),
    notConfiguredSubmittedResponseCount: 0,
    latestScoringActivityAt: submittedResponseCount ? '2026-05-05T10:50:00Z' : null,
    scoreCoverageStatus: submittedResponseCount ? 'partial' : 'not_started',
  };
}

function reportCampaign(seriesId: string) {
  return {
    id: campaignId,
    name: seriesId === autonomousProductPaths.longitudinalSampleSeriesId ? 'Pulse wave 1' : 'Pulse wave 1',
    status: 'closed',
    responseIdentityMode:
      seriesId === autonomousProductPaths.longitudinalSampleSeriesId ? 'anonymous_longitudinal' : 'anonymous',
    defaultLocale: 'en',
    latestLaunchSnapshotId: 'launch-snapshot-id',
    latestLaunchAt: '2026-05-05T10:15:00Z',
    scoringRuleId,
    consentDocumentId: 'consent-policy-id',
    retentionPolicyId: 'retention-policy-id',
    disclosurePolicyId: 'disclosure-policy-id',
    submittedResponseCount: 128,
    scoreCount: 120,
    exportArtifactCount: 2,
    visibleScoreCount: 115,
    suppressedScoreCount: 5,
    disclosureState: 'visible',
    disclosureKMin: 5,
    reportStatus: 'proof_only',
    interpretationStatus: 'not_validated_interpretation',
    latestExportArtifactId: '8e592f74-d0ca-4204-aead-fb00e9e5085a',
    latestExportArtifactFileName: 'report-proof.csv',
    latestExportArtifactStatus: 'succeeded',
    latestExportArtifactCreatedAt: '2026-05-05T11:00:00Z',
    latestExportArtifactCompletedAt: '2026-05-05T11:00:03Z',
    latestExportArtifactStartedAt: null,
    latestExportArtifactFailedAt: null,
    latestExportArtifactExpiresAt: null,
    latestExportArtifactDeletedAt: null,
    latestExportArtifactFailureReasonCode: null,
    latestExportArtifactCanDownload: true,
    closedAt: '2026-05-07T12:00:00Z',
    dataFinality: 'closed_wave',
  };
}

function reportDraftCampaign() {
  return {
    ...reportCampaign(autonomousProductPaths.completedSampleSeriesId),
    id: comparisonCampaignId,
    name: 'Draft wave',
    status: 'draft',
    submittedResponseCount: 0,
    scoreCount: 0,
    exportArtifactCount: 0,
    visibleScoreCount: 0,
    suppressedScoreCount: 0,
    disclosureState: 'not_available',
    reportStatus: 'blocked',
    interpretationStatus: 'not_available',
    latestExportArtifactId: null,
    latestExportArtifactFileName: null,
    latestExportArtifactStatus: null,
    latestExportArtifactCanDownload: false,
    closedAt: null,
    dataFinality: 'not_available',
  };
}

function wave(id: string, name: string) {
  return {
    id,
    campaignId: id,
    name,
    status: 'closed',
    responseIdentityMode: 'anonymous_longitudinal',
    defaultLocale: 'en',
    latestLaunchSnapshotId: `${id}-launch`,
    latestLaunchAt: name.endsWith('2') ? '2026-05-12T10:15:00Z' : '2026-05-05T10:15:00Z',
    launchedAt: name.endsWith('2') ? '2026-05-12T10:15:00Z' : '2026-05-05T10:15:00Z',
    scoringRuleId,
    scoringRuleKey: 'burnout.total',
    scoringRuleVersion: '1.0.0',
    scoringRuleDocumentHash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
    disclosurePolicyId: 'wave-disclosure-policy-id',
    disclosureKMin: 5,
    submittedResponseCount: 6,
    scoreCount: 6,
    linkedTrajectoryCount: 6,
    waveState: 'wave',
  };
}

function scoreCoverage(submittedResponseCount: number) {
  return {
    submittedResponseCount,
    scoredSubmittedResponseCount: Math.min(submittedResponseCount, 120),
    unscoredSubmittedResponseCount: Math.max(0, submittedResponseCount - 120),
    notConfiguredSubmittedResponseCount: 0,
    campaignsWithScoringRuleCount: submittedResponseCount ? 1 : 0,
    campaignsWithoutScoringRuleCount: submittedResponseCount ? 1 : 0,
    latestScoringActivityAt: submittedResponseCount ? '2026-05-05T10:50:00Z' : null,
    status: submittedResponseCount ? 'partial' : 'not_started',
    guidance: submittedResponseCount
      ? 'Some submitted responses still need scoring activity before score-dependent reports are complete.'
      : 'No submitted responses are available for scoring yet.',
  };
}

function exportArtifact() {
  return {
    id: '8e592f74-d0ca-4204-aead-fb00e9e5085a',
    targetKind: 'campaign',
    targetId: campaignId,
    targetLabel: 'Pulse wave 1',
    campaignId,
    campaignName: 'Pulse wave 1',
    artifactType: 'report_proof_csv_codebook',
    status: 'succeeded',
    format: 'csv_codebook',
    fileName: 'report-proof.csv',
    rowCount: 120,
    byteSize: 2048,
    checksumSha256: 'checksum-sha256',
    createdAt: '2026-05-05T11:00:00Z',
    completedAt: '2026-05-05T11:00:03Z',
    startedAt: null,
    failedAt: null,
    expiresAt: null,
    deletedAt: null,
    failureReasonCode: null,
    canDownload: true,
  };
}

function configuredPolicies() {
  return {
    consent: { id: 'consent-policy-id', version: '1.0.0', status: 'configured' },
    retention: { id: 'retention-policy-id', version: '1.0.0', status: 'configured' },
    disclosure: { id: 'disclosure-policy-id', version: '1.0.0', status: 'configured' },
  };
}

function lifecycleItem(id: 'setup' | 'operations' | 'reports' | 'waves', status: string) {
  const labels = {
    setup: 'Setup',
    operations: 'Collect',
    reports: 'Results',
    waves: 'Waves',
  };
  return {
    id,
    label: labels[id],
    status,
    guidance: status === 'ready' || status === 'proof_only'
      ? `${labels[id]} is ready to review.`
      : `${labels[id]} needs prerequisite work before it is ready.`,
    route: id,
    actionLabel: `Open ${labels[id]}`,
  };
}

function campaignSummary(seriesId: string) {
  return {
    id: campaignId,
    name: seriesId === autonomousProductPaths.collectionSampleSeriesId ? 'Collection wave 1' : 'Pulse wave 1',
    status: seriesId === autonomousProductPaths.completedSampleSeriesId ? 'closed' : 'live',
    responseIdentityMode:
      seriesId === autonomousProductPaths.longitudinalSampleSeriesId ? 'anonymous_longitudinal' : 'anonymous',
    defaultLocale: 'en',
    startAt: '2026-05-05T08:00:00Z',
    endAt: '2026-05-15T18:00:00Z',
    latestLaunchAt: '2026-05-05T10:15:00Z',
    submittedResponseCount: seriesId === autonomousProductPaths.collectionSampleSeriesId ? 0 : 128,
    scoreCount: seriesId === autonomousProductPaths.collectionSampleSeriesId ? 0 : 120,
    exportArtifactCount: seriesId === autonomousProductPaths.collectionSampleSeriesId ? 0 : 2,
  };
}

function studyListItem(
  id: string,
  name: string,
  ownership: ReturnType<typeof ownOwnership>,
  overrides: Partial<{
    campaignCount: number;
    liveCampaignCount: number;
    submittedResponseCount: number;
    readinessStatus: string;
  }> = {}
) {
  return {
    id,
    name,
    ...ownership,
    createdAt: '2026-05-01T08:00:00Z',
    updatedAt: '2026-05-12T09:30:00Z',
    campaignCount: overrides.campaignCount ?? 1,
    liveCampaignCount: overrides.liveCampaignCount ?? 0,
    submittedResponseCount: overrides.submittedResponseCount ?? 0,
    latestLaunchAt: overrides.liveCampaignCount ? '2026-05-05T10:15:00Z' : null,
    latestSubmissionAt: overrides.submittedResponseCount ? '2026-05-07T11:20:00Z' : null,
    readinessStatus: overrides.readinessStatus ?? 'ready',
    archived: false,
    archivedAt: null,
    archivedByUserId: null,
    archiveReason: null,
  };
}

function ownOwnership() {
  return {
    studyKind: 'own',
    isSample: false,
    sampleScenario: null,
    readOnlyReason: null,
  };
}

function sampleOwnership(sampleScenario: string) {
  return {
    studyKind: 'sample',
    isSample: true,
    sampleScenario,
    readOnlyReason: 'Read-only local sample for UX agent review.',
  };
}

function ownershipForSeries(seriesId: string) {
  if (seriesId === autonomousProductPaths.ownStudySeriesId) {
    return ownOwnership();
  }

  if (seriesId === autonomousProductPaths.completedSampleSeriesId) {
    return ownOwnership();
  }

  return sampleOwnership(seriesId);
}

function seriesName(seriesId: string) {
  if (seriesId === autonomousProductPaths.ownStudySeriesId) {
    return 'New team study';
  }

  if (seriesId === autonomousProductPaths.setupSampleSeriesId) {
    return 'Setup readiness sample';
  }

  if (seriesId === autonomousProductPaths.collectionSampleSeriesId) {
    return 'Collection in progress sample';
  }

  if (seriesId === autonomousProductPaths.longitudinalSampleSeriesId) {
    return 'Longitudinal wave sample';
  }

  return 'Quarterly pulse';
}

function subjectPreview() {
  return {
    id: 'subject-1',
    label: 'Respondent 1',
    displayName: 'Respondent 1',
    email: null,
    externalId: 'R-001',
  };
}

function isKnownSeries(seriesId: string) {
  return [
    autonomousProductPaths.completedSampleSeriesId,
    autonomousProductPaths.setupSampleSeriesId,
    autonomousProductPaths.collectionSampleSeriesId,
    autonomousProductPaths.longitudinalSampleSeriesId,
    autonomousProductPaths.ownStudySeriesId,
  ].includes(seriesId);
}

function ok(json: Record<string, unknown> | Array<Record<string, unknown>>): AutonomousProductApiResponse {
  return { status: 200, json };
}

function toApiPath(pathOrUrl: string) {
  try {
    return new URL(pathOrUrl).pathname;
  } catch {
    return pathOrUrl.split(/[?#]/)[0] ?? '';
  }
}
