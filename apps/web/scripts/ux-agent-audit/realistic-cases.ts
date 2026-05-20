export interface RealisticAuditQuestion {
  id: string;
  dimension: string;
  prompt: string;
  responseFormat: {
    kind: 'agreement-scale';
    points: number;
    lowAnchor: string;
    highAnchor: string;
  };
  scoringDirection: 'higher-is-risk' | 'higher-is-protective';
}

export interface RealisticAuditCase {
  id: string;
  studyName: string;
  instrumentName: string;
  campaignName: string;
  waveCampaignNames?: string[];
  summary: string;
  questions: RealisticAuditQuestion[];
  syntheticResponses: {
    respondentCount: number;
    completionCount: number;
    segments: Array<{
      label: string;
      count: number;
      signal: string;
    }>;
    signalSummary: string;
  };
}

export interface RealisticResponseSimulation {
  campaignName: string;
  seedMode: 'simulated-local-evidence';
  respondentCount: number;
  completedResponseCount: number;
  omittedResponseCount: number;
  responses: Array<{
    respondentKey: string;
    segment: string;
    completed: true;
    answers: Record<string, number>;
  }>;
  segmentSummaries: Array<{
    segment: string;
    completed: number;
    signal: string;
  }>;
  dimensionRisk: Array<{
    dimension: string;
    averageRiskScore: number;
    riskLevel: 'low' | 'moderate' | 'high';
  }>;
  signalSummary: string;
}

