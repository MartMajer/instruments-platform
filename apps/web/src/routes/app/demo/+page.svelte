<script lang="ts">
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import SurfaceHeader from '$lib/components/SurfaceHeader.svelte';
	import StatusBadge from '$lib/components/StatusBadge.svelte';
	import { appLocaleFromPageData } from '$lib/i18n/localization';
	import { routePageCopy } from '$lib/i18n/route-copy';

	type SampleStudyId = 'workload-recovery' | 'ergonomics-risk' | 'student-wellbeing';

	const locale = $derived(appLocaleFromPageData(page.data));
	const text = $derived(routePageCopy(locale));
	const selectedSampleId = $derived(page.url.searchParams.get('sample'));

	function sampleHref(sample: SampleStudyId) {
		return `${resolve('/app/demo')}?sample=${sample}`;
	}

	const sampleStudies = $derived([
		{
			id: 'workload-recovery' as const,
			title: text.workspaceHome.sampleWorkloadTitle,
			kicker: text.workspaceHome.sampleWorkloadKicker,
			description: text.workspaceHome.sampleWorkloadBody,
			meta: text.workspaceHome.sampleWorkloadMeta,
			href: sampleHref('workload-recovery'),
			metrics: [
				{
					label: text.workspaceHome.sampleDemo.responseMetric,
					value: '412',
					note: text.workspaceHome.sampleDemo.workloadMetrics[0]
				},
				{
					label: text.workspaceHome.sampleDemo.measurementMetric,
					value: '2',
					note: text.workspaceHome.sampleDemo.workloadMetrics[1]
				},
				{
					label: text.workspaceHome.sampleDemo.resultMetric,
					value: text.workspaceHome.sampleDemo.ready,
					note: text.workspaceHome.sampleDemo.workloadMetrics[2]
				},
				{
					label: text.workspaceHome.sampleDemo.exportMetric,
					value: 'CSV',
					note: text.workspaceHome.sampleDemo.workloadMetrics[3]
				}
			],
			checks: text.workspaceHome.sampleDemo.workloadChecks,
			questions: text.workspaceHome.sampleDemo.workloadQuestions,
			findings: text.workspaceHome.sampleDemo.workloadFindings,
			files: text.workspaceHome.sampleDemo.workloadFiles
		},
		{
			id: 'ergonomics-risk' as const,
			title: text.workspaceHome.sampleErgonomicsTitle,
			kicker: text.workspaceHome.sampleErgonomicsKicker,
			description: text.workspaceHome.sampleErgonomicsBody,
			meta: text.workspaceHome.sampleErgonomicsMeta,
			href: sampleHref('ergonomics-risk'),
			metrics: [
				{
					label: text.workspaceHome.sampleDemo.responseMetric,
					value: '128',
					note: text.workspaceHome.sampleDemo.ergonomicsMetrics[0]
				},
				{
					label: text.workspaceHome.sampleDemo.measurementMetric,
					value: '1',
					note: text.workspaceHome.sampleDemo.ergonomicsMetrics[1]
				},
				{
					label: text.workspaceHome.sampleDemo.resultMetric,
					value: text.workspaceHome.sampleDemo.closed,
					note: text.workspaceHome.sampleDemo.ergonomicsMetrics[2]
				},
				{
					label: text.workspaceHome.sampleDemo.exportMetric,
					value: text.workspaceHome.sampleDemo.available,
					note: text.workspaceHome.sampleDemo.ergonomicsMetrics[3]
				}
			],
			checks: text.workspaceHome.sampleDemo.ergonomicsChecks,
			questions: text.workspaceHome.sampleDemo.ergonomicsQuestions,
			findings: text.workspaceHome.sampleDemo.ergonomicsFindings,
			files: text.workspaceHome.sampleDemo.ergonomicsFiles
		},
		{
			id: 'student-wellbeing' as const,
			title: text.workspaceHome.sampleStudentTitle,
			kicker: text.workspaceHome.sampleStudentKicker,
			description: text.workspaceHome.sampleStudentBody,
			meta: text.workspaceHome.sampleStudentMeta,
			href: sampleHref('student-wellbeing'),
			metrics: [
				{
					label: text.workspaceHome.sampleDemo.responseMetric,
					value: '275',
					note: text.workspaceHome.sampleDemo.studentMetrics[0]
				},
				{
					label: text.workspaceHome.sampleDemo.measurementMetric,
					value: '2',
					note: text.workspaceHome.sampleDemo.studentMetrics[1]
				},
				{
					label: text.workspaceHome.sampleDemo.resultMetric,
					value: text.workspaceHome.sampleDemo.ready,
					note: text.workspaceHome.sampleDemo.studentMetrics[2]
				},
				{
					label: text.workspaceHome.sampleDemo.exportMetric,
					value: 'CSV',
					note: text.workspaceHome.sampleDemo.studentMetrics[3]
				}
			],
			checks: text.workspaceHome.sampleDemo.studentChecks,
			questions: text.workspaceHome.sampleDemo.studentQuestions,
			findings: text.workspaceHome.sampleDemo.studentFindings,
			files: text.workspaceHome.sampleDemo.studentFiles
		}
	]);
	const selectedSample = $derived(
		sampleStudies.find((sample) => sample.id === selectedSampleId) ?? sampleStudies[0]
	);
