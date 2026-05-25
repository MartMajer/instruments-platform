import type { AppLocale } from './localization';

type LocaleDictionary<T> = Record<AppLocale, T>;

export type AppShellCopy = {
	shell: {
		productWorkspace: string;
		privateBeta: string;
		tenantSetup: string;
		tenantWorkspace: string;
		productEntry: string;
		registration: string;
		signIn: string;
		respondent: string;
		tenantSetupPath: string;
		tenantCommandWorkspace: string;
		authenticatedWorkspaceGateway: string;
		createWorkspace: string;
		workspaceSignIn: string;
		respondentAccess: string;
		setupApisLaunchReadiness: string;
		tenantSetupWorkspace: string;
	};
	nav: {
		home: string;
		studies: string;
		study: string;
		directory: string;
		team: string;
		exports: string;
		settings: string;
		instruments: string;
		workspace: string;
		more: string;
	};
	descriptions: {
		workspaceAccess: string;
		filesAndDownloads: string;
		workspaceSettings: string;
	};
	actions: {
		signOut: string;
		openNavigationMenu: string;
		closeNavigationMenu: string;
	};
	aria: {
		mobileWorkspaceNavigation: string;
		workspaceMenu: string;
		primaryWorkspaceRoutes: string;
		moreWorkspaceRoutes: string;
		primaryMobileNavigation: string;
		setupStages: string;
		workspacePosture: string;
	};
	language: {
		label: string;
		switchTo: string;
	};
};

export type SurfaceNavCopy = {
	sections: {
		studies: string;
		peopleAccess: string;
		workspaceAdmin: string;
		selectedStudy: string;
		internalTools: string;
	};
	surfaces: {
		home: string;
		studies: string;
		instrumentLibrary: string;
		exports: string;
		directory: string;
		team: string;
		settings: string;
		overview: string;
		setup: string;
		collect: string;
		results: string;
		waves: string;
		demoFixtures: string;
	};
	descriptions: {
		startHere: string;
		planStudies: string;
		questionSets: string;
		files: string;
		audiencesGroups: string;
		workspaceAccess: string;
		workspaceProfile: string;
		planStatus: string;
		buildStudy: string;
		collect: string;
		reportsExports: string;
		compareWaves: string;
		localGatedStates: string;
	};
	aria: {
		productNavigation: string;
	};
};

export type AuthBoundaryCopy = {
	head: {
		title: string;
		description: string;
	};
	access: {
		workspaceAccess: string;
		checkingTitle: string;
		checkingDetail: string;
		verifyEmailTitle: string;
		verifyEmailDetail: string;
		signedInAs: string;
		workspaceSignInNeeded: string;
		emailVerificationReminder: string;
		verifyThenSignIn: string;
		chooseRequestedAccount: string;
		registrationSignInDidNotFinish: string;
		signInWithWorkspaceAccount: string;
		openVerificationEmail: string;
		wrongAccountSignOut: string;
		emailMismatchText: string;
		emailMismatchNote: string;
		registrationRetryText: string;
		registrationRetryNote: string;
		noWorkspaceAccountText: string;
		noWorkspaceAccountNote: string;
		emailVerificationRequired: string;
		useSameEmail: string;
		useSavedRegistrationLink: string;
		useWorkspaceAccount: string;
		signInWithTenantAccount: string;
		noWorkspaceSession: string;
		 signedInWorkspaceAccount: string;
		 sessionTechnicalDetails: string;
		 technicalDetails: string;
		 workspaceAccessUnavailable: string;
		 forbiddenDetail: string;
		 couldNotVerifyWorkspaceAccess: string;
		 apiRetry500: string;
		 apiRetryGeneral: string;
		 sessionCheckFailed: string;
		 tryRegistrationSignInAgain: string;
		signInAfterVerification: string;
		chooseAccountAgain: string;
		signOutCompletely: string;
		signIn: string;
		signInExistingWorkspace: string;
		createWorkspace: string;
		retry: string;
	};
};

export type RespondentReceiptCopy = {
	title: string;
	headline: (studyName: string) => string;
	notAvailable: string;
	metrics: {
		study: string;
		responseMode: string;
		locale: string;
		consentVersion: string;
		answersReceived: string;
	};
	guidance: {
		close: string;
		noScores: string;
		contact: string;
		participantCode: string;
	};
};

