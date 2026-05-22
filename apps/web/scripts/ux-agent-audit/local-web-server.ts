import { spawn, type ChildProcess } from 'node:child_process';
import { existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { join, resolve } from 'node:path';

export type LocalViteServerOptions = {
	host?: string;
	nodePath?: string;
	port?: number;
	webRoot?: string;
};

export type ResolvedLocalViteServerOptions = Required<LocalViteServerOptions> & {
	baseUrl: string;
};

export type LocalViteCommand = {
	command: string;
	args: string[];
	baseUrl: string;
	cwd: string;
};

export function resolveLocalViteServerOptions(
	options: LocalViteServerOptions = {}
): ResolvedLocalViteServerOptions {
	const host = options.host ?? '127.0.0.1';
	const port = options.port ?? 5176;
	const webRoot = resolve(options.webRoot ?? process.cwd());
	const nodePath = options.nodePath ?? process.execPath;

	return {
		host,
		nodePath,
		port,
		webRoot,
		baseUrl: `http://${host}:${port}`
	};
}

export function buildLocalViteCommand(options: LocalViteServerOptions = {}): LocalViteCommand {
	const resolved = resolveLocalViteServerOptions(options);
	const viteEntry = join(resolved.webRoot, 'node_modules', 'vite', 'bin', 'vite.js');

	return {
		command: resolved.nodePath,
		args: [viteEntry, '--host', resolved.host, '--port', String(resolved.port)],
		baseUrl: resolved.baseUrl,
		cwd: resolved.webRoot
	};
}

export async function waitForLocalVite(baseUrl: string, timeoutMs = 60_000): Promise<void> {
	const startedAt = Date.now();

	while (Date.now() - startedAt < timeoutMs) {
		try {
			const response = await fetch(baseUrl, { method: 'GET' });
			if (response.ok || response.status < 500) {
				return;
			}
		} catch {
			await new Promise((resolveWait) => setTimeout(resolveWait, 500));
		}
	}

	throw new Error(`Timed out waiting for local Vite server at ${baseUrl}.`);
}

export async function startLocalViteServer(
	options: LocalViteServerOptions = {}
): Promise<{ child: ChildProcess; baseUrl: string }> {
	const command = buildLocalViteCommand(options);
	const viteEntry = command.args[0];

	if (!existsSync(viteEntry)) {
		throw new Error(
			`Vite entry not found at ${viteEntry}. Run npm install in apps/web before starting the UX harness server.`
		);
	}

	const child = spawn(command.command, command.args, {
		cwd: command.cwd,
		env: process.env,
		stdio: 'inherit'
	});

	await waitForLocalVite(command.baseUrl);
	return { child, baseUrl: command.baseUrl };
}

function parseCliOptions(argv: string[]): LocalViteServerOptions {
	const options: LocalViteServerOptions = {};

	for (let index = 0; index < argv.length; index += 1) {
		const arg = argv[index];
		const next = argv[index + 1];

		if (arg === '--host' && next) {
			options.host = next;
			index += 1;
		} else if (arg === '--port' && next) {
			options.port = Number.parseInt(next, 10);
			index += 1;
		} else if (arg === '--web-root' && next) {
			options.webRoot = next;
			index += 1;
		}
	}

	return options;
}

async function main() {
	const command = buildLocalViteCommand(parseCliOptions(process.argv.slice(2)));
	console.log(`Starting UX harness web server at ${command.baseUrl}`);
	console.log(`${command.command} ${command.args.join(' ')}`);

	await startLocalViteServer(parseCliOptions(process.argv.slice(2)));
}

const currentModulePath = fileURLToPath(import.meta.url);
if (process.argv[1] && resolve(process.argv[1]) === currentModulePath) {
	main().catch((error: unknown) => {
		console.error(error instanceof Error ? error.message : error);
		process.exitCode = 1;
	});
}
