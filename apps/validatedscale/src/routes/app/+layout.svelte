<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { session, loadSession, logoutUrl } from '$lib/core/session.svelte';
	import DialogHost from '$lib/ui/DialogHost.svelte';
	import { initLocale, localeState, setLocale, t } from '$lib/core/locale.svelte';
	import {
		createGovernanceApi,
		type OperationalNotificationResponse
	} from '$lib/api/governance';
	import { api } from '$lib/core/client';
	import { formatDateTime, humanizeToken } from '$lib/core/format';

	let { children } = $props();

	let menuOpen = $state(false);

	const nav = [
		{ href: '/app', label: 'Today', exact: true },
		{ href: '/app/studies', label: 'Studies', exact: false },
		{ href: '/app/instruments', label: 'Instruments', exact: false },
		{ href: '/app/people', label: 'People', exact: false },
		{ href: '/app/exports', label: 'Exports', exact: false }
	];

	const initials = $derived.by(() => {
		const email = session.current?.email ?? '';
		return email.slice(0, 2).toUpperCase() || '··';
	});

	function isActive(item: { href: string; exact: boolean }): boolean {
		return item.exact
			? page.url.pathname === item.href
			: page.url.pathname.startsWith(item.href);
	}

	// operational notifications: failures need a place to appear
	const governance = createGovernanceApi(api());
	let bellOpen = $state(false);
	let unreadCount = $state(0);
	let notifications = $state<OperationalNotificationResponse[]>([]);

	async function refreshSummary() {
		try {
			const summary = await governance.getOperationalNotificationSummary();
			unreadCount = summary.unreadCount;
		} catch {
			// the bell stays quiet if the summary is unavailable
		}
	}

	async function toggleBell() {
		bellOpen = !bellOpen;
		if (bellOpen) {
			try {
				const list = await governance.listOperationalNotifications(15);
				notifications = list.notifications;
			} catch {
				notifications = [];
			}
		}
	}

	async function markAllRead() {
		try {
			await governance.markAllOperationalNotificationsRead();
			await refreshSummary();
			const list = await governance.listOperationalNotifications(15);
			notifications = list.notifications;
		} catch {
			// leave the list as-is; the next open retries
		}
	}

	onMount(async () => {
		initLocale();
		const current = await loadSession();
		if (!current) {
			location.assign('/signin');
			return;
		}
		void refreshSummary();
	});

	function closeMenu(event: MouseEvent) {
		if (menuOpen && event.target instanceof Element && !event.target.closest('.account')) {
			menuOpen = false;
		}
		if (bellOpen && event.target instanceof Element && !event.target.closest('.bell-area')) {
			bellOpen = false;
		}
	}
</script>

<svelte:window onclick={closeMenu} />

<DialogHost />

