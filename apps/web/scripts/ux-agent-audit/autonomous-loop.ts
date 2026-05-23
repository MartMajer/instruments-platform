import {
  linkPathForAction,
  validateAgentActionAgainstSnapshot,
  type UXAgentAction,
} from './autonomous-actions.ts';
import type { AutonomousFixtureMission } from './autonomous-fixtures.ts';
import type {
  JsonObject,
  MissionEvidenceScreenshot,
  MissionEvidenceStatus,
  MissionEvidenceStep,
} from './evidence.ts';
import type { MissionPageSnapshot } from './mission-executor.ts';
import { buildRealisticResponseSimulation } from './realistic-cases.ts';

export interface AutonomousPageAdapter {
  gotoPath(path: string, label?: string): Promise<MissionPageSnapshot>;
  clickLink(text: string, label?: string, path?: string): Promise<MissionPageSnapshot>;
  clickButton(text: string, label?: string): Promise<MissionPageSnapshot>;
  fillField(label: string, value: string, snapshotLabel?: string): Promise<MissionPageSnapshot>;
  captureSnapshot(label: string): Promise<MissionPageSnapshot>;
}

export interface AutonomousPersonaFinding {
  severity: 'blocker' | 'confusion' | 'polish';
  affectedStep: string;
  surface: string;
  userExpectation: string;
  observedConfusion: string;
  suggestedFix: string;
  ticketReadyWording: string;
}

export interface AutonomousPersonaContext {
  mission: AutonomousFixtureMission;
  currentSnapshot: MissionPageSnapshot;
  visitedProductPaths: string[];
  steps: MissionEvidenceStep[];
  findings: AutonomousPersonaFinding[];
}

export interface AutonomousPersonaActor {
  decide(context: AutonomousPersonaContext): UXAgentAction | Promise<UXAgentAction>;
}

export interface AutonomousMissionResult {
  status: MissionEvidenceStatus;
  steps: MissionEvidenceStep[];
  screenshots: MissionEvidenceScreenshot[];
  observations: JsonObject;
  snapshots: MissionPageSnapshot[];
  personaFindings: AutonomousPersonaFinding[];
  reviewerOutput: string;
}

