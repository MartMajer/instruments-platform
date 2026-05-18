import { createApiClient, type ApiClient } from '$lib/api/client';

export type CreateRegistrationIntentRequest = {
	email: string;
	organizationName: string;
	accessCode: string;
	returnUrl?: string;
};

export type CreateRegistrationIntentResponse = {
	loginUrl: string;
	expiresAt: string;
};

export type RegistrationApi = {
	createIntent(request: CreateRegistrationIntentRequest): Promise<CreateRegistrationIntentResponse>;
};

export function createRegistrationApi(client: ApiClient = createApiClient()): RegistrationApi {
	return {
		createIntent(request) {
			return client.request<CreateRegistrationIntentResponse>('/registration/intents', {
				method: 'POST',
				headers: {
					'content-type': 'application/json'
				},
				body: JSON.stringify(request)
			});
		}
	};
}
