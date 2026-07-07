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
	saveBlob(file.content, file.fileName || fileName || `export-${artifactId}.csv`);
}

/**
 * Download the codebook that documents a CSV export (variables, question text,
 * scales, reverse-coding, missing treatment). Served straight from the
 * artifact record, so it works with or without object storage.
 */
export async function downloadExportArtifactCodebook(artifactId: string): Promise<void> {
	const setup = createSetupApi(api());
	const file = await setup.downloadExportArtifactCodebook(artifactId);
	saveBlob(file.content, file.fileName || `export-${artifactId}-codebook.json`);
}

function saveBlob(content: Blob, fileName: string) {
	const url = URL.createObjectURL(content);
	const anchor = document.createElement('a');
	anchor.href = url;
	anchor.download = fileName;
	document.body.appendChild(anchor);
	anchor.click();
	anchor.remove();
	URL.revokeObjectURL(url);
}
