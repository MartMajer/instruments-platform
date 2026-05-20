import type {
  JsonObject,
  MissionEvidenceScreenshot,
  MissionEvidenceStatus,
  MissionEvidenceStep,
} from './evidence';

export interface MissionPageLink {
  text: string;
  path?: string;
}

export interface MissionNavigationLink {
  text: string;
  path: string;
}

export interface MissionPageSnapshot {
  label: string;
  title: string;
  url: string;
  visibleTextExcerpt: string;
  buttons: string[];
  links: MissionPageLink[];
  navigationLinks?: MissionNavigationLink[];
  screenshot?: MissionEvidenceScreenshot;
}

export interface MissionPageAdapter {
  gotoPath(path: string, label: string): Promise<MissionPageSnapshot>;
}

export interface MissionExecutionContext {
  baseUrl: string;
  missionId: string;
  personaId: string;
  missionGoal: string;
}

export interface MissionExecutionResult {
  status: MissionEvidenceStatus;
  steps: MissionEvidenceStep[];
  screenshots: MissionEvidenceScreenshot[];
  observations: JsonObject;
  snapshots: MissionPageSnapshot[];
}

const fixedMissionIds = new Set(['create-first-study']);

const workflowSurfaces = [
  {
    id: 'setup',
    label: 'study-setup',
    route: 'setup',
    action: 'Inspected selected-study setup guidance without saving changes.',
  },
  {
    id: 'collection',
    label: 'study-collection',
    route: 'operations',
    action: 'Inspected collection readiness guidance without launching a wave.',
  },
  {
    id: 'results',
    label: 'study-results',
    route: 'reports',
    action: 'Inspected results guidance without exporting or changing data.',
  },
  {
    id: 'waves',
    label: 'study-waves',
    route: 'waves',
    action: 'Inspected waves guidance without creating a follow-up wave.',
  },
] as const;

export function hasFixedMissionExecutor(missionId: string) {
  return fixedMissionIds.has(missionId);
}

export async function executeCreateFirstStudyMission(
  page: MissionPageAdapter,
  context: MissionExecutionContext
): Promise<MissionExecutionResult> {
  const recorder = createMissionRecorder();

  const signInSnapshot = await recorder.goto(
    page,
    '/signin',
    'signin-entry',
    'Opened the local sign-in route as the first-time researcher entry point.'
  );
  const appSnapshot = await recorder.goto(
    page,
    '/app',
    'app-entry',
    'Tried to enter the app workspace after the sign-in entry point.'
  );

  if (isSignInBlocked(appSnapshot)) {
    return recorder.complete('blocked', {
      startUrl: signInSnapshot.url,
      signInBlocked: true,
      appAccessible: false,
      avoidedUnsafePersistedArtifacts: true,
      visitedWorkflowSurfaces: [],
      visibleControls: recorder.visibleControls(),
      pages: recorder.pageObservations(),
      navigationPolicy: routeOnlyNavigationPolicy(),
    });
  }

  const studiesSnapshot = await recorder.goto(
    page,
    '/app/campaign-series',
    'studies-index',
    'Opened Studies to observe create/open-study guidance without submitting the create form.'
  );
  const selectedStudyPath = findSelectedStudyBasePath([
    studiesSnapshot,
    appSnapshot,
  ]);
  const visitedWorkflowSurfaces: string[] = [];

  if (!selectedStudyPath) {
    return recorder.complete('blocked', {
      startUrl: signInSnapshot.url,
      signInBlocked: false,
      appAccessible: true,
      avoidedUnsafePersistedArtifacts: true,
      selectedStudyPathFound: false,
      blockedReason: 'selected-study-required',
      seededStudyAccessPrerequisite:
        'Local app access succeeded, but no selected study link or path was visible. Seed or create a study and expose a selected study link under /app/campaign-series/<study-id> before running product-page workflow inspection.',
      visitedWorkflowSurfaces,
      visibleControls: recorder.visibleControls(),
      pages: recorder.pageObservations(),
      navigationPolicy: routeOnlyNavigationPolicy(),
      missionBoundary:
        'The fixed mission observes first-study creation/opening and selected-study navigation, but does not click create, save, launch, export, duplicate, delete, or invite actions.',
    });
  }

  for (const surface of workflowSurfaces) {
    await recorder.goto(
      page,
      `${selectedStudyPath}/${surface.route}`,
      surface.label,
      surface.action
    );
    visitedWorkflowSurfaces.push(surface.id);
  }

  return recorder.complete('completed', {
    startUrl: signInSnapshot.url,
    signInBlocked: false,
    appAccessible: true,
    avoidedUnsafePersistedArtifacts: true,
    selectedStudyPathFound: true,
    visitedWorkflowSurfaces,
    visibleControls: recorder.visibleControls(),
    pages: recorder.pageObservations(),
    navigationPolicy: routeOnlyNavigationPolicy(),
    missionBoundary:
      'The fixed mission observes first-study creation/opening and selected-study navigation, but does not click create, save, launch, export, duplicate, delete, or invite actions.',
  });
}

