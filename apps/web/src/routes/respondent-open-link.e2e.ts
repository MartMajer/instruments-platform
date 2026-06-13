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
					value: '4'
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
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(page.getByText('After work, I need time to recover mentally.')).not.toBeVisible();
	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await expect(page.getByTestId('surveyjs-runtime')).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime').locator('.sd-root-modern')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Complete' })).toHaveCount(0);
	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	const receipt = page.getByRole('region', { name: 'Response receipt' });
	await expect(receipt.getByRole('heading', { name: 'Response submitted' })).toBeVisible();
	await expect(receipt.getByText('Your response for Wave 1 was received.')).toBeVisible();
	await expect(receipt.getByText('2026-05-07T12:05:00Z')).toBeVisible();
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

test('hides and skips conditional follow-up until source answer matches', async ({ page }) => {
	const sourceQuestionId = '13f0dc3f-4cda-4d78-b855-5e09f345a4a1';
	const followUpQuestionId = 'cbf85834-5935-4378-bf1e-770531c66fc4';
	const conditionalEntry = {
		...sampleOpenLinkEntry,
		questions: [
			{
				id: sourceQuestionId,
				ordinal: 1,
				code: 'q01',
				type: 'single',
				textDefault: 'Did you handle urgent work today?',
				required: true,
				payload: JSON.stringify({
					options: [
						{ code: 'o01', label: 'No' },
						{ code: 'o02', label: 'Yes' }
					]
				})
			},
			{
				id: followUpQuestionId,
				ordinal: 2,
				code: 'q02',
				type: 'number',
				textDefault: 'Rate disruption severity.',
				required: true,
				scaleMinValue: 1,
				scaleMaxValue: 5,
				payload: JSON.stringify({
					validation: {
						min: 1,
						max: 5,
						integerOnly: true
					},
					displayLogic: {
						mode: 'show_when',
						sourceQuestionCode: 'q01',
						operator: 'equals',
						value: 'o02',
						requiredWhenVisible: true
					}
				})
			}
		]
	};
	const savedPayloads: unknown[] = [];

	await page.route('**/respondent/open-links/*/sessions/*/answers', async (route) => {
		const payload = route.request().postDataJSON();
		savedPayloads.push(payload);
		const answers = Array.isArray(payload.answers)
			? (payload.answers as Array<{ isSkipped?: boolean; value?: unknown }>)
			: [];
		await route.fulfill({
			json: {
				sessionId,
				savedAnswerCount: answers.filter((answer) => !answer.isSkipped && answer.value !== null).length
			}
		});
	});
	await page.route('**/respondent/open-links/*/sessions', async (route) => {
		await route.fulfill({ status: 201, json: sampleSession });
	});
	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({ json: conditionalEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	const survey = page.getByTestId('surveyjs-runtime');
	await expect(survey.getByText('Did you handle urgent work today?')).toBeVisible();
	await expect(survey.getByText('Rate disruption severity.')).not.toBeVisible();

	await survey.getByText('No', { exact: true }).click();
	await expect(survey.getByText('Rate disruption severity.')).not.toBeVisible();
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await expect.poll(() => savedPayloads.at(-1)).toEqual({
		answers: [
			{
				questionId: sourceQuestionId,
				value: '"o01"',
				isSkipped: false
			},
			{
				questionId: followUpQuestionId,
				value: null,
				isSkipped: true
			}
		]
	});

	await page.getByRole('button', { name: 'Back to edit' }).click();
	await survey.getByText('Yes', { exact: true }).click();
	await expect(survey.getByText('Rate disruption severity.')).toBeVisible();
	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect.poll(() => savedPayloads.at(-1)).toEqual({
		answers: [
			{
				questionId: sourceQuestionId,
				value: '"o02"',
				isSkipped: false
			},
			{
				questionId: followUpQuestionId,
				value: '4',
				isSkipped: false
			}
		]
	});
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
					value: '4'
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
	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();

	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await expect(page.getByText(`Session ${sessionId}`)).toBeVisible();
	await expect(page.getByText('1 answer saved')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/open-links/{token}',
		'/respondent/open-links/{token}/sessions',
		'/respondent/open-links/{token}/sessions/{id}/answers'
	]);

	await page.getByRole('button', { name: 'Back to edit' }).click();
	await expect(respondentAnswer(page)).toHaveValue('4');

	await page.getByRole('button', { name: 'Save and review' }).click();
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
	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();

	await page.reload();

	await expect(respondentAnswer(page)).toHaveValue('4');
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

	await expect(page.getByTestId('surveyjs-runtime')).toBeVisible();
	await expect(respondentAnswer(page)).toBeVisible();
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
	await expect(receipt.getByText(submittedAt)).toBeVisible();
	await expect(receipt.getByText('Answers received')).toBeVisible();
	await expect(receipt.getByText('1', { exact: true })).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Save and review' })).toHaveCount(0);
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

	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
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

	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(calls).toEqual([
		'/respondent/identified-entries/{token}',
		'/respondent/identified-entries/{token}/sessions',
		'/respondent/public-sessions/{handle}/answers',
		'/respondent/public-sessions/{handle}/submit'
	]);
});

test('operations identified queue link opens queue assignment and submits through queue endpoints', async ({
	page
}) => {
	const calls: string[] = [];
	let sessionChecks = 0;

	await page.route('**/auth/session', async (route) => {
		sessionChecks += 1;
		await route.fulfill({
			json: {
				userId: '22222222-2222-4222-8222-222222222222',
				tenantId: '11111111-1111-4111-8111-111111111111',
				email: 'owner@example.test',
				permissions: ['setup.manage']
			}
		});
	});
	await page.route('**/auth/csrf', async (route) => {
		await route.fulfill({ json: { csrfToken: 'test-csrf-token' } });
	});
	await page.route(
		`**/campaign-series/${identifiedQueueSeriesId}/operations-workspace`,
		async (route) => {
			await route.fulfill({ json: sampleIdentifiedOperationsWorkspace });
		}
	);
	await page.route('**/campaigns/*/identified-queue-access', async (route) => {
		calls.push('/campaigns/{id}/identified-queue-access');
		expect(new URL(route.request().url()).pathname).toBe(
			`/campaigns/${campaignId}/identified-queue-access`
		);
		await route.fulfill({
			status: 201,
			json: {
				campaignId,
				respondentCount: 1,
				assignmentCount: 1,
				createdAccessCount: 1,
				existingAccessCount: 0,
				links: [
					{
						invitationTokenId: 'd4d72f1a-d80d-44e5-9df8-1dd483910c64',
						respondentSubjectId: 'f6d54d90-52c4-4996-a275-f3050040955d',
						assignmentCount: 1,
						token: identifiedQueueToken,
						respondentPath: `/r/${identifiedQueueToken}`,
						status: 'created'
					}
				]
			}
		});
	});

	await page.goto(`/app/campaign-series/${identifiedQueueSeriesId}/operations`);
	const operations = page.getByRole('region', { name: 'Collection workspace' });
	const workflow = operations.getByRole('group', { name: 'Study collection flow' });
	const currentStep = workflow.getByRole('region', { name: 'Collection step' });
	await currentStep.getByRole('button', { name: 'Create feedback task links' }).click();
	await workflow.getByRole('button', { name: /Share access.*Done/ }).click();
	await expect(currentStep.getByText(`/r/${identifiedQueueToken}`, { exact: true })).toBeVisible();
	await expect(operations.getByText('Feedback task links ready')).toBeVisible();
	const sessionChecksAfterOperations = sessionChecks;

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({
			status: 500,
			json: {
				title: 'Unexpected open-link call',
				detail: 'Identified queue should not resolve through open-link endpoints.'
			}
		});
	});
	await page.route('**/respondent/identified-queues/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		assertNoSetupAuthHeaders(route.request().headers());

		if (pathname === `/respondent/identified-queues/${identifiedQueueToken}`) {
			calls.push('/respondent/identified-queues/{token}');
			await route.fulfill({ json: sampleIdentifiedQueue });
			return;
		}

		if (
			pathname ===
			`/respondent/identified-queues/${identifiedQueueToken}/assignments/${assignmentId}/sessions`
		) {
			calls.push('/respondent/identified-queues/{token}/assignments/{id}/sessions');
			expect(route.request().postDataJSON()).toEqual({
				locale: 'en',
				acceptedConsentDocumentId: consentDocumentId,
				acceptedGrants: ['data_processing', 'research_participation']
			});
			await route.fulfill({ status: 201, json: sampleSession });
			return;
		}

		if (
			pathname ===
			`/respondent/identified-queues/${identifiedQueueToken}/assignments/${assignmentId}/sessions/${sessionId}/answers`
		) {
			calls.push('/respondent/identified-queues/{token}/assignments/{id}/sessions/{id}/answers');
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
			return;
		}

		if (
			pathname ===
			`/respondent/identified-queues/${identifiedQueueToken}/assignments/${assignmentId}/sessions/${sessionId}/submit`
		) {
			calls.push('/respondent/identified-queues/{token}/assignments/{id}/sessions/{id}/submit');
			await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
			return;
		}

		await route.fulfill({
			status: 404,
			json: {
				title: 'Unexpected queue endpoint',
				detail: pathname
			}
		});
	});

	await page.goto(`/r/${identifiedQueueToken}`);
	await expect(page.getByRole('heading', { name: 'Identified queue wave' })).toBeVisible();
	await expect(
		page.getByText('Feedback tasks for Ana Analyst. Choose one person to give feedback for.')
	).toBeVisible();
	await expect(page.getByRole('region', { name: 'Assigned feedback tasks' })).toBeVisible();
	await page.getByRole('button', { name: /Manager.*Not started/ }).click();
	await expect(page.getByRole('heading', { name: 'Identified queue wave: Manager' })).toBeVisible();
	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();
	await page.getByTestId('surveyjs-runtime').locator('[data-text="4"]').click();
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();

	const receipt = page.getByRole('region', { name: 'Response receipt' });
	await expect(receipt.getByRole('heading', { name: 'Response submitted' })).toBeVisible();
	await expect(receipt.getByText('identified', { exact: true })).toBeVisible();
	await expect.poll(() => new URL(page.url()).pathname).toBe(`/r/${identifiedQueueToken}`);
	expect(sessionChecks).toBe(sessionChecksAfterOperations);
	expect(calls).toEqual(expect.arrayContaining([
		'/campaigns/{id}/identified-queue-access',
		'/respondent/identified-queues/{token}',
		'/respondent/identified-queues/{token}/assignments/{id}/sessions',
		'/respondent/identified-queues/{token}/assignments/{id}/sessions/{id}/answers',
		'/respondent/identified-queues/{token}/assignments/{id}/sessions/{id}/submit'
	]));
});