const realisticAuditCases = [
  {
    id: 'osh-warehouse-workload-recovery-pulse',
    studyName: 'Warehouse workload and recovery pulse',
    instrumentName: 'Warehouse workload and recovery instrument',
    campaignName: 'Baseline warehouse pulse - May 2026',
    summary:
      'Local synthetic OSH consultant case for a logistics warehouse team. The case checks whether a consultant can create a credible workload, recovery, support, and strain pulse without placeholder wording.',
    questions: [
      riskQuestion(
        'workload-pace',
        'Workload pace',
        'My work pace makes it hard to recover during the shift.'
      ),
      riskQuestion(
        'workload-staffing',
        'Workload pace',
        'Staffing levels are too low for the volume of orders we handle.'
      ),
      protectiveQuestion(
        'control-breaks',
        'Control and breaks',
        'I can take planned breaks without falling behind.'
      ),
      protectiveQuestion(
        'control-priorities',
        'Control and breaks',
        'I know which tasks can wait when the shift gets overloaded.'
      ),
      protectiveQuestion(
        'support-supervisor',
        'Supervisor support',
        'My supervisor helps remove blockers when workload spikes.'
      ),
      protectiveQuestion(
        'support-peer',
        'Peer support',
        'People on my shift help each other before work becomes unsafe.'
      ),
      riskQuestion(
        'recovery-fatigue',
        'Recovery',
        'I still feel physically drained when the next shift starts.'
      ),
      riskQuestion(
        'recovery-sleep',
        'Recovery',
        'Work stress from the warehouse affects my sleep.'
      ),
      riskQuestion(
        'strain-back',
        'Musculoskeletal strain',
        'My lower back hurts after a normal shift.'
      ),
      riskQuestion(
        'strain-shoulder',
        'Musculoskeletal strain',
        'My shoulders or neck hurt after repeated lifting or scanning.'
      ),
    ],
    syntheticResponses: {
      respondentCount: 24,
      completionCount: 21,
      segments: [
        {
          label: 'Day shift pickers',
          count: 9,
          signal: 'moderate workload risk with stronger supervisor support',
        },
        {
          label: 'Night shift pickers',
          count: 7,
          signal: 'highest recovery and fatigue risk',
        },
        {
          label: 'Forklift and loading team',
          count: 5,
          signal: 'highest musculoskeletal strain signal',
        },
      ],
      signalSummary:
        'Recovery risk is concentrated in night-shift pickers, while strain risk is strongest for loading roles.',
    },
  },
  {
    id: 'academic-workload-recovery-followup',
    studyName: 'Academic workload and recovery follow-up',
    instrumentName: 'Academic workload and recovery instrument',
    campaignName: 'Baseline academic workload survey - May 2026',
    waveCampaignNames: [
      'Baseline academic workload survey - May 2026',
      'Follow-up academic workload survey - June 2026',
    ],
    summary:
      'Local synthetic busy-professor case for a faculty workload and recovery study. The case checks whether a professor can understand repeated waves, anonymous longitudinal codes, and change-over-time reporting without placeholder wording.',
    questions: [
      riskQuestion(
        'teaching-load-prep',
        'Teaching load',
        'Preparing classes and feedback leaves too little time for focused research.'
      ),
      riskQuestion(
        'teaching-load-marking',
        'Teaching load',
        'Marking and student communication frequently spill into evenings or weekends.'
      ),
      riskQuestion(
        'administrative-friction',
        'Administrative load',
        'Administrative requests interrupt my planned academic work.'
      ),
      riskQuestion(
        'administrative-meetings',
        'Administrative load',
        'Meetings make it difficult to protect uninterrupted writing or analysis time.'
      ),
      protectiveQuestion(
        'recovery-detachment',
        'Recovery',
        'I can mentally detach from university work outside working hours.'
      ),
      protectiveQuestion(
        'recovery-sleep',
        'Recovery',
        'My sleep feels restorative during the teaching period.'
      ),
      protectiveQuestion(
        'support-chair',
        'Department support',
        'My department chair helps rebalance workload when pressure rises.'
      ),
      protectiveQuestion(
        'support-colleagues',
        'Department support',
        'Colleagues share practical help when deadlines cluster.'
      ),
      riskQuestion(
        'research-delay',
        'Research continuity',
        'Teaching or administration delays work on active research projects.'
      ),
      protectiveQuestion(
        'research-protection',
        'Research continuity',
        'I have protected time for research tasks that require sustained attention.'
      ),
    ],
    syntheticResponses: {
      respondentCount: 18,
      completionCount: 16,
      segments: [
        {
          label: 'Early-career lecturers',
          count: 7,
          signal: 'highest teaching-load and research-continuity risk',
        },
        {
          label: 'Clinical and practicum supervisors',
          count: 5,
          signal: 'highest administrative-friction signal',
        },
        {
          label: 'Senior faculty',
          count: 4,
          signal: 'stronger department support with moderate recovery risk',
        },
      ],
      signalSummary:
        'Teaching load and research continuity risk are strongest for early-career lecturers, while administrative friction concentrates among clinical supervisors.',
    },
  },
] satisfies RealisticAuditCase[];

export function getRealisticAuditCase(id: string) {
  return realisticAuditCases.find((auditCase) => auditCase.id === id);
}

export function buildRealisticResponseSimulation(
  auditCase: RealisticAuditCase
): RealisticResponseSimulation {
  const responses: RealisticResponseSimulation['responses'] = [];
  let respondentIndex = 0;

  for (const segment of auditCase.syntheticResponses.segments) {
    for (let index = 0; index < segment.count; index += 1) {
      respondentIndex += 1;
      responses.push({
        respondentKey: `sim-${String(respondentIndex).padStart(3, '0')}`,
        segment: segment.label,
        completed: true,
        answers: Object.fromEntries(
          auditCase.questions.map((question, questionIndex) => [
            question.id,
            answerForSegment(segment.label, question, respondentIndex, questionIndex),
          ])
        ),
      });
    }
  }

  return {
    campaignName: auditCase.campaignName,
    seedMode: 'simulated-local-evidence',
    respondentCount: auditCase.syntheticResponses.respondentCount,
    completedResponseCount: responses.length,
    omittedResponseCount: Math.max(
      0,
      auditCase.syntheticResponses.respondentCount - responses.length
    ),
    responses,
    segmentSummaries: auditCase.syntheticResponses.segments.map((segment) => ({
      segment: segment.label,
      completed: segment.count,
      signal: segment.signal,
    })),
    dimensionRisk: buildDimensionRisk(auditCase, responses),
    signalSummary: auditCase.syntheticResponses.signalSummary,
  };
}

