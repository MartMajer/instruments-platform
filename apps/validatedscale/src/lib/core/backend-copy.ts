/**
 * Localized recomposition of backend-composed sentences.
 *
 * The backend emits stable ids/status tokens plus the values a sentence was
 * composed from (`params`); English `title`/`description` stay in the contract
 * as the default-locale text and the fallback for unknown ids. In hr the
 * sentence is recomposed here, with proper 1 / 2–4 / 5+ agreement.
 */
import { localeState, hrPlural, t } from './locale.svelte';
import type { WorkspaceCommandCenterItemResponse } from '$lib/api/product';

function kindOf(id: string): string {
	if (id === 'campaign_series.create') return 'create';
	if (id === 'directory.setup') return 'directory';
	if (id === 'team.pending_provider_links') return 'team';
	if (id === 'workspace.review') return 'review';
	const seriesKind = /^series\.[0-9a-f]{32}\.(\w+)$/.exec(id);
	return seriesKind ? seriesKind[1] : '';
}

/**
 * Localized title + description for a Today command-center item.
 *
 * Recomposed in both locales: the backend's own sentences speak backend
 * vocabulary (campaign series, Operations, Reports) while the product speaks
 * study/wave/Field/Evidence. Unknown item kinds fall back to the backend text.
 */
export function commandCopy(item: WorkspaceCommandCenterItemResponse): {
	title: string;
	description: string;
	action: string;
} {
	const hr = localeState.current === 'hr';
	const p = item.params ?? {};
	const name = p.name ?? '';
	const count = Number(p.count ?? '0');
	const one = count === 1;

	const actions: Record<string, [en: string, hr: string]> = {
		create: ['Open Studies', 'Otvori Studije'],
		directory: ['Open People', 'Otvori Ljude'],
		setup: ['Open the protocol', 'Otvori protokol'],
		operations: ['Open Field', 'Otvori Prikupljanje'],
		reports: ['Open Evidence', 'Otvori Nalaze'],
		score_remediation: ['Open Evidence', 'Otvori Nalaze'],
		exports: ['Open exports', 'Otvori izvoze'],
		waves: ['Open waves', 'Otvori krugove'],
		team: ['Open Team', 'Otvori Tim'],
		review: ['Open Studies', 'Otvori Studije']
	};
	const kind = kindOf(item.id);
	const action = actions[kind] ? actions[kind][hr ? 1 : 0] : t(item.actionLabel);
	const text = commandText(item, kind, hr, name, count, one, p);

	return { ...text, action };
}

