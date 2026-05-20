import { resolveFullstackDevAuthHeaders } from './fullstack-dev-auth.ts';
import {
  buildRealisticResponseSimulation,
  type RealisticAuditCase,
} from './realistic-cases.ts';
import type { AutonomousFullstackDevAuthOptions } from './types.ts';

export interface RealisticFullstackSeedOptions {
  apiBaseUrl: string;
  fullstackDevAuth: AutonomousFullstackDevAuthOptions;
  seriesId: string;
  realisticCase: RealisticAuditCase;
  mode?: 'single-wave' | 'repeated-wave';
  fetchImpl?: typeof fetch;
}

export interface RealisticFullstackSeedCampaignResult {
  campaignId: string;
  campaignName: string;
  launchSnapshotId: string;
  responseIdentityMode: 'anonymous' | 'anonymous_longitudinal';
  invitationCount: number;
  submittedResponseCount: number;
  scoredResponseCount: number;
  closed: boolean;
}

export interface RealisticFullstackSeedResult {
  seedMode: 'local-fullstack-api';
  seriesId: string;
  campaignId: string;
  campaignName: string;
  templateVersionId: string;
  scoringRuleId: string;
  launchSnapshotId: string;
  invitationCount: number;
  submittedResponseCount: number;
  scoredResponseCount: number;
  waveCount: number;
  campaigns: RealisticFullstackSeedCampaignResult[];
  operationsPath: string;
  wavesPath: string;
  reportsPath: string;
}

type ApiClient = ReturnType<typeof createSeedApiClient>;

const defaultApiBaseUrl = 'http://127.0.0.1:5055';

