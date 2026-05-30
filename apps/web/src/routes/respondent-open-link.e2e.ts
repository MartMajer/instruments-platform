import { expect, test, type Page } from '@playwright/test';

test('keeps public respondent route outside the authenticated product shell', async ({ page }) => {
	let sessionChecks = 0;

	await page.route('**/auth/session', async (route) => {
		sessionChecks += 1;
		await route.fulfill({ status: 500, json: { title: 'Unexpected session check' } });
	});
	await page.route('**/respondent/open-links/*', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await expect(page.getByRole('heading', { name: 'Wave 1' })).toBeVisible();
	await expect(page.getByRole('navigation', { name: 'Product surfaces' })).toHaveCount(0);
	await expect(page.getByRole('navigation', { name: 'Setup stages' })).toHaveCount(0);
	await expect(page.getByRole('link', { name: 'Campaign series' })).toHaveCount(0);
	await expect(page.getByRole('link', { name: 'Setup' })).toHaveCount(0);
	await expect(page.getByRole('link', { name: 'Reports' })).toHaveCount(0);
	expect(sessionChecks).toBe(0);
});

test('submits a public open-link response without setup auth headers', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions/{id}/answers');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().postDataJSON()).toEqual({
			answers: [
				{
					questionId: sampleOpenLinkEntry.questions[0].id,
					value: '4',
					isSkipped: false
				}
			]
		});
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions/{id}/submit');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().postDataJSON()).toEqual({
			locale: 'en',
			acceptedConsentDocumentId: consentDocumentId,
			acceptedGrants: ['data_processing', 'research_participation']
		});
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		calls.push('/respondent/open-links/{token}');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await expect(page.getByRole('heading', { name: 'Wave 1' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Default participant disclosure' })).toBeVisible();
	await expect(page.getByTestId('respondent-question-runner')).toHaveCount(0);
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(page.getByText('After work, I need time to recover mentally.')).not.toBeVisible();
	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await expect(page.getByTestId('respondent-question-runner')).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(respondentRunner(page).locator('.answer-option__label')).toHaveCount(0);
	await expect(respondentRunner(page).locator('.scale-anchors span')).toContainText([
		'Strongly disagree',
		'Strongly agree'
	]);
	await expect(page.getByRole('button', { name: 'Complete' })).toHaveCount(0);
	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	const receipt = page.getByRole('region', { name: 'Response receipt' });
	await expect(receipt.getByRole('heading', { name: 'Response submitted' })).toBeVisible();
	await expect(receipt.getByText('Your response for Wave 1 was received.')).toBeVisible();
	await expect(receipt.getByText('May 7, 2026, 2:05 PM')).toBeVisible();
	await expect(receipt.getByText('Response mode')).toBeVisible();
	await expect(receipt.getByText('anonymous', { exact: true })).toBeVisible();
	await expect(receipt.getByText('Consent version')).toBeVisible();
	await expect(receipt.getByText('1.0.0')).toBeVisible();
	await expect(receipt.getByText('Answers received')).toBeVisible();
	await expect(receipt.getByText('1', { exact: true })).toBeVisible();
	await expect(receipt.getByText('This page does not show scores or interpretation.')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions',
		'/respondent/open-links/{token}/sessions/{id}/answers',
		'/respondent/open-links/{token}/sessions/{id}/submit'
	]);
});

test('saves answers for review before final submit', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions/{id}/answers');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().postDataJSON()).toEqual({
			answers: [
				{
					questionId: sampleOpenLinkEntry.questions[0].id,
					value: '4',
					isSkipped: false
				}
			]
		});
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions/{id}/submit');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		calls.push('/respondent/open-links/{token}');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();

	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await expect(page.getByText(`Session ${sessionId}`)).toBeVisible();
	await expect(page.getByText('1 answer saved')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions',
		'/respondent/open-links/{token}/sessions/{id}/answers'
	]);

	await page.getByRole('button', { name: 'Back to edit' }).click();
	await expectLikertAnswer(page, 4);

	await page.getByRole('button', { name: 'Review response' }).click();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions',
		'/respondent/open-links/{token}/sessions/{id}/answers',
		'/respondent/open-links/{token}/sessions/{id}/answers',
		'/respondent/open-links/{token}/sessions/{id}/submit'
	]);
});

