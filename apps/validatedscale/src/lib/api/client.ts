import { env } from '$env/dynamic/public';

const DEFAULT_API_BASE_URL = 'http://localhost:5055';

export type ApiClientOptions = {
	baseUrl?: string;
	defaultHeaders?: HeadersInit;
	fetch?: typeof fetch;
	csrf?: boolean;
};

export type ApiClient = {
	request<T>(path: string, init?: RequestInit): Promise<T>;
	requestText(path: string, init?: RequestInit): Promise<ApiTextResponse>;
};

export type ApiTextResponse = {
	body: string;
	contentType: string;
	contentDisposition: string | null;
	byteSize: number;
};

export class ApiError extends Error {
	constructor(
		message: string,
		public readonly status: number,
		public readonly body: unknown
	) {
		super(message);
		this.name = 'ApiError';
	}
}

export function createApiClient(options: ApiClientOptions = {}): ApiClient {
	const baseUrl = options.baseUrl ?? env.PUBLIC_API_BASE_URL ?? DEFAULT_API_BASE_URL;
	const defaultHeaders = options.defaultHeaders;
	const fetchImpl = options.fetch ?? fetch;
	const csrfEnabled = options.csrf === true;
	let csrfToken: string | null = null;

	async function getCsrfToken() {
		if (csrfToken) {
			return csrfToken;
		}

		const response = await fetchImpl(joinUrl(baseUrl, '/auth/csrf'), {
			credentials: 'include',
			headers: withDefaultHeaders(defaultHeaders, undefined)
		});
		const body = await readBody(response);

		if (!response.ok) {
			throw new ApiError(
				`API request failed with status ${response.status}`,
				response.status,
				body
			);
		}

		const token = csrfTokenFromBody(body);
		if (!token) {
			throw new ApiError('CSRF token response was invalid.', response.status, body);
		}

		csrfToken = token;
		return csrfToken;
	}

	async function buildHeaders(path: string, init: RequestInit) {
		const headers = withDefaultHeaders(defaultHeaders, init.headers);

		if (shouldAttachCsrf(csrfEnabled, path, init.method, headers)) {
			headers.set('X-CSRF-TOKEN', await getCsrfToken());
		}

		return headers;
	}

	return {
		async request<T>(path: string, init: RequestInit = {}) {
			const response = await fetchImpl(joinUrl(baseUrl, path), {
				...init,
				credentials: init.credentials ?? 'include',
				headers: await buildHeaders(path, init)
			});
			const body = await readBody(response);

			if (!response.ok) {
				throw new ApiError(
					`API request failed with status ${response.status}`,
					response.status,
					body
				);
			}

			return body as T;
		},
		async requestText(path: string, init: RequestInit = {}) {
			const response = await fetchImpl(joinUrl(baseUrl, path), {
				...init,
				credentials: init.credentials ?? 'include',
				headers: await buildHeaders(path, init)
			});
			const body = await response.text();

			if (!response.ok) {
				throw new ApiError(
					`API request failed with status ${response.status}`,
					response.status,
					body.length > 0 ? body : null
				);
			}

			return {
				body,
				contentType: response.headers.get('content-type') ?? '',
				contentDisposition: response.headers.get('content-disposition'),
				byteSize: new TextEncoder().encode(body).length
			};
		}
	};
}

function shouldAttachCsrf(
	csrfEnabled: boolean,
	path: string,
	method: string | undefined,
	headers: Headers
) {
	if (!csrfEnabled || isCsrfPath(path) || headers.has('X-CSRF-TOKEN')) {
		return false;
	}

	return isUnsafeMethod(method);
}

function isUnsafeMethod(method: string | undefined) {
	const normalized = (method ?? 'GET').toUpperCase();

	return normalized !== 'GET' && normalized !== 'HEAD' && normalized !== 'OPTIONS';
}

function isCsrfPath(path: string) {
	const normalizedPath = path.startsWith('/') ? path : `/${path}`;

	return normalizedPath.split('?')[0] === '/auth/csrf';
}

function csrfTokenFromBody(body: unknown) {
	if (body && typeof body === 'object' && 'csrfToken' in body) {
		const value = (body as { csrfToken?: unknown }).csrfToken;
		return typeof value === 'string' && value.length > 0 ? value : null;
	}

	return null;
}

function joinUrl(baseUrl: string, path: string) {
	const normalizedBase = baseUrl.replace(/\/+$/, '');
	const normalizedPath = path.startsWith('/') ? path : `/${path}`;

	return `${normalizedBase}${normalizedPath}`;
}

function withDefaultHeaders(
	defaultHeaders: HeadersInit | undefined,
	headers: HeadersInit | undefined
) {
	const normalized = new Headers(defaultHeaders);

	if (headers) {
		new Headers(headers).forEach((value, key) => normalized.set(key, value));
	}

	if (!normalized.has('accept')) {
		normalized.set('accept', 'application/json');
	}

	return normalized;
}

async function readBody(response: Response) {
	if (response.status === 204) {
		return null;
	}

	const contentType = response.headers.get('content-type') ?? '';

	if (isJsonContentType(contentType)) {
		return response.json();
	}

	const text = await response.text();
	return text.length > 0 ? text : null;
}

function isJsonContentType(contentType: string) {
	const normalized = contentType.split(';', 1)[0]?.trim().toLowerCase() ?? '';

	return normalized === 'application/json' || normalized.endsWith('+json');
}