export async function seedRealisticFullstackCase(
  options: RealisticFullstackSeedOptions
): Promise<RealisticFullstackSeedResult> {
  const apiBaseUrl = normalizeLocalApiBaseUrl(options.apiBaseUrl || defaultApiBaseUrl);
  const client = createSeedApiClient(
    apiBaseUrl,
    options.fullstackDevAuth,
    options.fetchImpl ?? fetch
  );
  const simulation = buildRealisticResponseSimulation(options.realisticCase);
  const template = await createRealisticTemplate(client, options.realisticCase);
  const scoringRule = await client.post<{ id: string }>('/scoring-rules', {
    templateVersionId: template.templateVersionId,
    ruleKey: `${options.realisticCase.id}.results`,
    ruleVersion: '1.0.0',
    schemaVersion: 'scoring-rule/v1',
    engineMinVersion: 'engine/v1',
    document: JSON.stringify(buildScoringDocument(options.realisticCase)),
    produces: JSON.stringify({
      scores: dimensionCodes(options.realisticCase),
    }),
  });
  const repeatedWaveMode = options.mode === 'repeated-wave';
  const responseIdentityMode = repeatedWaveMode ? 'anonymous_longitudinal' : 'anonymous';
  const campaignNames = repeatedWaveMode
    ? resolveRepeatedWaveCampaignNames(options.realisticCase)
    : [options.realisticCase.campaignName];
  const campaignResults: RealisticFullstackSeedCampaignResult[] = [];

  for (const [waveIndex, campaignName] of campaignNames.entries()) {
    const campaign = await client.post<{ id: string }>('/campaigns', {
      campaignSeriesId: options.seriesId,
      templateVersionId: template.templateVersionId,
      name: campaignName,
      responseIdentityMode,
      defaultLocale: 'en',
    });
    const readiness = await client.get<{ ready: boolean; issues?: unknown[] }>(
      `/campaigns/${campaign.id}/launch-readiness`
    );

    if (!readiness.ready) {
      throw new Error(
        `Realistic fullstack seed launch readiness failed for ${campaignName} with ${readiness.issues?.length ?? 0} issue(s).`
      );
    }

    const launch = await client.post<{ launchSnapshotId: string }>(
      `/campaigns/${campaign.id}/launch`,
      {}
    );
    const waveResponses = simulation.responses.map((response) =>
      waveIndex === 0
        ? response
        : shiftFollowUpResponse(options.realisticCase, response)
    );
    const openLink = repeatedWaveMode
      ? await client.post<{ token: string; respondentPath: string }>(
          `/campaigns/${campaign.id}/open-link`,
          {}
        )
      : undefined;
    const invitationBatch = repeatedWaveMode
      ? undefined
      : await client.post<{
          createdInvitationCount: number;
          invitations: Array<{ token: string; respondentPath: string }>;
        }>(`/campaigns/${campaign.id}/invitation-batches`, {
          recipients: waveResponses.map((response) => ({
            email: `${response.respondentKey}@example.test`,
          })),
        });
    let submittedResponseCount = 0;
    let scoredResponseCount = 0;

    for (const [index, response] of waveResponses.entries()) {
      const token = openLink?.token ?? invitationBatch?.invitations[index]?.token;
      if (!token) {
        throw new Error('Realistic fullstack seed did not receive enough invitation tokens.');
      }

      const entry = await client.get<{
        consentDocument: { id: string; requiredGrants: string[] };
        questions: Array<{ id: string; code: string }>;
      }>(`/respondent/open-links/${token}`, { publicEndpoint: true });
      const session = await client.post<{ id: string }>(
        `/respondent/open-links/${token}/sessions`,
        {
          locale: 'en',
          acceptedConsentDocumentId: entry.consentDocument.id,
          acceptedGrants: entry.consentDocument.requiredGrants,
          ...(repeatedWaveMode
            ? { participantCode: participantCodeFor(response.respondentKey) }
            : {}),
        },
        { publicEndpoint: true }
      );
      await client.put(`/respondent/open-links/${token}/sessions/${session.id}/answers`, {
        answers: entry.questions.map((question) => ({
          questionId: question.id,
          value: String(response.answers[template.caseQuestionIdByCode[question.code] ?? question.code] ?? 3),
        })),
      }, { publicEndpoint: true });
      await client.post(
        `/respondent/open-links/${token}/sessions/${session.id}/submit`,
        {
          timeTakenMs: 180000 + (campaignResults.length + submittedResponseCount) * 1000,
        },
        { publicEndpoint: true }
      );
      submittedResponseCount += 1;

      await client.post(`/respondent/sessions/${session.id}/scores`, {});
      scoredResponseCount += 1;
    }

    let closed = false;
    if (repeatedWaveMode) {
      await client.post(`/campaign-series/${options.seriesId}/campaigns/${campaign.id}/close`, {
        reason: `UX audit repeated-wave seed completed ${campaignName}`,
      });
      closed = true;
    }

    campaignResults.push({
      campaignId: campaign.id,
      campaignName,
      launchSnapshotId: launch.launchSnapshotId,
      responseIdentityMode,
      invitationCount: invitationBatch?.createdInvitationCount ?? waveResponses.length,
      submittedResponseCount,
      scoredResponseCount,
      closed,
    });
  }

  const primaryCampaign = campaignResults[0];
  if (!primaryCampaign) {
    throw new Error('Realistic fullstack seed did not create any campaigns.');
  }

  return {
    seedMode: 'local-fullstack-api',
    seriesId: options.seriesId,
    campaignId: primaryCampaign.campaignId,
    campaignName: primaryCampaign.campaignName,
    templateVersionId: template.templateVersionId,
    scoringRuleId: scoringRule.id,
    launchSnapshotId: primaryCampaign.launchSnapshotId,
    invitationCount: sumCampaignMetric(campaignResults, 'invitationCount'),
    submittedResponseCount: sumCampaignMetric(campaignResults, 'submittedResponseCount'),
    scoredResponseCount: sumCampaignMetric(campaignResults, 'scoredResponseCount'),
    waveCount: campaignResults.length,
    campaigns: campaignResults,
    operationsPath: `/app/campaign-series/${options.seriesId}/operations`,
    wavesPath: `/app/campaign-series/${options.seriesId}/waves`,
    reportsPath: `/app/campaign-series/${options.seriesId}/reports`,
  };
}

function createSeedApiClient(
  apiBaseUrl: string,
  fullstackDevAuth: AutonomousFullstackDevAuthOptions,
  fetchImpl: typeof fetch
) {
  const devHeaders = resolveFullstackDevAuthHeaders(fullstackDevAuth);
  if (!devHeaders) {
    throw new Error('Realistic fullstack seeding requires --fullstack-dev-auth.');
  }

  async function request<T>(
    method: string,
    path: string,
    body?: unknown,
    requestOptions: { publicEndpoint?: boolean } = {}
  ): Promise<T> {
    const response = await fetchImpl(new URL(path, `${apiBaseUrl}/`).toString(), {
      method,
      headers: {
        ...(requestOptions.publicEndpoint ? {} : devHeaders),
        ...(body === undefined ? {} : { 'content-type': 'application/json' }),
      },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    });

    if (!response.ok) {
      throw new Error(
        `Realistic fullstack seed API request failed: ${method} ${path} returned ${response.status} ${await response.text()}`
      );
    }

    return (await response.json()) as T;
  }

  return {
    get<T>(path: string, requestOptions?: { publicEndpoint?: boolean }) {
      return request<T>('GET', path, undefined, requestOptions);
    },
    post<T>(path: string, body: unknown, requestOptions?: { publicEndpoint?: boolean }) {
      return request<T>('POST', path, body, requestOptions);
    },
    put<T>(path: string, body: unknown, requestOptions?: { publicEndpoint?: boolean }) {
      return request<T>('PUT', path, body, requestOptions);
    },
  };
}

