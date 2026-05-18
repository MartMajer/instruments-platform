import { getContext, setContext } from 'svelte';
import { writable, type Writable } from 'svelte/store';
import type { AuthSessionResponse } from '$lib/api/setup';

export const setupManagePermission = 'setup.manage';
export const teamManagePermission = 'team.manage';

export type ProductAuthContext = {
	session: Writable<AuthSessionResponse | null>;
};

const productAuthContextKey = Symbol('product-auth-context');

export function createProductAuthContext(
	initialSession: AuthSessionResponse | null = null
): ProductAuthContext {
	return {
		session: writable(initialSession)
	};
}

export function setProductAuthContext(context: ProductAuthContext): ProductAuthContext {
	setContext(productAuthContextKey, context);
	return context;
}

export function getProductAuthContext(): ProductAuthContext {
	return getContext<ProductAuthContext>(productAuthContextKey) ?? createProductAuthContext();
}

export function hasProductPermission(
	session: AuthSessionResponse | null,
	permission: string
): boolean {
	return session?.permissions.includes(permission) ?? false;
}