function riskQuestion(
  id: string,
  dimension: string,
  prompt: string
): RealisticAuditQuestion {
  return {
    id,
    dimension,
    prompt,
    responseFormat: agreementScale(),
    scoringDirection: 'higher-is-risk',
  };
}

function protectiveQuestion(
  id: string,
  dimension: string,
  prompt: string
): RealisticAuditQuestion {
  return {
    id,
    dimension,
    prompt,
    responseFormat: agreementScale(),
    scoringDirection: 'higher-is-protective',
  };
}

function agreementScale(): RealisticAuditQuestion['responseFormat'] {
  return {
    kind: 'agreement-scale',
    points: 5,
    lowAnchor: 'Strongly disagree',
    highAnchor: 'Strongly agree',
  };
}

function answerForSegment(
  segment: string,
  question: RealisticAuditQuestion,
  respondentIndex: number,
  questionIndex: number
) {
  const jitter = (respondentIndex + questionIndex) % 3;
  const isNightShift = segment === 'Night shift pickers';
  const isLoadingTeam = segment === 'Forklift and loading team';
  const isEarlyCareerLecturer = segment === 'Early-career lecturers';
  const isClinicalSupervisor = segment === 'Clinical and practicum supervisors';
  const isStrain = question.dimension === 'Musculoskeletal strain';
  const isRecovery = question.dimension === 'Recovery';
  const isTeachingLoad = question.dimension === 'Teaching load';
  const isAdministrativeLoad = question.dimension === 'Administrative load';
  const isResearchContinuity = question.dimension === 'Research continuity';
  const isProtective = question.scoringDirection === 'higher-is-protective';
  let base = 3;

  if (isEarlyCareerLecturer && (isTeachingLoad || isResearchContinuity)) {
    base = isProtective ? 2 : 5;
  } else if (isClinicalSupervisor && isAdministrativeLoad) {
    base = isProtective ? 2 : 5;
  } else if (isNightShift && isRecovery) {
    base = 5;
  } else if (isLoadingTeam && isStrain) {
    base = 5;
  } else if (isNightShift && isProtective) {
    base = 2;
  } else if (isProtective) {
    base = 4;
  } else if (isLoadingTeam) {
    base = 4;
  }

  return clampScale(base + jitter - 1);
}

function buildDimensionRisk(
  auditCase: RealisticAuditCase,
  responses: RealisticResponseSimulation['responses']
) {
  const dimensions = Array.from(new Set(auditCase.questions.map((question) => question.dimension)));

  return dimensions.map((dimension) => {
    const questions = auditCase.questions.filter((question) => question.dimension === dimension);
    const riskScores = responses.flatMap((response) =>
      questions.map((question) => {
        const answer = response.answers[question.id] ?? 3;
        return question.scoringDirection === 'higher-is-protective' ? 6 - answer : answer;
      })
    );
    const averageRiskScore = roundOne(
      riskScores.reduce((sum, score) => sum + score, 0) / riskScores.length
    );

    return {
      dimension,
      averageRiskScore,
      riskLevel: riskLevelForAverage(averageRiskScore),
    };
  });
}

function riskLevelForAverage(averageRiskScore: number): 'low' | 'moderate' | 'high' {
  if (averageRiskScore >= 3.6) {
    return 'high';
  }

  if (averageRiskScore >= 2.6) {
    return 'moderate';
  }

  return 'low';
}

function clampScale(value: number) {
  return Math.min(5, Math.max(1, value));
}

function roundOne(value: number) {
  return Math.round(value * 10) / 10;
}