async function createRealisticTemplate(client: ApiClient, auditCase: RealisticAuditCase) {
  const response = await client.post<{
    templateVersionId: string;
    questions: Array<{ id: string; code: string }>;
  }>('/template-versions', {
    templateName: auditCase.instrumentName,
    semver: '1.0.0',
    defaultLocale: 'en',
    instrumentId: null,
    sections: Array.from(new Set(auditCase.questions.map((question) => question.dimension))).map(
      (dimension, index) => ({
        ordinal: index + 1,
        code: codeForDimension(dimension),
        titleDefault: dimension,
      })
    ),
    scales: [
      {
        code: 'agreement_5',
        type: 'likert',
        minValue: 1,
        maxValue: 5,
        step: 1,
        naAllowed: false,
        anchors: JSON.stringify([
          { value: 1, label: 'Strongly disagree' },
          { value: 5, label: 'Strongly agree' },
        ]),
      },
    ],
    questions: auditCase.questions.map((question, index) => ({
      ordinal: index + 1,
      code: codeForQuestion(index),
      type: 'likert',
      textDefault: question.prompt,
      sectionCode: codeForDimension(question.dimension),
      scaleCode: 'agreement_5',
      required: true,
      reverseCoded: question.scoringDirection === 'higher-is-protective',
      measurementLevel: 'ordinal',
      payload: JSON.stringify({
        uxAuditCaseQuestionId: question.id,
        dimension: question.dimension,
      }),
      missingCodes: '[]',
    })),
  });

  return {
    templateVersionId: response.templateVersionId,
    caseQuestionIdByCode: Object.fromEntries(
      auditCase.questions.map((question, index) => [codeForQuestion(index), question.id])
    ),
    questions: response.questions.map((question, index) => ({
      id: question.id,
      code: question.code,
      caseQuestionId: auditCase.questions[index]?.id ?? question.code,
    })),
  };
}

function buildScoringDocument(auditCase: RealisticAuditCase) {
  const operations = Array.from(new Set(auditCase.questions.map((question) => question.dimension))).map(
    (dimension) => ({
      op: 'mean',
      items: auditCase.questions
        .map((question, index) => ({ question, code: codeForQuestion(index) }))
        .filter(({ question }) => question.dimension === dimension)
        .map(({ code }) => code),
      output: codeForDimension(dimension),
    })
  );

  return {
    rule_id: `${auditCase.id}.results`,
    version: '1.0.0',
    operations,
  };
}

function dimensionCodes(auditCase: RealisticAuditCase) {
  return Array.from(new Set(auditCase.questions.map((question) => question.dimension))).map(
    codeForDimension
  );
}

function resolveRepeatedWaveCampaignNames(auditCase: RealisticAuditCase) {
  const configured = auditCase.waveCampaignNames?.filter((name) => name.trim());
  if (configured && configured.length >= 2) {
    return configured.slice(0, 2);
  }

  return [
    auditCase.campaignName,
    `${auditCase.campaignName.replace(/\s+-\s+May\s+2026$/u, '')} - follow-up`,
  ];
}

function shiftFollowUpResponse(
  auditCase: RealisticAuditCase,
  response: ReturnType<typeof buildRealisticResponseSimulation>['responses'][number]
) {
  return {
    ...response,
    answers: Object.fromEntries(
      auditCase.questions.map((question) => {
        const answer = response.answers[question.id] ?? 3;
        const direction = question.scoringDirection === 'higher-is-risk' ? -1 : 1;
        return [question.id, clampScale(answer + direction)];
      })
    ),
  };
}

function participantCodeFor(respondentKey: string) {
  return `uxa-${respondentKey}`;
}

function sumCampaignMetric(
  campaigns: RealisticFullstackSeedCampaignResult[],
  metric: 'invitationCount' | 'submittedResponseCount' | 'scoredResponseCount'
) {
  return campaigns.reduce((sum, campaign) => sum + campaign[metric], 0);
}

function clampScale(value: number) {
  return Math.min(5, Math.max(1, value));
}

function codeForQuestion(index: number) {
  return `q${String(index + 1).padStart(2, '0')}`;
}

function codeForDimension(dimension: string) {
  return dimension.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '');
}

function normalizeLocalApiBaseUrl(value: string) {
  const url = new URL(value);
  const localHosts = new Set(['localhost', '127.0.0.1', '::1', '0.0.0.0']);

  if (url.protocol !== 'http:' || !localHosts.has(url.hostname)) {
    throw new Error('Realistic fullstack seeding requires a local loopback API URL.');
  }

  url.pathname = url.pathname.replace(/\/+$/g, '');
  url.search = '';
  url.hash = '';

  return url.toString();
}
