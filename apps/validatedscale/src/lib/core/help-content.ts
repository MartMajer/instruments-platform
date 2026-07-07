import { localeState } from './locale.svelte';

/**
 * Long-form in-app help. Content lives here (not in the t() dictionary) so a
 * native-speaker pass can review whole sections in context; the established
 * register holds: Prikupljanje for Field, krug for wave, Nalazi for Evidence.
 * Claims stay inside the boundary: instruments are tenant-provided or from
 * the rights-cleared free gallery; nothing is "platform-canonical".
 */

export type HelpSection = {
	id: string;
	title: string;
	paragraphs: string[];
};

export type HelpTerm = { term: string; definition: string };

export type HelpContent = {
	intro: string;
	sections: HelpSection[];
	glossaryTitle: string;
	glossary: HelpTerm[];
};

const en: HelpContent = {
	intro:
		'ValidatedScale runs questionnaire studies the way a methods section describes them: versioned instruments, versioned consent, waves that freeze their setup at launch, and results that never show groups smaller than five.',
	sections: [
		{
			id: 'study',
			title: 'How a study runs',
			paragraphs: [
				'A study starts as a protocol: a questionnaire (its instrument), a scoring rule, a consent document, and policies for retention and disclosure. You can edit all of it while the study is a draft.',
				'Data collection happens in waves. When you launch a wave, it takes a snapshot of the questionnaire version, scoring rule, consent version and policies at that moment — and keeps it. Publishing a revised questionnaire or a new consent version later never changes what an already-launched wave shows respondents or how it scores them. The next wave picks up the new versions.',
				'Published questionnaire versions are immutable. A revision always becomes a new version (v1.0.0 → v1.1.0), so every response can forever say exactly which items it answered.'
			]
		},
		{
			id: 'collect',
			title: 'Collecting responses',
			paragraphs: [
				'Anonymous waves collect through an open link: anyone with the link can respond once per browser, and no identity is stored. This is the default for climate-style measurements.',
				'Identified waves know who answers. Respondents from People receive personal links — by email invitation, or minted on the Field page for target-aware setups like manager reviews, where one person answers a queue of questionnaires about each of their reports.',
				'Anonymous-linked studies use participant codes: respondents stay anonymous, but the same self-chosen code across waves lets change over time be measured without storing identity.',
				'Losing a link is not a problem: open links can be replaced, and replacing one retires the old link.'
			]
		},
		{
			id: 'evidence',
			title: 'Reading Evidence',
			paragraphs: [
				'Nothing is reported for groups smaller than five people. This disclosure floor (k = 5) is fixed — it cannot be lowered, configured, or bypassed, and it applies to every table, chart, comparison and export. Below the floor you see that results exist and are suppressed, never the values.',
				'Evidence shows descriptives per scored dimension — n, mean, median, spread, range — plus group and wave comparisons once enough visible data exists. The methods & provenance panel states exactly which instrument version, scoring rule, consent version and policies each wave ran with, ready to cite.',
				'Exports carry the same discipline: the responses CSV comes with a codebook that documents every column, and report PDFs state their provenance. What is suppressed on screen is suppressed in the export.'
			]
		},
		{
			id: 'rights',
			title: 'Respondent rights',
			paragraphs: [
				'Consent is versioned like everything else. Respondents see the consent version their wave launched with, and their acceptance is recorded against that exact version.',
				'Withdrawal requests — "delete my data" or "anonymize my data" — are recorded, reviewed, and executed with an audit trail under Privacy & data requests in the account menu. Requests received by email or in person can be recorded there too.',
				'The do-not-contact list is honored by every invitation send. Bounced addresses land on it automatically; addresses can be suppressed or released by hand.'
			]
		},
		{
			id: 'instruments',
			title: 'Instruments and rights',
			paragraphs: [
				'The gallery offers instruments whose item text may be freely reproduced — each entry states its basis and citation, and imports as content your workspace attests to. Rights-restricted instruments are listed with guidance only; their items are never shipped.',
				'Your own instruments — a validated scale from a paper, a questionnaire you authored — are yours to bring. The composer handles items, response scales, reverse-coded items and the scoring rule; you attest that your workspace holds the rights to the text it provides.'
			]
		}
	],
	glossaryTitle: 'Glossary',
	glossary: [
		{ term: 'Study', definition: 'One research protocol and everything collected under it. Lives in Studies.' },
		{ term: 'Wave', definition: 'One round of data collection with a launch-time snapshot of the whole setup.' },
		{ term: 'Field', definition: 'The collection console for a study: links, invitations, response counts, wave status.' },
		{ term: 'Evidence', definition: 'The results surface: descriptives, comparisons, provenance, exports.' },
		{ term: 'Open link', definition: 'An anonymous response link anyone can use once per browser.' },
		{ term: 'Personal link', definition: 'A single-person link for identified waves; shown once when minted.' },
		{ term: 'Instrument', definition: 'The questionnaire: items, scales and structure, versioned on publish.' },
		{ term: 'Scoring rule', definition: 'The published rule turning answers into scores; declares what it produces.' },
		{ term: 'k = 5 floor', definition: 'The fixed disclosure threshold: no value is shown for fewer than five people.' },
		{ term: 'Suppression', definition: 'What happens below the floor: the result exists but its values are hidden.' },
		{ term: 'Participant code', definition: 'A respondent-chosen code that links waves without storing identity.' },
		{ term: 'Withdrawal request', definition: 'A respondent’s recorded request to delete or anonymize their data.' }
	]
};

