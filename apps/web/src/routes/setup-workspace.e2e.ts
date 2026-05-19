import { expect, test, type Page } from '@playwright/test';

const proofLabPath = '/proof-lab';

test.beforeEach(async ({ page }) => {
	await routeAuthenticatedSession(page);
	await routeCsrfToken(page);
});

test('checks the current session before showing setup controls', async ({ page }) => {
	const calls: string[] = [];

	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		calls.push('/auth/session');
		await route.fulfill({ json: sampleAuthSession });
	});

	await page.goto(proofLabPath);

	await expect(page.getByRole('heading', { name: 'Tenant setup workspace' })).toBeVisible();
	await expect(page.getByRole('region', { name: 'Authenticated tenant session' })).toBeVisible();
	await expect(page.getByText(sampleAuthSession.tenantId)).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create instrument import' })).toBeVisible();
	expect(calls).toEqual(['/auth/session']);
});

test('shows email verification status without blocking an authenticated tenant session', async ({
	page
}) => {
	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, {
		...sampleAuthSession,
		emailVerificationRequired: true
	});

	await page.goto('/app');

	const verificationStatus = page.getByRole('status', { name: 'Email verification required' });
	await expect(verificationStatus).toBeVisible();
	await expect(verificationStatus.getByRole('heading', { name: 'Verify your email' })).toBeVisible();
	await expect(verificationStatus).toContainText(
		'Open the verification email from Auth0 to keep access after signing out.'
	);
	await expect(page.getByRole('region', { name: 'Authenticated tenant session' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Study cockpit' })).toBeVisible();
	await expect(page.getByRole('region', { name: 'Self-serve study cockpit' })).toBeVisible();
});

test('does not show email verification status for a fully verified authenticated tenant session', async ({
	page
}) => {
	await page.goto('/app');

	await expect(page.getByRole('status', { name: 'Email verification required' })).toHaveCount(0);
	await expect(page.getByRole('region', { name: 'Authenticated tenant session' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Study cockpit' })).toBeVisible();
});

test('does not show email verification status when verification is explicitly not required', async ({
	page
}) => {
	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, {
		...sampleAuthSession,
		emailVerificationRequired: false
	});

	await page.goto('/app');

	await expect(page.getByRole('status', { name: 'Email verification required' })).toHaveCount(0);
	await expect(page.getByRole('region', { name: 'Authenticated tenant session' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Study cockpit' })).toBeVisible();
});

test('uses the last authenticated workspace for home sign-in after sign-out', async ({ page }) => {
	const registeredTenantId = '33333333-3333-4333-8333-333333333333';
	const registeredEmail = 'registered-owner@example.test';

	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, {
		...sampleAuthSession,
		tenantId: registeredTenantId,
		email: registeredEmail
	});

	await page.goto('/app');
	await expect(page.getByRole('region', { name: 'Authenticated tenant session' })).toBeVisible();

	await page.goto('/');

	await expect(page.getByRole('link', { name: 'Sign in' }).first()).toHaveAttribute(
		'href',
		new RegExp(`tenantId=${registeredTenantId}`)
	);
	await expect(page.getByRole('link', { name: 'Sign in' }).first()).toHaveAttribute(
		'href',
		new RegExp(`login_hint=${encodeURIComponent(registeredEmail)}`)
	);
});

test('removes stale auth failure marker after successful workspace sign-in', async ({ page }) => {
	await page.goto('/app?auth=failed');

	await expect(page).toHaveURL(/\/app$/);
	await expect(page.getByRole('region', { name: 'Authenticated tenant session' })).toBeVisible();
});

test('shows email verification recovery on registration with sign-in-specific copy', async ({
	page
}) => {
	await page.route('**/registration/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});
	await page.addInitScript(() => {
		window.sessionStorage.setItem(
			'instruments-platform.pending-registration-login-url',
			JSON.stringify({
				loginUrl:
					'https://validatedscale-api-staging.croat.dev/auth/login?registrationToken=pending-token&returnUrl=https%3A%2F%2Fvalidatedscale-staging.croat.dev%2Fapp&prompt=login',
				createdAt: Date.now(),
				stage: 'auth0-sign-in'
			})
		);
	});

	await page.goto('/register?auth=email_unverified');

	const recovery = page.getByRole('status').filter({ hasText: 'Verify email, then sign in' });
	await expect(recovery).toBeVisible();
	await expect(recovery).toContainText(
		'Open the verification email from Auth0, then retry registration sign-in with the same email.'
	);
	await expect(page.getByRole('link', { name: 'Retry registration sign-in' })).toHaveAttribute(
		'href',
		'https://validatedscale-api-staging.croat.dev/auth/login?registrationToken=pending-token&returnUrl=https%3A%2F%2Fvalidatedscale-staging.croat.dev%2Fapp&prompt=login'
	);
	await expect(page.getByText('finish setup')).toHaveCount(0);
	await expect(page.getByText('continue workspace setup')).toHaveCount(0);
});

test('shows email verification recovery after unverified workspace sign-in', async ({ page }) => {
	const lastTenantId = '22222222-2222-4222-8222-222222222222';
	const lastWorkspaceEmail = 'verified-owner@example.test';
	await page.addInitScript(({ tenantId, email }) => {
		window.localStorage.setItem('instruments-platform.last-tenant-id', tenantId);
		window.localStorage.setItem('instruments-platform.last-workspace-email', email);
	}, { tenantId: lastTenantId, email: lastWorkspaceEmail });

	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});

	await page.goto('/app?auth=email_unverified');

	await expect(page.getByRole('heading', { name: 'Verify email, then sign in' })).toBeVisible();
	await expect(page.getByLabel('Email verification reminder')).toContainText(
		'Open the verification email from Auth0, then sign in again with the same account.'
	);
	const signInAfterVerify = page.getByRole('link', { name: 'Sign in after verifying email' });
	await expect(signInAfterVerify).toBeVisible();
	await expect(signInAfterVerify).toHaveAttribute('href', new RegExp(`[?&]tenantId=${lastTenantId}`));
	await expect(signInAfterVerify).toHaveAttribute('href', /[?&]prompt=login(?:&|$)/);
	await expect(signInAfterVerify).toHaveAttribute(
		'href',
		new RegExp(`[?&]login_hint=${encodeURIComponent(lastWorkspaceEmail)}(?:&|$)`)
	);
	await expect(page.getByRole('link', { name: 'Sign out completely' })).toBeVisible();
	await expect(page.getByText('does not have access to this workspace')).toHaveCount(0);
});

test('stores structured pending registration metadata before Auth0 registration redirect', async ({
	page
}) => {
	const registrationToken = 'structured-token';
	let intentRequest: unknown = null;

	await page.route('**/registration/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});
	await page.route('**/registration/intents', async (route) => {
		intentRequest = route.request().postDataJSON();
		await route.fulfill({
			status: 201,
			json: {
				loginUrl: `/auth/login?registrationToken=${registrationToken}&returnUrl=${encodeURIComponent(
					'https://validatedscale-staging.croat.dev/app'
				)}&screen_hint=signup`,
				expiresAt: '2026-05-19T13:30:00Z'
			}
		});
	});

	await page.goto('/register');
	await page.getByRole('textbox', { name: 'Email' }).fill('owner@example.com');
	await page.getByRole('textbox', { name: 'Workspace name' }).fill('Example Lab');
	await page.getByLabel('Beta access code').fill('beta-code');
	await page.getByRole('button', { name: 'Create account' }).click();

	await page.waitForFunction(() =>
		window.sessionStorage.getItem('instruments-platform.pending-registration-login-url')?.startsWith('{')
	);

	expect(intentRequest).toMatchObject({
		email: 'owner@example.com',
		organizationName: 'Example Lab',
		accessCode: 'beta-code'
	});

	const pendingValue = await page.evaluate(() =>
		window.sessionStorage.getItem('instruments-platform.pending-registration-login-url')
	);
	expect(pendingValue).not.toBeNull();
	expect(pendingValue).not.toContain('screen_hint');

	const metadata = JSON.parse(pendingValue ?? '') as {
		loginUrl?: string;
		createdAt?: number;
		stage?: string;
	};
	expect(metadata.stage).toBe('auth0-sign-in');
	expect(typeof metadata.createdAt).toBe('number');
	expect(metadata.loginUrl).toContain(`registrationToken=${registrationToken}`);
	expect(metadata.loginUrl).toContain('returnUrl=');
	expect(metadata.loginUrl).not.toContain('screen_hint');
});

test('remembers workspace account after registration workspace creation', async ({ page }) => {
	const registeredTenantId = '44444444-4444-4444-8444-444444444444';
	const registeredEmail = 'registered-owner@example.test';

	await page.unroute('**/auth/session');
	await routeAuthenticatedSession(page, {
		...sampleAuthSession,
		tenantId: registeredTenantId,
		email: registeredEmail
	});
	await page.route('**/registration/session', async (route) => {
		await route.fulfill({ json: { email: registeredEmail } });
	});
	await page.route('**/registration/workspaces', async (route) => {
		await route.fulfill({
			status: 201,
			json: {
				appUrl: '/app',
				tenantId: registeredTenantId,
				email: registeredEmail
			}
		});
	});

	await page.goto('/register');
	await expect(page.getByText(`Account ready: ${registeredEmail}`)).toBeVisible();
	await page.getByRole('textbox', { name: 'Workspace name' }).fill('Example Lab');
	await page.getByLabel('Beta access code').fill('beta-code');
	await page.getByRole('button', { name: 'Create workspace' }).click();

	await page.waitForFunction(
		({ tenantId, email }) =>
			window.localStorage.getItem('instruments-platform.last-tenant-id') === tenantId &&
			window.localStorage.getItem('instruments-platform.last-workspace-email') === email,
		{ tenantId: registeredTenantId, email: registeredEmail }
	);
});

test('shows sign-in required when the setup session is unauthenticated', async ({ page }) => {
	let protectedCalls = 0;

	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});
	await page.route('**/instruments', async (route) => {
		protectedCalls += 1;
		await route.fulfill({ json: [] });
	});

	await page.goto(proofLabPath);

	await expect(page.getByRole('heading', { name: 'Sign in required' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/auth/login');
	await expect(page.getByRole('button', { name: 'Create instrument import' })).toHaveCount(0);
	expect(protectedCalls).toBe(0);
});

test('shows pending registration recovery after failed workspace sign-in', async ({ page }) => {
	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});
	await page.addInitScript(() => {
		window.sessionStorage.setItem(
			'instruments-platform.pending-registration-login-url',
			JSON.stringify({
				loginUrl:
					'https://validatedscale-api-staging.croat.dev/auth/login?registrationToken=pending-token&returnUrl=https%3A%2F%2Fvalidatedscale-staging.croat.dev%2Fapp&prompt=login',
				createdAt: Date.now(),
				stage: 'auth0-sign-in'
			})
		);
	});

	await page.goto('/app?auth=failed');

	await expect(page.getByRole('heading', { name: 'Registration sign-in did not finish' })).toBeVisible();
	await expect(page.getByLabel('Email verification reminder')).toContainText(
		'Retry the saved registration sign-in link if the Auth0 callback was interrupted.'
	);
	await expect(page.getByLabel('Email verification reminder')).toContainText(
		'If Auth0 keeps choosing the wrong account, sign out completely first.'
	);
	await expect(page.getByRole('heading', { name: 'Workspace sign-in needed' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Try registration sign-in again' })).toHaveAttribute(
		'href',
		'https://validatedscale-api-staging.croat.dev/auth/login?registrationToken=pending-token&returnUrl=https%3A%2F%2Fvalidatedscale-staging.croat.dev%2Fapp&prompt=login'
	);
	await expect(page.getByRole('link', { name: 'Sign out completely' })).toHaveAttribute(
		'href',
		/\/auth\/logout\?.*provider=1.*returnUrl=/
	);
	await expect(page.getByRole('button', { name: 'Retry' })).toHaveCount(0);
});

test('uses the last authenticated workspace for failed sign-in recovery', async ({ page }) => {
	const registeredTenantId = '33333333-3333-4333-8333-333333333333';

	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});
	await page.addInitScript((tenantId) => {
		window.localStorage.setItem('instruments-platform.last-tenant-id', tenantId);
	}, registeredTenantId);

	await page.goto('/app?auth=failed');

	await expect(page.getByRole('heading', { name: 'Sign in with your workspace account' })).toBeVisible();
	await expect(page.getByRole('link', { name: 'Sign in to existing workspace' })).toHaveAttribute(
		'href',
		new RegExp(`tenantId=${registeredTenantId}`)
	);
});

test('ignores stale bare pending registration URLs after failed workspace sign-in', async ({
	page
}) => {
	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});
	await page.addInitScript(() => {
		window.sessionStorage.setItem(
			'instruments-platform.pending-registration-login-url',
			'https://validatedscale-api-staging.croat.dev/auth/login?registrationToken=stale-token&returnUrl=https%3A%2F%2Fvalidatedscale-staging.croat.dev%2Fapp&prompt=login'
		);
	});

	await page.goto('/app?auth=failed');

	await expect(page.getByRole('heading', { name: 'Sign in with your workspace account' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Registration sign-in did not finish' })).toHaveCount(
		0
	);
	await expect(page.getByRole('link', { name: 'Try registration sign-in again' })).toHaveCount(0);
	await expect(
		page.getByRole('link', { name: /Create workspace|Sign in to existing workspace/ })
	).toBeVisible();
	await expect(page.getByRole('link', { name: 'Sign out completely' })).toBeVisible();
	expect(
		await page.evaluate(() =>
			window.sessionStorage.getItem('instruments-platform.pending-registration-login-url')
		)
	).toBeNull();
});

test('ignores expired pending registration metadata after failed workspace sign-in', async ({
	page
}) => {
	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
	});
	await page.addInitScript(() => {
		window.sessionStorage.setItem(
			'instruments-platform.pending-registration-login-url',
			JSON.stringify({
				loginUrl:
					'https://validatedscale-api-staging.croat.dev/auth/login?registrationToken=expired-token&returnUrl=https%3A%2F%2Fvalidatedscale-staging.croat.dev%2Fapp&prompt=login',
				createdAt: Date.now() - 16 * 60 * 1000,
				stage: 'auth0-sign-in'
			})
		);
	});

	await page.goto('/app?auth=failed');

	await expect(page.getByRole('heading', { name: 'Sign in with your workspace account' })).toBeVisible();
	await expect(page.getByRole('heading', { name: 'Registration sign-in did not finish' })).toHaveCount(
		0
	);
	await expect(page.getByRole('link', { name: 'Try registration sign-in again' })).toHaveCount(0);
	await expect(page.getByRole('link', { name: 'Sign out completely' })).toBeVisible();
	expect(
		await page.evaluate(() =>
			window.sessionStorage.getItem('instruments-platform.pending-registration-login-url')
		)
	).toBeNull();
});

test('hides setup controls when the tenant session is forbidden', async ({ page }) => {
	await page.unroute('**/auth/session');
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ status: 403, json: { title: 'Forbidden' } });
	});

	await page.goto(proofLabPath);

	await expect(page.getByRole('heading', { name: 'Tenant access unavailable' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Retry session check' })).toBeVisible();
	await expect(page.getByRole('button', { name: 'Create instrument import' })).toHaveCount(0);
});

test('creates a setup draft through GF05 endpoints', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/instruments/private-imports', async (route) => {
		calls.push('/instruments/private-imports');
		await route.fulfill({ status: 201, json: sampleInstrument });
	});

	await page.route('**/instruments', async (route) => {
		calls.push('/instruments');
		await route.fulfill({ json: [sampleInstrument] });
	});

	await page.route('**/template-versions', async (route) => {
		calls.push('/template-versions');
		await route.fulfill({ status: 201, json: sampleTemplateVersion });
	});

	await page.route('**/scoring-rules', async (route) => {
		calls.push('/scoring-rules');
		const requestBody = route.request().postDataJSON() as { document: string };
		const scoringDocument = JSON.parse(requestBody.document) as {
			nodes: Array<{ op: string }>;
			outputs: Array<{ code: string }>;
		};
		expect(scoringDocument.nodes.map((node) => node.op)).toEqual([
			'select_answers',
			'reverse_code',
			'mean'
		]);
		expect(scoringDocument.outputs[0]?.code).toBe('total');
		await route.fulfill({ status: 201, json: { id: scoringRuleId } });
	});

	await page.route('**/campaign-series', async (route) => {
		calls.push('/campaign-series');
		await route.fulfill({ status: 201, json: { id: campaignSeriesId } });
	});

	await page.route('**/campaigns', async (route) => {
		calls.push('/campaigns');
		await route.fulfill({ status: 201, json: sampleCampaign });
	});

	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		calls.push('/campaigns/{id}/launch-readiness');
		await route.fulfill({ json: sampleReadiness });
	});

	await page.route('**/campaigns/*/launch', async (route) => {
		calls.push('/campaigns/{id}/launch');
		await route.fulfill({ json: sampleLaunch });
	});

	await page.route('**/campaigns/*/open-link', async (route) => {
		calls.push('/campaigns/{id}/open-link');
		await route.fulfill({ status: 201, json: sampleOpenLink });
	});

	await page.route('**/campaigns/*/invitation-batches', async (route) => {
		calls.push('/campaigns/{id}/invitation-batches');
		await route.fulfill({ status: 201, json: sampleInvitationBatch });
	});

	await page.route('**/campaigns/*/notification-deliveries/process', async (route) => {
		calls.push('/campaigns/{id}/notification-deliveries/process');
		await route.fulfill({ json: sampleNotificationDelivery });
	});

	await page.goto(proofLabPath);

	await page.getByRole('button', { name: 'Create instrument import' }).click();
	await expect(page.getByText(instrumentId)).toBeVisible();
	await expect(page.getByRole('region', { name: 'Existing tenant instruments' })).toBeVisible();
	await expect(page.getByText('Latest')).toBeVisible();

	await page.getByRole('button', { name: 'Create template version' }).click();
	await expect(page.getByText(templateVersionId)).toBeVisible();

	await page.getByRole('button', { name: 'Create scoring rule' }).click();
	await expect(page.getByText(scoringRuleId)).toBeVisible();

	await page.getByRole('button', { name: 'Create campaign draft' }).click();
	await expect(page.getByText(campaignId)).toBeVisible();

	const launchReadinessRegion = page.getByRole('region', { name: 'Launch readiness' });
	await page.getByRole('button', { name: 'Check launch readiness' }).click();
	await expect(launchReadinessRegion.getByText(/Ready:/)).toBeVisible();
	await expect(launchReadinessRegion.getByText('yes', { exact: true })).toBeVisible();
	await page.getByRole('button', { name: 'Launch campaign' }).click();
	await expect(launchReadinessRegion.getByText('live', { exact: true })).toBeVisible();
	await expect(page.getByText(launchSnapshotId)).toBeVisible();
	await page.getByRole('button', { name: 'Create open link' }).click();
	await expect(page.getByText('/r/' + openLinkToken)).toBeVisible();
	await page.getByRole('button', { name: 'Queue email invitations' }).click();
	const inviteRegion = page.getByRole('region', { name: 'Queued invitation intents' });
	await expect(inviteRegion.getByText('/r/' + inviteTokenA)).toBeVisible();
	await expect(inviteRegion.getByText('/r/' + inviteTokenB)).toBeVisible();
	await expect(inviteRegion.getByText('queued')).toHaveCount(2);
	await page.getByRole('button', { name: 'Process local delivery' }).click();
	const deliveryRegion = page.getByRole('region', { name: 'Local delivery proof' });
	await expect(deliveryRegion.getByText('/r/' + deliveredInviteTokenA)).toBeVisible();
	await expect(deliveryRegion.getByText('/r/' + deliveredInviteTokenB)).toBeVisible();
	await expect(deliveryRegion.getByText('sent')).toHaveCount(2);
	await expect(calls).toEqual([
		'/instruments/private-imports',
		'/instruments',
		'/template-versions',
		'/scoring-rules',
		'/campaign-series',
		'/campaigns',
		'/campaigns/{id}/launch-readiness',
		'/campaigns/{id}/launch',
		'/campaigns/{id}/open-link',
		'/campaigns/{id}/invitation-batches',
		'/campaigns/{id}/notification-deliveries/process'
	]);
});