test('identified queue submitted assignment reopens as receipt without a new session', async ({
	page
}) => {
	const submittedAt = '2026-05-07T12:05:00Z';
	const calls: string[] = [];
	const submittedQueue = {
		...sampleIdentifiedQueue,
		assignments: [
			{
				...sampleIdentifiedQueue.assignments[0],
				status: 'submitted',
				sessionId,
				submittedAt
			}
		]
	};

	await page.route('**/respondent/open-links/*', async (route) => {
		await route.fulfill({
			status: 500,
			json: {
				title: 'Unexpected open-link call',
				detail: 'Identified queue should not resolve through open-link endpoints.'
			}
		});
	});
	await page.route('**/respondent/identified-queues/**', async (route) => {
		const pathname = new URL(route.request().url()).pathname;
		assertNoSetupAuthHeaders(route.request().headers());

		if (pathname === `/respondent/identified-queues/${identifiedQueueToken}`) {
			calls.push('/respondent/identified-queues/{token}');
			await route.fulfill({ json: submittedQueue });
			return;
		}

		if (
			pathname ===
			`/respondent/identified-queues/${identifiedQueueToken}/assignments/${assignmentId}/sessions/${sessionId}/draft`
		) {
			calls.push(
				'/respondent/identified-queues/{token}/assignments/{id}/sessions/{id}/draft'
			);
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
			return;
		}

		await route.fulfill({
			status: 500,
			json: {
				title: 'Unexpected queue endpoint',
				detail: pathname
			}
		});
	});

	await page.goto(`/r/${identifiedQueueToken}`);
	await expect(page.getByRole('heading', { name: 'Identified queue wave' })).toBeVisible();
	await page.getByRole('button', { name: /Manager.*Submitted/ }).click();

	const receipt = page.getByRole('region', { name: 'Response receipt' });
	await expect(receipt.getByRole('heading', { name: 'Response submitted' })).toBeVisible();
	await expect(receipt.getByText('Your response for Identified queue wave: Manager was received.')).toBeVisible();
	await expect(receipt.getByText('May 7, 2026, 2:05 PM')).toBeVisible();
	await expect(page.getByTestId('surveyjs-runtime')).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Continue' })).toHaveCount(0);
	await expect(page.getByRole('button', { name: 'Submit reviewed response' })).toHaveCount(0);

	await receipt.getByRole('button', { name: 'Back to queue' }).click();
	await expect(page.getByRole('button', { name: /Manager.*Submitted/ })).toBeVisible();
	expect(calls).toEqual([
		'/respondent/identified-queues/{token}',
		'/respondent/identified-queues/{token}/assignments/{id}/sessions/{id}/draft'
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
	await expect(respondentAnswer(page)).toHaveValue('4');
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
					value: '5'
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
	await respondentAnswer(page).fill('5');
	await expect(page.getByRole('alert')).toContainText('Answers could not be saved.');
	const storedValues = await page.evaluate(() => Object.values(sessionStorage).join('\n'));
	expect(storedValues).not.toContain(openLinkToken);
	expect(storedValues).not.toContain(participantCode);

	await page.reload();

	await expect(respondentAnswer(page)).toHaveValue('5');
	await expect(page.getByText('Unsaved answers restored on this device')).toBeVisible();

	await page.getByRole('button', { name: 'Save and review' }).click();
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
	await expect(respondentAnswer(page)).toHaveValue('5');
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
	await respondentAnswer(page).fill('4');

	await expect.poll(() => savedPayloads).toEqual([
		{
			answers: [
				{
					questionId: sampleOpenLinkEntry.questions[0].id,
					value: '4'
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
	await respondentAnswer(page).fill('4');

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
	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();

	await page.getByRole('button', { name: 'Back to edit' }).click();
	await respondentAnswer(page).fill('5');

	await expect(page.getByText('Unsaved changes')).toBeVisible();
	await expect(page.getByRole('button', { name: 'Submit reviewed response' })).toHaveCount(0);
	await expect(page.getByText('Answers saved')).toBeVisible();
	await expect.poll(() => savedPayloads.at(-1)).toEqual({
		answers: [
			{
				questionId: sampleOpenLinkEntry.questions[0].id,
				value: '5'
			}
		]
	});

	await page.getByRole('button', { name: 'Save and review' }).click();
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
	await respondentAnswer(page).fill('5');

	await expect(page.getByRole('alert')).toContainText('Answers could not be saved.');
	await expect(respondentAnswer(page)).toHaveValue('5');

	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	expect(saveRequests).toBe(2);
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
	await expect(page.getByTestId('surveyjs-runtime')).toBeVisible();
	await expect(respondentAnswer(page)).toBeVisible();
	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
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

test('validates respondent answers before submit', async ({ page }) => {
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
		await route.fulfill({ json: sampleOpenLinkEntry });
	});

	await page.goto(`/r/${openLinkToken}`);

	await page.getByRole('checkbox', { name: 'Data processing' }).check();
	await page.getByRole('checkbox', { name: 'Research participation' }).check();
	await page.getByRole('button', { name: 'Continue' }).click();

	const answer = respondentAnswer(page);
	await answer.fill('');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('alert')).toContainText('q01 requires an answer.');
	expect(saveRequests).toBe(0);
	expect(submitRequests).toBe(0);

	await answer.fill('9');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('alert')).toContainText('q01 must be between 1 and 5.');
	expect(saveRequests).toBe(0);
	expect(submitRequests).toBe(0);

	await answer.fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
	await expect(page.getByRole('heading', { name: 'Review response' })).toBeVisible();
	expect(saveRequests).toBe(1);
	expect(submitRequests).toBe(0);
	await page.getByRole('button', { name: 'Submit reviewed response' }).click();
	await expect(page.getByText('Response submitted')).toBeVisible();
	expect(saveRequests).toBe(1);
	expect(submitRequests).toBe(1);
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

	const answer = respondentAnswer(page);
	await answer.fill('5');
	await page.getByRole('button', { name: 'Save and review' }).click();

	await expect(page.getByRole('alert')).toContainText('Answers could not be saved.');
	await expect(answer).toHaveValue('5');

	await page.getByRole('button', { name: 'Save and review' }).click();
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
	await respondentAnswer(page).fill('4');
	await page.getByRole('button', { name: 'Save and review' }).click();
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
	await expect(page.getByTestId('surveyjs-runtime')).toBeVisible();
	await expect(respondentAnswer(page)).toBeVisible();

	const saveButton = page.getByRole('button', { name: 'Save and review' });
	await expect(async () => {
		const box = await saveButton.boundingBox();
		expect(box?.width).toBeGreaterThanOrEqual(320);
	}).toPass();

	await respondentAnswer(page).fill('4');
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
const identifiedQueueToken =
	'idq_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ';
const publicSessionHandle =
	'rsh_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ';
const identifiedQueueSeriesId = '83e521a8-4b8c-44cd-9e13-115edaa89979';

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
			scaleMaxValue: 5
		}
	]
};

const sampleIdentifiedEntry = {
	...sampleOpenLinkEntry,
	name: 'Identified wave',
	responseIdentityMode: 'identified'
};

const sampleIdentifiedQueue = {
	campaignId,
	templateVersionId,
	name: 'Identified queue wave',
	status: 'live',
	responseIdentityMode: 'identified',
	defaultLocale: 'en',
	consentDocument: sampleOpenLinkEntry.consentDocument,
	questions: sampleOpenLinkEntry.questions,
	respondent: {
		id: 'f6d54d90-52c4-4996-a275-f3050040955d',
		label: 'Ana Analyst',
		displayName: 'Ana Analyst'
	},
	assignments: [
		{
			assignmentId,
			role: 'manager',
			status: 'not_started',
			target: {
				id: '45f3836f-e6e9-4695-a2ac-97ea125de78d',
				label: 'Manager',
				displayName: 'Morgan Manager'
			},
			sessionId: null,
			submittedAt: null
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

const sampleIdentifiedOperationsWorkspace = {
	series: {
		id: identifiedQueueSeriesId,
		name: 'Identified 360 pulse',
		studyKind: 'own',
		isSample: false,
		sampleScenario: null,
		readOnlyReason: null,
		createdAt: '2026-06-11T08:00:00Z',
		updatedAt: '2026-06-11T09:00:00Z'
	},
	summary: {
		campaignCount: 1,
		liveCampaignCount: 1,
		openLinkAssignmentCount: 0,
		queuedInvitationCount: 0,
		sentInvitationCount: 0,
		failedInvitationCount: 0,
		deliveryAttemptCount: 0,
		startedResponseCount: 0,
		draftResponseCount: 0,
		submittedResponseCount: 0,
		latestResponseStartedAt: null,
		latestResponseSubmittedAt: null,
		collectionStatus: 'not_started',
		reportVisibilityStatus: 'unknown_policy',
		collectionGuidance: 'Create identified access links for assigned respondents.',
		missingPrerequisiteCount: 0
	},
	selectedCampaign: {
		id: campaignId,
		name: 'Identified queue wave',
		status: 'live',
		responseIdentityMode: 'identified',
		defaultLocale: 'en',
		latestLaunchSnapshotId: '2eb09345-735c-4a29-b09b-e2d49c50da4a',
		latestLaunchAt: '2026-06-11T09:15:00Z',
		launchSnapshot: {
			id: '2eb09345-735c-4a29-b09b-e2d49c50da4a',
			templateVersionId,
			scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
			scoringRuleDocumentHash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
			consentDocumentId,
			retentionPolicyId: '06e242a5-6fc1-4e51-9af9-0d0cbf8c0872',
			disclosurePolicyId: 'a0910474-79b6-444d-9d21-8e89c82b6d72',
			responseIdentityMode: 'identified',
			defaultLocale: 'en',
			templateQuestionCount: 1,
			launchedAt: '2026-06-11T09:15:00Z',
			launchedByUserId: 'owner-user-id'
		},
		startedResponseCount: 0,
		draftResponseCount: 0,
		submittedResponseCount: 0,
		latestResponseStartedAt: null,
		latestResponseSubmittedAt: null,
		collectionStatus: 'not_started',
		reportVisibilityStatus: 'unknown_policy',
		collectionGuidance: 'Create identified access links for assigned respondents.',
		openLinkAssignmentCount: 0,
		queuedInvitationCount: 0,
		sentInvitationCount: 0,
		failedInvitationCount: 0,
		deliveryAttemptCount: 0,
		latestDeliveryAttemptAt: null,
		scoringRuleId: '716b2246-70f7-4728-9f44-150bd3b8da7a',
		scoredSubmittedResponseCount: 0,
		unscoredSubmittedResponseCount: 0,
		notConfiguredSubmittedResponseCount: 0,
		latestScoringActivityAt: null,
		scoreCoverageStatus: 'no_submissions'
	},
	missingPrerequisites: [],
	campaigns: [],
	scoreCoverage: {
		submittedResponseCount: 0,
		scoredSubmittedResponseCount: 0,
		unscoredSubmittedResponseCount: 0,
		campaignsWithScoringRuleCount: 1,
		campaignsWithoutScoringRuleCount: 0,
		latestScoringActivityAt: null,
		status: 'no_submissions',
		guidance: 'No submitted responses are ready for scoring yet.'
	}
};

function assertNoSetupAuthHeaders(headers: Record<string, string>) {
	expect(headers['x-tenant-id']).toBeUndefined();
	expect(headers['x-dev-user-id']).toBeUndefined();
	expect(headers['x-dev-tenant-memberships']).toBeUndefined();
	expect(headers['x-dev-permissions']).toBeUndefined();
	expect(headers['x-test-user-id']).toBeUndefined();
}

function respondentAnswer(page: Page) {
	return page.getByTestId('surveyjs-runtime').getByRole('spinbutton');
}

async function dispatchBeforeUnload(page: Page) {
	return page.evaluate(() => {
		const event = new Event('beforeunload', { cancelable: true });
		window.dispatchEvent(event);
		return event.defaultPrevented;
	});
}
