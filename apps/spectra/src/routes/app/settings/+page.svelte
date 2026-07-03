<script lang="ts">
	import { onMount } from 'svelte';
	import {
		createProductApi,
		type TenantSettingsWorkspaceResponse
	} from '$lib/api/product';
	import { api } from '$lib/core/client';
	import { formatCount, formatDate, humanizeToken } from '$lib/core/format';
	import LoadState from '$lib/ui/LoadState.svelte';

	const product = createProductApi(api());

	let settings = $state<TenantSettingsWorkspaceResponse | null>(null);
	let loadState = $state<'loading' | 'error' | 'ready'>('loading');

	onMount(async () => {
		try {
			settings = await product.getTenantSettings();
			loadState = 'ready';
		} catch {
			loadState = 'error';
		}
	});
</script>

<svelte:head><title>Settings — Spectra</title></svelte:head>

<header class="head">
	<p class="eyebrow">Workspace</p>
	<h1 class="doc-title">Settings</h1>
</header>

<LoadState state={loadState}>
	{#if settings}
		<div class="grid">
			<section class="panel block">
				<h2 class="eyebrow">Profile</h2>
				<dl>
					<div><dt>Organization</dt><dd>{settings.profile.name}</dd></div>
					<div><dt>Workspace</dt><dd class="datum">{settings.profile.slug}</dd></div>
					<div><dt>Region</dt><dd>{settings.profile.region}</dd></div>
					<div><dt>Default locale</dt><dd class="datum">{settings.profile.defaultLocale}</dd></div>
					<div><dt>Status</dt><dd>{humanizeToken(settings.profile.status)}</dd></div>
					<div><dt>Created</dt><dd>{formatDate(settings.profile.createdAt)}</dd></div>
				</dl>
			</section>

			<section class="panel block">
				<h2 class="eyebrow">Report branding</h2>
				<dl>
					<div><dt>Organization label</dt><dd>{settings.reportBranding.organizationLabel}</dd></div>
					<div><dt>Report title</dt><dd>{settings.reportBranding.reportTitle}</dd></div>
					<div>
						<dt>Accent</dt>
						<dd>
							<span class="swatch" style={`background:${settings.reportBranding.accentColorHex}`}></span>
							<span class="datum">{settings.reportBranding.accentColorHex}</span>
						</dd>
					</div>
					<div><dt>Layout</dt><dd>{humanizeToken(settings.reportBranding.layoutVariant)}</dd></div>
				</dl>
			</section>

			<section class="panel block">
				<h2 class="eyebrow">Counts</h2>
				<dl>
					<div><dt>Studies</dt><dd class="datum">{formatCount(settings.counts.campaignSeriesCount)}</dd></div>
					<div><dt>Waves</dt><dd class="datum">{formatCount(settings.counts.campaignCount)}</dd></div>
					<div><dt>Responses</dt><dd class="datum">{formatCount(settings.counts.submittedResponseCount)}</dd></div>
					<div><dt>People</dt><dd class="datum">{formatCount(settings.counts.subjectCount)}</dd></div>
					<div><dt>Members</dt><dd class="datum">{formatCount(settings.counts.tenantMemberCount)}</dd></div>
					<div><dt>Exports</dt><dd class="datum">{formatCount(settings.counts.exportArtifactCount)}</dd></div>
				</dl>
			</section>
		</div>
	{/if}
</LoadState>

<style>
	.head {
		margin-bottom: 2rem;
	}

	.head h1 {
		font-size: 2rem;
		margin-top: 0.375rem;
	}

	.grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
		gap: 1.25rem;
		align-items: start;
	}

	.block {
		padding: 1.25rem;
	}

	dl {
		margin-top: 0.75rem;
	}

	dl div {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		padding: 0.5rem 0;
		border-bottom: 1px dashed var(--color-line);
		font-size: 0.875rem;
	}

	dl div:last-child {
		border-bottom: none;
	}

	dt {
		color: var(--color-ink-3);
	}

	dd {
		text-align: right;
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
	}

	.swatch {
		display: inline-block;
		width: 14px;
		height: 14px;
		border-radius: 3px;
		border: 1px solid var(--color-line-2);
	}
</style>
