import { describe, expect, it } from 'vitest';

import {
  executeCreateFirstStudyMission,
  type MissionPageAdapter,
  type MissionPageSnapshot,
} from './mission-executor';

class FakeMissionPage implements MissionPageAdapter {
  readonly visitedPaths: string[] = [];

  constructor(private readonly snapshots: Record<string, MissionPageSnapshot>) {}

  async gotoPath(path: string, label: string): Promise<MissionPageSnapshot> {
    this.visitedPaths.push(path);

    const snapshot = this.snapshots[path];
    if (!snapshot) {
      throw new Error(`Missing fake snapshot for ${path}`);
    }

    return {
      ...snapshot,
      label,
      screenshot: snapshot.screenshot ?? {
        label,
        path: `screenshots/${label}.png`,
      },
    };
  }
}

const context = {
  baseUrl: 'http://127.0.0.1:5174',
  missionId: 'create-first-study',
  personaId: 'first-time-researcher',
  missionGoal:
    'Find how a first-time researcher creates or opens a first study without unsafe persisted writes.',
};

describe('create-first-study mission execution', () => {
  it('records sign-in-blocked observations instead of failing the run', async () => {
    const page = new FakeMissionPage({
      '/signin': snapshot({
        label: 'signin-entry',
        url: 'http://127.0.0.1:5174/signin',
        visibleTextExcerpt:
          'Sign in to continue. Use your workspace email to enter the private beta.',
        buttons: ['Sign in'],
        links: [{ text: 'Create workspace', path: '/register' }],
      }),
      '/app': snapshot({
        label: 'app-entry',
        url: 'http://127.0.0.1:5174/signin',
        visibleTextExcerpt: 'Sign in to continue before opening the app.',
        buttons: ['Sign in'],
        links: [],
      }),
    });

    const result = await executeCreateFirstStudyMission(page, context);

    expect(result.status).toBe('blocked');
    expect(result.steps.length).toBeGreaterThanOrEqual(2);
    expect(result.steps[0]).toEqual(
      expect.objectContaining({
        action: expect.stringContaining('Opened the local sign-in route'),
        url: 'http://127.0.0.1:5174/signin',
      })
    );
    expect(result.steps[1]).toEqual(
      expect.objectContaining({
        action: expect.stringContaining('Tried to enter the app workspace'),
        url: 'http://127.0.0.1:5174/signin',
      })
    );
    expect(result.screenshots).toContainEqual({
      label: 'signin-entry',
      path: 'screenshots/signin-entry.png',
    });
    expect(result.observations).toEqual(
      expect.objectContaining({
        startUrl: 'http://127.0.0.1:5174/signin',
        signInBlocked: true,
        visibleControls: expect.arrayContaining(['Sign in']),
      })
    );
  });

  it('records app navigation observations when local auth provides access', async () => {
    const page = new FakeMissionPage({
      '/signin': snapshot({
        label: 'signin-entry',
        url: 'http://127.0.0.1:5174/signin',
        visibleTextExcerpt: 'Sign in or continue with the local development session.',
        buttons: ['Continue'],
        links: [{ text: 'Open app', path: '/app' }],
      }),
      '/app': snapshot({
        label: 'app-entry',
        url: 'http://127.0.0.1:5174/app',
        visibleTextExcerpt:
          'Workspace home. Create first study. Open Studies. Setup Collection Results.',
        buttons: [],
        links: [{ text: 'Open Studies', path: '/app/campaign-series' }],
      }),
      '/app/campaign-series': snapshot({
        label: 'studies-index',
        url: 'http://127.0.0.1:5174/app/campaign-series',
        visibleTextExcerpt:
          'Create your study. Open a study. Researcher onboarding pilot.',
        buttons: ['Create study'],
        links: [
          {
            text: 'Researcher onboarding pilot',
            path: '/app/campaign-series/study-local-1',
          },
          {
            text: 'Setup',
            path: '/app/campaign-series/study-local-1/setup',
          },
        ],
      }),
      '/app/campaign-series/study-local-1/setup': snapshot({
        label: 'study-setup',
        url: 'http://127.0.0.1:5174/app/campaign-series/study-local-1/setup',
        visibleTextExcerpt:
          'Setup workspace. Questionnaire, Results setup, Audience, Launch readiness.',
        buttons: ['Save draft'],
        links: [],
      }),
      '/app/campaign-series/study-local-1/operations': snapshot({
        label: 'study-collection',
        url: 'http://127.0.0.1:5174/app/campaign-series/study-local-1/operations',
        visibleTextExcerpt:
          'Collection workspace. Prepare recipients and review launch readiness.',
        buttons: ['Preview recipients'],
        links: [],
      }),
      '/app/campaign-series/study-local-1/reports': snapshot({
        label: 'study-results',
        url: 'http://127.0.0.1:5174/app/campaign-series/study-local-1/reports',
        visibleTextExcerpt:
          'Results workspace. Review available findings and download CSV exports.',
        buttons: ['Download CSV'],
        links: [],
      }),
      '/app/campaign-series/study-local-1/waves': snapshot({
        label: 'study-waves',
        url: 'http://127.0.0.1:5174/app/campaign-series/study-local-1/waves',
        visibleTextExcerpt:
          'Waves. Review Wave 1 and Wave 2 group trends and linked change limits.',
        buttons: [],
        links: [],
      }),
    });

    const result = await executeCreateFirstStudyMission(page, context);

    expect(result.status).toBe('completed');
    expect(page.visitedPaths).toEqual([
      '/signin',
      '/app',
      '/app/campaign-series',
      '/app/campaign-series/study-local-1/setup',
      '/app/campaign-series/study-local-1/operations',
      '/app/campaign-series/study-local-1/reports',
      '/app/campaign-series/study-local-1/waves',
    ]);
    expect(result.screenshots).toEqual(
      expect.arrayContaining([
        { label: 'studies-index', path: 'screenshots/studies-index.png' },
        { label: 'study-setup', path: 'screenshots/study-setup.png' },
        { label: 'study-collection', path: 'screenshots/study-collection.png' },
        { label: 'study-results', path: 'screenshots/study-results.png' },
      ])
    );
    expect(result.observations).toEqual(
      expect.objectContaining({
        signInBlocked: false,
        visibleControls: expect.arrayContaining([
          'Continue',
          'Create study',
          'Save draft',
          'Preview recipients',
          'Download CSV',
        ]),
        visitedWorkflowSurfaces: ['setup', 'collection', 'results', 'waves'],
      })
    );
  });

  it('blocks with a prerequisite observation when no selected study link is visible', async () => {
    const page = new FakeMissionPage({
      '/signin': snapshot({
        label: 'signin-entry',
        url: 'http://127.0.0.1:5174/signin',
        visibleTextExcerpt: 'Sign in or continue with the local development session.',
        buttons: ['Continue'],
        links: [{ text: 'Open app', path: '/app' }],
      }),
      '/app': snapshot({
        label: 'app-entry',
        url: 'http://127.0.0.1:5174/app',
        visibleTextExcerpt:
          'Workspace home. Create first study. Open Studies. Setup Collection Results.',
        buttons: [],
        links: [{ text: 'Open Studies', path: '/app/campaign-series' }],
      }),
      '/app/campaign-series': snapshot({
        label: 'studies-index',
        url: 'http://127.0.0.1:5174/app/campaign-series',
        visibleTextExcerpt:
          'No study is selected yet. Create a study before opening setup.',
        buttons: ['Create study'],
        links: [{ text: 'Home', path: '/app' }],
      }),
    });

    const result = await executeCreateFirstStudyMission(page, context);

    expect(result.status).toBe('blocked');
    expect(page.visitedPaths).toEqual([
      '/signin',
      '/app',
      '/app/campaign-series',
    ]);
    expect(result.observations).toEqual(
      expect.objectContaining({
        appAccessible: true,
        selectedStudyPathFound: false,
        blockedReason: 'selected-study-required',
        seededStudyAccessPrerequisite: expect.stringContaining(
          'Seed or create a study'
        ),
        visitedWorkflowSurfaces: [],
      })
    );
  });
});

function snapshot(snapshot: MissionPageSnapshot): MissionPageSnapshot {
  return snapshot;
}