test('shows one first-party respondent question at a time', async ({ page }) => {
	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		await route.fulfill({ json: { sessionId, savedAnswerCount: 2 } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleTwoQuestionOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await expect(page.getByTestId('respondent-question-runner')).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(page.getByText('After work, I need time to recover mentally.')).toBeVisible();
	await expect(page.getByText('I can detach from work during breaks.')).not.toBeVisible();

	await page.getByRole('button', { name: /^4\b/ }).click();

	await expect(page.getByText('After work, I need time to recover mentally.')).not.toBeVisible();
	await expect(page.getByText('I can detach from work during breaks.')).toBeVisible();
	await page.getByRole('button', { name: /^5\b/ }).click();
	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
});

test('restores saved respondent draft after reload', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/respondent/open-links/*/sessions/*/draft', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions/{id}/draft');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(`/sessions/${sessionId}/draft`);
		await route.fulfill({
			json: {
				session: sampleSession,
				answers: [
					{
						questionId: sampleOpenLinkEntry.questions[0].id,
						value: '4',
						comment: null,
						isSkipped: false,
						isNa: false
					}
				],
				savedAnswerCount: 1
			}
		});
	});

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions/{id}/answers');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		calls.push('/respondent/open-links/{token}');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);
	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();

	await page.reload();

	await expectLikertAnswer(page, 4);
	await expect(page.getByText('Answers saved')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions',
		'/respondent/open-links/{token}/sessions/{id}/answers',
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions/{id}/draft'
	]);
});

test('removes stale respondent draft pointer and keeps consent usable', async ({ page }) => {
	const storageKey = `respondent-session:${assignmentId}`;
	let draftRequests = 0;

	await page.addInitScript(
		([key, value]) => sessionStorage.setItem(key, value),
		[storageKey, sessionId]
	);

	await page.route('**/respondent/open-links/*/sessions/*/draft', async (route) => {
		draftRequests += 1;
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({
			status: 404,
			json: {
				title: 'Response session not found',
				detail: 'Response session was not found.'
			}
		});
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await expect(page.getByRole('heading', { name: 'Default participant disclosure' })).toBeVisible();
	await expect(page.getByRole('alert')).toHaveCount(0);
	await expect
		.poll(() => page.evaluate((key) => sessionStorage.getItem(key), storageKey))
		.toBeNull();
	expect(draftRequests).toBe(1);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await expect(page.getByTestId('respondent-question-runner')).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(respondentScaleButton(page, 4)).toBeVisible();
});

test('restores submitted respondent draft to receipt after reload', async ({ page }) => {
	const storageKey = `respondent-session:${assignmentId}`;
	const submittedAt = '2026-05-07T12:05:00Z';
	const calls: string[] = [];

	await page.addInitScript(
		([key, value]) => sessionStorage.setItem(key, value),
		[storageKey, sessionId]
	);

	await page.route('**/respondent/open-links/*/sessions/*/draft', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions/{id}/draft');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({
			json: {
				session: {
					...sampleSession,
					submittedAt
				},
				answers: [
					{
						questionId: sampleOpenLinkEntry.questions[0].id,
						value: '4',
						comment: null,
						isSkipped: false,
						isNa: false
					}
				],
				savedAnswerCount: 1
			}
		});
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		calls.push('/respondent/open-links/{token}');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	const receipt = page.getByRole('region', { name: 'Response receipt' });
	await expect(receipt.getByRole('heading', { name: 'Response submitted' })).toBeVisible();
	await expect(receipt.getByText('Your response for Wave 1 was received.')).toBeVisible();
	await expect(receipt.getByText('May 7, 2026, 2:05 PM')).toBeVisible();
	await expect(receipt.getByText('Answers received')).toBeVisible();
	await expect(receipt.getByText('1', { exact: true })).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Review response' })).toHaveCount(0);
	expect(calls).toEqual([
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions/{id}/draft'
	]);
});

test('public session handle scrubs raw token URL and uses handle endpoints', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/respondent/public-sessions/*/answers', async (route) => {
		calls.push('/respondent/public-sessions/{handle}/answers');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(publicSessionHandle);
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/public-sessions/*/submit', async (route) => {
		calls.push('/respondent/public-sessions/{handle}/submit');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(publicSessionHandle);
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		calls.push('/respondent/open-links/{token}/sessions');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({
			status: 201,
			json: {
				...sampleSession,
				publicHandle: publicSessionHandle
			}
		});
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		calls.push('/respondent/open-links/{token}');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await expect
		.poll(() => new URL(page.url()).pathname)
		.toBe(`/r/${publicSessionHandle}`);

	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions',
		'/respondent/public-sessions/{handle}/answers',
		'/respondent/public-sessions/{handle}/submit'
	]);
});

