export interface RichButtonSnapshot {
  text: string;
  disabled: boolean;
}

export interface RichLinkSnapshot {
  text: string;
  path: string;
}

export interface RichFieldSnapshot {
  label: string;
  placeholder: string;
  value: string;
  required: boolean;
}

export interface RawRichScreenSnapshot {
  label: string;
  title: string;
  url: string;
  visibleText: string;
  headings: string[];
  buttons: RichButtonSnapshot[];
  links: RichLinkSnapshot[];
  fields: RichFieldSnapshot[];
  sections: string[];
  statusMessages: string[];
}

export type RichScreenSnapshot = RawRichScreenSnapshot;

const maxVisibleTextCharacters = 24000;
const maxItemCharacters = 500;
const maxItems = 120;
const emailPattern = /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/gi;
const uuidPattern =
  /\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b/gi;
const jwtLikePattern =
  /\b[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{3,}\.[A-Za-z0-9_-]{3,}\b/g;
const participantCodeLikePattern = /\b[A-Z0-9]{4,}(?:[-_][A-Z0-9]{3,})+\b/g;

export function normalizeRichScreenSnapshot(
  snapshot: RawRichScreenSnapshot
): RichScreenSnapshot {
  return {
    label: normalizeText(snapshot.label, 120),
    title: normalizeText(snapshot.title, 300),
    url: normalizeText(snapshot.url, 800),
    visibleText: normalizeText(snapshot.visibleText, maxVisibleTextCharacters),
    headings: normalizeTextList(snapshot.headings),
    buttons: snapshot.buttons.slice(0, maxItems).map((button) => ({
      text: normalizeText(button.text, maxItemCharacters),
      disabled: button.disabled === true,
    })).filter((button) => button.text),
    links: snapshot.links.slice(0, maxItems).map((link) => ({
      text: normalizeText(link.text, maxItemCharacters),
      path: normalizeText(stripQueryAndFragment(link.path), maxItemCharacters),
    })).filter((link) => link.text || link.path),
    fields: snapshot.fields.slice(0, maxItems).map((field) => ({
      label: normalizeText(field.label, maxItemCharacters),
      placeholder: normalizeText(field.placeholder, maxItemCharacters),
      value: normalizeText(field.value, maxItemCharacters),
      required: field.required === true,
    })).filter((field) => field.label || field.placeholder || field.value),
    sections: normalizeTextList(snapshot.sections),
    statusMessages: normalizeTextList(snapshot.statusMessages),
  };
}

export function buildRichTranscriptMarkdown(snapshots: RichScreenSnapshot[]) {
  const lines = [
    '# Local UX full transcript',
    '',
    'This transcript is local-only evidence for persona review. It captures visible app text and UI structure from the local browser session.',
    '',
  ];

  snapshots.forEach((snapshot, index) => {
    lines.push(
      `## ${index + 1}. ${snapshot.title || snapshot.label}`,
      '',
      `- Label: ${snapshot.label}`,
      `- URL: ${snapshot.url}`,
      '',
      '### Headings',
      ...formatList(snapshot.headings),
      '',
      '### Buttons',
      ...formatList(
        snapshot.buttons.map((button) =>
          button.disabled ? `${button.text} (disabled)` : button.text
        )
      ),
      '',
      '### Links',
      ...formatList(
        snapshot.links.map((link) =>
          link.path ? `${link.text} -> ${link.path}` : link.text
        )
      ),
      '',
      '### Fields',
      ...formatList(
        snapshot.fields.map((field) => {
          const details = [
            field.placeholder ? `placeholder: ${field.placeholder}` : '',
            field.value ? `value: ${field.value}` : '',
            field.required ? 'required' : '',
          ].filter(Boolean).join(', ');
          return details ? `${field.label || 'Unlabelled field'} (${details})` : field.label;
        })
      ),
      '',
      '### Sections and cards',
      ...formatList(snapshot.sections),
      '',
      '### Status and alert text',
      ...formatList(snapshot.statusMessages),
      '',
      '### Visible text',
      '',
      snapshot.visibleText || '(no visible text captured)',
      ''
    );
  });

  return `${lines.join('\n').trimEnd()}\n`;
}

function formatList(values: string[]) {
  return values.length ? values.map((value) => `- ${value}`) : ['- None captured.'];
}

function normalizeTextList(values: string[]) {
  return Array.from(
    new Set(values.map((value) => normalizeText(value, maxItemCharacters)).filter(Boolean))
  ).slice(0, maxItems);
}

function normalizeText(value: string, maxCharacters: number) {
  return redactSensitiveText((value ?? '').replace(/\s+/g, ' ').trim()).slice(
    0,
    maxCharacters
  );
}

function stripQueryAndFragment(value: string) {
  return value.split(/[?#]/)[0] ?? '';
}

function redactSensitiveText(text: string) {
  return text
    .replace(emailPattern, '[redacted-email]')
    .replace(uuidPattern, '[redacted-uuid]')
    .replace(jwtLikePattern, '[redacted-token]')
    .replace(participantCodeLikePattern, '[redacted-code]');
}