function commandText(
	item: WorkspaceCommandCenterItemResponse,
	kind: string,
	hr: boolean,
	name: string,
	count: number,
	one: boolean,
	p: Record<string, string>
): { title: string; description: string } {
	switch (kind) {
		case 'create':
			// The backend offers the create verb only to setup managers.
			if (item.requiredPermission) {
				return hr
					? {
							title: 'Stvorite prvu studiju',
							description: 'Za početak stvorite studiju u ovom radnom prostoru.'
						}
					: {
							title: 'Create your first study',
							description: 'Start by registering a study in this workspace.'
						};
			}
			return hr
				? {
						title: 'Još nema studija',
						description: 'Ovaj radni prostor još nema aktivnih studija.'
					}
				: {
						title: 'No studies yet',
						description: 'This workspace has no active studies yet.'
					};
		case 'directory': {
			const subjects = Number(p.subjects ?? '0');
			const groups = Number(p.groups ?? '0');
			return hr
				? {
						title: 'Postavite Ljude',
						description: `Imenik ima ${subjects} ${hrPlural(subjects, 'osobu', 'osobe', 'osoba')} i ${groups} ${hrPlural(groups, 'grupu', 'grupe', 'grupa')}. Dodajte popis prije nego što pravila sudjelovanja počnu ovisiti o njemu.`
					}
				: {
						title: 'Set up People',
						description: `People holds ${subjects} ${subjects === 1 ? 'person' : 'people'} and ${groups} ${groups === 1 ? 'group' : 'groups'}. Add your roster before participation rules depend on it.`
					};
		}
		case 'setup':
			return hr
				? {
						title: `Dovršite postavljanje: ${name}`,
						description: 'Ova studija još nema konfiguriran krug.'
					}
				: {
						title: `Finish setting up: ${name}`,
						description: 'This study has no wave configured yet.'
					};
		case 'operations':
			return hr
				? {
						title: `Pratite prikupljanje: ${name}`,
						description: `${count} ${hrPlural(count, 'aktivni krug', 'aktivna kruga', 'aktivnih krugova')} možete pratiti u Prikupljanju.`
					}
				: {
						title: `Watch the field: ${name}`,
						description: `${count} live ${one ? 'wave is' : 'waves are'} collecting — watch in Field.`
					};
		case 'reports':
			return hr
				? {
						title: `Pregledajte nalaze: ${name}`,
						description: `${count} ${hrPlural(count, 'predani odgovor čeka', 'predana odgovora čekaju', 'predanih odgovora čeka')} pregled u Nalazima.`
					}
				: {
						title: `Review evidence: ${name}`,
						description: `${count} submitted ${one ? 'response is' : 'responses are'} ready in Evidence.`
					};
		case 'score_remediation':
			return hr
				? {
						title: `Dovršite bodovanje: ${name}`,
						description: `${count} ${hrPlural(count, 'predani odgovor još nema', 'predana odgovora još nemaju', 'predanih odgovora još nema')} uspješno bodovanje.`
					}
				: {
						title: `Finish scoring: ${name}`,
						description: `${count} submitted ${one ? 'response has' : 'responses have'} no successful score run yet.`
					};
		case 'exports':
			return hr
				? {
						title: `Pregledajte izvoze: ${name}`,
						description: `${count} ${hrPlural(count, 'izvoz čeka', 'izvoza čekaju', 'izvoza čeka')} u Nalazima.`
					}
				: {
						title: `Review exports: ${name}`,
						description: `${count} ${one ? 'export is' : 'exports are'} waiting in Evidence.`
					};
		case 'waves':
			return hr
				? {
						title: `Usporedite krugove: ${name}`,
						description:
							'Ova studija ima najmanje dva anonimna longitudinalna kruga spremna za usporedbu.'
					}
				: {
						title: `Compare waves: ${name}`,
						description:
							'This study has at least two anonymous longitudinal waves ready to compare.'
					};
		case 'team':
			return hr
				? {
						title: 'Pregledajte pristup tima',
						description: `${count} ${hrPlural(count, 'član tima još čeka', 'člana tima još čekaju', 'članova tima još čeka')} poveznicu s prijavom.`
					}
				: {
						title: 'Review pending team access',
						description: `${count} team ${one ? 'member still needs' : 'members still need'} an identity-provider link.`
					};
		case 'review': {
			const series = Number(p.series ?? '0');
			const responses = Number(p.responses ?? '0');
			return hr
				? {
						title: 'Pregledajte studije',
						description: `Radni prostor ima ${series} ${hrPlural(series, 'studiju', 'studije', 'studija')} i ${responses} ${hrPlural(responses, 'predani odgovor', 'predana odgovora', 'predanih odgovora')}.`
					}
				: {
						title: 'Review your studies',
						description: `The workspace holds ${series} ${series === 1 ? 'study' : 'studies'} and ${responses} submitted ${responses === 1 ? 'response' : 'responses'}.`
					};
		}
		default:
			// Unknown item kind: show the backend text rather than guessing.
			return { title: item.title, description: item.description };
	}
}

/** Localized field-collection guidance, composed from the status tokens next to it. */
export function collectionGuidanceCopy(
	collectionStatus: string,
	reportVisibilityStatus: string,
	fallback: string
): string {
	if (localeState.current !== 'hr') {
		return fallback;
	}

	if (collectionStatus === 'closed_or_inactive') {
		return 'Prikupljanje je zatvoreno ili neaktivno; već predani podaci ostaju prikazivi kad pravila objavljivanja to dopuštaju.';
	}

	switch (reportVisibilityStatus) {
		case 'unknown_policy':
			return 'Spremnost prikaza nije poznata jer nedostaju pravila objavljivanja.';
		case 'ready_for_aggregate_report':
			return 'Predanih odgovora ima dovoljno za skupni prikaz nalaza.';
		case 'below_disclosure_minimum':
			return 'Prikupite još odgovora prije nego što se skupne vrijednosti mogu prikazati.';
		case 'not_ready':
			if (collectionStatus === 'collecting') {
				return 'Odgovaranje je počelo, ali nijedan odgovor još nije predan.';
			}
			return 'Podijelite javnu poveznicu ili pošaljite pozivnice.';
		default:
			return 'Podijelite javnu poveznicu ili pošaljite pozivnice.';
	}
}

