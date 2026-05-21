export type RecipientImportRowStatus = 'valid' | 'invalid' | 'duplicate';

export type RecipientImportReviewRow = {
	id: string;
	sourceText: string;
	email: string;
	displayName: string | null;
	status: RecipientImportRowStatus;
	reason: string;
};

export type RecipientImportReview = {
	rows: RecipientImportReviewRow[];
	validRows: RecipientImportReviewRow[];
	invalidRows: RecipientImportReviewRow[];
	duplicateRows: RecipientImportReviewRow[];
	validRecipientCount: number;
	invalidCount: number;
	duplicateCount: number;
	hasBlockingIssues: boolean;
	recipients: Array<{ email: string }>;
};

export type RecipientImportEntry = {
	email: string;
	displayName?: string | null;
};

const emailPattern = /^[^\s@<>;,]+@[^\s@<>;,]+\.[^\s@<>;,]+$/i;
const emailFinderPattern = /[^\s@<>;,]+@[^\s@<>;,]+\.[^\s@<>;,]+/i;
export const maxRecipientImportRecipients = 500;

export function reviewRecipientImport(input: string): RecipientImportReview {
	const seen = new Set<string>();
	const rows = splitRecipientInput(input).filter((row) => !isRecipientHeaderRow(row)).map((sourceText, index) => {
		const parsed = parseRecipientSource(sourceText);
		const id = `recipient-${index + 1}`;

		if (!parsed.email || !isValidEmail(parsed.email)) {
			return {
				id,
				sourceText,
				email: parsed.email ?? '',
				displayName: parsed.displayName,
				status: 'invalid' as const,
				reason: 'Enter a valid email address.'
			};
		}

		const email = parsed.email.toLowerCase();
		if (seen.has(email)) {
			return {
				id,
				sourceText,
				email,
				displayName: parsed.displayName,
				status: 'duplicate' as const,
				reason: 'This email appears more than once in this import.'
			};
		}

		if (seen.size >= maxRecipientImportRecipients) {
			return {
				id,
				sourceText,
				email,
				displayName: parsed.displayName,
				status: 'invalid' as const,
				reason: `Use at most ${maxRecipientImportRecipients} recipients per wave.`
			};
		}

		seen.add(email);
		return {
			id,
			sourceText,
			email,
			displayName: parsed.displayName,
			status: 'valid' as const,
			reason: 'Ready to invite.'
		};
	});

	const validRows = rows.filter((row) => row.status === 'valid');
	const invalidRows = rows.filter((row) => row.status === 'invalid');
	const duplicateRows = rows.filter((row) => row.status === 'duplicate');

	return {
		rows,
		validRows,
		invalidRows,
		duplicateRows,
		validRecipientCount: validRows.length,
		invalidCount: invalidRows.length,
		duplicateCount: duplicateRows.length,
		hasBlockingIssues: invalidRows.length > 0 || duplicateRows.length > 0,
		recipients: validRows.map((row) => ({ email: row.email }))
	};
}

export async function readRecipientImportFile(file: File): Promise<string> {
	const maxBytes = 256 * 1024;
	if (file.size > maxBytes) {
		throw new Error('Recipient file is too large. Use a CSV or text file under 256 KB.');
	}

	const text = await file.text();
	if (!text.trim()) {
		throw new Error('Recipient file is empty.');
	}

	return text;
}

export function appendRecipientImportEntry(input: string, entry: RecipientImportEntry): string {
	const line = formatRecipientImportEntry(entry);
	if (!line) {
		return input;
	}

	return [input.trim(), line].filter(Boolean).join('\n');
}

export function keepValidRecipientImportRows(input: string): string {
	return reviewRecipientImport(input)
		.validRows.map((row) =>
			formatRecipientImportEntry({ email: row.email, displayName: row.displayName })
		)
		.filter(Boolean)
		.join('\n');
}

function formatRecipientImportEntry(entry: RecipientImportEntry): string {
	const email = cleanEmail(entry.email);
	const displayName = cleanDisplayName(entry.displayName ?? '');
	return displayName ? `${displayName} <${email}>` : email;
}

function splitRecipientInput(input: string): string[] {
	const normalized = input.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
	const lines = normalized
		.split('\n')
		.map((line) => line.trim())
		.filter(Boolean);

	if (lines.length > 1) {
		return lines.flatMap(splitLineWithoutBreakingNameEmailRows);
	}

	return splitLineWithoutBreakingNameEmailRows(lines[0] ?? '');
}

function splitLineWithoutBreakingNameEmailRows(line: string): string[] {
	const trimmed = line.trim();
	if (!trimmed) {
		return [];
	}

	const angleEmailMatches = trimmed.match(/<[^>]+@[^>]+>/g)?.length ?? 0;
	if (angleEmailMatches > 1) {
		return trimmed
			.split(/[;,]/)
			.map((part) => part.trim())
			.filter(Boolean);
	}

	if (angleEmailMatches === 1 || /\t/.test(trimmed)) {
		return [trimmed];
	}

	const delimiterMatches = trimmed.match(/[,;]/g)?.length ?? 0;
	const emailMatches = trimmed.match(new RegExp(emailFinderPattern, 'gi'))?.length ?? 0;
	if (delimiterMatches > 0 && emailMatches > 1) {
		return trimmed
			.split(/[;,]/)
			.map((part) => part.trim())
			.filter(Boolean);
	}

	return [trimmed];
}

function isRecipientHeaderRow(row: string) {
	const cells = row
		.toLowerCase()
		.split(/[\t,;]/)
		.map((cell) => cell.trim())
		.filter(Boolean);

	return cells.includes('email') || cells.includes('e-mail') || cells.includes('mail');
}

function parseRecipientSource(sourceText: string): { email: string | null; displayName: string | null } {
	const trimmed = sourceText.trim();
	const angleMatch = trimmed.match(/^(.*?)<([^>]+)>$/);
	if (angleMatch) {
		return {
			displayName: cleanDisplayName(angleMatch[1]),
			email: cleanEmail(angleMatch[2])
		};
	}

	const cells = trimmed
		.split(/[\t,;]/)
		.map((cell) => cell.trim())
		.filter(Boolean);
	const emailCell = cells.find((cell) => emailFinderPattern.test(cell));
	if (emailCell) {
		return {
			displayName: cleanDisplayName(cells.find((cell) => cell !== emailCell) ?? ''),
			email: cleanEmail(emailCell.match(emailFinderPattern)?.[0] ?? emailCell)
		};
	}

	const emailMatch = trimmed.match(emailFinderPattern);
	return {
		displayName: null,
		email: emailMatch ? cleanEmail(emailMatch[0]) : trimmed
	};
}

function cleanDisplayName(value: string | undefined): string | null {
	const cleaned = (value ?? '').trim().replace(/^"|"$/g, '');
	return cleaned || null;
}

function cleanEmail(value: string | undefined): string {
	return (value ?? '').trim().replace(/^mailto:/i, '').toLowerCase();
}

function isValidEmail(value: string) {
	return emailPattern.test(value.trim());
}