const appShellCopies: LocaleDictionary<AppShellCopy> = {
	en: {
		shell: {
			productWorkspace: 'Product workspace',
			privateBeta: 'Private beta',
			tenantSetup: 'Tenant setup',
			tenantWorkspace: 'Tenant workspace',
			productEntry: 'Product entry',
			registration: 'Registration',
			signIn: 'Sign in',
			respondent: 'Respondent',
			tenantSetupPath: 'Tenant setup path',
			tenantCommandWorkspace: 'Tenant command workspace',
			authenticatedWorkspaceGateway: 'Authenticated workspace gateway',
			createWorkspace: 'Create workspace',
			workspaceSignIn: 'Workspace sign-in',
			respondentAccess: 'Respondent access',
			setupApisLaunchReadiness: 'Setup APIs and launch readiness',
			tenantSetupWorkspace: 'Tenant setup workspace'
		},
		nav: {
			home: 'Home',
			studies: 'Studies',
			study: 'Study',
			directory: 'Directory',
			team: 'Team',
			exports: 'Exports',
			settings: 'Settings',
			instruments: 'Instruments',
			workspace: 'Workspace',
			more: 'More'
		},
		descriptions: {
			workspaceAccess: 'Workspace access',
			filesAndDownloads: 'Files and downloads',
			workspaceSettings: 'Workspace settings'
		},
		actions: {
			signOut: 'Sign out',
			openNavigationMenu: 'Open navigation menu',
			closeNavigationMenu: 'Close navigation menu'
		},
		aria: {
			mobileWorkspaceNavigation: 'Mobile workspace navigation',
			workspaceMenu: 'Workspace menu',
			primaryWorkspaceRoutes: 'Primary workspace routes',
			moreWorkspaceRoutes: 'More workspace routes',
			primaryMobileNavigation: 'Primary mobile navigation',
			setupStages: 'Setup stages',
			workspacePosture: 'Workspace posture'
		},
		language: {
			label: 'Language',
			switchTo: 'Switch language to'
		}
	},
	'hr-HR': {
		shell: {
			productWorkspace: 'Radni prostor aplikacije',
			privateBeta: 'Privatna beta',
			tenantSetup: 'Postavljanje organizacije',
			tenantWorkspace: 'Radni prostor organizacije',
			productEntry: 'Ulaz u proizvod',
			registration: 'Registracija',
			signIn: 'Prijava',
			respondent: 'Sudionik',
			tenantSetupPath: 'Put postavljanja organizacije',
			tenantCommandWorkspace: 'Radni prostor organizacije',
			authenticatedWorkspaceGateway: 'Pristup radnom prostoru',
			createWorkspace: 'Izradi radni prostor',
			workspaceSignIn: 'Prijava u radni prostor',
			respondentAccess: 'Pristup za sudionike',
			setupApisLaunchReadiness: 'Postavljanje i spremnost za pokretanje',
			tenantSetupWorkspace: 'Radni prostor za postavljanje'
		},
		nav: {
			home: 'Početna',
			studies: 'Studije',
			study: 'Studija',
			directory: 'Imenik',
			team: 'Tim',
			exports: 'Izvozi',
			settings: 'Postavke',
			instruments: 'Instrumenti',
			workspace: 'Radni prostor',
			more: 'Više'
		},
		descriptions: {
			workspaceAccess: 'Pristup radnom prostoru',
			filesAndDownloads: 'Datoteke i preuzimanja',
			workspaceSettings: 'Postavke radnog prostora'
		},
		actions: {
			signOut: 'Odjava',
			openNavigationMenu: 'Otvori navigaciju',
			closeNavigationMenu: 'Zatvori navigaciju'
		},
		aria: {
			mobileWorkspaceNavigation: 'Mobilna navigacija radnog prostora',
			workspaceMenu: 'Izbornik radnog prostora',
			primaryWorkspaceRoutes: 'Glavne stranice radnog prostora',
			moreWorkspaceRoutes: 'Dodatne stranice radnog prostora',
			primaryMobileNavigation: 'Glavna mobilna navigacija',
			setupStages: 'Koraci postavljanja',
			workspacePosture: 'Stanje radnog prostora'
		},
		language: {
			label: 'Jezik',
			switchTo: 'Promijeni jezik na'
		}
	}
};