test('submits a response lab answer through R01 endpoints', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/instruments/private-imports', async (route) => {
		calls.push('/instruments/private-imports');
		await route.fulfill({ status: 201, json: sampleInstrument });
	});
	await page.route('**/instruments', async (route) => {
		calls.push('/instruments');
		await route.fulfill({ json: [sampleInstrument] });
	});
	await page.route('**/template-versions', async (route) => {
		calls.push('/template-versions');
		await route.fulfill({ status: 201, json: sampleTemplateVersion });
	});
	await page.route('**/campaign-series', async (route) => {
		calls.push('/campaign-series');
		await route.fulfill({ status: 201, json: { id: campaignSeriesId } });
	});
	await page.route('**/campaigns', async (route) => {
		calls.push('/campaigns');
		await route.fulfill({ status: 201, json: sampleCampaign });
	});
	await page.route('**/respondent/campaigns/*/lab-assignment', async (route) => {
		calls.push('/respondent/campaigns/{id}/lab-assignment');
		await route.fulfill({ status: 201, json: sampleLabAssignment });
	});
	await page.route('**/respondent/campaigns/*', async (route) => {
		calls.push('/respondent/campaigns/{id}');
		await route.fulfill({ json: sampleRespondentCampaign });
	});
	await page.route('**/respondent/sessions/*/answers', async (route) => {
		calls.push('/respondent/sessions/{id}/answers');
		await route.fulfill({ json: { sessionId, savedAnswerCount: 1 } });
	});
	await page.route('**/respondent/sessions/*/submit', async (route) => {
		calls.push('/respondent/sessions/{id}/submit');
		await route.fulfill({ json: { id: sessionId, submittedAt: '2026-05-07T12:05:00Z' } });
	});
	await page.route('**/respondent/sessions/*/scores', async (route) => {
		calls.push('/respondent/sessions/{id}/scores');
		await route.fulfill({ json: sampleScoreResponse });
	});
	await page.route('**/campaigns/*/report-proof', async (route) => {
		calls.push('/campaigns/{id}/report-proof');
		await route.fulfill({ json: sampleReportProof });
	});
	await page.route('**/campaigns/*/report-proof/exports', async (route) => {
		calls.push('/campaigns/{id}/report-proof/exports');
		await route.fulfill({ json: sampleReportProofExport });
	});
	await page.route(`**/export-artifacts/${exportArtifactId}`, async (route) => {
		calls.push('/export-artifacts/{id}');
		await route.fulfill({ json: sampleReportProofExport });
	});
	await page.route(`**/export-artifacts/${exportArtifactId}/download`, async (route) => {
		calls.push('/export-artifacts/{id}/download');
		await route.fulfill({
			contentType: 'text/csv',
			headers: {
				'content-disposition': `attachment; filename="${sampleReportProofExport.fileName}"`
			},
			body: sampleReportProofExport.csvContent
		});
	});
	await page.route('**/respondent/sessions', async (route) => {
		calls.push('/respondent/sessions');
		await route.fulfill({ status: 201, json: sampleSession });
	});

	await page.goto(proofLabPath);
	await page.getByRole('button', { name: 'Create instrument import' }).click();
	await page.getByRole('button', { name: 'Create template version' }).click();
	await page.getByRole('button', { name: 'Create campaign draft' }).click();

	await page.getByRole('button', { name: 'Load response lab' }).click();
	await expect(page.getByText('After work, I need time to recover mentally.')).toBeVisible();
	await page.getByRole('button', { name: 'Create lab assignment' }).click();
	await expect(page.getByText(assignmentId)).toBeVisible();
	await page.getByRole('button', { name: 'Start response session' }).click();
	await expect(page.getByText(sessionId)).toBeVisible();
	await page.getByLabel('q01 answer').fill('5');
	await page.getByRole('button', { name: 'Save response answers' }).click();
	await expect(page.getByText('Saved answers 1')).toBeVisible();
	await page.getByRole('button', { name: 'Submit response' }).click();
	await expect(page.getByText(/Submitted/)).toBeVisible();
	await page.getByRole('button', { name: 'Compute score' }).click();
	const scorePanel = page.getByRole('region', { name: 'Setup score result' });
	const totalScore = scorePanel.getByLabel('Score total');
	await expect(scorePanel.getByText('Setup lab result')).toBeVisible();
	await expect(totalScore).toContainText('4.00');
	await expect(totalScore).toContainText('n=1');
	await expect(scorePanel.getByText('Interpretation pending')).toBeVisible();
	await expect(scorePanel.getByText('Not a production report')).toBeVisible();
	await expect(scorePanel.getByText(scoreRunId)).toBeVisible();
	await page.getByRole('button', { name: 'View report proof' }).click();
	const reportPanel = page.getByRole('region', { name: 'Report proof' });
	await expect(reportPanel.getByText('Proof only')).toBeVisible();
	await expect(reportPanel.getByText('Not validated interpretation')).toBeVisible();
	await expect(reportPanel.getByText('Disclosure k=5')).toBeVisible();
	await expect(reportPanel.getByText('insufficient responses')).toBeVisible();
	await expect(reportPanel.getByText(launchSnapshotId)).toBeVisible();
	await expect(reportPanel).not.toContainText('recipient');
	await expect(reportPanel).not.toContainText('token');
	await page.getByRole('button', { name: 'Create export proof' }).click();
	const exportPanel = page.getByRole('region', { name: 'Export artifact proof' });
	await expect(exportPanel.getByText('report_proof_csv_codebook')).toBeVisible();
	await expect(exportPanel.getByText(exportArtifactId)).toBeVisible();
	await expect(exportPanel.getByText('rows 1')).toBeVisible();
	await expect(exportPanel.getByText(exportChecksum)).toBeVisible();
	await expect(exportPanel.getByText('campaign_id,dimension_code,disclosure')).toBeVisible();
	await expect(exportPanel).not.toContainText('recipient');
	await expect(exportPanel).not.toContainText('token');
	await expect(exportPanel).not.toContainText('raw response');
	await page.getByRole('button', { name: 'Fetch stored artifact' }).click();
	await expect(exportPanel.getByText('Stored artifact fetched')).toBeVisible();
	await page.getByRole('button', { name: 'Download CSV' }).click();
	await expect(exportPanel.getByText('Downloaded CSV')).toBeVisible();
	await expect(exportPanel.getByText('text/csv')).toBeVisible();
	await expect(exportPanel).not.toContainText('recipient');
	await expect(exportPanel).not.toContainText('token');
	await expect(exportPanel).not.toContainText('raw response');

	expect(calls).toContain('/respondent/campaigns/{id}');
	expect(calls).toContain('/respondent/campaigns/{id}/lab-assignment');
	expect(calls).toContain('/respondent/sessions');
	expect(calls).toContain('/respondent/sessions/{id}/answers');
	expect(calls).toContain('/respondent/sessions/{id}/submit');
	expect(calls).toContain('/respondent/sessions/{id}/scores');
	expect(calls).toContain('/campaigns/{id}/report-proof');
	expect(calls).toContain('/campaigns/{id}/report-proof/exports');
	expect(calls).toContain('/export-artifacts/{id}');
	expect(calls).toContain('/export-artifacts/{id}/download');
});

