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

export type RegistrationSessionResponse = {
	email: string;
};

export type CreateRegistrationWorkspaceRequest = {
	organizationName: string;
	accessCode: string;
	returnUrl?: string;
};

export type CreateRegistrationWorkspaceResponse = {
	appUrl: string;
	tenantId: string;
	email: string;
};

export type ExistingWorkspaceSignInRequest = {
	email: string;
	returnUrl?: string;
};

export type ExistingWorkspaceSignInResponse = {
	loginUrl: string;
};

export type RegistrationApi = {
	createIntent(request: CreateRegistrationIntentRequest): Promise<CreateRegistrationIntentResponse>;
	createExistingWorkspaceSignIn(
		request: ExistingWorkspaceSignInRequest
	): Promise<ExistingWorkspaceSignInResponse>;
	getSession(): Promise<RegistrationSessionResponse>;
	createWorkspace(
		request: CreateRegistrationWorkspaceRequest
	): Promise<CreateRegistrationWorkspaceResponse>;
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
		},
		createExistingWorkspaceSignIn(request) {
			return client.request<ExistingWorkspaceSignInResponse>('/registration/workspace-sign-in', {
				method: 'POST',
				headers: {
					'content-type': 'application/json'
				},
				body: JSON.stringify(request)
			});
		},
		getSession() {
			return client.request<RegistrationSessionResponse>('/registration/session');
		},
		createWorkspace(request) {
			return client.request<CreateRegistrationWorkspaceResponse>('/registration/workspaces', {
				method: 'POST',
				headers: {
					'content-type': 'application/json'
				},
				body: JSON.stringify(request)
			});
		}
	};
}
