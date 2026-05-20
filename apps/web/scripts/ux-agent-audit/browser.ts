import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

import { chromium, type Page } from '@playwright/test';

import { resolveAutonomousMissionForDataMode } from './autonomous-fixtures.ts';
import { resolveAutonomousProductApiResponse } from './autonomous-product-read-models.ts';
import {
  buildScriptedFixturePersonaActor,
  runAutonomousFixtureMission,
  type AutonomousPageAdapter,
} from './autonomous-loop.ts';
import {
  describeFullstackDevAuth,
  resolveFullstackDevAuthHeaders,
} from './fullstack-dev-auth.ts';
import {
  createRunDirectory,
  writeMissionEvidence,
  type JsonObject,
  type MissionEvidenceStatus,
} from './evidence.ts';
import {
  executeCreateFirstStudyMission,
  type MissionNavigationLink,
  type MissionPageAdapter,
  type MissionPageSnapshot,
} from './mission-executor.ts';
import {
  buildRichTranscriptMarkdown,
  normalizeRichScreenSnapshot,
  type RawRichScreenSnapshot,
  type RichScreenSnapshot,
} from './rich-transcript.ts';
import type {
  AutonomousDataMode,
  AutonomousFullstackDevAuthOptions,
  CaptureMode,
  ViewportPreset,
} from './types.ts';

export interface BrowserEvidenceOptions {
  baseUrl: string;
  missionId: string;
  personaId: string;
  missionGoal: string;
  viewport: ViewportPreset;
  captureScreenshots?: boolean;
  headless?: boolean;
  includeSanitizedVisibleText?: boolean;
  executeFixedMission?: boolean;
  maxVisibleTextCharacters?: number;
  captureMode?: CaptureMode;
  outputRoot?: string;
  runDirectory?: string;
  autonomousDataMode?: AutonomousDataMode;
  fullstackDevAuth?: AutonomousFullstackDevAuthOptions;
}

export interface BrowserSafeCapturePolicy {
  captureScreenshots: boolean;
  includeSanitizedVisibleText: boolean;
  maxVisibleTextCharacters: number;
}

export interface CapturedLink {
  text: string;
  path?: string;
}

export interface BrowserEvidenceCapture {
  title: string;
  url: string;
  visibleTextExcerpt: string;
  buttons: string[];
  links: CapturedLink[];
  screenshotPath?: string;
  status?: MissionEvidenceStatus;
  runDirectory?: string;
  evidencePath?: string;
  transcriptPath?: string;
  reviewerOutput?: string;
}

const viewportSizes: Record<ViewportPreset, { width: number; height: number }> = {
  desktop: { width: 1440, height: 1000 },
  tablet: { width: 900, height: 1100 },
  mobile: { width: 390, height: 844 },
};

const defaultVisibleTextLimit = 2000;
const maxVisibleTextLimit = 4000;
const controlTextLimit = 160;
const linkTextLimit = 160;

const emailPattern = /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/gi;
const uuidPattern =
  /\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b/gi;
