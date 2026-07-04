import { createSetupApi } from '$lib/api/setup';
import { api } from './client';

/**
 * Download an export artifact. Prefers the signed URL (external object storage,
 * used on staging); falls back to the direct authenticated download endpoint
 * and a client-side blob when object storage is absent (local dev).
 */
export async function downloadExportArtifact(artifactId: string, fileName?: string | null): Promise<void> {
	const setup = createSetupApi(api());

	try {
		const signed = await setup.getExportArtifactSignedDownloadUrl(artifactId);
		location.assign(signed.url);
		return;
	} catch {
		// fall through to direct download
	}

	const file = await setup.downloadExportArtifactCsv(artifactId);
	const url = URL.createObjectURL(file.content);
	const anchor = document.createElement('a');
	anchor.href = url;
	anchor.download = file.fileName || fileName || `export-${artifactId}.csv`;
	document.body.appendChild(anchor);
	anchor.click();
	anchor.remove();
	URL.revokeObjectURL(url);
}
