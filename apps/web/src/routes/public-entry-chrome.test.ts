import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const homePage = readFileSync(new URL('./+page.svelte', import.meta.url), 'utf8');
const signInPage = readFileSync(new URL('./signin/+page.svelte', import.meta.url), 'utf8');
const registerPage = readFileSync(new URL('./register/+page.svelte', import.meta.url), 'utf8');
const appShell = readFileSync(new URL('../lib/components/AppShell.svelte', import.meta.url), 'utf8');
const surfaceNav = readFileSync(new URL('../lib/components/SurfaceNav.svelte', import.meta.url), 'utf8');
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
		expect(appCss).toMatch(/\.public-nav__actions[\s\S]*flex-wrap:\s*nowrap/);
		expect(appCss).toMatch(/\.public-language-switcher[\s\S]*white-space:\s*nowrap/);
		expect(appCss).toMatch(/\.launchpad-brand strong,[\s\S]*white-space:\s*nowrap/);
		expect(appCss).toMatch(/\.launchpad-button[\s\S]*white-space:\s*nowrap/);
	});

	it('keeps hardcoded homepage preview text out of internal product jargon', () => {
		expect(homePage).not.toMatch(/tenant profile|private beta|Private response|owner-controlled/i);
	});

	it('uses Validated Scale branding in the logged-in app shell', () => {
		expect(appShell).toContain('Validated Scale');
		expect(appShell).toContain('/brand/validated-scale-mark.svg');
		expect(appShell).not.toContain('Instruments Platform');
		expect(appShell).not.toContain('>IP<');
	});

	it('keeps active study navigation stable before a study is selected', () => {
		expect(surfaceNav).toContain("id: 'active-study'");
		expect(surfaceNav).toContain('isDisabled: !activeSeriesId');
		expect(surfaceNav).toContain('aria-disabled={surface.isDisabled ?');
		expect(surfaceNav).toContain('copy.descriptions.selectStudyFirst');
		expect(surfaceNav).toContain('{#each section.surfaces as surface (surface.id)}');
		expect(surfaceNav).not.toContain('{#each section.surfaces as surface (surface.href)}');
		expect(surfaceNav).toContain('...utilitySections');
		expect(surfaceNav).not.toContain(': null');
	});

	it('keeps mobile study navigation contextual and non-duplicative', () => {
		expect(appShell).toContain('{#if isProductShell && activeSeriesId}');
		expect(appShell).toContain('aria-label={surfaceCopy.sections.selectedStudy}');
		expect(appShell).toContain("id: 'overview'");
		expect(appShell).toContain("id: 'setup'");
		expect(appShell).toContain("id: 'collect'");
		expect(appShell).toContain("id: 'results'");
		expect(appShell).toContain("id: 'waves'");
		expect(appShell).not.toContain('class="app-mobile-bottom-nav__link"\n\t\t\t\t\t\taria-current={mobileMenuOpen');
		expect(appCss).toMatch(/\.app-mobile-bottom-nav,[\s\S]*display:\s*none/);
		expect(appCss).toMatch(
			/@media[\s\S]*\.app-mobile-bottom-nav\s*\{[\s\S]*display:\s*grid;[\s\S]*grid-template-columns:\s*repeat\(5, minmax\(0, 1fr\)\)/
		);
		expect(appCss).not.toContain(".app-mobile-bottom-nav {\n\tdisplay: grid;\n\tgrid-template-columns");
	});

	it('offers locale switching without a visible mobile menu text button', () => {
		expect(homePage).toContain("import { appLocaleFromPageData, localizedHref }");
		expect(homePage).toContain("localizedHref(page.url, 'en')");
		expect(homePage).toContain("localizedHref(page.url, 'hr-HR')");
		expect(homePage).toContain('class="public-language-switcher"');
		expect(homePage).toContain('class="public-nav__menu-icon"');
		expect(homePage).not.toMatch(/<button[\s\S]*>\s*\{text\.publicEntry\.menu\}\s*<\/button>/);
	});
});
