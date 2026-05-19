export type VoiceCommand =
  | { type: 'next' }
  | { type: 'previous' }
  | { type: 'repeat' }
  | { type: 'stop' }
  | { type: 'ingredients' }
  | { type: 'timer'; minutes: number };

const COMMAND_PATTERNS: Array<{
  match: RegExp;
  build: (groups: RegExpMatchArray) => VoiceCommand;
}> = [
  { match: /^(next step|next|nächster schritt|weiter)$/i, build: () => ({ type: 'next' }) },
  { match: /^(previous step|previous|back|vorheriger schritt|zurück)$/i, build: () => ({ type: 'previous' }) },
  { match: /^(repeat|wiederhole|wiederholen)$/i, build: () => ({ type: 'repeat' }) },
  { match: /^(stop|stopp|stop it|halt)$/i, build: () => ({ type: 'stop' }) },
  { match: /^(ingredients|zutaten)$/i, build: () => ({ type: 'ingredients' }) },
  {
    match: /^timer\s+(\d{1,3})\s*(?:minutes?|min|minuten|minute)$/i,
    build: (match) => ({ type: 'timer', minutes: Number(match[1]) }),
  },
  {
    match: /^(\d{1,3})\s*(?:minute|minuten|min)\s+timer$/i,
    build: (match) => ({ type: 'timer', minutes: Number(match[1]) }),
  },
];

export function parseVoiceCommand(transcript: string): VoiceCommand | null {
  const normalized = transcript.trim().toLowerCase();
  if (normalized === '') return null;
  for (const pattern of COMMAND_PATTERNS) {
    const match = normalized.match(pattern.match);
    if (match) return pattern.build(match);
  }
  return null;
}
