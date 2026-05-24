import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const homePage = readFileSync(new URL('./+page.svelte', import.meta.url), 'utf8');
const signInPage = readFileSync(new URL('./signin/+page.svelte', import.meta.url), 'utf8');
const registerPage = readFileSync(new URL('./register/+page.svelte', import.meta.url), 'utf8');
const appCss = readFileSync(new URL('./app.css', import.meta.url), 'utf8');

describe('public entry chrome', () => {
	it('uses the same public navigation structure across marketing and auth entry pages', () => {
		expect(homePage).toContain('class="public-nav public-nav--home"');
		expect(signInPage).toContain('class="public-nav"');
		expect(registerPage).toContain('class="public-nav"');
		expect(signInPage).not.toContain('registration-nav');
		expect(registerPage).not.toContain('registration-nav');
	});

	it('keeps compact public navigation labels from wrapping awkwardly', () => {
		expect(appCss).toMatch(/\.public-nav__links a[\s\S]*white-space:\s*nowrap/);
		expect(appCss).toMatch(/\.launchpad-brand strong,[\s\S]*white-space:\s*nowrap/);
		expect(appCss).toMatch(/\.launchpad-button[\s\S]*white-space:\s*nowrap/);
	});

	it('keeps hardcoded homepage preview text out of internal product jargon', () => {
		expect(homePage).not.toMatch(/tenant profile|private beta|Private response|owner-controlled/i);
	});
});
