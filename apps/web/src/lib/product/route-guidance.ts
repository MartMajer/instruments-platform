import type { AppLocale } from '$lib/i18n/localization';

export type ProductRouteGuidanceId =
	| 'home'
	| 'studies'
	| 'selected-study'
	| 'setup'
	| 'operations'
	| 'reports'
	| 'waves'
	| 'exports'
	| 'instruments'
	| 'directory'
	| 'team'
	| 'settings';

export type ProductRouteGuidanceContext = {
	isSample?: boolean;
	canManageSetup?: boolean;
	canManageTeam?: boolean;
	isEmpty?: boolean;
};

export type ProductRouteGuidanceView = {
	ariaLabel: string;
	kicker: string;
	inspectFirstLabel: string;
	whenItMattersLabel: string;
	nextMoveLabel: string;
	limitsLabel: string;
	title: string;
	summary: string;
	inspectFirst: string;
	whenItMatters: string;
	nextMove: string;
	limits?: string;
};

const englishChrome = {
	ariaLabel: 'Route guidance',
	kicker: 'Route guidance',
	inspectFirstLabel: 'Inspect first',
	whenItMattersLabel: 'When it matters',
	nextMoveLabel: 'Next move',
	limitsLabel: 'Limits'
};

const croatianChrome = {
	ariaLabel: 'Smjernice rute',
	kicker: 'Smjernice rute',
	inspectFirstLabel: 'Prvo pregledajte',
	whenItMattersLabel: 'Kada je važno',
	nextMoveLabel: 'Sljedeći korak',
	limitsLabel: 'Ograničenja'
};

const baseGuidance: Record<ProductRouteGuidanceId, ProductRouteGuidanceView> = {
	home: {
		...englishChrome,
		title: 'Start with study context',
		summary: 'Use this page to choose between read-only sample studies and your own study work.',
		inspectFirst:
			'Inspect the sample studies first, then compare them with your own active studies.',
		whenItMatters:
			'Use this as the first stop after sign-in or when you lose track of the study lifecycle.',
		nextMove: 'Open Studies when you need the full portfolio.'
	},
	studies: {
		...englishChrome,
		title: 'Choose the right study',
		summary: 'Compare sample and own studies by lifecycle state before opening deeper work routes.',
		inspectFirst: 'Inspect Sample studies for examples and Your studies for editable work.',
		whenItMatters: 'Use this when deciding what to prepare, collect, review, archive, or restore.',
		nextMove: 'Open a study, or create your study when you have setup management access.'
	},
	'selected-study': {
		...englishChrome,
		title: 'Orient inside one study',
		summary: 'Use the selected-study overview to understand where this study is in the lifecycle.',
		inspectFirst: 'Inspect the lifecycle map before opening Prepare, Collect, Results, or Waves.',
		whenItMatters: 'Use this after choosing a study or when you need to switch lifecycle phases.',
		nextMove: 'Open the lifecycle phase with the next blocked or ready action.'
	},
	setup: {
		...englishChrome,
		title: 'Prepare before collection',
		summary: 'Review the setup checklist before the study moves into collection.',
		inspectFirst:
			'Inspect blocked checklist items for instrument, scoring, policy, campaign, and launch readiness.',
		whenItMatters: 'Use this before launching collection or when a study is not configured.',
		nextMove: 'Move to collection after launch readiness is clear.'
	},
	operations: {
		...englishChrome,
		title: 'Track collection',
		summary:
			'Follow collection state, respondent access, response progress, and scoring readiness.',
		inspectFirst: 'Inspect respondent access and response progress before operational actions.',
		whenItMatters:
			'Use this while a study is launching, live, partially complete, or ready to close.',
		nextMove: 'Open Review results when submitted responses and scoring are ready.'
	},
	reports: {
		...englishChrome,
		title: 'Read results carefully',
		summary:
			'Start with availability, coverage, limitations, and export next use before reference metadata.',
		inspectFirst:
			'Inspect result availability and coverage before acting on charts or export files.',
		whenItMatters:
			'Use this after collection has submissions or when results look suppressed or preliminary.',
		nextMove: 'Use exports after the result story and limitations are understood.'
	},
	waves: {
		...englishChrome,
		title: 'Advanced repeat-participation context',
		summary: 'Use Waves for repeat-participation comparison across repeated study waves.',
		inspectFirst:
			'Inspect selected waves, compatibility, linked repeat responses, and disclosure limits.',
		whenItMatters: 'Use this only for repeated-wave studies where change over time matters.',
		nextMove: 'For a single-wave study, return to Review results or Exports.'
	},
	exports: {
		...englishChrome,
		title: 'Use generated files',
		summary: 'Find study-support files by readiness, purpose, source context, and next use.',
		inspectFirst: 'Inspect ready downloads and attention items before opening file details.',
		whenItMatters:
			'Use this when analysis handoff, codebook review, or result packets need generated files.',
		nextMove:
			'Open the source study results page when you need to understand how an export file was generated.'
	},
	instruments: {
		...englishChrome,
		title: 'Check launchable instruments',
		summary: 'Review tenant-visible instruments and whether they can be used in study setup.',
		inspectFirst: 'Inspect launch eligibility, rights labels, and validity labels.',
		whenItMatters: 'Use this before creating or preparing a study that needs an instrument.',
		nextMove: 'Open Studies and prepare a study with the right instrument context.'
	},
	directory: {
		...englishChrome,
		title: 'Prepare people and targeting',
		summary:
			'Use Directory when studies need subjects, groups, memberships, or manager relationships.',
		inspectFirst:
			'Inspect subjects, groups, and manager links before launch targeting or hierarchy reporting.',
		whenItMatters:
			'Use this when a study needs identified audiences, groups, or hierarchy context.',
		nextMove: 'Return to Prepare study when directory prerequisites are ready.'
	},
	team: {
		...englishChrome,
		title: 'Review access and roles',
		summary:
			'Use Team to understand who can enter the tenant and what app-owned permissions they have.',
		inspectFirst:
			'Inspect active members, role assignments, identity status, and effective permissions.',
		whenItMatters: 'Use this before adding collaborators or debugging read-only access.',
		nextMove: 'Prepare or change member access when you have team management access.'
	},
	settings: {
		...englishChrome,
		title: 'Inspect workspace profile',
		summary: 'Use Settings for tenant profile, workspace scale, and management destinations.',
		inspectFirst: 'Inspect the tenant profile and workspace counts before using management links.',
		whenItMatters: 'Use this for workspace administration context, not as the main study workflow.',
		nextMove: 'Return to Studies for normal study work.'
	}
};

