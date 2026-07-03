export type RespondentLocale = 'en' | 'hr';

export function respondentLocale(defaultLocale: string, override?: string | null): RespondentLocale {
	const candidate = (override ?? defaultLocale ?? 'en').toLowerCase();
	return candidate.startsWith('hr') ? 'hr' : 'en';
}

export const respondentCopy = {
	en: {
		invitedTo: 'You are invited to take part in',
		consentTitle: 'Before you begin',
		consentAgree: 'I agree and want to participate',
		consentRequiredNote: 'Participation requires the consent above.',
		participantCodeLabel: 'Your participant code',
		participantCodeHelp:
			'This code links your answers across rounds without revealing who you are. Use the same code every time.',
		begin: 'Begin',
		notAvailable: 'This survey is not open right now.',
		notFound: 'This link is not valid. Check that you copied it completely.',
		question: 'Question',
		of: 'of',
		required: 'Required',
		optionalHint: 'Optional',
		naLabel: 'Not applicable',
		saving: 'Saving…',
		saved: 'Saved',
		submit: 'Submit answers',
		submitting: 'Submitting…',
		missingRequired: 'Some required questions are still unanswered.',
		reviewMissing: 'Go to the first unanswered question',
		thanksTitle: 'Thank you.',
		thanksBody: 'Your answers were received. You can close this page.',
		answersReceived: 'answers received',
		progressLabel: 'Progress',
		unsubscribeTitle: 'Stop receiving emails for this study?',
		unsubscribeConfirm: 'Stop emails',
		unsubscribeDone: 'You will not receive further emails for this study.',
		typeUnsupported: 'Write your answer below.',
		submitFailed: 'Your answers could not be submitted. They are saved — try again.',
		startFailed: 'The survey could not be started. Reload the page to try again.',
		queueHint: 'You have been asked to answer about the following. Each one is a short, separate questionnaire.',
		aboutYourself: 'About yourself',
		answer: 'Answer',
		done: 'Done'
	},
	hr: {
		invitedTo: 'Pozvani ste sudjelovati u istraživanju',
		consentTitle: 'Prije početka',
		consentAgree: 'Slažem se i želim sudjelovati',
		consentRequiredNote: 'Za sudjelovanje je potrebna gornja privola.',
		participantCodeLabel: 'Vaš kod sudionika',
		participantCodeHelp:
			'Ovaj kod povezuje vaše odgovore kroz kruge istraživanja bez otkrivanja identiteta. Koristite isti kod svaki put.',
		begin: 'Započni',
		notAvailable: 'Ovaj upitnik trenutno nije otvoren.',
		notFound: 'Ova poveznica nije valjana. Provjerite jeste li je kopirali u cijelosti.',
		question: 'Pitanje',
		of: 'od',
		required: 'Obavezno',
		optionalHint: 'Neobavezno',
		naLabel: 'Nije primjenjivo',
		saving: 'Spremanje…',
		saved: 'Spremljeno',
		submit: 'Pošalji odgovore',
		submitting: 'Slanje…',
		missingRequired: 'Neka obavezna pitanja još nemaju odgovor.',
		reviewMissing: 'Idi na prvo neodgovoreno pitanje',
		thanksTitle: 'Hvala vam.',
		thanksBody: 'Vaši su odgovori zaprimljeni. Možete zatvoriti ovu stranicu.',
		answersReceived: 'zaprimljenih odgovora',
		progressLabel: 'Napredak',
		unsubscribeTitle: 'Želite li prestati primati e-poštu za ovo istraživanje?',
		unsubscribeConfirm: 'Prestani slati e-poštu',
		unsubscribeDone: 'Nećete više primati e-poštu za ovo istraživanje.',
		typeUnsupported: 'Upišite svoj odgovor u polje ispod.',
		submitFailed: 'Odgovore nije bilo moguće poslati. Spremljeni su — pokušajte ponovno.',
		startFailed: 'Upitnik nije bilo moguće pokrenuti. Osvježite stranicu i pokušajte ponovno.',
		queueHint: 'Zamoljeni ste odgovoriti o sljedećem. Svaki je kratak, zaseban upitnik.',
		aboutYourself: 'O sebi',
		answer: 'Odgovori',
		done: 'Gotovo'
	}
} satisfies Record<RespondentLocale, Record<string, string>>;
