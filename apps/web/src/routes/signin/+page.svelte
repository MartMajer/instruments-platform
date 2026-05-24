<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { env } from '$env/dynamic/public';
	import { ApiError } from '$lib/api/client';
	import {
		createLoginUrlFromEnv,
		normalizeTenantId,
		readLastTenantId,
		readLastWorkspaceEmail,
		rememberLastTenantId,
		rememberLastWorkspaceEmail
	} from '$lib/api/session-headers';
	import { createRegistrationApi } from '$lib/api/registration';
	import { appLocaleFromPageData, localizedHref } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';

	const registrationApi = createRegistrationApi();

	let email = $state('');
	let rememberedEmail = $state('');
	let rememberedSignInUrl = $state('');
	let errorMessage = $state('');
	let statusMessage = $state('');
	let isSubmitting = $state(false);
	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const englishLocaleHref = $derived(localizedHref(page.url, 'en'));
	const croatianLocaleHref = $derived(localizedHref(page.url, 'hr-HR'));

	onMount(() => {
		const tenantId = readLastTenantId(window.localStorage);
		const storedEmail = readLastWorkspaceEmail(window.localStorage);

		rememberedEmail = storedEmail;
		email = storedEmail;

		if (tenantId) {
			rememberedSignInUrl = createLoginUrlFromEnv(env, tenantId, storedEmail);
		}
	});

	async function submitExistingWorkspaceSignIn(event: SubmitEvent) {
		event.preventDefault();
		errorMessage = '';
		statusMessage = '';
		isSubmitting = true;

		try {
			const response = await registrationApi.createExistingWorkspaceSignIn({
				email,
				returnUrl: absoluteWebUrl(resolve('/app'))
			});
			const loginUrl = resolveAuthRedirectUrl(response.loginUrl);
			rememberTenantFromLoginUrl(loginUrl, email);
			statusMessage = text.signIn.openingWorkspace;
			window.location.assign(loginUrl);
		} catch (error) {
			errorMessage = toWorkspaceSignInError(error);
			isSubmitting = false;
		}
	}

	function absoluteWebUrl(path: string) {
		return new URL(path, window.location.origin).toString();
	}

	function resolveAuthRedirectUrl(loginUrl: string) {
		if (/^https?:\/\//i.test(loginUrl)) {
			return loginUrl;
		}

		const authOrigin = absoluteOrigin(env.PUBLIC_AUTH_LOGIN_URL);
		if (authOrigin) {
			return new URL(loginUrl, authOrigin).toString();
		}

		const apiOrigin = absoluteOrigin(env.PUBLIC_API_BASE_URL);
		if (apiOrigin) {
			return new URL(loginUrl, apiOrigin).toString();
		}

		return loginUrl;
	}

	function absoluteOrigin(value: string | undefined) {
		if (!value || !/^https?:\/\//i.test(value)) {
			return null;
		}

		try {
			return new URL(value).origin;
		} catch {
			return null;
		}
	}

	function rememberTenantFromLoginUrl(loginUrl: string, email: string) {
		try {
			const parsedUrl = new URL(loginUrl, window.location.origin);
			const tenantId = normalizeTenantId(parsedUrl.searchParams.get('tenantId'));
			if (!tenantId) {
				return;
			}

			rememberLastTenantId(window.localStorage, tenantId);
			rememberLastWorkspaceEmail(window.localStorage, email);
		} catch {
			return;
		}
	}

	function toWorkspaceSignInError(error: unknown) {
		if (error instanceof ApiError) {
			const problem = error.body as { title?: unknown; detail?: unknown } | null;
			const code = typeof problem?.title === 'string' ? problem.title : '';
			const detail = typeof problem?.detail === 'string' ? problem.detail : '';

			if (code === 'registration.workspace_not_found') {
				return text.signIn.workspaceNotFound;
			}

			if (code === 'registration.email_invalid') {
				return text.signIn.emailInvalid;
			}

			if (detail) {
				return detail;
			}
		}

		return text.signIn.fallbackError;
	}
</script>

<svelte:head>
	<title>{text.signIn.metaTitle}</title>
	<meta
		name="description"
		content={text.signIn.metaDescription}
	/>