export async function runAutonomousFixtureMission(
  adapter: AutonomousPageAdapter,
  mission: AutonomousFixtureMission,
  actor: AutonomousPersonaActor
): Promise<AutonomousMissionResult> {
  const steps: MissionEvidenceStep[] = [];
  const snapshots: MissionPageSnapshot[] = [];
  const screenshots = new Map<string, MissionEvidenceScreenshot>();
  const visitedProductPaths: string[] = [];
  const actionLog: JsonObject[] = [];
  const findings: AutonomousPersonaFinding[] = [];
  let status: MissionEvidenceStatus = 'blocked';
  let currentSnapshot = normalizeSnapshotUrl(
    await adapter.gotoPath(mission.entryPath, 'fixture-entry')
  );

  recordSnapshot(currentSnapshot);
  recordStep(`Opened autonomous product mission entry route ${mission.entryPath}.`);

  for (let index = 0; index < mission.maxSteps; index += 1) {
    const action = await actor.decide({
      mission,
      currentSnapshot,
      visitedProductPaths,
      steps,
      findings,
    });
    const validation = validateAgentActionAgainstSnapshot(action, currentSnapshot);
    actionLog.push(toActionLog(action, validation));

    if (!validation.allowed) {
      findings.push({
        severity: 'blocker',
        affectedStep: currentSnapshot.label,
        surface: currentSnapshot.title || currentSnapshot.label,
        userExpectation: 'The autonomous persona should only operate visible local UI controls.',
        observedConfusion: validation.reason ?? 'Unsafe action was rejected.',
        suggestedFix: 'Expose a visible local control or adjust the mission path.',
        ticketReadyWording: `Fix autonomous UX mission blocker: ${validation.reason ?? 'unsafe action'}`,
      });
      recordStep(`Blocked unsafe autonomous action: ${validation.reason ?? action.kind}.`);
      status = 'blocked';
      break;
    }

    if (action.kind === 'stop') {
      recordStep(`Stopped autonomous product mission: ${trimTrailingPeriod(action.reason)}.`);
      status = findings.length ? 'blocked' : 'completed';
      break;
    }

    if (action.kind === 'complain') {
      findings.push(toFinding(action, currentSnapshot));
      recordStep(`Persona complained on ${currentSnapshot.title || currentSnapshot.label}.`);
      status = 'blocked';
      break;
    }

    if (action.kind === 'goto') {
      currentSnapshot = normalizeSnapshotUrl(
        await adapter.gotoPath(action.path, `step-${steps.length + 1}`)
      );
      recordSnapshot(currentSnapshot);
      recordVisited(action.path);
      recordStep(`Navigated to local product path ${action.path}.`);
      continue;
    }

    if (action.kind === 'click-link') {
      const path = action.path ?? linkPathForAction(currentSnapshot, action);
      currentSnapshot = normalizeSnapshotUrl(
        await adapter.clickLink(action.text, `step-${steps.length + 1}`, path)
      );
      recordSnapshot(currentSnapshot);
      if (path) {
        recordVisited(path);
      }
      recordStep(`Clicked visible link "${action.text}".`);
      continue;
    }

    if (action.kind === 'click-button') {
      currentSnapshot = normalizeSnapshotUrl(
        await adapter.clickButton(action.text, `step-${steps.length + 1}`)
      );
      recordSnapshot(currentSnapshot);
      recordStep(`Clicked visible button "${action.text}".`);
      continue;
    }

    currentSnapshot = normalizeSnapshotUrl(
      await adapter.fillField(action.label, action.value, `step-${steps.length + 1}`)
    );
    recordSnapshot(currentSnapshot);
    recordStep(`Filled visible field "${action.label}".`);
  }

  if (steps.length === 1 || !isTerminalStep(steps.at(-1)?.action ?? '')) {
    findings.push({
      severity: 'blocker',
      affectedStep: currentSnapshot.label,
      surface: currentSnapshot.title || currentSnapshot.label,
      userExpectation: 'The autonomous persona should reach every target fixture or explain why it cannot.',
      observedConfusion: 'The mission ended before a terminal stop or complaint action.',
      suggestedFix: 'Increase mission coverage or expose a clearer local product route.',
      ticketReadyWording: 'Fix autonomous product mission termination so every run ends with stop or complaint.',
    });
    status = 'blocked';
  }

  const personaGoalAssessment = buildPersonaGoalAssessment(
    mission,
    status,
    findings,
    visitedProductPaths,
    snapshots
  );
  const observations = {
    autonomousMode: true,
    localOnly: true,
    productEntryPath: mission.entryPath,
    personaGoal: mission.personaProfile,
    personaGoalAssessment,
    targetProductPaths: mission.targetProductPaths,
    visitedProductPaths,
    reviewFocus: mission.reviewFocus,
    ...(mission.realisticCase ? { realisticCase: mission.realisticCase } : {}),
    ...(mission.realisticCase
      ? { realisticResponseSimulation: buildRealisticResponseSimulation(mission.realisticCase) }
      : {}),
    actionLog,
    personaFindings: findings,
  } satisfies JsonObject;

  return {
    status,
    steps,
    screenshots: Array.from(screenshots.values()),
    observations,
    snapshots,
    personaFindings: findings,
    reviewerOutput: buildReviewerOutput(mission, status, findings, personaGoalAssessment),
  };

  function recordStep(action: string) {
    steps.push({
      index: steps.length + 1,
      action,
      url: currentSnapshot.url,
    });
  }

  function recordSnapshot(snapshot: MissionPageSnapshot) {
    snapshots.push(snapshot);
    if (snapshot.screenshot) {
      screenshots.set(snapshot.screenshot.path, snapshot.screenshot);
    }
  }

  function recordVisited(path: string) {
    if (mission.targetProductPaths.includes(path) && !visitedProductPaths.includes(path)) {
      visitedProductPaths.push(path);
    }
  }
}

