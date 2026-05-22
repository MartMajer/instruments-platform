import { describe, expect, it } from 'vitest';

import { buildLocalViteCommand, resolveLocalViteServerOptions } from './local-web-server';

describe('local UX harness web server command', () => {
	it('builds a direct Vite command without relying on package manager shims', () => {
		const command = buildLocalViteCommand({
			host: '127.0.0.1',
			nodePath: 'C:/Program Files/nodejs/node.exe',
			port: 5176,
			webRoot: 'C:/repo/apps/web'
		});

		expect(command.command).toBe('C:/Program Files/nodejs/node.exe');
		expect(command.args.slice(1)).toEqual(['--host', '127.0.0.1', '--port', '5176']);
		expect(command.args[0].replace(/\\/g, '/')).toBe(
			'C:/repo/apps/web/node_modules/vite/bin/vite.js'
		);
		expect(command.baseUrl).toBe('http://127.0.0.1:5176');
	});

	it('keeps autonomous browser capture on loopback by default', () => {
		const options = resolveLocalViteServerOptions({ port: 5180 });

		expect(options.host).toBe('127.0.0.1');
		expect(options.port).toBe(5180);
		expect(options.baseUrl).toBe('http://127.0.0.1:5180');
	});
});
