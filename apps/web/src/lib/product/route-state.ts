import { createApiClient } from '../api/client';
import { createProductApi } from '../api/product';
import {
	createSessionHeadersFromEnv,
	defaultDevTenantId,
	defaultDevUserId
} from '../api/session-headers';
import { createSetupApi } from '../api/setup';
import { toProductApiErrorMessage } from './view-models';

export { defaultDevTenantId, defaultDevUserId };

export function createDevelopmentHeadersFromEnv(
	env: Record<string, string | undefined>
): HeadersInit | undefined {
	return createSessionHeadersFromEnv(env);
}

export function createProductApiFromEnv(env: Record<string, string | undefined>) {
	return createProductApi(
		createApiClient({
			defaultHeaders: createDevelopmentHeadersFromEnv(env),
			csrf: true
		})
	);
}

export function createSetupApiFromEnv(env: Record<string, string | undefined>) {
	return createSetupApi(
		createApiClient({
			defaultHeaders: createDevelopmentHeadersFromEnv(env),
			csrf: true
		})
	);
}

export function createProductRequestGate() {
	let currentRequestId = 0;

	return {
		next() {
			currentRequestId += 1;
			return currentRequestId;
		},
		isCurrent(requestId: number) {
			return requestId === currentRequestId;
		}
	};
}

export function toSelectedSeriesErrorMessage(error: unknown, fallback: string) {
	return toProductApiErrorMessage(error, fallback);
}