const jwtLikePattern =
  /\b[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{3,}\.[A-Za-z0-9_-]{3,}\b/g;
const longTokenPattern =
  /\b(?=[A-Za-z0-9_-]{24,}\b)(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9_-]+\b/g;
const participantCodeLikePattern = /\b[A-Z0-9]{4,}(?:[-_][A-Z0-9]{3,})+\b/g;

export { resolveFullstackDevAuthHeaders } from './fullstack-dev-auth.ts';

export async function captureBrowserEvidence(
  options: BrowserEvidenceOptions
): Promise<BrowserEvidenceCapture> {
  const safeCapturePolicy = resolveBrowserSafeCapturePolicy(options);
  const captureMode = options.captureMode ?? 'local-full';
  const browser = await chromium.launch({ headless: options.headless ?? true });
  const startedAt = new Date();

  try {
    const context = await browser.newContext({
      viewport: viewportSizes[options.viewport],
    });
    const page = await context.newPage();

    if (options.executeFixedMission) {
      return await captureFixedMissionEvidence(
        page,
        options,
        safeCapturePolicy,
        startedAt
      );
    }

    await page.goto(options.baseUrl, { waitUntil: 'domcontentloaded' });
    await waitForPageReadyForSnapshot(page);

    const title = sanitizeVisibleTextForEvidence(
      await page.title(),
      controlTextLimit
    );
    const url = sanitizeEvidenceUrl(page.url());
    const visibleTextExcerpt = safeCapturePolicy.includeSanitizedVisibleText
      ? sanitizeVisibleTextForEvidence(
          await readVisibleBodyText(page),
          safeCapturePolicy.maxVisibleTextCharacters
        )
      : '';
    const buttons = await collectVisibleButtonLabels(page);
    const links = await collectVisibleLinks(page);
    const richTranscript =
      captureMode === 'local-full'
        ? await collectRichScreenSnapshot(page, 'initial-page')
        : undefined;

    const capture: BrowserEvidenceCapture = {
      title,
      url,
      visibleTextExcerpt,
      buttons,
      links,
    };

    if (options.outputRoot || options.runDirectory) {
      const runDirectory =
        options.runDirectory ??
        (
          await createRunDirectory({
            outputRoot: options.outputRoot ?? '.',
          })
        ).runDirectory;
      const screenshots: Array<{ label: string; path: string }> = [];

      if (safeCapturePolicy.captureScreenshots) {
        const screenshotDirectory = join(
          runDirectory,
          'missions',
          options.missionId,
          'screenshots'
        );
        const screenshotPath = join(screenshotDirectory, 'initial-page.png');

        await mkdir(screenshotDirectory, { recursive: true });
        await page.screenshot({ path: screenshotPath, fullPage: true });

        screenshots.push({
          label: 'initial-page',
          path: 'screenshots/initial-page.png',
        });
        capture.screenshotPath = screenshotPath;
      }

      const evidencePaths = await writeMissionEvidence(runDirectory, {
        missionId: options.missionId,
        personaId: options.personaId,
        missionGoal: options.missionGoal,
        status: 'completed',
        startedAt,
        completedAt: new Date(),
        steps: [
          {
            index: 1,
            action: 'Opened the audit base URL and captured the initial page state.',
            url,
          },
        ],
        screenshots,
        observations: {
          captureMode,
          safeCapturePolicy: describeSafeCapturePolicy(safeCapturePolicy),
          ...(richTranscript ? { localFullTranscript: [richTranscript] } : {}),
          title,
          url,
          visibleTextExcerpt,
          buttons,
          links,
        } satisfies JsonObject,
        ...(richTranscript
          ? { transcriptMarkdown: buildRichTranscriptMarkdown([richTranscript]) }
          : {}),
      });

      capture.runDirectory = runDirectory;
      capture.evidencePath = evidencePaths.evidencePath;
      capture.transcriptPath = evidencePaths.transcriptPath;
    }

    return capture;
  } finally {
    await browser.close();
  }
}

export async function captureAutonomousBrowserEvidence(
  options: BrowserEvidenceOptions
): Promise<BrowserEvidenceCapture> {
  const autonomousDataMode = options.autonomousDataMode ?? 'fixture';
  const mission = resolveAutonomousMissionForDataMode(
    options.missionId,
    autonomousDataMode
  );

  const safeCapturePolicy = resolveBrowserSafeCapturePolicy(options);
  const captureMode = options.captureMode ?? 'local-full';
  const browser = await chromium.launch({ headless: options.headless ?? true });
  const startedAt = new Date();

  try {
    const fullstackDevAuthHeaders =
      autonomousDataMode === 'fullstack'
        ? resolveFullstackDevAuthHeaders(options.fullstackDevAuth)
        : undefined;
    const context = await browser.newContext({
      viewport: viewportSizes[options.viewport],
      ...(fullstackDevAuthHeaders ? { extraHTTPHeaders: fullstackDevAuthHeaders } : {}),
    });
    const page = await context.newPage();
    if (autonomousDataMode === 'fixture') {
      await routeLocalAutonomousFixtureSession(page);
    }
    const runDirectory =
      options.runDirectory ??
      (options.outputRoot
        ? (
            await createRunDirectory({
              outputRoot: options.outputRoot,
            })
          ).runDirectory
        : undefined);
    const adapter = new PlaywrightAutonomousPageAdapter(
      page,
      options.baseUrl,
      safeCapturePolicy,
      captureMode,
      runDirectory
        ? join(runDirectory, 'missions', options.missionId, 'screenshots')
        : undefined
    );
    const execution = await runAutonomousFixtureMission(
      adapter,
      mission,
      buildScriptedFixturePersonaActor(mission)
    );
    const finalSnapshot = execution.snapshots.at(-1);
    const firstScreenshot = execution.screenshots.at(0);
    const capture: BrowserEvidenceCapture = {
      title: finalSnapshot?.title ?? '',
      url: finalSnapshot?.url ?? '',
      visibleTextExcerpt: finalSnapshot?.visibleTextExcerpt ?? '',
      buttons: finalSnapshot?.buttons ?? [],
      links: finalSnapshot?.links ?? [],
      status: execution.status,
      reviewerOutput: execution.reviewerOutput,
    };

    if (runDirectory) {
      const richScreens = execution.snapshots
        .map((snapshot) => snapshot.richTranscript)
        .filter((snapshot): snapshot is RichScreenSnapshot => Boolean(snapshot));
      const evidencePaths = await writeMissionEvidence(runDirectory, {
        missionId: options.missionId,
        personaId: mission.personaId,
        missionGoal: mission.goal,
        status: execution.status,
        startedAt,
        completedAt: new Date(),
        steps: execution.steps,
        screenshots: execution.screenshots,
        observations: {
          ...execution.observations,
          captureMode,
          autonomousDataMode,
          productReadModelMocks:
            autonomousDataMode === 'fixture' ? 'enabled' : 'disabled',
          fullstackDevAuth:
            autonomousDataMode === 'fullstack'
              ? describeFullstackDevAuth(options.fullstackDevAuth)
              : 'not-applicable',
          safeCapturePolicy: describeSafeCapturePolicy(safeCapturePolicy),
          ...(richScreens.length ? { localFullTranscript: richScreens } : {}),
        },
        ...(richScreens.length
          ? { transcriptMarkdown: buildRichTranscriptMarkdown(richScreens) }
          : {}),
      });

      capture.runDirectory = runDirectory;
      capture.evidencePath = evidencePaths.evidencePath;
      capture.transcriptPath = evidencePaths.transcriptPath;
      capture.screenshotPath = firstScreenshot
        ? join(runDirectory, 'missions', options.missionId, firstScreenshot.path)
        : undefined;
    }

    return capture;
  } finally {
    await browser.close();
  }
}

async function captureFixedMissionEvidence(
  page: Page,
  options: BrowserEvidenceOptions,
  safeCapturePolicy: BrowserSafeCapturePolicy,
  startedAt: Date
): Promise<BrowserEvidenceCapture> {
  if (options.missionId !== 'create-first-study') {
    throw new Error(`No fixed mission executor for ${options.missionId}`);
  }

  const runDirectory =
    options.runDirectory ??
    (options.outputRoot
      ? (
          await createRunDirectory({
            outputRoot: options.outputRoot,
          })
        ).runDirectory
      : undefined);
  const adapter = new PlaywrightMissionPageAdapter(
    page,
    options.baseUrl,
    safeCapturePolicy,
    options.captureMode ?? 'local-full',
    runDirectory
      ? join(runDirectory, 'missions', options.missionId, 'screenshots')
      : undefined
  );
  const execution = await executeCreateFirstStudyMission(adapter, {
    baseUrl: options.baseUrl,
    missionId: options.missionId,
    personaId: options.personaId,
    missionGoal: options.missionGoal,
  });
  const finalSnapshot = execution.snapshots.at(-1);
  const firstScreenshot = execution.screenshots.at(0);
  const capture: BrowserEvidenceCapture = {
    title: finalSnapshot?.title ?? '',
    url: finalSnapshot?.url ?? '',
    visibleTextExcerpt: finalSnapshot?.visibleTextExcerpt ?? '',
    buttons: finalSnapshot?.buttons ?? [],
    links: finalSnapshot?.links ?? [],
    status: execution.status,
  };

  if (runDirectory) {
    const richScreens = execution.snapshots
      .map((snapshot) => snapshot.richTranscript)
      .filter((snapshot): snapshot is RichScreenSnapshot => Boolean(snapshot));
    const evidencePaths = await writeMissionEvidence(runDirectory, {
      missionId: options.missionId,
      personaId: options.personaId,
      missionGoal: options.missionGoal,
      status: execution.status,
      startedAt,
      completedAt: new Date(),
      steps: execution.steps,
      screenshots: execution.screenshots,
      observations: {
        ...execution.observations,
        captureMode: options.captureMode ?? 'local-full',
        ...(richScreens.length ? { localFullTranscript: richScreens } : {}),
      },
      ...(richScreens.length
        ? { transcriptMarkdown: buildRichTranscriptMarkdown(richScreens) }
        : {}),
    });

    capture.runDirectory = runDirectory;
    capture.evidencePath = evidencePaths.evidencePath;
    capture.transcriptPath = evidencePaths.transcriptPath;
    capture.screenshotPath = firstScreenshot
      ? join(runDirectory, 'missions', options.missionId, firstScreenshot.path)
      : undefined;
  }

  return capture;
}

class PlaywrightMissionPageAdapter implements MissionPageAdapter {
  private readonly page: Page;
  private readonly baseUrl: string;
  private readonly safeCapturePolicy: BrowserSafeCapturePolicy;
  private readonly captureMode: CaptureMode;
  private readonly screenshotDirectory: string | undefined;

  constructor(
    page: Page,
    baseUrl: string,
    safeCapturePolicy: BrowserSafeCapturePolicy,
    captureMode: CaptureMode,
    screenshotDirectory: string | undefined
  ) {
    this.page = page;
    this.baseUrl = baseUrl;
    this.safeCapturePolicy = safeCapturePolicy;
    this.captureMode = captureMode;
    this.screenshotDirectory = screenshotDirectory;
  }

  async gotoPath(path: string, label: string): Promise<MissionPageSnapshot> {
    await this.page.goto(new URL(path, this.baseUrl).toString(), {
      waitUntil: 'domcontentloaded',
    });
    await waitForPageReadyForSnapshot(this.page);

    return await this.captureSnapshot(label);
  }

  private async captureSnapshot(label: string): Promise<MissionPageSnapshot> {
    const title = sanitizeVisibleTextForEvidence(
      await this.page.title(),
      controlTextLimit
    );
    const url = sanitizeEvidenceUrl(this.page.url());
    const visibleTextExcerpt = this.safeCapturePolicy.includeSanitizedVisibleText
      ? sanitizeVisibleTextForEvidence(
          await readVisibleBodyText(this.page),
          this.safeCapturePolicy.maxVisibleTextCharacters
        )
      : '';
    const buttons = await collectVisibleButtonLabels(this.page);
    const links = await collectVisibleLinks(this.page);
    const navigationLinks = await collectVisibleNavigationLinks(this.page);
    const screenshot = await this.captureScreenshot(label);
    const richTranscript =
      this.captureMode === 'local-full'
        ? await collectRichScreenSnapshot(this.page, label)
        : undefined;

    return {
      label,
      title,
      url,
      visibleTextExcerpt,
      buttons,
      links,
      navigationLinks,
      ...(richTranscript ? { richTranscript } : {}),
      ...(screenshot ? { screenshot } : {}),
    };
  }

  private async captureScreenshot(label: string) {
    if (!this.safeCapturePolicy.captureScreenshots || !this.screenshotDirectory) {
      return undefined;
    }

    await mkdir(this.screenshotDirectory, { recursive: true });

    const fileName = `${label}.png`;
    const screenshotPath = join(this.screenshotDirectory, fileName);
    await this.page.screenshot({ path: screenshotPath, fullPage: true });

    return {
      label,
      path: `screenshots/${fileName}`,
    };
  }
}

class PlaywrightAutonomousPageAdapter implements AutonomousPageAdapter {
  private readonly page: Page;
  private readonly baseUrl: string;
  private readonly safeCapturePolicy: BrowserSafeCapturePolicy;
  private readonly captureMode: CaptureMode;
  private readonly screenshotDirectory: string | undefined;

  constructor(
    page: Page,
    baseUrl: string,
    safeCapturePolicy: BrowserSafeCapturePolicy,
    captureMode: CaptureMode,
    screenshotDirectory: string | undefined
  ) {
    this.page = page;
    this.baseUrl = baseUrl;
    this.safeCapturePolicy = safeCapturePolicy;
    this.captureMode = captureMode;
    this.screenshotDirectory = screenshotDirectory;
  }

  async gotoPath(path: string, label = 'autonomous-route'): Promise<MissionPageSnapshot> {
    await this.page.goto(new URL(path, this.baseUrl).toString(), {
      waitUntil: 'domcontentloaded',
    });
    await waitForPageReadyForSnapshot(this.page);

    return await this.captureSnapshot(label);
  }

  async clickLink(
    text: string,
    label = 'autonomous-link',
    path?: string
  ): Promise<MissionPageSnapshot> {
    if (path) {
      const clickedByPath = await this.page.locator('a[href]').evaluateAll(
        (anchors, targetPath) => {
          const target = anchors.find((anchor) => {
            const href = (anchor as HTMLAnchorElement).href;
            try {
              return new URL(href).pathname === targetPath;
            } catch {
              return (anchor as HTMLAnchorElement).getAttribute('href') === targetPath;
            }
          }) as HTMLElement | undefined;

          if (!target) {
            return false;
          }

          target.click();
          return true;
        },
        path
      ).catch(() => false);
      if (clickedByPath) {
        await waitForPageReadyForSnapshot(this.page);

        return await this.captureSnapshot(label);
      }

      await this.page.goto(new URL(path, this.baseUrl).toString(), {
        waitUntil: 'domcontentloaded',
      });
      await waitForPageReadyForSnapshot(this.page);

      return await this.captureSnapshot(label);
    }

    await this.page.getByRole('link', { name: text }).first().click({ timeout: 5000 });
    await waitForPageReadyForSnapshot(this.page);

    return await this.captureSnapshot(label);
  }

  async clickButton(text: string, label = 'autonomous-button'): Promise<MissionPageSnapshot> {
    await this.page.getByRole('button', { name: text }).first().click({ timeout: 5000 });
    await waitForPageReadyForSnapshot(this.page);

    return await this.captureSnapshot(label);
  }

  async fillField(
    fieldLabel: string,
    value: string,
    label = 'autonomous-field'
  ): Promise<MissionPageSnapshot> {
    const labelled = this.page.getByLabel(fieldLabel).first();
    const placeholder = this.page.getByPlaceholder(fieldLabel).first();

    if ((await labelled.count().catch(() => 0)) > 0) {
      await labelled.fill(value, { timeout: 5000 });
    } else {
      await placeholder.fill(value, { timeout: 5000 });
    }
    await waitForPageReadyForSnapshot(this.page);

    return await this.captureSnapshot(label);
  }

  async captureSnapshot(label: string): Promise<MissionPageSnapshot> {
    const title = sanitizeVisibleTextForEvidence(
      await this.page.title(),
      controlTextLimit
    );
    const url = sanitizeEvidenceUrl(this.page.url());
    const visibleTextExcerpt = this.safeCapturePolicy.includeSanitizedVisibleText
      ? sanitizeVisibleTextForEvidence(
          await readVisibleBodyText(this.page),
          this.safeCapturePolicy.maxVisibleTextCharacters
        )
      : '';
    const buttons = await collectVisibleButtonLabels(this.page);
    const links = await collectVisibleLinks(this.page);
    const navigationLinks = await collectVisibleNavigationLinks(this.page);
    const screenshot = await this.captureScreenshot(label);
    const richTranscript =
      this.captureMode === 'local-full'
        ? await collectRichScreenSnapshot(this.page, label)
        : undefined;

    return {
      label,
      title,
      url,
      visibleTextExcerpt,
      buttons,
      links,
      navigationLinks,
      ...(richTranscript ? { richTranscript } : {}),
      ...(screenshot ? { screenshot } : {}),
    };
  }

  private async captureScreenshot(label: string) {
    if (!this.safeCapturePolicy.captureScreenshots || !this.screenshotDirectory) {
      return undefined;
    }

    await mkdir(this.screenshotDirectory, { recursive: true });

    const fileName = `${safeScreenshotLabel(label)}.png`;
    const screenshotPath = join(this.screenshotDirectory, fileName);
    await this.page.screenshot({ path: screenshotPath, fullPage: true });

    return {
      label,
      path: `screenshots/${fileName}`,
    };
  }
}

async function collectRichScreenSnapshot(
  page: Page,
  label: string
): Promise<RichScreenSnapshot> {
  if (typeof (page as { evaluate?: unknown }).evaluate !== 'function') {
    return normalizeRichScreenSnapshot(await fallbackRichScreenSnapshot(page, label));
  }

  const raw = await page.evaluate((snapshotLabel): RawRichScreenSnapshot => {
    const isVisible = (element: Element) => {
      const style = window.getComputedStyle(element);
      const rect = element.getBoundingClientRect();
      return (
        style.visibility !== 'hidden' &&
        style.display !== 'none' &&
        rect.width > 0 &&
        rect.height > 0
      );
    };
    const textOf = (element: Element | null | undefined) =>
      (element?.textContent ?? '').replace(/\s+/g, ' ').trim();
    const visibleElements = (selector: string) =>
      Array.from(document.querySelectorAll(selector)).filter(isVisible);
    const pathOf = (href: string) => {
      try {
        return new URL(href).pathname;
      } catch {
        return href.split(/[?#]/)[0] ?? '';
      }
    };
    const labelFor = (input: HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement) => {
      if (input.id) {
        const label = document.querySelector(`label[for="${CSS.escape(input.id)}"]`);
        if (label) {
          return textOf(label);
        }
      }

      const wrappingLabel = input.closest('label');
      if (wrappingLabel) {
        return textOf(wrappingLabel);
      }

      return (
        input.getAttribute('aria-label') ??
        input.getAttribute('name') ??
        input.getAttribute('placeholder') ??
        input.id ??
        ''
      );
    };

    return {
      label: snapshotLabel,
      title: document.title,
      url: window.location.href,
      visibleText: (document.body as HTMLElement | null)?.innerText ?? '',
      headings: visibleElements('h1,h2,h3,h4,h5,h6,[role="heading"]').map(textOf),
      buttons: visibleElements('button,[role="button"],input[type="button"],input[type="submit"]').map((element) => {
        const control = element as HTMLButtonElement | HTMLInputElement;
        return {
          text: textOf(element) || control.value || element.getAttribute('aria-label') || '',
          disabled:
            control.disabled === true ||
            element.getAttribute('aria-disabled') === 'true',
        };
      }),
      links: visibleElements('a[href]').map((element) => {
        const anchor = element as HTMLAnchorElement;
        return {
          text: textOf(anchor) || anchor.getAttribute('aria-label') || '',
          path: pathOf(anchor.href),
        };
      }),
      fields: visibleElements('input:not([type="hidden"]),textarea,select').map((element) => {
        const input = element as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement;
        return {
          label: labelFor(input),
          placeholder: input.getAttribute('placeholder') ?? '',
          value: input.value ?? '',
          required: input.required === true || input.getAttribute('aria-required') === 'true',
        };
      }),
      sections: visibleElements('main section,article,[role="region"],.card,.panel,[data-testid]').map((element) =>
        element.getAttribute('aria-label') ||
        element.getAttribute('data-testid') ||
        textOf(element).slice(0, 220)
      ),
      statusMessages: visibleElements('[role="alert"],[role="status"],.error,.warning,.success,.notice,.banner').map(textOf),
    };
  }, label).catch(() => fallbackRichScreenSnapshot(page, label));

  return normalizeRichScreenSnapshot(raw);
}

async function fallbackRichScreenSnapshot(
  page: Page,
  label: string
): Promise<RawRichScreenSnapshot> {
  return {
    label,
    title: await page.title().catch(() => ''),
    url: page.url(),
    visibleText: '',
    headings: [],
    buttons: [],
    links: [],
    fields: [],
    sections: [],
    statusMessages: [],
  };
}

async function readVisibleBodyText(page: Page) {
  return page.locator('body').innerText({ timeout: 5000 }).catch(() => '');
}

async function routeLocalAutonomousFixtureSession(page: Page) {
  await page.route('**/auth/session', async (route) => {
    await route.fulfill({
      json: {
        userId: '22222222-2222-4222-8222-222222222222',
        tenantId: '11111111-1111-4111-8111-111111111111',
        email: 'ux-agent@example.test',
        permissions: ['setup.manage', 'team.manage', 'export.read'],
      },
    });
  });
  await page.route('**/auth/csrf', async (route) => {
    await route.fulfill({ json: { csrfToken: 'local-ux-agent-csrf' } });
  });
  await page.route('**/*', async (route) => {
    const response = resolveAutonomousProductApiResponse(
      route.request().method(),
      route.request().url()
    );

    if (!response) {
      await route.fallback();
      return;
    }

    await route.fulfill({
      status: response.status,
      json: response.json,
    });
  });
}

async function collectVisibleButtonLabels(page: Page) {
  const labels = await page.locator('button:visible').evaluateAll((nodes) =>
    nodes
      .map((node) => (node.textContent ?? '').replace(/\s+/g, ' ').trim())
      .filter(Boolean)
      .slice(0, 30)
  ).catch(() => []);

  return labels
    .map((label) => sanitizeVisibleTextForEvidence(label, controlTextLimit))
    .filter(Boolean)
    .slice(0, 30);
}

async function collectVisibleLinks(page: Page): Promise<CapturedLink[]> {
  const links = await page.locator('a:visible').evaluateAll((nodes) =>
    nodes
      .map((node) => {
        const anchor = node as HTMLAnchorElement;
        return {
          text: (anchor.textContent ?? '').replace(/\s+/g, ' ').trim(),
          href: anchor.href,
        };
      })
      .filter((link) => link.text || link.href)
      .slice(0, 30)
  ).catch(() => []);

  return links
    .map(toSafeCapturedLink)
    .filter((link) => link.text || link.path)
    .slice(0, 30);
}

async function collectVisibleNavigationLinks(
  page: Page
): Promise<MissionNavigationLink[]> {
  const links = await page.locator('a:visible').evaluateAll((nodes) =>
    nodes
      .map((node) => {
        const anchor = node as HTMLAnchorElement;
        return {
          text: (anchor.textContent ?? '').replace(/\s+/g, ' ').trim(),
          href: anchor.href,
        };
      })
      .filter((link) => link.href)
      .slice(0, 50)
  ).catch(() => []);

  return links
    .map((link) => ({
      text: sanitizeVisibleTextForEvidence(link.text, linkTextLimit),
      path: toNavigationPath(link.href),
    }))
    .filter((link) => link.path)
    .slice(0, 50);
}

export function resolveBrowserSafeCapturePolicy(
  options: Pick<
    BrowserEvidenceOptions,
    | 'captureScreenshots'
    | 'includeSanitizedVisibleText'
    | 'maxVisibleTextCharacters'
  >
): BrowserSafeCapturePolicy {
  return {
    captureScreenshots: options.captureScreenshots === true,
    includeSanitizedVisibleText: options.includeSanitizedVisibleText === true,
    maxVisibleTextCharacters: normalizeVisibleTextLimit(
      options.maxVisibleTextCharacters
    ),
  };
}

export function sanitizeEvidenceUrl(url: string) {
  const value = (url ?? '').trim();

  if (!value) {
    return '';
  }

  try {
    const parsed = new URL(value);
    return `${parsed.origin}${sanitizeUrlPath(parsed.pathname)}`;
  } catch {
    return sanitizeUrlPath(stripQueryAndFragment(value));
  }
}

export function sanitizeVisibleTextForEvidence(
  text: string,
  maxCharacters = defaultVisibleTextLimit
) {
  const normalized = (text ?? '').replace(/\s+/g, ' ').trim();
  return redactSensitiveText(normalized).slice(
    0,
    normalizeVisibleTextLimit(maxCharacters)
  );
}

export function toSafeCapturedLink(link: {
  text?: string;
  href?: string;
}): CapturedLink {
  const text = sanitizeVisibleTextForEvidence(link.text ?? '', linkTextLimit);
  const path = sanitizeLinkPath(link.href ?? '');

  return {
    text,
    ...(path ? { path } : {}),
  };
}

export async function waitForPageReadyForSnapshot(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: 2000 }).catch(() => undefined);
  await page
    .waitForFunction(
      () =>
        document.readyState === 'interactive' ||
        document.readyState === 'complete',
      undefined,
      { timeout: 3000 }
    )
    .catch(() => undefined);
  await page
    .locator('body')
    .waitFor({ state: 'visible', timeout: 2000 })
    .catch(() => undefined);
  await page
    .locator('main, [role="main"], nav, form, button, a')
    .first()
    .waitFor({ state: 'visible', timeout: 2000 })
    .catch(() => undefined);
  await page.waitForTimeout(125);
}

function describeSafeCapturePolicy(policy: BrowserSafeCapturePolicy) {
  return {
    pageUrlCapture: 'origin-and-path-only-query-and-fragment-removed',
    linkCapture: 'sanitized-label-and-path-only',
    screenshotCapture: policy.captureScreenshots
      ? 'enabled-explicitly'
      : 'disabled-by-default',
    visibleTextCapture: policy.includeSanitizedVisibleText
      ? 'sanitized-and-bounded'
      : 'disabled-by-default',
    maxVisibleTextCharacters: policy.maxVisibleTextCharacters,
  };
}

function sanitizeLinkPath(href: string) {
  const value = (href ?? '').trim();

  if (!value) {
    return '';
  }

  try {
    return sanitizeUrlPath(new URL(value).pathname);
  } catch {
    return sanitizeUrlPath(stripQueryAndFragment(value));
  }
}

function sanitizeUrlPath(path: string) {
  return redactSensitiveText(stripQueryAndFragment(path));
}

function stripQueryAndFragment(value: string) {
  return value.split(/[?#]/)[0] ?? '';
}

function safeScreenshotLabel(label: string) {
  const normalized = (label ?? '')
    .replace(/[^A-Za-z0-9_-]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80);

  return normalized || 'snapshot';
}

function toNavigationPath(href: string) {
  const value = (href ?? '').trim();

  if (!value) {
    return '';
  }

  try {
    return stripQueryAndFragment(new URL(value).pathname);
  } catch {
    return stripQueryAndFragment(value);
  }
}

function normalizeVisibleTextLimit(value: number | undefined) {
  if (!Number.isFinite(value)) {
    return defaultVisibleTextLimit;
  }

  return Math.max(0, Math.min(maxVisibleTextLimit, Math.floor(value)));
}

function redactSensitiveText(text: string) {
  return text
    .replace(emailPattern, '[redacted-email]')
    .replace(uuidPattern, '[redacted-uuid]')
    .replace(jwtLikePattern, '[redacted-token]')
    .replace(longTokenPattern, '[redacted-token]')
    .replace(participantCodeLikePattern, '[redacted-code]');
}
