import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

import { chromium, type Page } from '@playwright/test';

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
import type { ViewportPreset } from './types.ts';

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
  outputRoot?: string;
  runDirectory?: string;
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

export async function captureBrowserEvidence(
  options: BrowserEvidenceOptions
): Promise<BrowserEvidenceCapture> {
  const safeCapturePolicy = resolveBrowserSafeCapturePolicy(options);
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
          safeCapturePolicy: describeSafeCapturePolicy(safeCapturePolicy),
          title,
          url,
          visibleTextExcerpt,
          buttons,
          links,
        } satisfies JsonObject,
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
    const evidencePaths = await writeMissionEvidence(runDirectory, {
      missionId: options.missionId,
      personaId: options.personaId,
      missionGoal: options.missionGoal,
      status: execution.status,
      startedAt,
      completedAt: new Date(),
      steps: execution.steps,
      screenshots: execution.screenshots,
      observations: execution.observations,
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
  constructor(
    private readonly page: Page,
    private readonly baseUrl: string,
    private readonly safeCapturePolicy: BrowserSafeCapturePolicy,
    private readonly screenshotDirectory: string | undefined
  ) {}

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

    return {
      label,
      title,
      url,
      visibleTextExcerpt,
      buttons,
      links,
      navigationLinks,
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

async function readVisibleBodyText(page: Page) {
  return page.locator('body').innerText({ timeout: 5000 }).catch(() => '');
}

async function collectVisibleButtonLabels(page: Page) {
  const labels = await page.locator('button:visible').evaluateAll((nodes) =>
    nodes
      .map((node) => (node.textContent ?? '').replace(/\s+/g, ' ').trim())
      .filter(Boolean)
      .slice(0, 30)
  );

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
  );

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
  );

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
