import type { ApiClient } from './client';

/**
 * GDPR and operations verbs: withdrawal requests, the email do-not-contact
 * list, and operational notifications. Thin typed wrappers over the existing
 * backend slices.
 */

export type WithdrawalRequestReviewResponse = {
	requestId: string;
	targetKind: string;
	targetId: string;
	requestedAction: string;
	status: string;
	requestedAt: string;
	processedAt: string | null;
	consentRecordCount: number;
	responseSessionCount: number;
	answerCount: number;
	scoreRunCount: number;
	scoreCount: number;
	canApprove: boolean;
	canDeny: boolean;
	canExecute: boolean;
};

export type CreateWithdrawalRequestRequest = {
	targetKind: string;
	targetId: string;
	requestedAction: string;
	reasonCode?: string | null;
};

export type WithdrawalRequestResponse = {
	requestId: string;
	targetKind: string;
	targetId: string;
	requestedAction: string;
	status: string;
	requestedAt: string;
	idempotent: boolean;
};

export type WithdrawalExecutionStateResponse = {
	withdrawalEventId: string;
	status: string;
	processedAt: string | null;
	dryRun: {
		status: string;
		targetMatched: boolean;
		consentRecordCount: number;
		responseSessionCount: number;
		answerCount: number;
		scoreRunCount: number;
		scoreCount: number;
	};
};

export type EmailSuppressionResponse = {
	id: string;
	recipient: string;
	reason: string;
	source: string;
	note: string | null;
	createdAt: string;
	releasedAt: string | null;
	releaseReason: string | null;
	active: boolean;
	campaignSeriesId: string | null;
	campaignSeriesName: string | null;
};

export type ListEmailSuppressionsResponse = {
	requestedLimit: number;
	activeCount: number;
	releasedCount: number;
	suppressions: EmailSuppressionResponse[];
};

export type OperationalNotificationResponse = {
	id: string;
	notificationType: string;
	severity: string;
	status: string;
	sourceAggregateId: string;
	sourceEventType: string;
	createdAt: string;
	campaignSeriesId: string | null;
	artifactStatus: string | null;
	failureReasonCode: string | null;
	readAt: string | null;
};

export type ListOperationalNotificationsResponse = {
	requestedLimit: number;
	notifications: OperationalNotificationResponse[];
};

export type OperationalNotificationSummaryResponse = {
	unreadCount: number;
	infoUnreadCount: number;
	warningUnreadCount: number;
	latestUnreadAt: string | null;
};

function jsonRequest(method: string, body: unknown): RequestInit {
	return {
		method,
		headers: { 'content-type': 'application/json' },
		body: JSON.stringify(body)
	};
}

export function createGovernanceApi(client: ApiClient) {
	return {
		listWithdrawalRequests: () =>
			client.request<WithdrawalRequestReviewResponse[]>('/withdrawal-requests'),
		createWithdrawalRequest: (request: CreateWithdrawalRequestRequest) =>
			client.request<WithdrawalRequestResponse>(
				'/withdrawal-requests',
				jsonRequest('POST', request)
			),
		approveWithdrawalRequest: (requestId: string, reasonCode?: string | null) =>
			client.request<WithdrawalRequestReviewResponse>(
				`/withdrawal-requests/${requestId}/approve`,
				jsonRequest('POST', { reasonCode: reasonCode ?? null })
			),
		denyWithdrawalRequest: (requestId: string, reasonCode?: string | null) =>
			client.request<WithdrawalRequestReviewResponse>(
				`/withdrawal-requests/${requestId}/deny`,
				jsonRequest('POST', { reasonCode: reasonCode ?? null })
			),
		executeWithdrawalRequest: (requestId: string) =>
			client.request<WithdrawalExecutionStateResponse>(
				`/withdrawal-requests/${requestId}/execute`,
				jsonRequest('POST', {})
			),

		listEmailSuppressions: () =>
			client.request<ListEmailSuppressionsResponse>('/email-suppressions?includeReleased=true'),
		addEmailSuppression: (request: { recipient: string; reason?: string | null; note?: string | null }) =>
			client.request<EmailSuppressionResponse>(
				'/email-suppressions',
				jsonRequest('POST', request)
			),
		releaseEmailSuppression: (suppressionId: string, reason?: string | null) =>
			client.request<EmailSuppressionResponse>(
				`/email-suppressions/${suppressionId}/release`,
				jsonRequest('POST', { reason: reason ?? null })
			),

		listOperationalNotifications: (limit = 25) =>
			client.request<ListOperationalNotificationsResponse>(
				`/operational-notifications?limit=${limit}`
			),
		getOperationalNotificationSummary: () =>
			client.request<OperationalNotificationSummaryResponse>('/operational-notifications/summary'),
		markOperationalNotificationRead: (notificationId: string) =>
			client.request<OperationalNotificationResponse>(
				`/operational-notifications/${notificationId}/mark-read`,
				jsonRequest('POST', {})
			),
		markAllOperationalNotificationsRead: () =>
			client.request<{ markedReadCount: number; readAt: string }>(
				'/operational-notifications/mark-all-read',
				jsonRequest('POST', {})
			)
	};
}