export function buildScriptedFixturePersonaActor(
  mission: AutonomousFixtureMission
): AutonomousPersonaActor {
  return {
    decide(context) {
      const supportedDataModes = mission.supportedDataModes ?? ['fixture'];
      const fullstackOnlyMission =
        supportedDataModes.includes('fullstack') && !supportedDataModes.includes('fixture');
      const currentPath = extractPath(context.currentSnapshot.url);
      const visibleText = combinedSnapshotText(context.currentSnapshot);
      const unvisitedPath = mission.targetProductPaths.find(
        (path) => !context.visitedProductPaths.includes(path)
      );

      if (hasWorkspaceAccessLoading(visibleText) && currentPath === mission.entryPath) {
        return {
          kind: 'complain',
          severity: 'blocker',
          surface: 'Product app entry',
          problem:
            'The local product app is still checking workspace access, so autonomous review cannot reach the normal cockpit.',
          suggestedFix: fullstackOnlyMission
            ? 'Fix local full-stack auth/session and seed data so autonomous review starts from /app without product read-model mocks.'
            : 'Fix local product app auth/session mocking or readiness waiting so autonomous review starts from /app.',
          ticketReadyWording: fullstackOnlyMission
            ? 'Fix local full-stack auth/session and seed data for autonomous UX review: /app must render the cockpit without product read-model mocks.'
            : 'Fix local product app auth for autonomous UX review: /app must render the cockpit, not workspace access loading.',
        };
      }

      if (currentPath !== mission.entryPath && hasHardProductFailure(visibleText)) {
        const surface = context.currentSnapshot.title || context.currentSnapshot.label;
        const workspaceAccessLoading = hasWorkspaceAccessLoading(visibleText);
        return {
          kind: 'complain',
          severity: 'blocker',
          surface,
          problem: workspaceAccessLoading
            ? `The target fixture route still shows workspace access loading instead of product fixture content: ${visibleText.slice(
                0,
                240
              )}`
            : `The target product route rendered a hard app failure instead of reviewable product content: ${visibleText.slice(
                0,
                240
              )}`,
          suggestedFix: workspaceAccessLoading
            ? fullstackOnlyMission
              ? 'Fix local full-stack auth/session and seed data so autonomous review sees product content without product read-model mocks.'
              : 'Fix local product app auth/session mocking or app readiness waiting so autonomous review sees product content.'
            : 'Fix local product read-model routing or product route handling so autonomous review sees the intended surface.',
          ticketReadyWording: workspaceAccessLoading
            ? fullstackOnlyMission
              ? `Fix local full-stack auth/session and seed data for ${surface}: autonomous review must not proceed while workspace access is still loading.`
              : `Fix local product app auth for ${surface}: autonomous review must not proceed while workspace access is still loading.`
            : `Fix autonomous product route for ${surface}: hard app failures must not be reported as product UX findings.`,
        };
      }

      const primaryCopyFinding = buildPrimaryCopyFinding(mission, context.currentSnapshot);
      if (primaryCopyFinding) {
        return {
          kind: 'complain',
          severity: primaryCopyFinding.severity,
          surface: primaryCopyFinding.surface,
          problem: primaryCopyFinding.observedConfusion,
          suggestedFix: primaryCopyFinding.suggestedFix,
          ticketReadyWording: primaryCopyFinding.ticketReadyWording,
        };
      }

      if (mission.mutationPlan?.kind === 'create-study') {
        const createStudyAction = decideCreateStudyMutation(
          mission.mutationPlan,
          currentPath,
          context.steps,
          context.currentSnapshot
        );
        if (createStudyAction) {
          return createStudyAction;
        }
      }

      if (!unvisitedPath) {
        return {
          kind: 'stop',
          reason: 'inspected all target product paths.',
        };
      }

      const visibleLink = context.currentSnapshot.richTranscript?.links
        .concat(context.currentSnapshot.links)
        .find((link) => link.path === unvisitedPath);
      if (visibleLink?.text) {
        return {
          kind: 'click-link',
          text: visibleLink.text,
          path: visibleLink.path,
          reason: `inspect product path ${unvisitedPath}`,
        };
      }

      return {
        kind: 'goto',
        path: unvisitedPath,
        reason: `directly inspect product path ${unvisitedPath}`,
      };
    },
  };
}

