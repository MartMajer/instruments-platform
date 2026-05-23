import type { AppLocale } from '$lib/i18n/localization';
import type { SetupStageStatus } from '$lib/setup/stages';

export type ProductStatus =
	| SetupStageStatus
	| 'archived'
	| 'proof'
	| 'demo'
	| 'visible'
	| 'suppressed'
	| 'preliminary'
	| 'preliminary_live'
	| 'pending'
	| 'empty'
	| 'failed'
	| 'unsupported'
	| 'neutral'
	| 'not_available'
	| 'not_configured'
	| 'proof_only'
	| 'draft'
	| 'scheduled'
	| 'live'
	| 'closed'
	| 'cancelled';

const productStatusLabels: Record<AppLocale, Record<ProductStatus, string>> = {
	en: {
		ready: 'Ready',
		next: 'Next',
		blocked: 'Blocked',
		archived: 'Archived',
		proof: 'Preview',
		demo: 'Demo data',
		visible: 'Visible',
		suppressed: 'Suppressed',
		preliminary: 'Preliminary',
		preliminary_live: 'Preliminary live',
		pending: 'Pending',
		empty: 'Empty',
		failed: 'Failed',
		unsupported: 'Unsupported',
		neutral: 'Neutral',
		not_available: 'Not available',
		not_configured: 'Not configured',
		proof_only: 'Preview',
		draft: 'Draft',
		scheduled: 'Scheduled',
		live: 'Live',
		closed: 'Closed',
		cancelled: 'Cancelled'
	},
	'hr-HR': {
		ready: 'Spremno',
		next: 'Sljedeće',
		blocked: 'Blokirano',
		archived: 'Arhivirano',
		proof: 'Pregled',
		demo: 'Demo podaci',
		visible: 'Vidljivo',
		suppressed: 'Potisnuto',
		preliminary: 'Preliminarno',
		preliminary_live: 'Preliminarno uživo',
		pending: 'Na čekanju',
		empty: 'Prazno',
		failed: 'Neuspjelo',
		unsupported: 'Nije podržano',
		neutral: 'Neutralno',
		not_available: 'Nije dostupno',
		not_configured: 'Nije konfigurirano',
		proof_only: 'Pregled',
		draft: 'Nacrt',
		scheduled: 'Zakazano',
		live: 'U tijeku',
		closed: 'Zatvoreno',
		cancelled: 'Otkazano'
	}
};

export function productStatusLabel(status: ProductStatus, locale: AppLocale = 'en'): string {
	return productStatusLabels[locale][status] ?? productStatusLabels.en[status];
}
