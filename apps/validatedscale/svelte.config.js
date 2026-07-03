import adapter from '@sveltejs/adapter-node';

const versionName = process.env.SVELTEKIT_VERSION ?? process.env.npm_package_version ?? '0.0.1';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	compilerOptions: {
		// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
		runes: ({ filename }) => (filename.split(/[/\\]/).includes('node_modules') ? undefined : true)
	},
	kit: {
		adapter: adapter(),
		version: {
			name: versionName
		}
	}
};

export default config;