</script>

<SurfaceHeader
	eyebrow={text.workspaceHome.sampleDemo.eyebrow}
	title={text.workspaceHome.sampleDemo.title}
	description={text.workspaceHome.sampleDemo.description}
	statusLabel={text.workspaceHome.sampleDemo.status}
/>

<section class="sample-demo" aria-label={text.workspaceHome.sampleDemo.aria}>
	<section class="sample-demo-hero" aria-label={selectedSample.title}>
		<div class="sample-demo-hero__copy">
			<p class="workspace-home-kicker">{selectedSample.kicker}</p>
			<h2>{selectedSample.title}</h2>
			<p>{selectedSample.description}</p>
			<div class="sample-demo-hero__actions">
				<a class="secondary-button" href={resolve('/app#sample-studies')}>
					{text.workspaceHome.sampleDemo.backToHome}
				</a>
				<StatusBadge status="demo" label={text.workspaceHome.sampleDemo.synthetic} />
			</div>
		</div>
		<aside class="sample-demo-readonly">
			<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.readOnlyTitle}</p>
			<p>{text.workspaceHome.sampleDemo.readOnlyBody}</p>
		</aside>
	</section>

	<section class="workspace-home-section" aria-label={text.workspaceHome.sampleDemo.chooseSample}>
		<div class="workspace-home-section__header">
			<div>
				<p class="workspace-home-kicker">{text.workspaceHome.examples}</p>
				<h2>{text.workspaceHome.sampleDemo.chooseSample}</h2>
			</div>
			<p>{text.workspaceHome.sampleDemo.chooseSampleBody}</p>
		</div>
		<div class="sample-demo-picker">
			{#each sampleStudies as sample}
				<a
					class={`sample-demo-picker__card ${sample.id === selectedSample.id ? 'sample-demo-picker__card--active' : ''}`}
					href={sample.href}
					aria-current={sample.id === selectedSample.id ? 'page' : undefined}
				>
					<span>{sample.kicker}</span>
					<strong>{sample.title}</strong>
					<small>{sample.meta}</small>
				</a>
			{/each}
		</div>
	</section>

	<section
		class="workspace-home-section sample-demo-detail"
		aria-label={text.workspaceHome.sampleDemo.snapshot}
	>
		<div class="workspace-home-section__header">
			<div>
				<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.snapshot}</p>
				<h2>{selectedSample.title}</h2>
			</div>
			<p>{selectedSample.meta}</p>
		</div>

		<div class="sample-demo-metric-grid">
			{#each selectedSample.metrics as metric}
				<article class="sample-demo-metric">
					<span>{metric.label}</span>
					<strong>{metric.value}</strong>
					<small>{metric.note}</small>
				</article>
			{/each}
		</div>

		<div class="sample-demo-columns">
			<section class="sample-demo-panel" aria-label={text.workspaceHome.sampleDemo.inspect}>
				<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.inspect}</p>
				<ul>
					{#each selectedSample.checks as check}
						<li>{check}</li>
					{/each}
				</ul>
			</section>
			<section class="sample-demo-panel" aria-label={text.workspaceHome.sampleDemo.questionnaire}>
				<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.questionnaire}</p>
				<ul>
					{#each selectedSample.questions as question}
						<li>{question}</li>
					{/each}
				</ul>
			</section>
			<section class="sample-demo-panel" aria-label={text.workspaceHome.sampleDemo.findings}>
				<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.findings}</p>
				<ul>
					{#each selectedSample.findings as finding}
						<li>{finding}</li>
					{/each}
				</ul>
			</section>
			<section class="sample-demo-panel" aria-label={text.workspaceHome.sampleDemo.files}>
				<p class="workspace-home-kicker">{text.workspaceHome.sampleDemo.files}</p>
				<ul>
					{#each selectedSample.files as file}
						<li>{file}</li>
					{/each}
				</ul>
			</section>
		</div>
	</section>
</section>