function decideCreateStudyMutation(
  plan: NonNullable<AutonomousFixtureMission['mutationPlan']>,
  currentPath: string,
  steps: MissionEvidenceStep[],
  snapshot: MissionPageSnapshot
): UXAgentAction | undefined {
  if (isCreatedStudySetupPath(currentPath)) {
    if (plan.setupInstrument) {
      return decideSetupInstrumentMutation(plan.setupInstrument, steps, snapshot);
    }

    return {
      kind: 'stop',
      reason: 'created study setup route reached.',
    };
  }

  if (currentPath !== '/app/campaign-series') {
    return undefined;
  }

  const filledStudyName = steps.some(
    (step) => step.action === `Filled visible field "${plan.fieldLabel}".`
  );
  if (!filledStudyName) {
    const generatedStudyName = `${plan.studyNamePrefix ?? 'UXA local study'} ${new Date()
      .toISOString()
      .replace(/[-:.TZ]/g, '')
      .slice(0, 14)}`;

    return {
      kind: 'fill',
      label: plan.fieldLabel,
      value: plan.studyName ?? generatedStudyName,
      reason: plan.studyName
        ? 'name the realistic local full-stack study'
        : 'name the synthetic local full-stack study',
    };
  }

  const createStudyButtonCandidates = candidateCreateStudyButtons(plan);
  const clickedCreateStudy = steps.some((step) =>
    createStudyButtonCandidates.some(
      (buttonText) => step.action === `Clicked visible button "${buttonText}".`
    )
  );
  if (!clickedCreateStudy) {
    const visibleButtonText =
      resolveVisibleCreateStudyButtonText(snapshot, createStudyButtonCandidates) ?? plan.buttonText;

    return {
      kind: 'click-button',
      text: visibleButtonText,
      reason: 'create the synthetic local full-stack study',
    };
  }

  return {
    kind: 'complain',
    severity: 'blocker',
    surface: 'Studies',
    problem:
      'The create-study mutation did not navigate to the new study setup route after clicking Create study.',
    suggestedFix:
      'Fix local full-stack API/database mutation handling or the Studies create-study redirect.',
    ticketReadyWording:
      'Fix UXA02 full-stack create-study mutation: clicking Create study must create a study and navigate to its setup route.',
  };
}

function candidateCreateStudyButtons(
  plan: NonNullable<AutonomousFixtureMission['mutationPlan']>
) {
  return uniqueTextCandidates([
    plan.buttonText,
    ...(plan.buttonTextAlternatives ?? []),
    'Continue to guided setup',
  ]);
}

function resolveVisibleCreateStudyButtonText(
  snapshot: MissionPageSnapshot,
  candidates: string[]
) {
  const candidateSet = new Set(candidates.map((candidate) => normalizeForMatching(candidate)));
  const richButtons = snapshot.richTranscript?.buttons ?? [];

  for (const button of richButtons) {
    if (button.disabled) {
      continue;
    }

    if (candidateSet.has(normalizeForMatching(button.text))) {
      return button.text;
    }
  }

  for (const buttonText of snapshot.buttons) {
    if (candidateSet.has(normalizeForMatching(buttonText))) {
      return buttonText;
    }
  }

  return undefined;
}

function uniqueTextCandidates(candidates: string[]) {
  const seen = new Set<string>();
  const result: string[] = [];

  for (const candidate of candidates) {
    const trimmed = candidate.trim();
    const key = normalizeForMatching(trimmed);
    if (!trimmed || seen.has(key)) {
      continue;
    }

    seen.add(key);
    result.push(trimmed);
  }

  return result;
}