test('identified entry token uses identified endpoints then public session handle', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({
			status: 500,
			json: {
				title: 'Unexpected open-link call',
				detail: 'Identified entry should not resolve through open-link endpoints.'
			}
		});
	});

	await page.route('**/respondent/public-sessions/*/answers', async (route) => {
		calls.push('/respondent/public-sessions/{handle}/answers');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(publicSessionHandle);
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/public-sessions/*/submit', async (route) => {
		calls.push('/respondent/public-sessions/{handle}/submit');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(publicSessionHandle);
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/identified-entries/*/sessions', async (route) => {
		calls.push('/respondent/identified-entries/{token}/sessions');
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().postDataJSON()).toEqual({
			locale: 'en',
			acceptedConsentDocumentId: consentDocumentId,
			acceptedGrants: ['data_processing', 'research_participation']
		});
		await route.fulfill({
			status: 201,
			json: {
				...sampleSession,
				publicHandle: publicSessionHandle
			}
		});
	});

	await page.route('**/respondent/identified-entries/*', async (route) => {
		calls.push('/respondent/identified-entries/{token}');
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleIdentifiedEntry });
	});

	await page.goto(`/r/${identifiedEntryToken}`);

	await expect(page.getByRole('heading', { name: 'Identified wave' })).toBeVisible();
	await expect(page.getByText('identified', { exact: true })).toBeVisible();
	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await expect
		.poll(() => new URL(page.url()).pathname)
		.toBe(`/r/${publicSessionHandle}`);

	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/identified-entries/{token}',
		'/respondent/identified-entries/{token}/sessions',
		'/respondent/public-sessions/{handle}/answers',
		'/respondent/public-sessions/{handle}/submit'
	]);
});

test('public session handle route restores draft without raw token entry call', async ({ page }) => {
	let rawEntryRequests = 0;

	await page.route('**/respondent/open-links/*', async (route) => {
		rawEntryRequests += 1;
		await route.fulfill({
			status: 500,
			json: {
				title: 'Unexpected raw token entry call',
				detail: 'Handle route should not resolve open-link token entry.'
			}
		});
	});

	await page.route('**/respondent/public-sessions/*/draft', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(publicSessionHandle);
		await route.fulfill({
			json: {
				entry: sampleOpenLinkEntry,
				session: {
					...sampleSession,
					publicHandle: publicSessionHandle
				},
				answers: [
					{
						questionId: sampleOpenLinkEntry.questions[0].id,
						value: '4',
						comment: null,
						isSkipped: false,
						isNa: false
					}
				],
				savedAnswerCount: 1
			}
		});
	});

	await page.goto(`/r/${publicSessionHandle}`);

	await expect(page.getByRole('heading', { name: 'Wave 1' })).toBeVisible();
	await expectLikertAnswer(page, 4);
	await expect(page.getByText('Answers saved')).toBeVisible();
	expect(rawEntryRequests).toBe(0);
});

