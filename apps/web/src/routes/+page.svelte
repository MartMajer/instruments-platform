<script lang="ts">
	import { env } from '$env/dynamic/public';
	import { resolve } from '$app/paths';
	import { createLoginUrlFromEnv } from '$lib/api/session-headers';

	const loginUrl = createLoginUrlFromEnv(env);
</script>

<svelte:head>
	<title>Instruments Platform</title>
	<meta
		name="description"
		content="Private beta workspace for tenant-provided validated-instrument campaigns."
	/>
</svelte:head>

<section class="setup-panel" aria-labelledby="product-entry-title">
	<div class="setup-panel__header">
		<div>
			<p class="setup-panel__eyebrow">Private beta</p>
			<h1 id="product-entry-title" class="setup-panel__title">Instruments Platform</h1>
		</div>
	</div>
	<div class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_18rem]">
		<div class="grid gap-4">
			<p class="max-w-3xl text-sm leading-6 text-[var(--color-text-muted)]">
				Run tenant-provided instrument campaigns, collect responses, review aggregate reports,
				and manage the tenant workspace from the authenticated app.
			</p>
			<div class="flex flex-wrap gap-3">
				<a class="primary-button" href={resolve('/app')}>Open workspace</a>
				<a class="secondary-button" href={loginUrl}>Sign in</a>
			</div>
		</div>
		<section class="setup-callout" aria-label="Private beta boundary">
			<p class="setup-callout__key">Workspace model</p>
			<p class="setup-callout__value">Tenant app first</p>
			<p class="setup-callout__note">
				Demo tenants and proof data use the same workspace paths as regular tenant data.
			</p>
		</section>
	</div>
</section>
