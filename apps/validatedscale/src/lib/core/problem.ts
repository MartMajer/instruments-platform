import { ApiError } from '$lib/api/client';

/**
 * The API rejects with RFC-7807 problem bodies whose `title` carries a stable
 * error code (e.g. "registration.invalid_access_code") and whose `detail` is a
 * human-readable sentence. Resolve a user-facing message in that order:
 * mapped code → backend detail → caller's fallback.
 */
export function problemMessage(
	error: unknown,
	codeMap: Record<string, string>,
	fallback: string
): string {
	if (!(error instanceof ApiError)) return fallback;

	const body = error.body as { title?: unknown; detail?: unknown } | null;
	const code = typeof body?.title === 'string' ? body.title : null;
	if (code && codeMap[code]) return codeMap[code];

	if (typeof body?.detail === 'string' && body.detail.trim().length > 0) {
		return body.detail;
	}

	return fallback;
}