function createMissionRecorder() {
  const steps: MissionEvidenceStep[] = [];
  const snapshots: MissionPageSnapshot[] = [];
  const screenshots = new Map<string, MissionEvidenceScreenshot>();

  return {
    async goto(
      page: MissionPageAdapter,
      path: string,
      label: string,
      action: string
    ) {
      const snapshot = await page.gotoPath(path, label);
      snapshots.push(snapshot);
      steps.push({
        index: steps.length + 1,
        action,
        url: snapshot.url,
      });

      if (snapshot.screenshot) {
        screenshots.set(snapshot.screenshot.path, snapshot.screenshot);
      }

      return snapshot;
    },
    visibleControls() {
      const controls = new Set<string>();

      for (const snapshot of snapshots) {
        for (const button of snapshot.buttons) {
          if (button) {
            controls.add(button);
          }
        }

        for (const link of snapshot.links) {
          if (link.text) {
            controls.add(link.text);
          }
        }
      }

      return Array.from(controls).slice(0, 80);
    },
    pageObservations() {
      return snapshots.map(toPageObservation);
    },
    complete(status: MissionEvidenceStatus, observations: JsonObject) {
      return {
        status,
        steps,
        screenshots: Array.from(screenshots.values()),
        observations,
        snapshots,
      };
    },
  };
}

function findSelectedStudyBasePath(snapshots: MissionPageSnapshot[]) {
  for (const snapshot of snapshots) {
    const links = snapshot.navigationLinks ?? snapshot.links;

    for (const link of links) {
      const path = link.path;
      if (!path || path.includes('[redacted')) {
        continue;
      }

      const match = /^\/app\/campaign-series\/([^/?#]+)(?:\/(?:setup|operations|reports|waves))?$/.exec(
        path
      );
      if (match) {
        return `/app/campaign-series/${match[1]}`;
      }
    }
  }

  return undefined;
}

function isSignInBlocked(snapshot: MissionPageSnapshot) {
  const path = extractPath(snapshot.url);
  if (path.startsWith('/signin') || path.startsWith('/auth/')) {
    return true;
  }

  if (path.startsWith('/app') && hasAppWorkflowSignal(snapshot)) {
    return false;
  }

  return hasSignInSignal(snapshot) && !hasAppWorkflowSignal(snapshot);
}

function hasSignInSignal(snapshot: MissionPageSnapshot) {
  return /\b(sign in|log in|continue with|workspace email|create workspace)\b/i.test(
    combinedSnapshotText(snapshot)
  );
}

function hasAppWorkflowSignal(snapshot: MissionPageSnapshot) {
  return /\b(study|studies|setup|collection|results|directory|waves|campaign|portfolio)\b/i.test(
    combinedSnapshotText(snapshot)
  );
}

function combinedSnapshotText(snapshot: MissionPageSnapshot) {
  return [
    snapshot.title,
    snapshot.visibleTextExcerpt,
    ...snapshot.buttons,
    ...snapshot.links.map((link) => link.text),
  ].join(' ');
}

function extractPath(url: string) {
  try {
    return new URL(url).pathname;
  } catch {
    return url.split(/[?#]/)[0] ?? '';
  }
}

function toPageObservation(snapshot: MissionPageSnapshot): JsonObject {
  return {
    label: snapshot.label,
    title: snapshot.title,
    url: snapshot.url,
    visibleTextExcerpt: snapshot.visibleTextExcerpt,
    buttons: snapshot.buttons,
    links: snapshot.links.map(toLinkObservation),
  };
}

function toLinkObservation(link: MissionPageLink): JsonObject {
  return link.path ? { text: link.text, path: link.path } : { text: link.text };
}

function routeOnlyNavigationPolicy(): JsonObject {
  return {
    routeNavigationOnly: true,
    mutatingActionsClicked: false,
    avoidedActions:
      'create, save, launch, export, duplicate, delete, invite, import, and submit',
  };
}