test('creates a two-wave anonymous longitudinal proof setup', async ({ page }) => {
	const calls: string[] = [];
	const campaignCreates: Array<{
		templateVersionId: string;
		name: string;
		responseIdentityMode: string;
		campaignSeriesId: string | null;
	}> = [];

	await page.route('**/template-versions', async (route) => {
		calls.push('/template-versions');
		await route.fulfill({ status: 201, json: sampleTemplateVersion });
	});
	await page.route('**/scoring-rules', async (route) => {
		calls.push('/scoring-rules');
		await route.fulfill({ status: 201, json: { id: scoringRuleId } });
	});
	await page.route('**/campaign-series', async (route) => {
		calls.push('/campaign-series');
		await route.fulfill({ status: 201, json: { id: campaignSeriesId } });
	});
	await page.route('**/campaigns', async (route) => {
		const body = route.request().postDataJSON() as {
			name: string;
			responseIdentityMode: string;
			templateVersionId: string;
			campaignSeriesId: string | null;
		};
		campaignCreates.push(body);
		calls.push(`/campaigns:${body.name}:${body.responseIdentityMode}`);
		await route.fulfill({
			status: 201,
			json: body.name.includes('Wave 2')
				? {
						...sampleCampaign,
						id: wave2CampaignId,
						name: 'Wave 2',
						responseIdentityMode: 'anonymous_longitudinal'
					}
				: {
						...sampleCampaign,
						id: wave1CampaignId,
						name: 'Wave 1',
						responseIdentityMode: 'anonymous_longitudinal'
					}
		});
	});
	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		calls.push('/campaigns/{id}/launch-readiness');
		await route.fulfill({ json: sampleReadiness });
	});
	await page.route('**/campaigns/*/launch', async (route) => {
		calls.push('/campaigns/{id}/launch');
		await route.fulfill({
			json: {
				...sampleLaunch,
				campaignId: route.request().url().includes(wave2CampaignId)
					? wave2CampaignId
					: wave1CampaignId,
				responseIdentityMode: 'anonymous_longitudinal'
			}
		});
	});
	await page.route('**/campaigns/*/open-link', async (route) => {
		const isWave2 = route.request().url().includes(wave2CampaignId);
		calls.push('/campaigns/{id}/open-link');
		await route.fulfill({
			status: 201,
			json: isWave2
				? {
						...sampleOpenLink,
						campaignId: wave2CampaignId,
						token: wave2OpenLinkToken,
						respondentPath: `/r/${wave2OpenLinkToken}`
					}
				: {
						...sampleOpenLink,
						campaignId: wave1CampaignId,
						token: wave1OpenLinkToken,
						respondentPath: `/r/${wave1OpenLinkToken}`
					}
		});
	});
	await page.route('**/campaign-series/*/two-wave-proof', async (route) => {
		calls.push('/campaign-series/{id}/two-wave-proof');
		await route.fulfill({ json: sampleTwoWaveProof });
	});

	await page.goto(proofLabPath);
	await page.getByRole('button', { name: 'Create template version' }).click();
	await page.getByRole('button', { name: 'Create scoring rule' }).click();
	await page.getByRole('button', { name: 'Create two-wave proof' }).click();

	const panel = page.getByRole('region', { name: 'Two-wave proof' });
	await expect(panel.getByText(wave1CampaignId)).toBeVisible();
	await expect(panel.getByText(wave2CampaignId)).toBeVisible();
	await expect(panel.getByText(`/r/${wave1OpenLinkToken}`)).toBeVisible();
	await expect(panel.getByText(`/r/${wave2OpenLinkToken}`)).toBeVisible();
	expect(campaignCreates).toHaveLength(2);
	expect(campaignCreates.map((body) => body.responseIdentityMode)).toEqual([
		'anonymous_longitudinal',
		'anonymous_longitudinal'
	]);
	expect(campaignCreates.map((body) => body.campaignSeriesId)).toEqual([
		campaignSeriesId,
		campaignSeriesId
	]);
	expect(campaignCreates.map((body) => body.templateVersionId)).toEqual([
		templateVersionId,
		templateVersionId
	]);

	await page.getByRole('button', { name: 'Refresh two-wave proof' }).click();
	await expect(panel.getByText('complete trajectories 1')).toBeVisible();
	await expect(panel).not.toContainText('participantCodeId');
	await expect(panel).not.toContainText('hash');
	expect(calls).toContain('/campaign-series/{id}/two-wave-proof');
});

