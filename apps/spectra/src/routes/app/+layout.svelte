<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { session, loadSession, logoutUrl } from '$lib/core/session.svelte';

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

	onMount(async () => {
		const current = await loadSession();
		if (!current) {
			location.assign('/signin');
		}
	});

	function closeMenu(event: MouseEvent) {
		if (menuOpen && event.target instanceof Element && !event.target.closest('.account')) {
			menuOpen = false;
		}
	}
</script>

<svelte:window onclick={closeMenu} />

{#if session.status === 'authenticated'}
	<div class="app">
		<header class="topbar">
			<a class="brand eyebrow" href="/app">Spectra</a>

			<nav aria-label="Primary">
				{#each nav as item (item.href)}
					<a href={item.href} class:active={isActive(item)} aria-current={isActive(item) ? 'page' : undefined}>
						{item.label}
					</a>
				{/each}
			</nav>

			<div class="account">
				<button
					class="account-trigger"
					aria-haspopup="menu"
					aria-expanded={menuOpen}
					onclick={() => (menuOpen = !menuOpen)}
				>
					<span class="avatar datum" aria-hidden="true">{initials}</span>
					<span class="account-email">{session.current?.email ?? 'Account'}</span>
				</button>

				{#if menuOpen}
					<div class="menu panel" role="menu">
						<a role="menuitem" href="/app/settings">Workspace settings</a>
						<a role="menuitem" href={logoutUrl()}>Sign out</a>
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
		<span class="eyebrow">Spectra</span>
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
		color: #fff;
		font-size: 0.8125rem;
		text-decoration: none;
		flex-shrink: 0;
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

	.account {
		position: relative;
		margin-left: auto;
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
