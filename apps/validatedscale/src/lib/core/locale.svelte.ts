/**
 * Researcher-app locale (en / hr). The respondent flow has its own per-study
 * locale handling in $lib/respondent/copy.ts — this store is for the signed-in app.
 */
const STORAGE_KEY = 'validatedscale.app-locale';

export type AppLocale = 'en' | 'hr';

export const localeState = $state<{ current: AppLocale }>({ current: 'en' });

export function initLocale() {
	const stored = globalThis.localStorage?.getItem(STORAGE_KEY);
	if (stored === 'hr' || stored === 'en') {
		localeState.current = stored;
	}
}

export function setLocale(locale: AppLocale) {
	localeState.current = locale;
	globalThis.localStorage?.setItem(STORAGE_KEY, locale);
}

/** t('Studies') — dictionary lookup with English fallback. */
export function t(english: string): string {
	if (localeState.current === 'en') return english;
	return HR[english] ?? english;
}

/** English source string → Croatian. Keep alphabetized by English. */
const HR: Record<string, string> = {
	'Add example studies': 'Dodaj primjere studija',
	'Add person': 'Dodaj osobu',
	'Add wave': 'Dodaj val',
	Account: 'Račun',
	'Archive study': 'Arhiviraj studiju',
	Cancel: 'Odustani',
	'Close wave': 'Zatvori val',
	Collected: 'Prikupljeno',
	Collecting: 'Prikupljanje u tijeku',
	Confirm: 'Potvrdi',
	'Create open link': 'Stvori otvorenu poveznicu',
	'Create study': 'Stvori studiju',
	'Design': 'Nacrt',
	'Duplicate study': 'Dupliciraj studiju',
	Evidence: 'Nalazi',
	Exports: 'Izvozi',
	Field: 'Teren',
	'Find a study': 'Pronađi studiju',
	'In preparation': 'U pripremi',
	'In the field': 'Na terenu',
	Instrument: 'Instrument',
	Instruments: 'Instrumenti',
	'Invite by email': 'Pozovi e-poštom',
	Launch: 'Pokreni',
	'New study': 'Nova studija',
	People: 'Ljudi',
	Policies: 'Pravila',
	Protocol: 'Protokol',
	'Publish new consent version': 'Objavi novu verziju privole',
	Recipients: 'Primatelji',
	'Register study': 'Registriraj studiju',
	Scoring: 'Bodovanje',
	Settings: 'Postavke',
	'Sign out': 'Odjava',
	Studies: 'Studije',
	Today: 'Danas',
	Waves: 'Valovi',
	'Workspace settings': 'Postavke radnog prostora'
};
