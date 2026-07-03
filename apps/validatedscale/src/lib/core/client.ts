import { env } from '$env/dynamic/public';
import { createApiClient, type ApiClient } from '$lib/api/client';
import { createSessionHeadersFromEnv } from '$lib/api/session-headers';

let shared: ApiClient | null = null;

export function api(): ApiClient {
	if (!shared) {
		shared = createApiClient({
			csrf: true,
			defaultHeaders: createSessionHeadersFromEnv(env)
		});
	}

	return shared;
}