test('restores unsaved local answers after failed public-handle save and reload', async ({
	page
}) => {
	const participantCode = 'alpha-001';
	const participantCodeEntry = {
		...sampleOpenLinkEntry,
		responseIdentityMode: 'anonymous_longitudinal',
		requiresParticipantCode: true
	};
	let saveRequests = 0;

	await page.route('**/respondent/public-sessions/*/answers', async (route) => {
		saveRequests += 1;
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(publicSessionHandle);

		if (saveRequests === 1) {
			await route.fulfill({
				status: 503,
				json: {
					title: 'Save failed',
					detail: 'Answers could not be saved.'
				}
			});
			return;
		}

		expect(route.request().postDataJSON()).toEqual({
			answers: [
				{
					questionId: sampleOpenLinkEntry.questions[0].id,
					value: '5',
					isSkipped: false
				}
			]
		});
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/public-sessions/*/draft', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({
			json: {
				entry: participantCodeEntry,
				session: {
					...sampleSession,
					publicHandle: publicSessionHandle
				},
				answers: [
					{
						questionId: sampleOpenLinkEntry.questions[0].id,
						value: '4',
						comment: null,
						isSkipped: false,
						isNa: false
					}
				],
				savedAnswerCount: 1
			}
		});
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({
			status: 201,
			json: {
				...sampleSession,
				publicHandle: publicSessionHandle
			}
		});
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: participantCodeEntry });
	});

	await page.goto(`/r/${openLinkToken}`);
	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByLabel('Participant code').fill(participantCode);
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 5);
	await expect(page.getByText('Answers are not saved yet. Review will retry before submit.')).toBeVisible();
	await expect(page.getByRole('alert')).toHaveCount(0);
	const storedValues = await page.evaluate(() => Object.values(sessionStorage).join('\n'));
	expect(storedValues).not.toContain(openLinkToken);
	expect(storedValues).not.toContain(participantCode);

	await page.reload();

	await expectLikertAnswer(page, 5);
	await expect(page.getByText('Unsaved answers restored on this device')).toBeVisible();

	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	expect(saveRequests).toBe(2);
});

test('restores local unsaved draft when public draft read fails', async ({ page }) => {
	let rawEntryRequests = 0;

	await page.addInitScript(
		({ key, draft }) => sessionStorage.setItem(key, JSON.stringify(draft)),
		{
			key: `respondent-unsaved-draft:${publicSessionHandle}`,
			draft: {
				version: 1,
				publicHandle: publicSessionHandle,
				sessionId,
				assignmentId,
				updatedAt: '2026-05-07T12:04:00Z',
				entry: sampleOpenLinkEntry,
				answers: {
					[sampleOpenLinkEntry.questions[0].id]: '5'
				}
			}
		}
	);

	await page.route('**/respondent/open-links/*', async (route) => {
		rawEntryRequests += 1;
		await route.fulfill({
			status: 500,
			json: {
				title: 'Unexpected raw token entry call',
				detail: 'Handle route should not resolve open-link token entry.'
			}
		});
	});

	await page.route('**/respondent/public-sessions/*/draft', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		expect(route.request().url()).toContain(publicSessionHandle);
		await route.fulfill({
			status: 503,
			json: {
				title: 'Draft unavailable',
				detail: 'Saved draft could not be loaded.'
			}
		});
	});

	await page.goto(`/r/${publicSessionHandle}`);

	await expect(page.getByRole('heading', { name: 'Wave 1' })).toBeVisible();
	await expectLikertAnswer(page, 5);
	await expect(page.getByText('Unsaved answers restored on this device')).toBeVisible();
	expect(rawEntryRequests).toBe(0);
});

test('autosaves changed respondent answers after session creation', async ({ page }) => {
	const savedPayloads: unknown[] = [];

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		savedPayloads.push(route.request().postDataJSON());
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		assertNoSetupAuthHeaders(route.request().headers());
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 4);

	await expect.poll(() => savedPayloads).toEqual([
		{
			answers: [
				{
					questionId: sampleOpenLinkEntry.questions[0].id,
					value: '4',
					isSkipped: false
				}
			]
		}
	]);
	await expect(page.getByText('Answers saved')).toBeVisible();
});

test('warns before leaving with unsaved respondent answers', async ({ page }) => {
	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 4);

	const blockedWhileDirty = await dispatchBeforeUnload(page);
	expect(blockedWhileDirty).toBe(true);

	await expect(page.getByText('Answers saved')).toBeVisible();

	const blockedAfterSave = await dispatchBeforeUnload(page);
	expect(blockedAfterSave).toBe(false);
});

test('invalidates reviewed answers after editing', async ({ page }) => {
	const savedPayloads: unknown[] = [];

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		savedPayloads.push(route.request().postDataJSON());
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();

	await page.getByRole('button', { name: 'Back to edit' }).click();
	await answerLikert(page, 5);

	await expect(page.getByText('Unsaved changes')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Submit reviewed response' })).toHaveCount(0);
	await expect(page.getByText('Answers saved')).toBeVisible();
	await expect.poll(() => savedPayloads.at(-1)).toEqual({
		answers: [
			{
				questionId: sampleOpenLinkEntry.questions[0].id,
				value: '5',
				isSkipped: false
			}
		]
	});

	await page.getByRole('button', { name: 'Review response' }).click();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();
	await expect(page.getByText('Response submitted')).toBeVisible();
});

test('failed autosave preserves respondent answers for manual retry', async ({ page }) => {
	let saveRequests = 0;

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		saveRequests += 1;

		if (saveRequests === 1) {
			await route.fulfill({
				status: 503,
				json: {
					title: 'Save failed',
					detail: 'Answers could not be saved.'
				}
			});
			return;
		}

		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 5);

	await expect(page.getByText('Answers are not saved yet. Review will retry before submit.')).toBeVisible();
	await expect(page.getByRole('alert')).toHaveCount(0);
	await expectLikertAnswer(page, 5);

	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	expect(saveRequests).toBe(2);
});

test('does not expose raw fetch failures after background autosave fails', async ({ page }) => {
	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		await route.abort('failed');
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 5);

	await expect(page.getByText('Answers are not saved yet. Review will retry before submit.')).toBeVisible();
	await expect(page.getByText('Failed to fetch')).toHaveCount(0);
	await expect(page.getByRole('alert')).toHaveCount(0);

	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('alert')).toContainText('Connection problem. Try again before submitting.');
	await expect(page.getByText('Failed to fetch')).toHaveCount(0);
});

test('requires participant code before starting anonymous longitudinal open-link response', async ({
	page
}) => {
	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		expect(route.request().postDataJSON()).toEqual({
			locale: 'en',
			acceptedConsentDocumentId: consentDocumentId,
			acceptedGrants: ['data_processing', 'research_participation'],
			participantCode: 'alpha-001'
		});
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({
			json: {
				...sampleOpenLinkEntry,
				responseIdentityMode: 'anonymous_longitudinal',
				requiresParticipantCode: true
			}
		});
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await expect(page.getByRole('button', { name: 'Continue' })).toBeDisabled();
	await page.getByLabel('Participant code').fill('  alpha-001  ');
	await page.getByRole('button', { name: 'Continue' }).click();
	await expect(page.getByTestId('respondent-question-runner')).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(respondentScaleButton(page, 4)).toBeVisible();
	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	const receipt = page.getByRole('region', { name: 'Response receipt' });
	await expect(receipt.getByText('anonymous longitudinal')).toBeVisible();
	await expect(
		receipt.getByText('Keep your participant code. The platform cannot recover it later.')
	).toBeVisible();
});

test('retries public entry load after failure', async ({ page }) => {
	let entryRequests = 0;

	await page.route('**/respondent/open-links/*', async (route) => {
		entryRequests += 1;

		if (entryRequests === 1) {
			await route.fulfill({
				status: 500,
				json: {
					title: 'Temporary entry failure',
					detail: 'The survey link could not be loaded yet.'
				}
			});
			return;
		}

		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await expect(page.getByText('The survey link could not be loaded yet.')).toBeVisible();
	await page.getByRole('button', { name: 'Try again' }).click();
	await expect(page.getByRole('heading', { name: 'Wave 1' })).toBeVisible();
	expect(entryRequests).toBe(2);
});

test('keeps consent visible after session failure', async ({ page }) => {
	let sessionRequests = 0;

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		sessionRequests += 1;
		assertNoSetupAuthHeaders(route.request().headers());

		if (sessionRequests === 1) {
			await route.fulfill({
				status: 400,
				json: {
					title: 'Consent failed',
					detail: 'Consent could not be recorded.'
				}
			});
			return;
		}

		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await expect(page.getByRole('heading', { name: 'Default participant disclosure' })).toBeVisible();
	await expect(page.getByRole('alert')).toContainText('Consent could not be recorded.');

	await page.getByRole('button', { name: 'Continue' }).click();
	await expect(page.getByText('After work, I need time to recover mentally.')).toBeVisible();
	expect(sessionRequests).toBe(2);
});

test('validates respondent answers before review', async ({ page }) => {
	let saveRequests = 0;
	let submitRequests = 0;

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		saveRequests += 1;
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		submitRequests += 1;
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleNumberValidationEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('alert')).toContainText('q01 requires an answer.');
	expect(saveRequests).toBe(0);
	expect(submitRequests).toBe(0);

	const answer = respondentNumberAnswer(page);
	await answer.fill('9');
	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('alert')).toContainText('q01 must be between 1 and 5.');
	expect(saveRequests).toBe(0);
	expect(submitRequests).toBe(0);

	await answer.fill('4');
	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	expect(saveRequests).toBe(1);
	expect(submitRequests).toBe(0);
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();
	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(saveRequests).toBe(1);
	expect(submitRequests).toBe(1);
});

test('requires a selected answer for multi-choice questions before review', async ({ page }) => {
	let saveRequests = 0;

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		saveRequests += 1;
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleMultiChoiceRequiredEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await page.getByRole('button', { name: 'Quiet room' }).click();
	await page.getByRole('button', { name: 'Quiet room' }).click();
	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('alert')).toContainText('q01 requires an answer.');
	expect(saveRequests).toBe(0);
});

test('retries respondent save without losing answers', async ({ page }) => {
	let saveRequests = 0;
	let submitRequests = 0;

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		saveRequests += 1;

		if (saveRequests === 1) {
			await route.fulfill({
				status: 503,
				json: {
					title: 'Save failed',
					detail: 'Answers could not be saved.'
				}
			});
			return;
		}

		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		submitRequests += 1;
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	await answerLikert(page, 5);
	await page.getByRole('button', { name: 'Review response' }).click();

	await expect(page.getByRole('alert')).toContainText('Answers could not be saved.');
	await expectLikertAnswer(page, 5);

	await page.getByRole('button', { name: 'Review response' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();
	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(saveRequests).toBe(2);
	expect(submitRequests).toBe(1);
});

test('retries reviewed submit without leaving review state', async ({ page }) => {
	let submitRequests = 0;

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		submitRequests += 1;

		if (submitRequests === 1) {
			await route.fulfill({
				status: 503,
				json: {
					title: 'Submit failed',
					detail: 'Response could not be submitted.'
				}
			});
			return;
		}

		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await answerLikert(page, 4);
	await page.getByRole('button', { name: 'Review response' }).click();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	await expect(page.getByRole('alert')).toContainText('Response could not be submitted.');
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await expect(page.getByText(`Session ${sessionId}`)).toBeVisible();

	await page.getByRole('button', { name: 'Submit reviewed response' }).click();
	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(submitRequests).toBe(2);
});

test('keeps respondent flow usable on mobile', async ({ page }) => {
	await page.setViewportSize({ width: 390, height: 844 });

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});

	await page.route('**/respondent/open-links/*/sessions/*/submit', async (route) => {
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});

	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		expect(route.request().postDataJSON()).toEqual({
			locale: 'en',
			acceptedConsentDocumentId: consentDocumentId,
			acceptedGrants: ['data_processing', 'research_participation'],
			participantCode: 'mobile-001'
		});
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({
			json: {
				...sampleOpenLinkEntry,
				responseIdentityMode: 'anonymous_longitudinal',
				requiresParticipantCode: true
			}
		});
	});

	await page.goto(`/r/${openLinkToken}`);

	await expect(page.getByRole('heading', { name: 'Wave 1' })).toBeVisible();
	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByLabel('Participant code').fill('mobile-001');

	const continueButton = page.getByRole('button', { name: 'Continue' });
	await expect(continueButton).toBeVisible();
	await expect(async () => {
		const box = await continueButton.boundingBox();
		expect(box?.width).toBeGreaterThanOrEqual(320);
	}).toPass();

	await continueButton.click();
	await expect(page.getByTestId('respondent-question-runner')).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(respondentScaleButton(page, 4)).toBeVisible();

	const saveButton = page.getByRole('button', { name: 'Review response' });
	await expect(async () => {
		const box = await saveButton.boundingBox();
		expect(box?.width).toBeGreaterThanOrEqual(320);
	}).toPass();

	await answerLikert(page, 4);
	await saveButton.click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	const submitButton = page.getByRole('button', { name: 'Submit reviewed response' });
	await expect(async () => {
		const box = await submitButton.boundingBox();
		expect(box?.width).toBeGreaterThanOrEqual(320);
	}).toPass();
	await submitButton.click();
	await expect(page.getByText('Response submitted')).toBeVisible();
});

