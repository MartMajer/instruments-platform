import { redirect } from '@sveltejs/kit';
import { mapPlatformRoute } from '$lib/core/routes';

export function load({ params }) {
	const surface = params.surface ? `/${params.surface}` : '';
	redirect(307, mapPlatformRoute(`/app/campaign-series/${params.seriesId}${surface}`));
}