const surfaceNavCopies: LocaleDictionary<SurfaceNavCopy> = {
	en: {
		sections: {
			studies: 'Studies',
			peopleAccess: 'People and access',
			workspaceAdmin: 'Workspace admin',
			selectedStudy: 'Selected study',
			internalTools: 'Internal tools'
		},
		surfaces: {
			home: 'Home',
			studies: 'Studies',
			instrumentLibrary: 'Instrument library',
			exports: 'Exports',
			directory: 'Directory',
			team: 'Team',
			settings: 'Settings',
			overview: 'Overview',
			setup: 'Setup',
			collect: 'Collect',
			results: 'Results',
			waves: 'Waves',
			demoFixtures: 'Demo fixtures'
		},
		descriptions: {
			startHere: 'Start here',
			planStudies: 'Plan studies',
			questionSets: 'Question sets',
			files: 'Files',
			audiencesGroups: 'Audiences and groups',
			workspaceAccess: 'Workspace access',
			workspaceProfile: 'Workspace profile',
			planStatus: 'Plan and status',
			buildStudy: 'Build study',
			collect: 'Run collection',
			reportsExports: 'Reports and exports',
			compareWaves: 'Compare waves',
			localGatedStates: 'Local gated states'
		},
		aria: {
			productNavigation: 'Product navigation'
		}
	},
	'hr-HR': {
		sections: {
			studies: 'Studije',
			peopleAccess: 'Ljudi i pristup',
			workspaceAdmin: 'Administracija',
			selectedStudy: 'Odabrana studija',
			internalTools: 'Interni alati'
		},
		surfaces: {
			home: 'Početna',
			studies: 'Studije',
			instrumentLibrary: 'Knjižnica instrumenata',
			exports: 'Izvozi',
			directory: 'Imenik',
			team: 'Tim',
			settings: 'Postavke',
			overview: 'Pregled',
			setup: 'Postavljanje',
			collect: 'Prikupljanje',
			results: 'Rezultati',
			waves: 'Mjerenja',
			demoFixtures: 'Demo stanja'
		},
		descriptions: {
			startHere: 'Počnite ovdje',
			planStudies: 'Planiraj studije',
			questionSets: 'Skupovi pitanja',
			files: 'Datoteke',
			audiencesGroups: 'Publike i grupe',
			workspaceAccess: 'Pristup radnom prostoru',
			workspaceProfile: 'Profil radnog prostora',
			planStatus: 'Plan i status',
			buildStudy: 'Izradi studiju',
			collect: 'Provedi prikupljanje',
			reportsExports: 'Izvještaji i izvozi',
			compareWaves: 'Usporedi mjerenja',
			localGatedStates: 'Lokalna testna stanja'
		},
		aria: {
			productNavigation: 'Navigacija proizvoda'
		}
	}
};