const campaignId = 'b9514b8e-ecbc-4085-bc44-c6f377152f32';
const assignmentId = 'a8f5d2e2-48b8-4651-9d61-163381d8e4a9';
const sessionId = '6c361316-9447-4644-a41f-b048d80d3d1b';
const templateVersionId = 'f6d8cc42-b721-4789-a933-83895f5d064e';
const consentDocumentId = 'ff4e1864-f691-4e26-a198-c8fa72d29bd9';
const openLinkToken =
	'opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ';
const identifiedEntryToken =
	'idn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ';
const publicSessionHandle =
	'rsh_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ';

const sampleOpenLinkEntry = {
	campaignId,
	assignmentId,
	templateVersionId,
	name: 'Wave 1',
	status: 'live',
	responseIdentityMode: 'anonymous',
	requiresParticipantCode: false,
	defaultLocale: 'en',
	consentDocument: {
		id: consentDocumentId,
		locale: 'en',
		version: '1.0.0',
		title: 'Default participant disclosure',
		bodyMarkdown:
			'By continuing, the participant confirms the tenant may collect anonymous survey responses.',
		requiredGrants: ['data_processing', 'research_participation'],
		optionalGrants: []
	},
	questions: [
		{
			id: '07d19d0a-8417-41d7-8b36-5147d96ad7f8',
			ordinal: 1,
			code: 'q01',
			type: 'likert',
			textDefault: 'After work, I need time to recover mentally.',
			required: true,
			scaleMinValue: 1,
			scaleMaxValue: 5,
			scaleAnchors: JSON.stringify([
				{ value: 1, label: 'Strongly disagree' },
				{ value: 5, label: 'Strongly agree' }
			])
		}
	]
};

