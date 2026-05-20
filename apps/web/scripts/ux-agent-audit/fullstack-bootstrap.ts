import { spawn } from 'node:child_process';
import { join } from 'node:path';

import {
  checkFullstackPreflight,
  type FullstackPreflightOptions,
  type FullstackPreflightReport,
} from './fullstack-preflight.ts';
import type { AutonomousFullstackDevAuthOptions } from './types.ts';

export type FullstackBootstrapStatus = 'ready' | 'blocked';
export type FullstackBootstrapStepStatus = 'passed' | 'failed' | 'skipped';

export interface FullstackBootstrapOptions {
  repoRoot: string;
  apiBaseUrl: string;
  fullstackDevAuth: AutonomousFullstackDevAuthOptions;
  start: boolean;
  timeoutMs?: number;
  checkDocker?: () => Promise<boolean>;
  runCommand?: (command: FullstackBootstrapCommand) => Promise<FullstackBootstrapCommandResult>;
  preflight?: (options: FullstackPreflightOptions) => Promise<FullstackPreflightReport>;
}

export interface FullstackBootstrapCommand {
  filePath: string;
  args: string[];
  cwd?: string;
  timeoutMs?: number;
}

export interface FullstackBootstrapCommandResult {
  exitCode: number;
  output: string;
}

export interface FullstackBootstrapStep {
  id: 'docker' | 'start-local-staging' | 'fullstack-preflight';
  label: string;
  status: FullstackBootstrapStepStatus;
  detail: string;
  guidance?: string;
}

export interface FullstackBootstrapReport {
  status: FullstackBootstrapStatus;
  apiBaseUrl: string;
  repoRoot: string;
  steps: FullstackBootstrapStep[];
  commands: FullstackBootstrapCommands;
  preflight?: FullstackPreflightReport;
}

export interface FullstackBootstrapCommands {
  startLocalStaging: string;
  smokeLocalStaging: string;
  preflight: string;
  runMutation: string;
}

const defaultTimeoutMs = 120_000;

export async function runFullstackBootstrap(
  options: FullstackBootstrapOptions
): Promise<FullstackBootstrapReport> {
  const repoRoot = options.repoRoot;
  const timeoutMs = options.timeoutMs ?? defaultTimeoutMs;
  const runCommand = options.runCommand ?? runSystemCommand;
  const checkDocker =
    options.checkDocker ?? (() => checkDockerAvailable(runCommand, repoRoot, timeoutMs));
  const preflight = options.preflight ?? checkFullstackPreflight;
  const commands = buildFullstackBootstrapCommands(repoRoot, options.apiBaseUrl);
  const steps: FullstackBootstrapStep[] = [];

  if (!(await checkDocker())) {
    steps.push({
      id: 'docker',
      label: 'Docker availability',
      status: 'failed',
      detail: 'Docker command is unavailable or Docker Desktop is not running.',
      guidance:
        'Start Docker Desktop, then run the local staging bootstrap before UXA02 full-stack mutation.',
    });

    return {
      status: 'blocked',
      apiBaseUrl: options.apiBaseUrl,
      repoRoot,
      steps,
      commands,
    };
  }

  steps.push({
    id: 'docker',
    label: 'Docker availability',
    status: 'passed',
    detail: 'Docker command is available.',
  });

  if (options.start) {
    const startScript = join(repoRoot, 'deploy', 'staging', 'start-local-staging.ps1');
    const result = await runCommand({
      filePath: 'powershell',
      args: ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', startScript],
      cwd: repoRoot,
      timeoutMs,
    });

    if (result.exitCode !== 0) {
      steps.push({
        id: 'start-local-staging',
        label: 'Start local staging',
        status: 'failed',
        detail: result.output || `Process exited with code ${result.exitCode}.`,
        guidance:
          'Fix the local staging bootstrap failure, then rerun UXA02 full-stack bootstrap.',
      });

      return {
        status: 'blocked',
        apiBaseUrl: options.apiBaseUrl,
        repoRoot,
        steps,
        commands,
      };
    }

    steps.push({
      id: 'start-local-staging',
      label: 'Start local staging',
      status: 'passed',
      detail: result.output || 'Local staging bootstrap command completed.',
    });
  } else {
    steps.push({
      id: 'start-local-staging',
      label: 'Start local staging',
      status: 'skipped',
      detail: 'Skipped because --start was not provided.',
      guidance: `Run ${commands.startLocalStaging} or rerun this command with --start.`,
    });
  }

  const preflightReport = await preflight({
    apiBaseUrl: options.apiBaseUrl,
    fullstackDevAuth: options.fullstackDevAuth,
    timeoutMs,
  });
  steps.push({
    id: 'fullstack-preflight',
    label: 'Full-stack preflight',
    status: preflightReport.status === 'ready' ? 'passed' : 'failed',
    detail: `Preflight status: ${preflightReport.status}.`,
    ...(preflightReport.status === 'ready'
      ? {}
      : {
          guidance:
            'Use the failed preflight check guidance before running fullstack-create-study.',
        }),
  });

  return {
    status: preflightReport.status === 'ready' ? 'ready' : 'blocked',
    apiBaseUrl: options.apiBaseUrl,
    repoRoot,
    steps,
    commands,
    preflight: preflightReport,
  };
}

