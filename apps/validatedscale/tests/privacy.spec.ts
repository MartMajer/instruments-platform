import { expect, test } from '@playwright/test';

/**
 * The real-cohort GDPR rails, driven end to end: a do-not-contact list with
 * add/release, and a withdrawal request recorded against real identified
 * data, approved, and executed — with the system event surfacing in the bell.
 */

test('privacy: suppressed addresses are listed, honest and releasable', async ({ page }) => {
	const email = `e2e.suppress.${Date.now()}@example.org`;

	await page.goto('/app/privacy');
	await page.getByLabel('Email to suppress').fill(email);
	await page.getByRole('button', { name: 'Suppress address' }).click();

	const row = page.locator('tr', { hasText: email });
	await expect(row).toContainText('Suppressed', { timeout: 15_000 });

	await row.getByRole('button', { name: 'Release' }).click();
	await expect(row).toContainText('Released', { timeout: 15_000 });
});

test('privacy: a withdrawal request against real data is recorded, approved and executed', async ({
	page,
	browser
}) => {
	test.setTimeout(150_000);
	const studyName = `Withdrawal ${Date.now()}`;

	// roster (idempotent: stable external ids)
	await page.goto('/app/people');
	await page.getByRole('button', { name: 'Import CSV' }).click();
	await page.fill(
		'#csv',
		[
			'display_name,email,external_id,group_name,manager_external_id',
			'E2E 360 Ana,e2e360.ana@example.org,e2e-360-a1,E2E 360 pilot,',
			'E2E 360 Marko,e2e360.marko@example.org,e2e-360-m1,E2E 360 pilot,e2e-360-a1'
		].join('\n')
	);
	await page.getByRole('button', { name: 'Import', exact: true }).click();
	await expect(page.locator('.note').first()).toBeVisible({ timeout: 15_000 });

	// identified study: the manager (Ana) answers about her report (Marko)
	await page.goto('/app/studies/new');
	await page.fill('#name', studyName);
	await page.getByRole('button', { name: 'Register study' }).click();
	await page.waitForURL('**/app/studies/*');
	const items = page.locator('.item-row input[aria-label^="Item"]');
	await items.nth(0).fill('I would recommend this workplace.');
	await page.locator('.item-row').nth(2).getByRole('button', { name: /Remove item/ }).click();
	await page.locator('.item-row').nth(1).getByRole('button', { name: /Remove item/ }).click();
	await page.getByRole('button', { name: /Create questionnaire/ }).click();

	const waveInput = page.locator('.add-wave input');
	await expect(waveInput).toBeVisible({ timeout: 20_000 });
	await waveInput.fill('Manager view');
	await page.locator('.add-wave select').first().selectOption('identified');
	await page.getByRole('button', { name: 'Add wave' }).click();

	await page.getByRole('button', { name: 'Recipients' }).click();
	await page.getByLabel('Recipient rule').selectOption('manager_review');
	await page.getByRole('button', { name: 'Save recipients' }).click();
	await expect(page.locator('.recipients-note').first()).toContainText(/resolved|saved/i, {
		timeout: 15_000
	});

	const launch = page.getByRole('button', { name: /Launch Manager view/ });
	await expect(launch).toBeVisible({ timeout: 20_000 });
	await launch.click();
	await page.getByRole('dialog').getByRole('button', { name: 'Launch' }).click();
	await expect(page.locator('#waves')).toContainText('Live', { timeout: 20_000 });

	// Ana answers her queue
	await page.getByRole('link', { name: 'Field', exact: true }).click();
	await page.getByRole('button', { name: 'Create respondent links' }).click();
	const anaRow = page.locator('.queue-links li', { hasText: 'E2E 360 Ana' });
	await expect(anaRow).toBeVisible({ timeout: 15_000 });
	const url = (await anaRow.locator('.minted-url').textContent())?.trim();

	const phone = await browser.newContext({ viewport: { width: 390, height: 844 } });
	const respondent = await phone.newPage();
	await respondent.goto(url!);
	await respondent.waitForSelector('html[data-hydrated="true"]');
	await respondent.getByRole('checkbox').check();
	await respondent.getByRole('button', { name: 'Begin' }).click();
	// answer every assignment in the queue until it collapses to the thank-you state
	for (let guard = 0; guard < 10; guard++) {
		await respondent.waitForFunction(
			() =>
				document.querySelector('.queue-start') !== null ||
				(document.querySelector('h1')?.textContent ?? '').includes('Thank you'),
			undefined,
			{ timeout: 20_000 }
		);
		if (await respondent.locator('h1', { hasText: 'Thank you' }).isVisible().catch(() => false)) {
			break;
		}
		await respondent.locator('.queue-start').first().click();
		await respondent.locator('.item').first().getByRole('radio', { name: '4' }).click();
		await respondent.getByRole('button', { name: 'Submit answers' }).click();
	}
	await expect(respondent.locator('h1')).toContainText('Thank you', { timeout: 20_000 });
	await phone.close();

	// record the withdrawal request for Ana (the respondent), approve, execute
	await page.goto('/app/privacy');
	await page.getByRole('button', { name: 'Record a request' }).click();
	await page.getByLabel('Person').selectOption({ label: 'E2E 360 Ana' });
	await page.getByRole('button', { name: 'Record request' }).click();
	await expect(page.locator('.note')).toContainText('recorded', { timeout: 15_000 });

	const requestRow = page
		.locator('tbody tr', { hasText: 'E2E 360 Ana' })
		.filter({ has: page.getByRole('button', { name: 'Approve' }) })
		.first();
	await requestRow.getByRole('button', { name: 'Approve' }).click();
	const approvedRow = page
		.locator('tbody tr', { hasText: 'E2E 360 Ana' })
		.filter({ has: page.getByRole('button', { name: 'Execute' }) })
		.first();
	await expect(approvedRow).toBeVisible({ timeout: 15_000 });
	await approvedRow.getByRole('button', { name: 'Execute' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Execute' }).click();
	await expect(page.locator('.note')).toContainText('Executed', { timeout: 20_000 });
	await expect(page.locator('tbody tr', { hasText: 'E2E 360 Ana' }).first()).toContainText(
		'Completed'
	);

	// the lifecycle surfaced as system events; mark them read
	await page.getByRole('button', { name: 'System events' }).click();
	await expect(page.locator('.bell-list li').first()).toBeVisible({ timeout: 15_000 });
	const markAll = page.getByRole('button', { name: 'Mark all read' });
	if (await markAll.isVisible().catch(() => false)) {
		await markAll.click();
	}

	// clean up the study
	await page.goto('/app/studies');
	await page.getByLabel('Find a study').fill(studyName);
	await page.locator('.bucket a.name').first().click();
	await page.getByRole('button', { name: 'Archive study' }).click();
	await page.getByRole('dialog').getByRole('button', { name: 'Archive' }).click();
	await expect(page.getByRole('button', { name: 'Restore from archive' })).toBeVisible({
		timeout: 15_000
	});
});