const sampleIdentifiedEntry = {
	...sampleOpenLinkEntry,
	name: 'Identified wave',
	responseIdentityMode: 'identified'
};

const sampleNumberValidationEntry = {
	...sampleOpenLinkEntry,
	questions: [
		{
			id: sampleOpenLinkEntry.questions[0].id,
			ordinal: 1,
			code: 'q01',
			type: 'number',
			textDefault: 'How many recovery hours did you get yesterday?',
			required: true,
			payload: JSON.stringify({
				validation: { min: 1, max: 5, integerOnly: true }
			})
		}
	]
};

const sampleTwoQuestionOpenLinkEntry = {
	...sampleOpenLinkEntry,
	questions: [
		...sampleOpenLinkEntry.questions,
		{
			id: '46bc58e2-4e7d-4dc1-a3bb-f1767974fbb9',
			ordinal: 2,
			code: 'q02',
			type: 'likert',
			textDefault: 'I can detach from work during breaks.',
			required: true,
			scaleMinValue: 1,
			scaleMaxValue: 5,
			scaleAnchors: JSON.stringify([
				{ value: 1, label: 'Strongly disagree' },
				{ value: 5, label: 'Strongly agree' }
			])
		}
	]
};

const sampleMultiChoiceRequiredEntry = {
	...sampleOpenLinkEntry,
	questions: [
		{
			id: sampleOpenLinkEntry.questions[0].id,
			ordinal: 1,
			code: 'q01',
			type: 'multi',
			textDefault: 'Which recovery supports did you use this week?',
			required: true,
			payload: JSON.stringify({
				options: [
					{ code: 'o01', label: 'Quiet room' },
					{ code: 'o02', label: 'Supervisor check-in' }
				]
			})
		}
	]
};