test('shows wave comparison proof for a two-wave setup', async ({ page }) => {
	const calls: string[] = [];

	await page.route('**/template-versions', async (route) => {
		await route.fulfill({ status: 201, json: sampleTemplateVersion });
	});
	await page.route('**/scoring-rules', async (route) => {
		await route.fulfill({ status: 201, json: { id: scoringRuleId } });
	});
	await page.route('**/campaign-series', async (route) => {
		await route.fulfill({ status: 201, json: { id: campaignSeriesId } });
	});
	await page.route('**/campaigns', async (route) => {
		const body = route.request().postDataJSON() as { name: string };
		await route.fulfill({
			status: 201,
			json: body.name.includes('Wave 2')
				? {
						...sampleCampaign,
						id: wave2CampaignId,
						name: 'Wave 2',
						responseIdentityMode: 'anonymous_longitudinal'
					}
				: {
						...sampleCampaign,
						id: wave1CampaignId,
						name: 'Wave 1',
						responseIdentityMode: 'anonymous_longitudinal'
					}
		});
	});
	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		await route.fulfill({ json: sampleReadiness });
	});
	await page.route('**/campaigns/*/launch', async (route) => {
		await route.fulfill({
			json: {
				...sampleLaunch,
				campaignId: route.request().url().includes(wave2CampaignId)
					? wave2CampaignId
					: wave1CampaignId,
				responseIdentityMode: 'anonymous_longitudinal'
			}
		});
	});
	await page.route('**/campaigns/*/open-link', async (route) => {
		const isWave2 = route.request().url().includes(wave2CampaignId);
		await route.fulfill({
			status: 201,
			json: isWave2
				? {
						...sampleOpenLink,
						campaignId: wave2CampaignId,
						token: wave2OpenLinkToken,
						respondentPath: `/r/${wave2OpenLinkToken}`
					}
				: {
						...sampleOpenLink,
						campaignId: wave1CampaignId,
						token: wave1OpenLinkToken,
						respondentPath: `/r/${wave1OpenLinkToken}`
					}
		});
	});
	await page.route('**/campaign-series/*/wave-comparison-proof', async (route) => {
		calls.push('/campaign-series/{id}/wave-comparison-proof');
		await route.fulfill({ json: sampleWaveComparisonProof });
	});

	await page.goto(proofLabPath);
	await page.getByRole('button', { name: 'Create template version' }).click();
	await page.getByRole('button', { name: 'Create scoring rule' }).click();
	await page.getByRole('button', { name: 'Create two-wave proof' }).click();
	await page.getByRole('button', { name: 'View wave comparison proof' }).click();

	const panel = page.getByRole('region', { name: 'Wave comparison proof' });
	await expect(panel.getByText('compatible')).toBeVisible();
	await expect(panel.getByText('Disclosure k=5')).toBeVisible();
	await expect(panel.getByText('baseline mean 3.80')).toBeVisible();
	await expect(panel.getByText('comparison mean 4.60')).toBeVisible();
	await expect(panel.getByText('aggregate delta 0.80')).toBeVisible();
	await expect(panel.getByText('paired delta 0.80')).toBeVisible();
	await expect(panel).not.toContainText('participantCodeId');
	await expect(panel).not.toContainText('hash');
	expect(calls).toContain('/campaign-series/{id}/wave-comparison-proof');
});

