import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

import { chromium, type Page } from '@playwright/test';

import {
  createRunDirectory,
  writeMissionEvidence,
  type JsonObject,
} from './evidence.ts';
import type { ViewportPreset } from './types.ts';

export interface BrowserEvidenceOptions {
  baseUrl: string;
  missionId: string;
  personaId: string;
  missionGoal: string;
  viewport: ViewportPreset;
  headless?: boolean;
  outputRoot?: string;
  runDirectory?: string;
}

export interface CapturedLink {
  text: string;
  href: string;
}

export interface BrowserEvidenceCapture {
  title: string;
  url: string;
  visibleTextExcerpt: string;
  buttons: string[];
  links: CapturedLink[];
  screenshotPath?: string;
  runDirectory?: string;
  evidencePath?: string;
  transcriptPath?: string;
}

const viewportSizes: Record<ViewportPreset, { width: number; height: number }> = {
  desktop: { width: 1440, height: 1000 },
  tablet: { width: 900, height: 1100 },
  mobile: { width: 390, height: 844 },
};

export async function captureBrowserEvidence(
  options: BrowserEvidenceOptions
): Promise<BrowserEvidenceCapture> {
  const browser = await chromium.launch({ headless: options.headless ?? true });
  const startedAt = new Date();

  try {
    const context = await browser.newContext({
      viewport: viewportSizes[options.viewport],
    });
    const page = await context.newPage();

    await page.goto(options.baseUrl, { waitUntil: 'domcontentloaded' });

    const title = await page.title();
    const url = page.url();
    const visibleTextExcerpt = excerpt(await readVisibleBodyText(page));
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
      const screenshotDirectory = join(
        runDirectory,
        'missions',
        options.missionId,
        'screenshots'
      );
      const screenshotPath = join(screenshotDirectory, 'initial-page.png');

      await mkdir(screenshotDirectory, { recursive: true });
      await page.screenshot({ path: screenshotPath, fullPage: true });

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
        screenshots: [
          {
            label: 'initial-page',
            path: 'screenshots/initial-page.png',
          },
        ],
        observations: {
          title,
          url,
          visibleTextExcerpt,
          buttons,
          links,
        } satisfies JsonObject,
      });

      capture.screenshotPath = screenshotPath;
      capture.runDirectory = runDirectory;
      capture.evidencePath = evidencePaths.evidencePath;
      capture.transcriptPath = evidencePaths.transcriptPath;
    }

    return capture;
  } finally {
    await browser.close();
  }
}

async function readVisibleBodyText(page: Page) {
  return page.locator('body').innerText({ timeout: 5000 }).catch(() => '');
}

async function collectVisibleButtonLabels(page: Page) {
  return page.locator('button:visible').evaluateAll((nodes) =>
    nodes
      .map((node) => (node.textContent ?? '').replace(/\s+/g, ' ').trim())
      .filter(Boolean)
      .slice(0, 30)
  );
}

async function collectVisibleLinks(page: Page): Promise<CapturedLink[]> {
  return page.locator('a:visible').evaluateAll((nodes) =>
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
}

function excerpt(text: string) {
  return text.replace(/\s+/g, ' ').trim().slice(0, 4000);
}