const sampleSession = {
	id: sessionId,
	assignmentId,
	locale: 'en',
	startedAt: '2026-05-07T12:00:00Z',
	submittedAt: null,
	timeTakenMs: null
};

function assertNoSetupAuthHeaders(headers: Record<string, string>) {
	expect(headers['x-tenant-id']).toBeUndefined();
	expect(headers['x-dev-user-id']).toBeUndefined();
	expect(headers['x-dev-tenant-memberships']).toBeUndefined();
	expect(headers['x-dev-permissions']).toBeUndefined();
	expect(headers['x-test-user-id']).toBeUndefined();
}

function respondentRunner(page: Page) {
	return page.getByTestId('respondent-question-runner');
}

function respondentScaleButton(page: Page, value: number) {
	return respondentRunner(page).getByRole('button', { name: new RegExp(`^${value}\\b`) });
}

async function answerLikert(page: Page, value: number) {
	await respondentScaleButton(page, value).click();
}

async function expectLikertAnswer(page: Page, value: number) {
	await expect(respondentScaleButton(page, value)).toHaveAttribute('aria-pressed', 'true');
}

function respondentNumberAnswer(page: Page) {
	return respondentRunner(page).getByRole('spinbutton');
}

async function dispatchBeforeUnload(page: Page) {
	return page.evaluate(() => {
		const event = new Event('beforeunload', { cancelable: true });
		window.dispatchEvent(event);
		return event.defaultPrevented;
	});
}