test('clears stale two-wave anonymous longitudinal proof when setup is regenerated or retry fails', async ({
	page
}) => {
	let failNextWaveLaunch = false;

	await page.route('**/template-versions', async (route) => {
		await route.fulfill({ status: 201, json: sampleTemplateVersion });
	});
	await page.route('**/scoring-rules', async (route) => {
		await route.fulfill({ status: 201, json: { id: scoringRuleId } });
	});
	await page.route('**/campaign-series', async (route) => {
		await route.fulfill({ status: 201, json: { id: campaignSeriesId } });
	});
	await page.route('**/campaigns', async (route) => {
		const body = route.request().postDataJSON() as { name: string };
		await route.fulfill({
			status: 201,
			json: body.name.includes('Wave 2')
				? {
						...sampleCampaign,
						id: wave2CampaignId,
						name: 'Wave 2',
						responseIdentityMode: 'anonymous_longitudinal'
					}
				: {
						...sampleCampaign,
						id: wave1CampaignId,
						name: 'Wave 1',
						responseIdentityMode: 'anonymous_longitudinal'
					}
		});
	});
	await page.route('**/campaigns/*/launch-readiness', async (route) => {
		await route.fulfill({ json: sampleReadiness });
	});
	await page.route('**/campaigns/*/launch', async (route) => {
		if (failNextWaveLaunch) {
			failNextWaveLaunch = false;
			await route.fulfill({ status: 500, body: 'launch failed' });
			return;
		}

		await route.fulfill({
			json: {
				...sampleLaunch,
				campaignId: route.request().url().includes(wave2CampaignId)
					? wave2CampaignId
					: wave1CampaignId,
				responseIdentityMode: 'anonymous_longitudinal'
			}
		});
	});
	await page.route('**/campaigns/*/open-link', async (route) => {
		const isWave2 = route.request().url().includes(wave2CampaignId);
		await route.fulfill({
			status: 201,
			json: isWave2
				? {
						...sampleOpenLink,
						campaignId: wave2CampaignId,
						token: wave2OpenLinkToken,
						respondentPath: `/r/${wave2OpenLinkToken}`
					}
				: {
						...sampleOpenLink,
						campaignId: wave1CampaignId,
						token: wave1OpenLinkToken,
						respondentPath: `/r/${wave1OpenLinkToken}`
					}
		});
	});
	await page.route('**/campaign-series/*/two-wave-proof', async (route) => {
		await route.fulfill({ json: sampleTwoWaveProof });
	});

	await page.goto(proofLabPath);
	await page.getByRole('button', { name: 'Create template version' }).click();
	await page.getByRole('button', { name: 'Create scoring rule' }).click();
	await page.getByRole('button', { name: 'Create two-wave proof' }).click();

	const panel = page.getByRole('region', { name: 'Two-wave proof' });
	await expect(panel.getByText(wave1CampaignId)).toBeVisible();

	failNextWaveLaunch = true;
	await page.getByRole('button', { name: 'Create two-wave proof' }).click();
	await expect(panel.getByText(wave1CampaignId)).toHaveCount(0);
	await expect(panel.getByText(`/r/${wave1OpenLinkToken}`)).toHaveCount(0);
	await expect(panel.locator('.step-pill')).toContainText('Failed');
	await expect(panel.locator('.error-line')).toBeVisible();

	await page.getByRole('button', { name: 'Create scoring rule' }).click();
	await expect(panel.locator('.step-pill')).toContainText('Ready');
	await expect(panel.locator('.error-line')).toHaveCount(0);

	await page.getByRole('button', { name: 'Create two-wave proof' }).click();
	await expect(panel.getByText(wave1CampaignId)).toBeVisible();
	await page.getByRole('button', { name: 'Create scoring rule' }).click();
	await expect(panel.getByText(wave1CampaignId)).toHaveCount(0);
	await expect(panel.getByText(`/r/${wave1OpenLinkToken}`)).toHaveCount(0);
	await expect(panel.locator('.step-pill')).toContainText('Ready');
	await expect(panel.locator('.error-line')).toHaveCount(0);

	await page.getByRole('button', { name: 'Create two-wave proof' }).click();
	await expect(panel.getByText(wave1CampaignId)).toBeVisible();
	await page.getByRole('button', { name: 'Create template version' }).click();
	await expect(panel.getByText(wave1CampaignId)).toHaveCount(0);
	await expect(panel.getByText(`/r/${wave1OpenLinkToken}`)).toHaveCount(0);
	await expect(panel.locator('.step-pill')).toContainText('Ready');
	await expect(panel.locator('.error-line')).toHaveCount(0);
});