const croatianGuidance: Record<ProductRouteGuidanceId, ProductRouteGuidanceView> = {
	home: {
		...croatianChrome,
		title: 'Krenite od konteksta studije',
		summary: 'Ovdje birate između oglednih studija samo za čitanje i vlastitog rada.',
		inspectFirst: 'Prvo pregledajte ogledne studije, zatim ih usporedite s vlastitim aktivnim studijama.',
		whenItMatters: 'Koristite ovo kao prvo mjesto nakon prijave ili kada izgubite pregled životnog ciklusa studije.',
		nextMove: 'Otvorite Studije kada trebate cijeli portfelj.'
	},
	studies: {
		...croatianChrome,
		title: 'Odaberite pravu studiju',
		summary: 'Usporedite ogledne i vlastite studije prema stanju životnog ciklusa prije dubljih ruta.',
		inspectFirst: 'Pregledajte Ogledne studije za primjere i Vaše studije za vlastiti rad.',
		whenItMatters: 'Koristite ovo kada odlučujete što pripremiti, prikupiti, pregledati, arhivirati ili vratiti.',
		nextMove: 'Otvorite studiju ili izradite vlastitu studiju kada imate pristup postavljanju.'
	},
	'selected-study': {
		...croatianChrome,
		title: 'Orijentirajte se unutar jedne studije',
		summary: 'Pregled odabrane studije pokazuje gdje je studija u životnom ciklusu.',
		inspectFirst: 'Prvo pregledajte kartu životnog ciklusa prije Pripreme, Prikupljanja, Rezultata ili Mjerenja.',
		whenItMatters: 'Koristite ovo nakon odabira studije ili kada trebate promijeniti fazu rada.',
		nextMove: 'Otvorite fazu životnog ciklusa sa sljedećom blokiranom ili spremnom radnjom.'
	},
	setup: {
		...croatianChrome,
		title: 'Pripremite prije prikupljanja',
		summary: 'Pregledajte kontrolni popis postavljanja prije prelaska studije u prikupljanje.',
		inspectFirst: 'Prvo pregledajte blokirane stavke za upitnik, rezultate, pravila, mjerenje i spremnost pokretanja.',
		whenItMatters: 'Koristite ovo prije pokretanja prikupljanja ili kada studija nije postavljena.',
		nextMove: 'Prijeđite na Prikupljanje kada je spremnost pokretanja čista.'
	},
	operations: {
		...croatianChrome,
		title: 'Pratite prikupljanje',
		summary: 'Pratite stanje prikupljanja, pristup ispitanika, napredak odgovora i spremnost rezultata.',
		inspectFirst: 'Prvo pregledajte pristup ispitanika i napredak odgovora prije operativnih radnji.',
		whenItMatters: 'Koristite ovo dok se studija pokreće, traje, djelomično je dovršena ili spremna za zatvaranje.',
		nextMove: 'Otvorite Pregled rezultata kada su predani odgovori i bodovanje spremni.'
	},
	reports: {
		...croatianChrome,
		title: 'Pažljivo čitajte rezultate',
		summary: 'Počnite od dostupnosti, pokrivenosti, ograničenja i sljedeće upotrebe izvoza.',
		inspectFirst: 'Prvo pregledajte dostupnost rezultata i pokrivenost prije grafova ili izvoznih datoteka.',
		whenItMatters: 'Koristite ovo nakon što prikupljanje ima odgovore ili kada rezultati izgledaju skriveno ili preliminarno.',
		nextMove: 'Koristite Izvoze nakon što razumijete priču rezultata i ograničenja.'
	},
	waves: {
		...croatianChrome,
		title: 'Napredni kontekst ponovljenih mjerenja',
		summary: 'Koristite Mjerenja za usporedbu kroz ponovljene krugove prikupljanja.',
		inspectFirst: 'Prvo pregledajte odabrana mjerenja, kompatibilnost, povezani ponovljeni odgovori i ograničenja prikaza.',
		whenItMatters: 'Koristite ovo samo za studije s ponovljenim mjerenjima gdje je važna promjena kroz vrijeme.',
		nextMove: 'Za studiju s jednim mjerenjem vratite se na Pregled rezultata ili Izvoze.'
	},
	exports: {
		...croatianChrome,
		title: 'Koristite izrađene datoteke',
		summary: 'Pronađite datoteke podrške prema spremnosti, namjeni, izvoru i sljedećoj upotrebi.',
		inspectFirst: 'Prvo pregledajte spremna preuzimanja i stavke koje trebaju pažnju.',
		whenItMatters: 'Koristite ovo kada analiza, opis podataka ili paket rezultata trebaju izrađene datoteke.',
		nextMove: 'Otvorite izvornu stranicu rezultata kada trebate razumjeti kako je datoteka izrađena.'
	},
	instruments: {
		...croatianChrome,
		title: 'Provjerite izvore upitnika',
		summary: 'Pregledajte izvore upitnika vidljive radnom prostoru i mogu li se koristiti u postavljanju.',
		inspectFirst: 'Prvo pregledajte spremnost za pokretanje, prava korištenja i oznake valjanosti.',
		whenItMatters: 'Koristite ovo prije izrade ili pripreme studije koja treba izvor upitnika.',
		nextMove: 'Otvorite Studije i pripremite studiju s pravim kontekstom upitnika.'
	},
	directory: {
		...croatianChrome,
		title: 'Pripremite osobe i ciljanje',
		summary: 'Koristite Imenik kada studije trebaju osobe, grupe, članstva ili nadređene odnose.',
		inspectFirst: 'Prvo pregledajte osobe, grupe i odnose nadređenih prije ciljanja ili hijerarhijskih izvještaja.',
		whenItMatters: 'Koristite ovo kada studija treba identificirane publike, grupe ili hijerarhijski kontekst.',
		nextMove: 'Vratite se na Pripremu studije kada su preduvjeti imenika spremni.'
	},
	team: {
		...croatianChrome,
		title: 'Pregledajte pristup i uloge',
		summary: 'Koristite Tim za pregled tko može ući u radni prostor i koje dozvole ima.',
		inspectFirst: 'Prvo pregledajte aktivne članove, dodjele uloga, status identiteta i efektivne dozvole.',
		whenItMatters: 'Koristite ovo prije dodavanja suradnika ili rješavanja pristupa samo za čitanje.',
		nextMove: 'Pripremite ili promijenite pristup članova kada imate pristup upravljanju timom.'
	},
	settings: {
		...croatianChrome,
		title: 'Pregledajte profil radnog prostora',
		summary: 'Koristite Postavke za profil radnog prostora, opseg i upravljačke destinacije.',
		inspectFirst: 'Prvo pregledajte profil radnog prostora i brojeve prije upravljačkih poveznica.',
		whenItMatters: 'Koristite ovo za administrativni kontekst, ne kao glavni tijek studije.',
		nextMove: 'Vratite se na Studije za normalan rad.'
	}
};

