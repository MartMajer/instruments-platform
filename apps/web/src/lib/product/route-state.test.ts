import { describe, expect, it } from 'vitest';
import { ApiError } from '../api/client';
import {
	createDevelopmentHeadersFromEnv,
	createProductRequestGate,
	toSelectedSeriesErrorMessage
} from './route-state';

describe('product route state helpers', () => {
	it('does not create development auth headers unless explicitly enabled', () => {
		expect(createDevelopmentHeadersFromEnv({ PUBLIC_DEV_AUTH_ENABLED: 'false' })).toBeUndefined();
		expect(createDevelopmentHeadersFromEnv({})).toBeUndefined();
	});

	it('creates default development auth headers when enabled', () => {
		const headers = new Headers(createDevelopmentHeadersFromEnv({ PUBLIC_DEV_AUTH_ENABLED: 'true' }));

		expect(headers.get('x-tenant-id')).toBe('11111111-1111-4111-8111-111111111111');
		expect(headers.get('x-dev-user-id')).toBe('22222222-2222-4222-8222-222222222222');
		expect(headers.get('x-dev-tenant-memberships')).toBe(
			'11111111-1111-4111-8111-111111111111'
		);
		expect(headers.get('x-dev-permissions')).toBe('setup.manage');
	});

	it('creates configured development auth headers when tenant and user are provided', () => {
		const headers = new Headers(
			createDevelopmentHeadersFromEnv({
				PUBLIC_DEV_AUTH_ENABLED: 'true',
				PUBLIC_DEV_TENANT_ID: 'tenant-a',
				PUBLIC_DEV_USER_ID: 'user-a'
			})
		);

		expect(headers.get('x-tenant-id')).toBe('tenant-a');
		expect(headers.get('x-dev-user-id')).toBe('user-a');
		expect(headers.get('x-dev-tenant-memberships')).toBe('tenant-a');
	});

	it('tracks current product route requests so stale responses can be ignored', () => {
		const gate = createProductRequestGate();

		const first = gate.next();
		const second = gate.next();

		expect(gate.isCurrent(first)).toBe(false);
		expect(gate.isCurrent(second)).toBe(true);
	});

	it('maps selected-series product API errors to display messages', () => {
		expect(
			toSelectedSeriesErrorMessage(
				new ApiError('Not found', 404, { detail: 'Campaign series was not found.' }),
				'Fallback message.'
			)
		).toBe('Campaign series was not found.');
		expect(
			toSelectedSeriesErrorMessage(new ApiError('Failure', 500, null), 'Fallback message.')
		).toBe('API request failed with status 500.');
		expect(toSelectedSeriesErrorMessage(new Error('network'), 'Fallback message.')).toBe(
			'Fallback message.'
		);
	});
});