function decideSetupInstrumentMutation(
  setupInstrument: NonNullable<
    NonNullable<AutonomousFixtureMission['mutationPlan']>['setupInstrument']
  >,
  steps: MissionEvidenceStep[],
  snapshot: MissionPageSnapshot
): UXAgentAction {
  const fieldLabelCandidates = candidateSetupInstrumentFieldLabels(setupInstrument);
  const filledInstrumentName = steps.some((step) =>
    fieldLabelCandidates.some(
      (fieldLabel) => step.action === `Filled visible field "${fieldLabel}".`
    )
  );
  if (!filledInstrumentName) {
    const visibleFieldLabel =
      resolveVisibleFieldLabel(snapshot, fieldLabelCandidates) ?? setupInstrument.fieldLabel;

    return {
      kind: 'fill',
      label: visibleFieldLabel,
      value: setupInstrument.value,
      reason: 'name the realistic local instrument',
    };
  }

  if (setupInstrument.versionFieldLabel && setupInstrument.versionValue) {
    const filledInstrumentVersion = steps.some(
      (step) => step.action === `Filled visible field "${setupInstrument.versionFieldLabel}".`
    );
    if (!filledInstrumentVersion) {
      return {
        kind: 'fill',
        label: setupInstrument.versionFieldLabel,
        value: setupInstrument.versionValue,
        reason: 'set the realistic local instrument version',
      };
    }
  }

  const buttonCandidates = candidateSetupInstrumentButtons(setupInstrument);
  const clickedSaveInstrument = steps.some((step) =>
    buttonCandidates.some(
      (buttonText) => step.action === `Clicked visible button "${buttonText}".`
    )
  );
  if (!clickedSaveInstrument) {
    const visibleButtonText =
      resolveVisibleCreateStudyButtonText(snapshot, buttonCandidates) ?? setupInstrument.buttonText;

    return {
      kind: 'click-button',
      text: visibleButtonText,
      reason: 'save the realistic local instrument setup',
    };
  }

  return {
    kind: 'stop',
    reason: 'created realistic study and saved instrument setup.',
  };
}

function candidateSetupInstrumentFieldLabels(
  setupInstrument: NonNullable<
    NonNullable<AutonomousFixtureMission['mutationPlan']>['setupInstrument']
  >
) {
  return uniqueTextCandidates([
    setupInstrument.fieldLabel,
    ...(setupInstrument.fieldLabelAlternatives ?? []),
    'Study source name',
  ]);
}

function candidateSetupInstrumentButtons(
  setupInstrument: NonNullable<
    NonNullable<AutonomousFixtureMission['mutationPlan']>['setupInstrument']
  >
) {
  return uniqueTextCandidates([
    setupInstrument.buttonText,
    ...(setupInstrument.buttonTextAlternatives ?? []),
    'Save study source',
  ]);
}

function resolveVisibleFieldLabel(snapshot: MissionPageSnapshot, candidates: string[]) {
  const candidateSet = new Set(candidates.map((candidate) => normalizeForMatching(candidate)));

  for (const field of snapshot.richTranscript?.fields ?? []) {
    if (candidateSet.has(normalizeForMatching(field.label))) {
      return field.label;
    }
  }

  return undefined;
}

function isCreatedStudySetupPath(path: string) {
  return /^\/app\/campaign-series\/[^/]+\/setup$/u.test(path);
}

function toFinding(
  action: Extract<UXAgentAction, { kind: 'complain' }>,
  snapshot: MissionPageSnapshot
): AutonomousPersonaFinding {
  return {
    severity: action.severity,
    affectedStep: snapshot.label,
    surface: action.surface,
    userExpectation: `The persona expected ${action.surface} to make the next product action understandable.`,
    observedConfusion: action.problem,
    suggestedFix: action.suggestedFix,
    ticketReadyWording:
      action.ticketReadyWording ?? `Clarify ${action.surface}: ${action.suggestedFix}`,
  };
}

function toActionLog(action: UXAgentAction, validation: { allowed: boolean; reason?: string }) {
  return {
    kind: action.kind,
    allowed: validation.allowed,
    ...(validation.reason ? { reason: validation.reason } : {}),
    ...('text' in action ? { text: action.text } : {}),
    ...('path' in action ? { path: action.path } : {}),
    ...('label' in action ? { label: action.label } : {}),
  } satisfies JsonObject;
}

function buildReviewerOutput(
  mission: AutonomousFixtureMission,
  status: MissionEvidenceStatus,
  findings: AutonomousPersonaFinding[],
  personaGoalAssessment: JsonObject
) {
  return JSON.stringify(
    {
      summary:
        findings.length > 0
          ? `Autonomous ${mission.personaId} review found ${findings.length} issue(s).`
          : `Autonomous ${mission.personaId} review completed without findings.`,
      missionStatus: status,
      ...(mission.realisticCase ? { realisticCase: mission.realisticCase } : {}),
      ...(mission.realisticCase
        ? { realisticResponseSimulation: buildRealisticResponseSimulation(mission.realisticCase) }
        : {}),
      personaGoal: {
        name: mission.personaProfile.name,
        role: mission.personaProfile.role,
        appGoal: mission.personaProfile.appGoal,
        successCriteria: mission.personaProfile.successCriteria,
        reviewerInstructions: mission.personaProfile.reviewerInstructions,
      },
      personaGoalAssessment,
      findings,
      openQuestions: [],
    },
    null,
    2
  );
}

