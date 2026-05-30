import { expect, test } from '@playwright/test';

test('respondent preview uses explicit locale and preserves it when returning to setup', async ({
	page
}) => {
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({
			json: {
				userId: 'b8f75ad5-a592-41f0-8fb6-a28d645cfbd5',
				tenantId: '80d7f151-df9d-4428-8de1-3f6693c66f71',
				email: 'owner@example.com',
				permissions: ['setup.manage']
			}
		});
	});

	await page.addInitScript((preview) => {
		window.sessionStorage.setItem(`validatedscale.respondent-preview.${preview.previewId}`, JSON.stringify(preview));
	}, sampleCroatianPreview);

	await page.goto(
		`/app/campaign-series/${sampleSeriesId}/setup/respondent-preview?previewId=${samplePreviewId}&locale=hr-HR`
	);

	await expect(page.getByRole('heading', { name: 'Studija pregled za sudionika' })).toBeVisible();
	await expect(page.getByTestId('respondent-question-runner')).toContainText('Pitanje 1 od 1');
	await expect(page.getByTestId('respondent-question-runner')).toContainText('Obavezno');
	await expect(page.getByRole('link', { name: 'Natrag na postavljanje' }).first()).toHaveAttribute(
		'href',
		`/app/campaign-series/${sampleSeriesId}/setup?locale=hr-HR`
	);
});

const sampleSeriesId = '2f2f819f-f6eb-486a-9e0f-872ac30af3d4';
const samplePreviewId = 'preview-hr';

const sampleCroatianPreview = {
	schemaVersion: 'validatedscale.respondent-preview.v1',
	previewId: samplePreviewId,
	seriesId: sampleSeriesId,
	seriesName: 'Studija',
	questionnaireName: 'Upitnik',
	locale: 'hr-HR',
	createdAt: Date.now(),
	questions: [
		{
			id: '4c1ff2cf-166a-498a-99ca-55991a6ef536',
			ordinal: 1,
			code: 'q01',
			type: 'likert',
			textDefault: 'Koliko je opterecenje bilo odrzivo?',
			required: true,
			scaleMinValue: 1,
			scaleMaxValue: 5,
			scaleAnchors: JSON.stringify([
				{ value: 1, label: 'Uopce se ne slazem' },
				{ value: 5, label: 'U potpunosti se slazem' }
			])
		}
	]
};
