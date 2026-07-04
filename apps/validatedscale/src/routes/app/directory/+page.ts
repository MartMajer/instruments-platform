import { redirect } from '@sveltejs/kit';

export function load({ url }) {
	// preserve query params — Microsoft's consent callback returns here
	redirect(307, `/app/people${url.search}`);
}
