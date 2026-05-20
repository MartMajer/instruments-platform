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
  let currentSnapshot = await adapter.gotoPath(mission.entryPath, 'fixture-entry');

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
      currentSnapshot = await adapter.gotoPath(action.path, `step-${steps.length + 1}`);
      recordSnapshot(currentSnapshot);
      recordVisited(action.path);
      recordStep(`Navigated to local product path ${action.path}.`);
      continue;
    }

    if (action.kind === 'click-link') {
      const path = action.path ?? linkPathForAction(currentSnapshot, action);
      currentSnapshot = await adapter.clickLink(
        action.text,
        `step-${steps.length + 1}`,
        path
      );
      recordSnapshot(currentSnapshot);
      if (path) {
        recordVisited(path);
      }
      recordStep(`Clicked visible link "${action.text}".`);
      continue;
    }

    if (action.kind === 'click-button') {
      currentSnapshot = await adapter.clickButton(action.text, `step-${steps.length + 1}`);
      recordSnapshot(currentSnapshot);
      recordStep(`Clicked visible button "${action.text}".`);
      continue;
    }

    currentSnapshot = await adapter.fillField(
      action.label,
      action.value,
      `step-${steps.length + 1}`
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

  const observations = {
    autonomousMode: true,
    localOnly: true,
    productEntryPath: mission.entryPath,
    targetProductPaths: mission.targetProductPaths,
    visitedProductPaths,
    reviewFocus: mission.reviewFocus,
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
    reviewerOutput: buildReviewerOutput(mission, status, findings),
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
          suggestedFix:
            'Fix local product app auth/session mocking or readiness waiting so autonomous review starts from /app.',
          ticketReadyWording:
            'Fix local product app auth for autonomous UX review: /app must render the cockpit, not workspace access loading.',
        };
      }

      if (currentPath !== mission.entryPath && hasConfusingBlockedState(visibleText)) {
        const surface = context.currentSnapshot.title || context.currentSnapshot.label;
        return {
          kind: 'complain',
          severity: hasWorkspaceAccessLoading(visibleText) ? 'blocker' : 'confusion',
          surface,
          problem: hasWorkspaceAccessLoading(visibleText)
            ? `The target fixture route still shows workspace access loading instead of product fixture content: ${visibleText.slice(
                0,
                240
              )}`
            : `The persona hit a blocked or missing-prerequisite state: ${visibleText.slice(
                0,
                240
              )}`,
          suggestedFix: hasWorkspaceAccessLoading(visibleText)
            ? 'Fix local product app auth/session mocking or app readiness waiting so autonomous review sees product content.'
            : 'Add clearer next-action wording, prerequisite explanation, or recovery link on this fixture state.',
          ticketReadyWording: hasWorkspaceAccessLoading(visibleText)
            ? `Fix local product app auth for ${surface}: autonomous review must not proceed while workspace access is still loading.`
            : `Clarify ${surface}: explain the blocked state and the exact next action.`,
        };
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
  findings: AutonomousPersonaFinding[]
) {
  return JSON.stringify(
    {
      summary:
        findings.length > 0
          ? `Autonomous ${mission.personaId} review found ${findings.length} issue(s).`
          : `Autonomous ${mission.personaId} review completed without findings.`,
      missionStatus: status,
      findings,
      openQuestions: [],
    },
    null,
    2
  );
}

function combinedSnapshotText(snapshot: MissionPageSnapshot) {
  return [
    snapshot.title,
    snapshot.visibleTextExcerpt,
    snapshot.richTranscript?.visibleText ?? '',
    ...(snapshot.richTranscript?.statusMessages ?? []),
  ].join(' ');
}

function hasConfusingBlockedState(text: string) {
  return (
    hasWorkspaceAccessLoading(text) ||
    /\b(blocked|missing|not available|unavailable|failed|cannot|error)\b/i.test(text)
  );
}

function hasWorkspaceAccessLoading(text: string) {
  return /workspace access\s+checking workspace access/i.test(text);
}

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

function trimTrailingPeriod(value: string) {
  return value.replace(/\.+$/g, '');
}