export function buildPersonaGoalAssessment(
  mission: AutonomousFixtureMission,
  status: MissionEvidenceStatus,
  findings: AutonomousPersonaFinding[],
  visitedProductPaths: string[],
  snapshots: MissionPageSnapshot[],
  targetProductPaths = mission.targetProductPaths
) {
  const visitedTargetCount = targetProductPaths.filter((path) =>
    visitedProductPaths.includes(path)
  ).length;

  return {
    status,
    appGoal: mission.personaProfile.appGoal,
    checkedCriteriaCount: mission.personaProfile.successCriteria.length,
    successCriteria: mission.personaProfile.successCriteria.map((criterion) =>
      assessCriterion(criterion, status, snapshots)
    ),
    visitedTargetCount,
    targetCount: targetProductPaths.length,
    unresolvedFindingCount: findings.length,
    reviewerInstructions: mission.personaProfile.reviewerInstructions,
  } satisfies JsonObject;
}

function assessCriterion(
  criterion: string,
  status: MissionEvidenceStatus,
  snapshots: MissionPageSnapshot[]
) {
  const evidence = findCriterionEvidence(criterion, snapshots);

  if (evidence) {
    return {
      criterion,
      status: 'observed',
      evidence: `Observed in ${evidence.label}: ${evidence.excerpt}`,
    };
  }

  if (status === 'completed') {
    return {
      criterion,
      status: 'unclear',
      evidence:
        'No direct transcript evidence found for this criterion; reviewer must judge it from captured snapshots rather than route visitation.',
    };
  }

  return {
    criterion,
    status: 'not_observed',
    evidence:
      'Mission stopped before transcript evidence for this criterion was captured.',
  };
}

function findCriterionEvidence(criterion: string, snapshots: MissionPageSnapshot[]) {
  const terms = criterionTerms(criterion);
  const normalizedCriterion = normalizeForMatching(criterion);
  const preferredRoute = preferredRouteForCriterion(criterion);
  let best:
    | {
        label: string;
        excerpt: string;
        score: number;
      }
    | undefined;

  for (const snapshot of snapshots) {
    const path = extractPath(snapshot.url);
    for (const candidate of evidenceCandidates(snapshot)) {
      const normalized = normalizeForMatching(candidate.text);
      const matchCount = terms.filter((term) => normalized.includes(term)).length;

      if (preferredRoute && !path.endsWith(preferredRoute)) {
        continue;
      }

      if (matchCount === 0) {
        continue;
      }

      if (!candidateMeetsRequiredCriterionTerms(normalizedCriterion, normalized)) {
        continue;
      }

      const preferredRouteBonus = preferredRoute && path.endsWith(preferredRoute) ? 4 : 0;
      const genericEntryPenalty = preferredRoute && path === missionEntryPath ? 4 : 0;
      const sectionBonus = candidate.kind === 'section' || candidate.kind === 'status' ? 1 : 0;
      const excerptPenalty = candidate.kind === 'excerpt' ? 2 : 0;
      const score =
        matchCount + preferredRouteBonus + sectionBonus - genericEntryPenalty - excerptPenalty;

      if (!best || score > best.score) {
        best = {
          label: `${routeLabel(path)}${candidate.label ? ` ${candidate.label}` : ''}`.trim(),
          excerpt: excerptForEvidence(candidate.text, terms),
          score,
        };
      }
    }
  }

  if (!best || best.score < 2) {
    return undefined;
  }

  return best;
}