export function buildFullstackBootstrapCommands(repoRoot: string, apiBaseUrl: string) {
  const startScript = join(repoRoot, 'deploy', 'staging', 'start-local-staging.ps1');
  const smokeScript = join(repoRoot, 'deploy', 'staging', 'smoke-local-staging.ps1');

  return {
    startLocalStaging: `powershell -NoProfile -ExecutionPolicy Bypass -File ${quote(startScript)}`,
    smokeLocalStaging: `powershell -NoProfile -ExecutionPolicy Bypass -File ${quote(smokeScript)}`,
    preflight: `cd ${quote(join(repoRoot, 'apps', 'web'))}; node --experimental-strip-types scripts/ux-agent-audit/run.ts fullstack-preflight --api-base-url ${apiBaseUrl} --fullstack-dev-auth`,
    runMutation: `cd ${quote(join(repoRoot, 'apps', 'web'))}; node --experimental-strip-types scripts/ux-agent-audit/run.ts autonomous --base-url http://127.0.0.1:5174 --mission fullstack-create-study --data-mode fullstack --fullstack-dev-auth --output ../../artifacts/ux-agent-runs/local`,
  } satisfies FullstackBootstrapCommands;
}

async function checkDockerAvailable(
  runCommand: (command: FullstackBootstrapCommand) => Promise<FullstackBootstrapCommandResult>,
  cwd: string,
  timeoutMs: number
) {
  const result = await runCommand({
    filePath: 'docker',
    args: ['info', '--format', '{{.ServerVersion}}'],
    cwd,
    timeoutMs,
  });

  return result.exitCode === 0;
}

function runSystemCommand(command: FullstackBootstrapCommand) {
  return new Promise<FullstackBootstrapCommandResult>((resolve) => {
    const child = spawn(command.filePath, command.args, {
      cwd: command.cwd,
      shell: false,
      windowsHide: true,
    });
    let output = '';
    const timer = command.timeoutMs
      ? setTimeout(() => {
          output += `Timed out after ${command.timeoutMs}ms.`;
          child.kill();
        }, command.timeoutMs)
      : undefined;

    child.stdout.on('data', (chunk) => {
      output += chunk.toString();
    });
    child.stderr.on('data', (chunk) => {
      output += chunk.toString();
    });
    child.on('error', (error) => {
      if (timer) {
        clearTimeout(timer);
      }
      resolve({ exitCode: 1, output: error.message });
    });
    child.on('close', (code) => {
      if (timer) {
        clearTimeout(timer);
      }
      resolve({ exitCode: code ?? 1, output: output.trim() });
    });
  });
}

function quote(value: string) {
  return value.includes(' ') ? `"${value.replace(/"/g, '\\"')}"` : value;
}