</svelte:head>

<section class="registration-page" aria-labelledby="signin-title">
	<div class="registration-page__glow registration-page__glow--one" aria-hidden="true"></div>
	<div class="registration-page__glow registration-page__glow--two" aria-hidden="true"></div>

	<header class="public-nav">
		<a class="launchpad-brand" href={resolve('/')}>
			<img class="launchpad-brand__mark" src="/brand/validated-scale-mark.svg" alt="" aria-hidden="true" />
			<span>
				<strong>Validated Scale</strong>
				<small>{text.signIn.brandSubtitle}</small>
			</span>
		</a>
		<div class="public-nav__actions">
			<nav class="public-nav__links" aria-label={text.signIn.navAria}>
				<a href={resolve('/')}>{text.common.product}</a>
				<a href={resolve('/register')}>{text.common.createWorkspace}</a>
			</nav>
			<div class="public-language-switcher" aria-label={text.publicEntry.languageSwitchAria}>
				<a
					class={`public-language-switcher__option${locale === 'en' ? ' public-language-switcher__option--active' : ''}`}
					href={englishLocaleHref}
					hreflang="en"
					aria-current={locale === 'en' ? 'true' : undefined}>EN</a
				>
				<a
					class={`public-language-switcher__option${locale === 'hr-HR' ? ' public-language-switcher__option--active' : ''}`}
					href={croatianLocaleHref}
					hreflang="hr-HR"
					aria-current={locale === 'hr-HR' ? 'true' : undefined}>HR</a
				>
			</div>
		</div>
	</header>

	<div class="registration-grid">
		<section class="registration-copy">
			<p class="launchpad-kicker">{text.signIn.eyebrow}</p>
			<h1 id="signin-title">{text.signIn.title}</h1>
			<p>
				{text.signIn.body}
			</p>
			<div class="registration-steps" aria-label={text.signIn.stepsAria}>
				<div>
					<span>01</span>
					<strong>{text.signIn.stepFind}</strong>
					<p>{text.signIn.stepFindBody}</p>
				</div>
				<div>
					<span>02</span>
					<strong>{text.signIn.stepSignIn}</strong>
					<p>{text.signIn.stepSignInBody}</p>
				</div>
				<div>
					<span>03</span>
					<strong>{text.signIn.stepOpenApp}</strong>
					<p>{text.signIn.stepOpenAppBody}</p>
				</div>
			</div>
		</section>

		<section class="registration-panel" aria-label={text.signIn.formAria}>
			<div class="registration-panel__header">
				<span>{text.signIn.panelEyebrow}</span>
				<strong>{text.signIn.panelTitle}</strong>
				<p>
					{text.signIn.createInsteadPrefix} <a href={resolve('/register')}>{text.signIn.createInsteadLink}</a>.
				</p>
			</div>

			{#if rememberedSignInUrl}
				<div class="registration-alert" role="status">
					<strong>{text.signIn.recentWorkspace}</strong>
					<span>
						{#if rememberedEmail}
							{text.signIn.continueAs(rememberedEmail)}
						{:else}
							{text.signIn.continueRecentBody}
						{/if}
					</span>
				</div>
				<a class="registration-submit" href={rememberedSignInUrl}>{text.signIn.continueRecent}</a>
			{/if}

			<form class="registration-form" onsubmit={submitExistingWorkspaceSignIn}>
				<label>
					<span>{text.common.email}</span>
					<input
						type="email"
						bind:value={email}
						autocomplete="email"
						placeholder="you@example.com"
						required
					/>
				</label>

				{#if errorMessage}
					<p class="registration-alert registration-alert--error" role="alert">{errorMessage}</p>
				{/if}

				{#if statusMessage}
					<p class="registration-alert registration-alert--success" role="status">{statusMessage}</p>
				{/if}

				<button class="registration-submit" type="submit" disabled={isSubmitting}>
					{isSubmitting ? text.signIn.findingWorkspace : text.signIn.continueToSignIn}
				</button>
			</form>

			<div class="registration-boundary">
				<strong>{text.common.privateBeta}</strong>
				<p>{text.signIn.betaBoundaryBody}</p>
			</div>
		</section>
	</div>
</section>