test('summarizes long API failures without dumping server stack traces', async ({ page }) => {
	await page.route('**/instruments/private-imports', async (route) => {
		await route.fulfill({
			status: 500,
			contentType: 'text/plain',
			body: 'Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5432\n'.repeat(80)
		});
	});

	await page.goto(proofLabPath);
	await page.waitForTimeout(1500);
	await page.getByRole('button', { name: 'Create instrument import' }).click();

	const error = page.locator('.error-line');
	await expect(error).toBeVisible();
	await expect(error).toContainText('API request failed with status 500');
	await expect(error).toContainText('truncated');
	expect((await error.textContent())?.length).toBeLessThan(700);
});

test('formats API problem details as readable setup errors', async ({ page }) => {
	await page.route('**/instruments/private-imports', async (route) => {
		await route.fulfill({
			status: 409,
			json: {
				title: 'instrument.duplicate_code_version',
				status: 409,
				detail: 'An instrument with this code and version already exists for this tenant.'
			}
		});
	});

	await page.goto(proofLabPath);
	await page.getByRole('button', { name: 'Create instrument import' }).click();

	const error = page.locator('.error-line');
	await expect(error).toBeVisible();
	await expect(error).toContainText('API request failed with status 409');
	await expect(error).toContainText(
		'instrument.duplicate_code_version: An instrument with this code and version already exists for this tenant.'
	);
	await expect(error).not.toContainText('{"title"');
});

test('prefills generated sample values so repeated local setup runs do not collide', async ({
	page
}) => {
	await page.goto(proofLabPath);

	await expect(page.getByRole('textbox', { name: 'Code', exact: true })).toHaveValue(
		/^tenant-burnout-pulse-[a-z0-9]+$/
	);
});

