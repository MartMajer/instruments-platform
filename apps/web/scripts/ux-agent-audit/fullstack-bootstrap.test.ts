import { describe, expect, it, vi } from 'vitest';

import { runFullstackBootstrap } from './fullstack-bootstrap.ts';

describe('UXA02 full-stack bootstrap', () => {
  it('blocks with Docker guidance before running scripts when Docker is unavailable', async () => {
    const report = await runFullstackBootstrap({
      repoRoot: 'C:\\repo',
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      start: true,
      checkDocker: vi.fn(async () => false),
      runCommand: vi.fn(),
      preflight: vi.fn(),
    });

    expect(report.status).toBe('blocked');
    expect(report.steps[0]).toEqual(
      expect.objectContaining({
        id: 'docker',
        status: 'failed',
        guidance: expect.stringContaining('Docker Desktop'),
      })
    );
    expect(report.commands.startLocalStaging).toContain('start-local-staging.ps1');
    expect(report.commands.runMutation).toContain('fullstack-create-study');
  });

  it('starts the project local staging stack before running preflight when requested', async () => {
    const runCommand = vi.fn(async () => ({ exitCode: 0, output: 'started' }));
    const preflight = vi.fn(async () => ({
      status: 'ready' as const,
      apiBaseUrl: 'http://127.0.0.1:5055',
      checks: [],
    }));

    const report = await runFullstackBootstrap({
      repoRoot: 'C:\\repo',
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      start: true,
      checkDocker: vi.fn(async () => true),
      runCommand,
      preflight,
    });

    expect(report.status).toBe('ready');
    expect(runCommand).toHaveBeenCalledWith(
      expect.objectContaining({
        filePath: 'powershell',
        args: expect.arrayContaining([
          '-File',
          'C:\\repo\\deploy\\staging\\start-local-staging.ps1',
        ]),
      })
    );
    expect(preflight).toHaveBeenCalledWith(
      expect.objectContaining({
        apiBaseUrl: 'http://127.0.0.1:5055',
        fullstackDevAuth: { enabled: true },
      })
    );
  });

  it('requires a reachable Docker engine when using the default Docker check', async () => {
    const runCommand = vi.fn(async () => ({
      exitCode: 1,
      output: 'Cannot connect to the Docker daemon',
    }));

    const report = await runFullstackBootstrap({
      repoRoot: 'C:\\repo',
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      start: true,
      runCommand,
      preflight: vi.fn(),
    });

    expect(report.status).toBe('blocked');
    expect(runCommand).toHaveBeenCalledWith(
      expect.objectContaining({
        filePath: 'docker',
        args: ['info', '--format', '{{.ServerVersion}}'],
      })
    );
    expect(report.steps[0].detail).toContain('Docker command is unavailable');
  });

  it('does not start Compose when start is false and still returns next commands', async () => {
    const runCommand = vi.fn();
    const preflight = vi.fn(async () => ({
      status: 'blocked' as const,
      apiBaseUrl: 'http://127.0.0.1:5055',
      checks: [
        {
          id: 'api-health' as const,
          label: 'Local API health',
          status: 'failed' as const,
          detail: 'fetch failed',
        },
      ],
    }));

    const report = await runFullstackBootstrap({
      repoRoot: 'C:\\repo',
      apiBaseUrl: 'http://127.0.0.1:5055',
      fullstackDevAuth: { enabled: true },
      start: false,
      checkDocker: vi.fn(async () => true),
      runCommand,
      preflight,
    });

    expect(report.status).toBe('blocked');
    expect(runCommand).not.toHaveBeenCalled();
    expect(report.steps.map((step) => [step.id, step.status])).toEqual([
      ['docker', 'passed'],
      ['start-local-staging', 'skipped'],
      ['fullstack-preflight', 'failed'],
    ]);
    expect(report.commands.startLocalStaging).toContain('start-local-staging.ps1');
  });
});