const hr: HelpContent = {
	intro:
		'ValidatedScale provodi upitničke studije onako kako ih opisuje poglavlje o metodama: verzionirani instrumenti, verzionirani pristanak, krugovi koji pri pokretanju zamrzavaju svoju postavu i rezultati koji nikad ne prikazuju skupine manje od pet ljudi.',
	sections: [
		{
			id: 'study',
			title: 'Kako studija teče',
			paragraphs: [
				'Studija počinje kao protokol: upitnik (njezin instrument), pravilo bodovanja, dokument pristanka te politike čuvanja i objave. Sve se može uređivati dok je studija nacrt.',
				'Podaci se prikupljaju u krugovima. Kad pokrenete krug, on u tom trenutku snima verziju upitnika, pravilo bodovanja, verziju pristanka i politike — i zadržava ih. Kasnija objava revidiranog upitnika ili nove verzije pristanka nikad ne mijenja ono što već pokrenuti krug prikazuje ispitanicima niti kako ih boduje. Sljedeći krug preuzima nove verzije.',
				'Objavljene verzije upitnika nepromjenjive su. Revizija uvijek postaje nova verzija (v1.0.0 → v1.1.0), pa svaki odgovor zauvijek može reći na koje je točno čestice odgovarao.'
			]
		},
		{
			id: 'collect',
			title: 'Prikupljanje odgovora',
			paragraphs: [
				'Anonimni krugovi prikupljaju putem otvorene poveznice: svatko s poveznicom može odgovoriti jednom po pregledniku, a identitet se ne pohranjuje. To je zadani način za mjerenja klime.',
				'Identificirani krugovi znaju tko odgovara. Ispitanici iz Ljudi dobivaju osobne poveznice — pozivnicom e-poštom ili izradom na stranici Prikupljanje za postave s ciljnim osobama, poput voditeljskih procjena, gdje jedna osoba odgovara na niz upitnika o svakom svom suradniku.',
				'Anonimno povezane studije koriste kodove sudionika: ispitanici ostaju anonimni, ali isti samoodabrani kod kroz krugove omogućuje mjerenje promjene kroz vrijeme bez pohrane identiteta.',
				'Izgubljena poveznica nije problem: otvorene poveznice mogu se zamijeniti, a zamjena povlači staru poveznicu.'
			]
		},
		{
			id: 'evidence',
			title: 'Čitanje Nalaza',
			paragraphs: [
				'Ništa se ne izvještava za skupine manje od pet ljudi. Taj prag objave (k = 5) fiksan je — ne može se sniziti, konfigurirati ni zaobići, a vrijedi za svaku tablicu, grafikon, usporedbu i izvoz. Ispod praga vidite da rezultati postoje i da su potisnuti, nikad vrijednosti.',
				'Nalazi prikazuju deskriptivne pokazatelje po bodovanoj dimenziji — n, aritmetičku sredinu, medijan, raspršenje, raspon — te usporedbe skupina i krugova čim postoji dovoljno vidljivih podataka. Ploča Metode i podrijetlo navodi točno s kojom je verzijom instrumenta, pravilom bodovanja, verzijom pristanka i politikama svaki krug proveden, spremno za citiranje.',
				'Izvozi nose istu disciplinu: CSV s odgovorima dolazi s kodnom knjigom koja dokumentira svaki stupac, a PDF izvještaji navode svoje podrijetlo. Što je potisnuto na zaslonu, potisnuto je i u izvozu.'
			]
		},
		{
			id: 'rights',
			title: 'Prava ispitanika',
			paragraphs: [
				'Pristanak je verzioniran kao i sve ostalo. Ispitanici vide verziju pristanka s kojom je njihov krug pokrenut, a njihovo prihvaćanje bilježi se uz točno tu verziju.',
				'Zahtjevi za povlačenje — "izbrišite moje podatke" ili "anonimizirajte moje podatke" — bilježe se, pregledavaju i izvršavaju s revizijskim tragom pod Privatnost i zahtjevi ispitanika u izborniku računa. Ondje se mogu zabilježiti i zahtjevi zaprimljeni e-poštom ili osobno.',
				'Popis "ne kontaktirati" poštuje se pri svakom slanju pozivnica. Odbijene adrese dolaze na njega automatski; adrese se mogu blokirati ili odblokirati i ručno.'
			]
		},
		{
			id: 'instruments',
			title: 'Instrumenti i prava',
			paragraphs: [
				'Galerija nudi instrumente čiji se tekst čestica smije slobodno reproducirati — svaki unos navodi svoju osnovu i citat, a uvozi se kao sadržaj za koji jamči vaš radni prostor. Instrumenti s ograničenim pravima navedeni su samo s uputama; njihove čestice nikad se ne isporučuju.',
				'Vaši vlastiti instrumenti — validirana ljestvica iz rada, upitnik koji ste sami sastavili — vaši su i možete ih donijeti. Sastavljač pokriva čestice, ljestvice odgovora, obrnuto bodovane čestice i pravilo bodovanja; vi jamčite da vaš radni prostor drži prava na tekst koji unosi.'
			]
		}
	],
	glossaryTitle: 'Pojmovnik',
	glossary: [
		{ term: 'Studija', definition: 'Jedan istraživački protokol i sve prikupljeno pod njim. Živi u Studijama.' },
		{ term: 'Krug', definition: 'Jedan ciklus prikupljanja podataka sa snimkom cijele postave pri pokretanju.' },
		{ term: 'Prikupljanje', definition: 'Konzola prikupljanja studije: poveznice, pozivnice, brojevi odgovora, status krugova.' },
		{ term: 'Nalazi', definition: 'Površina rezultata: deskriptivni pokazatelji, usporedbe, podrijetlo, izvozi.' },
		{ term: 'Otvorena poveznica', definition: 'Anonimna poveznica za odgovaranje koju svatko može upotrijebiti jednom po pregledniku.' },
		{ term: 'Osobna poveznica', definition: 'Poveznica za jednu osobu u identificiranim krugovima; prikazuje se jednom pri izradi.' },
		{ term: 'Instrument', definition: 'Upitnik: čestice, ljestvice i struktura, verzionirani pri objavi.' },
		{ term: 'Pravilo bodovanja', definition: 'Objavljeno pravilo koje odgovore pretvara u rezultate; deklarira što proizvodi.' },
		{ term: 'Prag k = 5', definition: 'Fiksni prag objave: vrijednosti se ne prikazuju za manje od pet ljudi.' },
		{ term: 'Potiskivanje', definition: 'Što se događa ispod praga: rezultat postoji, ali su njegove vrijednosti skrivene.' },
		{ term: 'Kod sudionika', definition: 'Kod koji ispitanik sam odabere i koji povezuje krugove bez pohrane identiteta.' },
		{ term: 'Zahtjev za povlačenje', definition: 'Zabilježeni zahtjev ispitanika da se njegovi podaci izbrišu ili anonimiziraju.' }
	]
};

export function helpContent(): HelpContent {
	return localeState.current === 'hr' ? hr : en;
}