const sampleAwareRoutes = new Set<ProductRouteGuidanceId>([
	'selected-study',
	'setup',
	'operations',
	'reports',
	'waves'
]);

const setupManagedRoutes = new Set<ProductRouteGuidanceId>([
	'studies',
	'selected-study',
	'setup',
	'operations',
	'reports',
	'waves',
	'directory'
]);

export function toProductRouteGuidance(
	id: ProductRouteGuidanceId,
	context: ProductRouteGuidanceContext = {},
	locale: AppLocale = 'en'
): ProductRouteGuidanceView {
	const guidance = { ...(locale === 'hr-HR' ? croatianGuidance[id] : baseGuidance[id]) };
	const limits: string[] = [];

	if (context.isEmpty) {
		applyEmptyState(guidance, id);
	}

	if (context.isSample && sampleAwareRoutes.has(id)) {
		limits.push(
			locale === 'hr-HR'
				? 'Ogledne studije su primjeri samo za čitanje. Duplicirajte ogledni primjer kao vlastitu studiju prije promjene postavljanja, prikupljanja ili izvještavanja.'
				: 'Sample studies are read-only examples. Duplicate the sample as your own study before changing setup, collection, or reporting state.'
		);
		guidance.nextMove =
			locale === 'hr-HR'
				? `Duplicirajte ovaj ogledni primjer kao vlastitu studiju prije uređivanja. ${guidance.nextMove}`
				: `Duplicate this sample as your own study before editing. ${guidance.nextMove}`;
	}

	if (context.canManageSetup === false && setupManagedRoutes.has(id)) {
		limits.push(
			id === 'directory'
				? locale === 'hr-HR'
					? 'Postavljanje imenika traži pristup upravljanju postavljanjem.'
					: 'Directory setup requires setup management access.'
				: locale === 'hr-HR'
					? 'Promjena studija traži pristup upravljanju postavljanjem.'
					: 'Changing studies requires setup management access.'
		);
		guidance.nextMove = toReadOnlySetupNextMove(guidance.nextMove, locale);
	}

	if (context.canManageTeam === false && id === 'team') {
		limits.push(
			locale === 'hr-HR'
				? 'Priprema članova i promjene uloga traže pristup upravljanju timom.'
				: 'Member preparation and role changes require team management access.'
		);
		guidance.nextMove =
			locale === 'hr-HR'
				? 'Koristite ovu rutu za pregled trenutnog pristupa; za promjene uloga pitajte upravitelja tima.'
				: 'Use this route to inspect current access; ask a team manager to change roles.';
	}

	if (limits.length > 0) {
		guidance.limits = limits.join(' ');
	}

	return guidance;
}

function applyEmptyState(guidance: ProductRouteGuidanceView, id: ProductRouteGuidanceId) {
	if (id === 'studies') {
		guidance.inspectFirst = 'No studies are visible in the current portfolio view.';
		guidance.nextMove =
			'Create your study when you have setup access, or adjust filters if studies should exist.';
		return;
	}

	if (id === 'exports') {
		guidance.inspectFirst = 'No export files exist yet.';
		guidance.nextMove = 'Create an export from a study results page after results are available.';
		return;
	}

	if (id === 'home') {
		guidance.inspectFirst = 'No sample or own studies are visible yet.';
		guidance.nextMove = 'Open Studies to create or inspect tenant study work.';
	}
}

function toReadOnlySetupNextMove(currentNextMove: string, locale: AppLocale) {
	return locale === 'hr-HR'
		? `Koristite ovu rutu za pregled trenutnog stanja; prije promjene pitajte upravitelja postavljanja. ${currentNextMove}`
		: `Use this route to inspect current state; ask a setup manager before changing it. ${currentNextMove}`;
}