function candidateMeetsRequiredCriterionTerms(
  normalizedCriterion: string,
  normalizedCandidate: string
) {
  if (
    normalizedCriterion.includes('questionnaire') &&
    normalizedCriterion.includes('scoring')
  ) {
    return (
      normalizedCandidate.includes('questionnaire') &&
      /\b(scoring|score|scores|results setup|study results|answers become)\b/.test(
        normalizedCandidate
      )
    );
  }

  if (
    normalizedCriterion.includes('avoids') &&
    normalizedCriterion.includes('claims') &&
    (normalizedCriterion.includes('validated') || normalizedCriterion.includes('clinical'))
  ) {
    return (
      normalizedCandidate.includes('not validated') ||
      normalizedCandidate.includes('not client ready') ||
      normalizedCandidate.includes('internal review only') ||
      normalizedCandidate.includes('client facing claims') ||
      normalizedCandidate.includes('interpretation status')
    );
  }

  return true;
}

const missionEntryPath = '/app';

function evidenceCandidates(snapshot: MissionPageSnapshot) {
  const candidates: Array<{ kind: 'section' | 'status' | 'heading' | 'excerpt'; label: string; text: string }> = [];
  const transcript = snapshot.richTranscript;

  if (transcript) {
    for (const [index, section] of transcript.sections.entries()) {
      candidates.push({ kind: 'section', label: `section ${index + 1}`, text: section });
    }

    for (const [index, status] of transcript.statusMessages.entries()) {
      candidates.push({ kind: 'status', label: `status ${index + 1}`, text: status });
    }

    for (const [index, heading] of transcript.headings.entries()) {
      candidates.push({ kind: 'heading', label: `heading ${index + 1}`, text: heading });
    }
  }

  if (snapshot.visibleTextExcerpt.trim().length > 0) {
    candidates.push({ kind: 'excerpt', label: '', text: snapshot.visibleTextExcerpt });
  }

  return candidates.filter((candidate) => candidate.text.trim().length > 0);
}

function preferredRouteForCriterion(criterion: string) {
  const normalized = normalizeForMatching(criterion);

  if (/\b(collection|draft|live|closed|waiting|responses|submission)\b/.test(normalized)) {
    return '/operations';
  }

  if (/\b(results|export|handoff|analysis|download)\b/.test(normalized)) {
    return '/reports';
  }

  if (
    /\b(clinical|validated|claims|interpretation)\b/.test(normalized) ||
    (/\b(disclosure|anonymity|anonymous|suppression|limitations)\b/.test(normalized) &&
      /\b(interpretation|claim|claims|report|results)\b/.test(normalized))
  ) {
    return '/reports';
  }

  if (/\b(wave|comparison|disclosure|anonymity|anonymous|change)\b/.test(normalized)) {
    return '/waves';
  }

  if (/\b(setup|questionnaire|scoring|recipient|invitation|roster|launch)\b/.test(normalized)) {
    return '/setup';
  }

  return undefined;
}

function routeLabel(path: string) {
  if (path.endsWith('/setup')) {
    return 'Setup route';
  }

  if (path.endsWith('/operations')) {
    return 'Collection route';
  }

  if (path.endsWith('/reports')) {
    return 'Reports route';
  }

  if (path.endsWith('/waves')) {
    return 'Waves route';
  }

  if (path === '/app/exports') {
    return 'Exports route';
  }

  if (path === '/app') {
    return 'App cockpit';
  }

  return path || 'Captured route';
}

function criterionTerms(criterion: string) {
  const stopWords = new Set([
    'about',
    'after',
    'before',
    'being',
    'clear',
    'clearly',
    'does',
    'enough',
    'from',
    'into',
    'know',
    'makes',
    'normal',
    'obvious',
    'page',
    'quickly',
    'says',
    'should',
    'that',
    'their',
    'there',
    'this',
    'what',
    'when',
    'where',
    'whether',
    'will',
    'with',
    'work',
  ]);

  const terms = Array.from(
    new Set(
      normalizeForMatching(criterion)
        .split(/\s+/)
        .filter((term) => term.length > 3 && !stopWords.has(term))
    )
  );

  const normalized = normalizeForMatching(criterion);
  if (normalized.includes('anonymity')) {
    terms.push('anonymous');
  }
  if (normalized.includes('suppression')) {
    terms.push('suppressed', 'disclosure');
  }
  if (normalized.includes('limitations')) {
    terms.push('limits', 'limited', 'disclosure');
  }
  if (normalized.includes('validated') || normalized.includes('clinical')) {
    terms.push('not validated', 'not client ready', 'internal review');
  }

  return Array.from(new Set(terms));
}

