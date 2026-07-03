/**
 * Backend read models emit old-app route paths (`/app/campaign-series/{id}/reports`,
 * `/app/directory`, …). Map them onto this app's information architecture.
 */
export function mapPlatformRoute(route: string): string {
	const series = route.match(/^\/app\/campaign-series\/([^/]+)(?:\/(setup|operations|reports|waves))?\/?$/);
	if (series) {
		const [, id, surface] = series;
		if (surface === 'operations') return `/app/studies/${id}/field`;
		if (surface === 'reports' || surface === 'waves') return `/app/studies/${id}/evidence`;
		return `/app/studies/${id}`;
	}

	if (route === '/app/campaign-series') return '/app/studies';
	if (route === '/app/directory') return '/app/people';
	if (route === '/app/team') return '/app/settings';
	return route;
}