/** Localized Evidence insight notes, recomposed from kind + severity + count. */
export function insightCopy(insight: {
	kind: string;
	severity: string;
	title: string;
	detail: string;
	count?: number | null;
}): { title: string; detail: string } {
	if (localeState.current !== 'hr') {
		return { title: insight.title, detail: insight.detail };
	}

	const n = insight.count ?? 0;
	switch (`${insight.kind}/${insight.severity}`) {
		case 'score_outputs/blocked':
			return {
				title: 'Još nema izračunatih rezultata',
				detail: 'Pokrenite bodovanje predanih odgovora prije tumačenja rezultata.'
			};
		case 'score_outputs/pending':
			return {
				title: 'Rezultati su skriveni pravilima objavljivanja',
				detail:
					'Predani odgovori možda postoje, ali skupne vrijednosti ostaju skrivene dok se ne ispune uvjeti objavljivanja i bodovanja.'
			};
		case 'score_outputs/ready':
			return {
				title: `${n} ${hrPlural(n, 'vidljivi rezultat spreman', 'vidljiva rezultata spremna', 'vidljivih rezultata spremno')}`,
				detail:
					'Prije dijeljenja zaključaka pregledajte prosjek, medijan, raspršenje, raspon i pokrivenost odgovora.'
			};
		case 'groups/pending':
			// The k-suppression variant carries the disclosure minimum as count.
			return insight.count != null
				? {
						title: 'Neke su grupe skrivene',
						detail: `Redci ispod praga objavljivanja od ${n} odgovora su skriveni.`
					}
				: {
						title: 'Još nema usporedbe grupa',
						detail:
							'Usporedbe grupa zahtijevaju da primatelji pripadaju grupama imenika u odabranom krugu.'
					};
		case 'groups/ready':
			return {
				title: `${n} ${hrPlural(n, 'usporedba grupa spremna', 'usporedbe grupa spremne', 'usporedbi grupa spremno')}`,
				detail:
					'Grupne retke koristite samo kao skupne usporedbe; nikad za prepoznavanje ispitanika.'
			};
		case 'waves/ready':
			return {
				title: `${n} ${hrPlural(n, 'mjerenje se može usporediti', 'mjerenja se mogu usporediti', 'mjerenja se može usporediti')}`,
				detail:
					'Retke krugova koristite za praćenje promjena kroz vrijeme. Mjerenja u tijeku smatrajte preliminarnima.'
			};
		case 'waves/pending':
			return {
				title: 'Usporedba krugova još nije moguća',
				detail:
					'Najmanje dva mjerenja moraju imati vidljive rezultate da bi usporedba kroz vrijeme imala smisla.'
			};
		default:
			return { title: insight.title, detail: insight.detail };
	}
}

/**
 * Prerequisite messages in product vocabulary (the backend speaks
 * "campaign"/"template"), with the protocol chapter to jump to.
 */
export function prerequisiteCopy(
	code: string,
	message: string
): { text: string; anchor: string | null } {
	switch (code) {
		case 'campaign.missing':
			return {
				text: t('This study has no wave yet. Add one in chapter 05 — Waves.'),
				anchor: '#waves'
			};
		case 'template.missing':
			return {
				text: t('No instrument is attached yet. Compose or pick one in chapter 02 — Instrument.'),
				anchor: '#instrument'
			};
		case 'scoring_rule.missing':
			return {
				text: t('No scoring rule is bound. It is created with the questionnaire in chapter 02.'),
				anchor: '#instrument'
			};
		case 'consent_document.missing':
			return {
				text: t('No consent text is set. Add it in chapter 04 — Consent & guarantees.'),
				anchor: '#policies'
			};
		case 'retention_policy.missing':
			return {
				text: t('The retention guarantee is missing. Review chapter 04 — Consent & guarantees.'),
				anchor: '#policies'
			};
		case 'disclosure_policy.missing':
			return {
				text: t('The disclosure guarantee is missing. Review chapter 04 — Consent & guarantees.'),
				anchor: '#policies'
			};
		case 'launchable_campaign.missing':
			return {
				text: t('No wave is ready to launch. Add the next wave in the protocol.'),
				anchor: '#waves'
			};
		case 'live_campaign.missing':
			return { text: t('No wave is collecting right now. Launch one from the protocol.'), anchor: null };
		case 'public_entry.missing':
			return {
				text: t('No open link exists yet. Create one on a live wave to collect anonymously.'),
				anchor: null
			};
		case 'invitations.missing':
			return {
				text: t('No invitations have been sent yet. Invite respondents by email on a live wave.'),
				anchor: null
			};
		default: {
			// Unknown code: fall back to the backend sentence, translated when known.
			const lowered = `${code} ${message}`.toLowerCase();
			if (lowered.includes('campaign')) {
				return {
					text: t('This study has no wave yet. Add one in chapter 05 — Waves.'),
					anchor: '#waves'
				};
			}
			return { text: t(message), anchor: null };
		}
	}
}
