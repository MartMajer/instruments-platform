<script lang="ts">
	import { setupStageStatusLabels, type SetupStageStatus } from '$lib/setup/stages';

	type ProductStatus =
		| SetupStageStatus
		| 'archived'
		| 'proof'
		| 'demo'
		| 'visible'
		| 'suppressed'
		| 'preliminary'
		| 'preliminary_live'
		| 'pending'
		| 'empty'
		| 'failed'
		| 'unsupported'
		| 'neutral'
		| 'not_available'
		| 'not_configured'
		| 'proof_only'
		| 'draft'
		| 'scheduled'
		| 'live'
		| 'closed'
		| 'cancelled';

	const productStatusLabels: Record<ProductStatus, string> = {
		...setupStageStatusLabels,
		archived: 'Archived',
		proof: 'Preview',
		demo: 'Demo data',
		visible: 'Visible',
		suppressed: 'Suppressed',
		preliminary: 'Preliminary',
		preliminary_live: 'Preliminary live',
		pending: 'Pending',
		empty: 'Empty',
		failed: 'Failed',
		unsupported: 'Unsupported',
		neutral: 'Neutral',
		not_available: 'Not available',
		not_configured: 'Not configured',
		proof_only: 'Preview',
		draft: 'Draft',
		scheduled: 'Scheduled',
		live: 'Live',
		closed: 'Closed',
		cancelled: 'Cancelled'
	};

	let { status, label }: { status: ProductStatus; label?: string } = $props();
</script>

<span class="status-badge" data-status={status}>{label ?? productStatusLabels[status]}</span>