const authBoundaryCopies: LocaleDictionary<AuthBoundaryCopy> = {
	en: {
		head: {
			title: 'Workspace',
			description: 'Authenticated campaign-series workspace for Validated Scale.'
		},
		access: {
			workspaceAccess: 'Workspace access',
			checkingTitle: 'Checking workspace access',
			checkingDetail: 'Confirming your signed-in account and workspace membership.',
			verifyEmailTitle: 'Verify your email',
			verifyEmailDetail:
				'Open the verification email from your sign-in provider to keep access after signing out.',
			signedInAs: 'Signed in as',
			workspaceSignInNeeded: 'Workspace sign-in needed',
			emailVerificationReminder: 'Email verification reminder',
			verifyThenSignIn: 'Verify email, then sign in',
			chooseRequestedAccount: 'Choose the requested account',
			registrationSignInDidNotFinish: 'Registration sign-in did not finish',
			signInWithWorkspaceAccount: 'Sign in with your workspace account',
			openVerificationEmail:
				'Open the verification email from your sign-in provider, then sign in again with the same account.',
			wrongAccountSignOut:
				'If the browser keeps choosing the wrong account, sign out completely and choose the intended email.',
			emailMismatchText:
				'The selected sign-in account did not match the workspace email you entered. Sign out completely, then choose the same account again.',
			emailMismatchNote:
				'This protects the workspace from stale provider sessions and wrong account selection.',
			registrationRetryText: 'Retry the saved registration sign-in link if sign-in was interrupted.',
			registrationRetryNote: 'If the browser keeps choosing the wrong account, sign out completely first.',
			noWorkspaceAccountText:
				'The selected account does not have access to this workspace. Sign in with the email that owns the workspace, or create a new workspace from registration.',
			noWorkspaceAccountNote:
				'If the browser keeps choosing the wrong account, sign out completely and choose the intended email.',
			emailVerificationRequired: 'Email verification is required before signing in again after sign-out.',
			useSameEmail: 'Use the same email you entered on the workspace sign-in page.',
			useSavedRegistrationLink:
				'Use the saved registration link only when registration was interrupted before the workspace opened.',
			useWorkspaceAccount:
				'Use an account that already belongs to this workspace. If this is not the account you intended, sign out completely.',
			signInWithTenantAccount:
				'Sign in with an account that belongs to this workspace before opening product screens.',
			noWorkspaceSession:
				'No workspace session is active. Sign in with your workspace email, or create a workspace first.',
			 signedInWorkspaceAccount: 'Signed-in workspace account',
			 sessionTechnicalDetails: 'Session technical details',
			 technicalDetails: 'Technical details',
			 workspaceAccessUnavailable: 'Workspace access unavailable',
			 forbiddenDetail:
				'This account is signed in, but it is not a member of the workspace the app tried to open.',
			 couldNotVerifyWorkspaceAccess: 'Could not verify workspace access',
			 apiRetry500:
				'The API could not confirm workspace access. Retry, then sign out and sign in again if it continues.',
			 apiRetryGeneral:
				'The app could not confirm workspace access. Sign out and sign in again if retry does not recover.',
			 sessionCheckFailed: 'Session check failed.',
			 tryRegistrationSignInAgain: 'Try registration sign-in again',
			signInAfterVerification: 'Sign in after verifying email',
			chooseAccountAgain: 'Choose account again',
			signOutCompletely: 'Sign out completely',
			signIn: 'Sign in',
			signInExistingWorkspace: 'Sign in to existing workspace',
			createWorkspace: 'Create workspace',
			retry: 'Retry'
		}
	},
	'hr-HR': {
		head: {
			title: 'Radni prostor',
			description: 'Autentificirani radni prostor za studije u Validated Scaleu.'
		},
		access: {
			workspaceAccess: 'Pristup radnom prostoru',
			checkingTitle: 'Provjera pristupa radnom prostoru',
			checkingDetail: 'Provjeravamo prijavljeni račun i članstvo u radnom prostoru.',
			verifyEmailTitle: 'Potvrdite e-poštu',
			verifyEmailDetail:
				'Otvorite poruku za potvrdu e-pošte od pružatelja prijave kako biste zadržali pristup nakon odjave.',
			signedInAs: 'Prijavljeni ste kao',
			workspaceSignInNeeded: 'Potrebna je prijava u radni prostor',
			emailVerificationReminder: 'Podsjetnik za potvrdu e-pošte',
			verifyThenSignIn: 'Potvrdite e-poštu, zatim se prijavite',
			chooseRequestedAccount: 'Odaberite traženi račun',
			registrationSignInDidNotFinish: 'Prijava tijekom registracije nije dovršena',
			signInWithWorkspaceAccount: 'Prijavite se računom radnog prostora',
			openVerificationEmail:
				'Otvorite poruku za potvrdu e-pošte od pružatelja prijave, zatim se ponovno prijavite istim računom.',
			wrongAccountSignOut:
				'Ako preglednik stalno bira pogrešan račun, potpuno se odjavite i odaberite željenu e-poštu.',
			emailMismatchText:
				'Odabrani račun za prijavu ne odgovara e-pošti radnog prostora koju ste unijeli. Potpuno se odjavite, zatim ponovno odaberite isti račun.',
			emailMismatchNote:
				'Ovo štiti radni prostor od starih sesija pružatelja prijave i pogrešnog odabira računa.',
			registrationRetryText:
				'Ponovno pokušajte spremljenu poveznicu za registracijsku prijavu ako je prijava prekinuta.',
			registrationRetryNote: 'Ako preglednik stalno bira pogrešan račun, prvo se potpuno odjavite.',
			noWorkspaceAccountText:
				'Odabrani račun nema pristup ovom radnom prostoru. Prijavite se e-poštom vlasnika radnog prostora ili izradite novi radni prostor u registraciji.',
			noWorkspaceAccountNote:
				'Ako preglednik stalno bira pogrešan račun, potpuno se odjavite i odaberite željenu e-poštu.',
			emailVerificationRequired: 'Potvrda e-pošte potrebna je prije ponovne prijave nakon odjave.',
			useSameEmail: 'Upotrijebite istu e-poštu koju ste unijeli na stranici prijave u radni prostor.',
			useSavedRegistrationLink:
				'Spremljenu registracijsku poveznicu upotrijebite samo ako je registracija prekinuta prije otvaranja radnog prostora.',
			useWorkspaceAccount:
				'Upotrijebite račun koji već pripada ovom radnom prostoru. Ako to nije željeni račun, potpuno se odjavite.',
			signInWithTenantAccount:
				'Prijavite se računom koji pripada ovom radnom prostoru prije otvaranja aplikacije.',
			noWorkspaceSession:
				'Nema aktivne sesije radnog prostora. Prijavite se e-poštom radnog prostora ili prvo izradite radni prostor.',
			 signedInWorkspaceAccount: 'Prijavljeni račun radnog prostora',
			 sessionTechnicalDetails: 'Tehnički detalji sesije',
			 technicalDetails: 'Tehnički detalji',
			 workspaceAccessUnavailable: 'Pristup radnom prostoru nije dostupan',
			 forbiddenDetail:
				'Ovaj je račun prijavljen, ali nije član radnog prostora koji je aplikacija pokušala otvoriti.',
			 couldNotVerifyWorkspaceAccess: 'Nije moguće provjeriti pristup radnom prostoru',
			 apiRetry500:
				'API nije mogao potvrditi pristup radnom prostoru. Pokušajte ponovno, zatim se odjavite i ponovno prijavite ako se problem nastavi.',
			 apiRetryGeneral:
				'Aplikacija nije mogla potvrditi pristup radnom prostoru. Odjavite se i ponovno prijavite ako ponovni pokušaj ne pomogne.',
			 sessionCheckFailed: 'Provjera sesije nije uspjela.',
			 tryRegistrationSignInAgain: 'Pokušaj registracijsku prijavu ponovno',
			signInAfterVerification: 'Prijavite se nakon potvrde e-pošte',
			chooseAccountAgain: 'Ponovno odaberite račun',
			signOutCompletely: 'Potpuna odjava',
			signIn: 'Prijava',
			signInExistingWorkspace: 'Prijava u postojeći radni prostor',
			createWorkspace: 'Izradi radni prostor',
			retry: 'Pokušaj ponovno'
		}
	}
};

