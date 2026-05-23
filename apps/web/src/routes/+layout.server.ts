import {
	localeCookieName,
	normalizeAppLocale,
	preferredAppLocaleFromAcceptLanguage
} from '$lib/i18n/localization';

export function load({ cookies, request, url }) {
	const explicitLocale = url.searchParams.get('locale') ?? url.searchParams.get('lang');
	const locale = explicitLocale
		? normalizeAppLocale(explicitLocale)
		: normalizeAppLocale(
				cookies.get(localeCookieName) ??
					preferredAppLocaleFromAcceptLanguage(request.headers.get('accept-language'))
			);

	if (explicitLocale) {
		cookies.set(localeCookieName, locale, {
			path: '/',
			httpOnly: false,
			sameSite: 'lax',
			secure: url.protocol === 'https:',
			maxAge: 60 * 60 * 24 * 365
		});
	}

	return {
		appLocale: locale
	};
}