test('shows current setup run and regenerates sample values', async ({ page }) => {
	await page.goto(proofLabPath);

	const run = page.getByLabel('Current setup run');
	const codeInput = page.getByRole('textbox', { name: 'Code', exact: true });
	await expect(run).toContainText(/sample [a-z0-9]+/);
	const firstCode = await codeInput.inputValue();

	await page.getByRole('button', { name: 'Generate new sample values' }).click();

	const nextCode = await codeInput.inputValue();
	await expect(run).toContainText(nextCode.replace('tenant-burnout-pulse-', 'sample '));
	expect(nextCode).toMatch(/^tenant-burnout-pulse-[a-z0-9]+$/);
	expect(nextCode).not.toEqual(firstCode);
});

test('regenerating sample values clears stale setup results', async ({ page }) => {
	await page.route('**/instruments/private-imports', async (route) => {
		await route.fulfill({ status: 201, json: sampleInstrument });
	});
	await page.route('**/instruments', async (route) => {
		await route.fulfill({ json: [sampleInstrument] });
	});

	await page.goto(proofLabPath);
	await page.getByRole('button', { name: 'Create instrument import' }).click();
	await expect(page.getByText(instrumentId)).toBeVisible();

	await page.getByRole('button', { name: 'Generate new sample values' }).click();

	await expect(page.getByText(instrumentId)).toHaveCount(0);
	await expect(page.getByRole('region', { name: 'Existing tenant instruments' })).toHaveCount(0);
});

test('exposes generic provenance without mock-specific content', async ({ page }) => {
	await page.goto(proofLabPath);
	const provenance = page.getByLabel('Setup provenance');

	await expect(provenance.getByText('Content rights')).toBeVisible();
	await expect(provenance.getByText('Attested by tenant')).toBeVisible();
	await expect(provenance.getByText('Identity mode')).toBeVisible();
	await expect(provenance.getByText('Anonymous')).toBeVisible();
	await expect(provenance.getByText('Backend contract')).toBeVisible();
	await expect(provenance.getByText('GF05 setup endpoints')).toBeVisible();
	await expect(page.getByText('OLBI')).toHaveCount(0);
	await expect(page.getByText('KBC')).toHaveCount(0);
	await expect(page.getByText('Spectra')).toHaveCount(0);
});

const instrumentId = '3b3531a9-6a8d-40a7-b6a8-8e5e02282c3a';
const templateId = '58b0b19f-9296-43f2-8895-555660a62759';
const templateVersionId = 'f6d8cc42-b721-4789-a933-83895f5d064e';
const scoringRuleId = '93abb3c0-75d5-4c85-b7f3-f31ce7763e89';
const campaignSeriesId = '985c65ad-f919-4c87-a40d-7445868dc587';
const campaignId = 'b9514b8e-ecbc-4085-bc44-c6f377152f32';
const wave1CampaignId = '42c9c2e1-8ad2-465d-8a91-6f81b8e8a101';
const wave2CampaignId = '42c9c2e1-8ad2-465d-8a91-6f81b8e8a102';
const launchSnapshotId = '28a948ff-3d3a-4534-91bb-adc3840163d5';
const assignmentId = 'a8f5d2e2-48b8-4651-9d61-163381d8e4a9';
const sessionId = '6c361316-9447-4644-a41f-b048d80d3d1b';
const scoreRunId = '6e0dd7a6-3d3a-4d66-82d8-3d3ad17efb67';
const exportArtifactId = '8a768e8c-6df1-451f-b2cd-c7a69b29dc70';
const exportChecksum = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';
const openLinkToken =
	'opn_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQ';
const wave1OpenLinkToken =
	'opn_11111111111141118111111111111111_waveoneabcdefghijklmnopqrstuvwxyzABC';
const wave2OpenLinkToken =
	'opn_11111111111141118111111111111111_wavetwoabcdefghijklmnopqrstuvwxyzABC';
const inviteTokenA =
	'inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDE';
const inviteTokenB =
	'inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDF';
const deliveredInviteTokenA =
	'inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDE2';
const deliveredInviteTokenB =
	'inv_11111111111141118111111111111111_abcdefghijklmnopqrstuvwxyzABCDF2';

const sampleAuthSession = {
	userId: '22222222-2222-4222-8222-222222222222',
	tenantId: '11111111-1111-4111-8111-111111111111',
	email: 'owner@example.test',
	permissions: ['setup.manage']
};

async function routeAuthenticatedSession(page: Page, session = sampleAuthSession) {
	await page.route('**/auth/session', async (route) => {
		await route.fulfill({ json: session });
	});
}

async function routeCsrfToken(page: Page) {
	await page.route('**/auth/csrf', async (route) => {
		await route.fulfill({ json: { csrfToken: 'test-csrf-token' } });
	});
}

const sampleInstrument = {
	id: instrumentId,
	code: 'tenant-burnout-pulse',
	version: '1.0.0',
	fullName: 'Tenant burnout pulse',
	rightsStatus: 'attested_by_tenant',
	validityLabel: 'tenant_provided',
	canStartNewCampaign: true
};

const sampleTemplateVersion = {
	templateId,
	templateVersionId,
	templateName: 'Tenant burnout pulse template',
	semver: '1.0.0',
	status: 'draft',
	defaultLocale: 'en',
	instrumentId,
	sections: [
		{ id: '5bba5c37-ce54-4ec0-889e-960f881830bb', ordinal: 1, code: 'core', titleDefault: 'Core' }
	],
	scales: [
		{
			id: '7f8e6238-4710-4f6b-bd78-542f6e9fb0da',
			code: 'agreement',
			type: 'likert',
			minValue: 1,
			maxValue: 5,
			step: 1,
			naAllowed: false,
			anchors: '[{"value":1,"label":"Strongly disagree"},{"value":5,"label":"Strongly agree"}]'
		}
	],
	questions: [
		{
			id: '07d19d0a-8417-41d7-8b36-5147d96ad7f8',
			ordinal: 1,
			code: 'q01',
			type: 'likert',
			scaleId: '7f8e6238-4710-4f6b-bd78-542f6e9fb0da',
			textDefault: 'After work, I need time to recover mentally.',
			required: true,
			reverseCoded: false,
			measurementLevel: 'ordinal'
		}
	]
};

const sampleCampaign = {
	id: campaignId,
	campaignSeriesId,
	templateVersionId,
	name: 'Wave 1',
	status: 'draft',
	responseIdentityMode: 'anonymous'
};

const sampleReadiness = {
	campaignId,
	ready: true,
	issues: []
};

const sampleLaunch = {
	campaignId,
	status: 'live',
	launchSnapshotId,
	templateVersionId,
	scoringRuleId,
	responseIdentityMode: 'anonymous',
	defaultLocale: 'en',
	launchedAt: '2026-05-07T10:15:00Z'
};