{#if session.status === 'authenticated'}
	<div class="app">
		<header class="topbar">
			<a class="brand" href="/app">
				<img src="/logo.svg" alt="" class="brand-logo" />
				<span class="eyebrow brand-word">ValidatedScale</span>
			</a>

			<nav aria-label="Primary">
				{#each nav as item (item.href)}
					<a href={item.href} class:active={isActive(item)} aria-current={isActive(item) ? 'page' : undefined}>
						{t(item.label)}
					</a>
				{/each}
			</nav>

			<div class="bell-area">
				<button
					class="bell"
					aria-haspopup="menu"
					aria-expanded={bellOpen}
					aria-label={t('System events')}
					onclick={toggleBell}
				>
					<svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
						<path
							d="M8 1.5a4.5 4.5 0 0 0-4.5 4.5v2.8L2.3 11a.6.6 0 0 0 .53.9h10.34a.6.6 0 0 0 .53-.9l-1.2-2.2V6A4.5 4.5 0 0 0 8 1.5Z"
							stroke="currentColor"
							stroke-width="1.3"
							stroke-linejoin="round"
						/>
						<path d="M6.5 14a1.5 1.5 0 0 0 3 0" stroke="currentColor" stroke-width="1.3" />
					</svg>
					{#if unreadCount > 0}<span class="bell-count datum">{unreadCount}</span>{/if}
				</button>

				{#if bellOpen}
					<div class="menu panel bell-menu" role="menu">
						<div class="bell-head">
							<span class="eyebrow">{t('System events')}</span>
							{#if unreadCount > 0}
								<button class="bell-mark" onclick={markAllRead}>{t('Mark all read')}</button>
							{/if}
						</div>
						{#if notifications.length === 0}
							<p class="bell-empty">{t('No system events. Failures and data-request updates appear here.')}</p>
						{:else}
							<ul class="bell-list">
								{#each notifications as notification (notification.id)}
									<li class:unread={!notification.readAt}>
										<span class="bell-type" class:warn={notification.severity === 'warning'}>
											{t(humanizeToken(notification.notificationType))}
										</span>
										<span class="datum bell-meta">
											{formatDateTime(notification.createdAt)}
											{#if notification.failureReasonCode}
												· {t(humanizeToken(notification.failureReasonCode))}
											{/if}
										</span>
									</li>
								{/each}
							</ul>
						{/if}
					</div>
				{/if}
			</div>

			<div class="account">
				<button
					class="account-trigger"
					aria-haspopup="menu"
					aria-expanded={menuOpen}
					onclick={() => (menuOpen = !menuOpen)}
				>
					<span class="avatar datum" aria-hidden="true">{initials}</span>
					<span class="account-email">{session.current?.email ?? t('Account')}</span>
				</button>

				{#if menuOpen}
					<div class="menu panel" role="menu">
						<a role="menuitem" href="/app/settings">{t('Workspace settings')}</a>
						<a role="menuitem" href="/app/privacy">{t('Privacy & data requests')}</a>
						<a role="menuitem" href="/app/help">{t('How ValidatedScale works')}</a>
						<a role="menuitem" href={logoutUrl()}>{t('Sign out')}</a>
						<div class="locale-row" role="group" aria-label="Language">
							<button class:on={localeState.current === 'en'} onclick={() => setLocale('en')}>EN</button>
							<button class:on={localeState.current === 'hr'} onclick={() => setLocale('hr')}>HR</button>
						</div>
					</div>
				{/if}
			</div>
		</header>

		<main class="content">
			{@render children()}
		</main>
	</div>
{:else}
	<div class="boot" role="status" aria-live="polite">
		<span class="eyebrow">ValidatedScale</span>
		<span class="boot-rail rail-h" aria-hidden="true"></span>
	</div>
{/if}

<style>
	.app {
		min-height: 100dvh;
		display: flex;
		flex-direction: column;
	}

	.topbar {
		display: flex;
		align-items: center;
		gap: 2rem;
		height: 3.25rem;
		padding: 0 1.5rem;
		background: var(--color-ink);
		color: #fff;
	}

	.brand {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		text-decoration: none;
		flex-shrink: 0;
	}

	.brand-logo {
		width: 1.375rem;
		height: 1.375rem;
		border-radius: 4px;
	}

	.brand-word {
		color: #fff;
		font-size: 0.8125rem;
	}

	.topbar nav {
		display: flex;
		gap: 0.25rem;
		height: 100%;
	}

	.topbar nav a {
		display: inline-flex;
		align-items: center;
		padding: 0 0.875rem;
		font-size: 0.875rem;
		font-weight: 520;
		color: rgb(255 255 255 / 0.72);
		text-decoration: none;
		border-bottom: 2px solid transparent;
		border-top: 2px solid transparent;
	}

	.topbar nav a:hover {
		color: #fff;
	}

	.topbar nav a.active {
		color: #fff;
		border-bottom-color: var(--color-stain-bright);
	}

	.bell-area {
		position: relative;
		margin-left: auto;
	}

	.bell {
		display: inline-flex;
		align-items: center;
		gap: 0.375rem;
		padding: 0.375rem 0.5rem;
		background: none;
		border: 0;
		color: var(--color-topbar-ink, #fff);
		cursor: pointer;
		opacity: 0.85;
	}

	.bell:hover {
		opacity: 1;
	}

	.bell-count {
		font-size: 0.6875rem;
		font-weight: 600;
		background: var(--color-stain);
		color: #fff;
		border-radius: 999px;
		padding: 0.05rem 0.375rem;
	}

	.bell-menu {
		position: absolute;
		right: 0;
		top: calc(100% + 0.5rem);
		width: 22rem;
		max-height: 24rem;
		overflow-y: auto;
		padding: 0.875rem;
		z-index: 30;
	}

	.bell-head {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		gap: 1rem;
	}

	.bell-mark {
		font: inherit;
		font-size: 0.75rem;
		color: var(--color-stain);
		background: none;
		border: 0;
		cursor: pointer;
		padding: 0;
	}

	.bell-empty {
		margin-top: 0.625rem;
		font-size: 0.8125rem;
		color: var(--color-ink-3);
	}

	.bell-list {
		list-style: none;
		margin-top: 0.5rem;
	}

	.bell-list li {
		display: flex;
		flex-direction: column;
		gap: 0.125rem;
		padding: 0.5rem 0;
		border-bottom: 1px solid var(--color-line);
	}

	.bell-list li.unread .bell-type {
		font-weight: 600;
	}

	.bell-type {
		font-size: 0.8125rem;
		color: var(--color-ink);
	}

	.bell-type.warn {
		color: var(--color-danger);
	}

	.bell-meta {
		font-size: 0.6875rem;
		color: var(--color-ink-3);
	}

	.account {
		position: relative;
	}

	.account-trigger {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		background: none;
		border: none;
		color: rgb(255 255 255 / 0.85);
		font: inherit;
		font-size: 0.8125rem;
		cursor: pointer;
		padding: 0.25rem;
	}

	.avatar {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 1.75rem;
		height: 1.75rem;
		border-radius: 999px;
		border: 1px solid rgb(255 255 255 / 0.35);
		font-size: 0.6875rem;
	}

	.menu {
		position: absolute;
		right: 0;
		top: calc(100% + 0.5rem);
		min-width: 13rem;
		padding: 0.375rem;
		z-index: 30;
		box-shadow: 0 8px 24px rgb(20 28 38 / 0.14);
	}

	.menu a {
		display: block;
		padding: 0.5rem 0.625rem;
		font-size: 0.875rem;
		color: var(--color-ink);
		text-decoration: none;
		border-radius: 3px;
	}

	.menu a:hover {
		background: var(--color-stain-wash);
		color: var(--color-stain-deep);
	}

	.locale-row {
		display: flex;
		gap: 0.25rem;
		padding: 0.5rem 0.625rem 0.25rem;
		border-top: 1px solid var(--color-line);
		margin-top: 0.25rem;
	}

	.locale-row button {
		font: inherit;
		font-size: 0.75rem;
		font-weight: 600;
		padding: 0.25rem 0.5rem;
		border: 1px solid var(--color-line-2);
		border-radius: 3px;
		background: none;
		cursor: pointer;
		color: var(--color-ink-2);
	}

	.locale-row button.on {
		background: var(--color-stain);
		border-color: var(--color-stain);
		color: #fff;
	}

	.content {
		flex: 1;
		width: 100%;
		max-width: 74rem;
		margin: 0 auto;
		padding: 2rem 1.5rem 4rem;
	}

	.boot {
		min-height: 100dvh;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 1rem;
	}

	.boot-rail {
		width: 8rem;
		height: 7px;
		--rail-pitch: 7px;
	}

	@media (max-width: 44rem) {
		.topbar {
			gap: 0.75rem;
			padding: 0 0.75rem;
			overflow-x: auto;
		}

		.account-email {
			display: none;
		}
	}
</style>