function normalizeForMatching(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim();
}

function excerptForEvidence(value: string, terms: string[] = []) {
  const collapsed = value.replace(/\s+/g, ' ').trim();
  const lower = collapsed.toLowerCase();
  const matchedTerm = terms.find((term) => lower.includes(term));
  const matchIndex = matchedTerm ? lower.indexOf(matchedTerm) : 0;
  const start = Math.max(0, matchIndex - 80);

  return collapsed.slice(start, start + 240);
}

function combinedSnapshotText(snapshot: MissionPageSnapshot) {
  return [
    snapshot.title,
    snapshot.visibleTextExcerpt,
    snapshot.richTranscript?.visibleText ?? '',
    ...(snapshot.richTranscript?.statusMessages ?? []),
  ].join(' ');
}

function hasHardProductFailure(text: string) {
  return (
    hasWorkspaceAccessLoading(text) ||
    /\b(workspace sign-in needed|tenant access unavailable|campaign series unavailable|api request failed|failed to load|network error|service outage)\b/i.test(
      text
    ) ||
    /oops!,?\s*something went wrong/i.test(text)
  );
}

function hasWorkspaceAccessLoading(text: string) {
  return /workspace access\s+checking workspace access/i.test(text);
}

export function buildPrimaryCopyFinding(
  mission: AutonomousFixtureMission,
  snapshot: MissionPageSnapshot
): AutonomousPersonaFinding | undefined {
  const text = combinedSnapshotText(snapshot);
  const smell = primaryCopySmells.find((candidate) => candidate.pattern.test(text));
  if (!smell) {
    return undefined;
  }

  const surface = snapshot.title || snapshot.label;
  return {
    severity: 'confusion',
    affectedStep: snapshot.label,
    surface,
    userExpectation: `${mission.personaProfile.name} expects product pages to use client/researcher-facing language.`,
    observedConfusion: `Primary product copy includes "${smell.label}", which reads as internal proof/development language in the ${mission.personaProfile.name} workflow.`,
    suggestedFix: smell.suggestedFix,
    ticketReadyWording: `Replace primary-path "${smell.label}" copy on ${surface} with product-facing language.`,
  };
}

const primaryCopySmells = [
  {
    label: 'Proof foundation',
    pattern: /\bproof foundation\b/i,
    suggestedFix:
      'Change shared product header/status copy to neutral study, collection, results, or workspace language.',
  },
  {
    label: 'Proof only',
    pattern: /\bproof only\b/i,
    suggestedFix:
      'Keep proof-lab copy out of normal product journeys and show Preview or Not ready where a status is needed.',
  },
  {
    label: 'Export artifact',
    pattern: /\bexport artifact(?: registry)?\b/i,
    suggestedFix:
      'Use export file, download, or export library language on client-facing results and export routes.',
  },
  {
    label: 'Artifact registry',
    pattern: /\bartifact registry\b/i,
    suggestedFix:
      'Use export files or export library language instead of implementation registry wording.',
  },
] as const;

function isDemoUnavailable(snapshot: MissionPageSnapshot) {
  return /demo states are not enabled|demo fixture unavailable|unavailable/i.test(
    combinedSnapshotText(snapshot)
  );
}

function isTerminalStep(action: string) {
  return action.startsWith('Stopped autonomous product mission') || action.startsWith('Persona complained');
}

function extractPath(url: string) {
  try {
    return new URL(url).pathname;
  } catch {
    return url.split(/[?#]/)[0] ?? '';
  }
}

function normalizeSnapshotUrl(snapshot: MissionPageSnapshot): MissionPageSnapshot {
  const transcriptUrl = snapshot.richTranscript?.url;
  if (!transcriptUrl || transcriptUrl === snapshot.url) {
    return snapshot;
  }

  return {
    ...snapshot,
    url: transcriptUrl,
  };
}

function trimTrailingPeriod(value: string) {
  return value.replace(/\.+$/g, '');
}