const sampleOpenLink = {
	campaignId,
	assignmentId,
	token: openLinkToken,
	respondentPath: `/r/${openLinkToken}`
};

const sampleTwoWaveProof = {
	campaignSeriesId,
	proofStatus: 'ready',
	expectedWaveCount: 2,
	launchedWaveCount: 2,
	submittedWaveCount: 2,
	linkedTrajectoryCount: 1,
	completeTrajectoryCount: 1,
	waves: [
		{
			campaignId: wave1CampaignId,
			name: 'Wave 1',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			submittedResponseCount: 1
		},
		{
			campaignId: wave2CampaignId,
			name: 'Wave 2',
			status: 'live',
			responseIdentityMode: 'anonymous_longitudinal',
			submittedResponseCount: 1
		}
	]
};

const sampleWaveComparisonProof = {
	campaignSeriesId,
	proofStatus: 'ready',
	interpretationStatus: 'not_validated_interpretation',
	baselineWave: {
		campaignId: wave1CampaignId,
		name: 'Wave 1',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-08T10:00:00Z',
		scoringRuleId,
		scoringRuleKey: 'tenant-burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
		submittedResponseCount: 5
	},
	comparisonWave: {
		campaignId: wave2CampaignId,
		name: 'Wave 2',
		status: 'live',
		responseIdentityMode: 'anonymous_longitudinal',
		launchedAt: '2026-05-08T11:00:00Z',
		scoringRuleId,
		scoringRuleKey: 'tenant-burnout.total',
		scoringRuleVersion: '1.0.0',
		scoringRuleDocumentHash: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
		submittedResponseCount: 5
	},
	disclosurePolicy: {
		id: '77bd14f6-36d7-4af4-92b8-ff7931232569',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'hide_cell'
	},
	scores: [
		{
			dimensionCode: 'total',
			compatibilityStatus: 'compatible',
			disclosure: 'visible',
			baselineSubmittedResponseCount: 5,
			comparisonSubmittedResponseCount: 5,
			linkedPairCount: 5,
			baselineScoreCount: 5,
			comparisonScoreCount: 5,
			baselineMean: 3.8,
			comparisonMean: 4.6,
			aggregateDelta: 0.8,
			pairedDeltaMean: 0.8,
			suppressionReason: null,
			compatibilityReason: null
		}
	]
};

const sampleInvitationBatch = {
	campaignId,
	requestedRecipientCount: 2,
	createdInvitationCount: 2,
	invitations: [
		{
			assignmentId: '1f889d9c-63c5-4ec9-b5af-a110d995a001',
			invitationTokenId: '1f889d9c-63c5-4ec9-b5af-a110d995a002',
			notificationId: '1f889d9c-63c5-4ec9-b5af-a110d995a003',
			recipient: 'ada@example.com',
			token: inviteTokenA,
			respondentPath: `/r/${inviteTokenA}`,
			status: 'queued'
		},
		{
			assignmentId: '1f889d9c-63c5-4ec9-b5af-a110d995a004',
			invitationTokenId: '1f889d9c-63c5-4ec9-b5af-a110d995a005',
			notificationId: '1f889d9c-63c5-4ec9-b5af-a110d995a006',
			recipient: 'bo@example.com',
			token: inviteTokenB,
			respondentPath: `/r/${inviteTokenB}`,
			status: 'queued'
		}
	]
};

const sampleNotificationDelivery = {
	campaignId,
	requestedBatchSize: 25,
	processedCount: 2,
	sentCount: 2,
	failedCount: 0,
	deliveries: [
		{
			notificationId: '1f889d9c-63c5-4ec9-b5af-a110d995a003',
			recipient: 'ada@example.com',
			status: 'sent',
			provider: 'local-dev',
			providerMessageId: 'local-dev-message-1',
			respondentPath: `/r/${deliveredInviteTokenA}`,
			error: null
		},
		{
			notificationId: '1f889d9c-63c5-4ec9-b5af-a110d995a006',
			recipient: 'bo@example.com',
			status: 'sent',
			provider: 'local-dev',
			providerMessageId: 'local-dev-message-2',
			respondentPath: `/r/${deliveredInviteTokenB}`,
			error: null
		}
	]
};

const sampleRespondentCampaign = {
	campaignId,
	templateVersionId,
	name: 'Wave 1',
	status: 'draft',
	responseIdentityMode: 'anonymous',
	defaultLocale: 'en',
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

const sampleLabAssignment = {
	assignmentId,
	campaignId,
	responseIdentityMode: 'anonymous'
};

const sampleSession = {
	id: sessionId,
	assignmentId,
	locale: 'en',
	startedAt: '2026-05-07T12:00:00Z',
	submittedAt: null,
	timeTakenMs: null
};

const sampleScoreResponse = {
	scoreRunId,
	sessionId,
	scores: [{ dimensionCode: 'total', value: 4, nValid: 1, nExpected: 1, missingPolicyStatus: 'ok' }]
};

const sampleReportProof = {
	campaignId,
	campaignSeriesId,
	campaignName: 'Wave 1',
	campaignStatus: 'live',
	proofStatus: 'proof_only',
	interpretationStatus: 'not_validated_interpretation',
	launchSnapshot: {
		id: launchSnapshotId,
		templateVersionId,
		scoringRuleId,
		scoringRuleDocumentHash: 'hash',
		consentDocumentId: '5ed430c0-d1d6-445d-ae6f-0376c0c27db3',
		retentionPolicyId: '4aa6890c-1b63-4701-88bf-611c924295d8',
		disclosurePolicyId: '77bd14f6-36d7-4af4-92b8-ff7931232569',
		responseIdentityMode: 'anonymous',
		launchedAt: '2026-05-07T10:15:00Z'
	},
	disclosurePolicy: {
		id: '77bd14f6-36d7-4af4-92b8-ff7931232569',
		version: '1.0.0',
		kMin: 5,
		suppressionStrategy: 'hide_cell'
	},
	scores: [
		{
			dimensionCode: 'total',
			disclosure: 'suppressed',
			submittedResponseCount: 1,
			scoreCount: null,
			mean: null,
			min: null,
			max: null,
			suppressionReason: 'insufficient_responses'
		}
	]
};

const sampleReportProofExport = {
	id: exportArtifactId,
	campaignId,
	campaignSeriesId,
	artifactType: 'report_proof_csv_codebook',
	status: 'succeeded',
	format: 'csv_codebook',
	fileName: `campaign-${campaignId}-report-proof.csv`,
	contentType: 'text/csv',
	rowCount: 1,
	byteSize: 512,
	checksumSha256: exportChecksum,
	createdAt: '2026-05-07T12:10:00Z',
	completedAt: '2026-05-07T12:10:00Z',
	csvContent:
		'campaign_id,dimension_code,disclosure\\r\\n' +
		`${campaignId},total,suppressed\\r\\n`,
	codebookJson:
		'{"artifactType":"report_proof_csv_codebook","suppressionBasis":"same_suppression_as_report_proof","columns":[]}'
};