const respondentReceiptCopies: LocaleDictionary<RespondentReceiptCopy> = {
	en: {
		title: 'Response submitted',
		headline: (studyName) => `Your response for ${studyName} was received.`,
		notAvailable: 'Not available',
		metrics: {
			study: 'Study',
			responseMode: 'Response mode',
			locale: 'Locale',
			consentVersion: 'Consent version',
			answersReceived: 'Answers received'
		},
		guidance: {
			close: 'You can close this page.',
			noScores: 'This page does not show scores or interpretation.',
			contact:
				'For questions about withdrawal or data use, use the study contact named in the consent information.',
			participantCode: 'Keep your participant code. The platform cannot recover it later.'
		}
	},
	'hr-HR': {
		title: 'Odgovor je poslan',
		headline: (studyName) => `Vaš odgovor za ${studyName} je zaprimljen.`,
		notAvailable: 'Nije dostupno',
		metrics: {
			study: 'Studija',
			responseMode: 'Način odgovora',
			locale: 'Jezik',
			consentVersion: 'Verzija privole',
			answersReceived: 'Zaprimljeni odgovori'
		},
		guidance: {
			close: 'Možete zatvoriti ovu stranicu.',
			noScores: 'Ova stranica ne prikazuje rezultate ni tumačenje.',
			contact:
				'Za pitanja o povlačenju sudjelovanja ili upotrebi podataka obratite se kontaktu navedenom u informacijama o privoli.',
			participantCode: 'Sačuvajte svoj sudionički kod. Platforma ga kasnije ne može vratiti.'
		}
	}
};

export function appShellCopy(locale: AppLocale): AppShellCopy {
	return appShellCopies[locale];
}

export function surfaceNavCopy(locale: AppLocale): SurfaceNavCopy {
	return surfaceNavCopies[locale];
}

export function authBoundaryCopy(locale: AppLocale): AuthBoundaryCopy {
	return authBoundaryCopies[locale];
}

export function respondentReceiptCopy(locale: AppLocale): RespondentReceiptCopy {
	return respondentReceiptCopies[locale];
}
