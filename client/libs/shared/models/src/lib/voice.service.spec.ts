import { VoiceService } from './voice.service';

describe('VoiceService.parseCommand', () => {
  it('should parse "next step" as next command', () => {
    expect(VoiceService.parseCommand('next step')).toEqual({ type: 'next' });
  });

  it('should parse German "nächster schritt" as next command', () => {
    expect(VoiceService.parseCommand('nächster schritt')).toEqual({ type: 'next' });
  });

  it('should parse "previous step" as previous command', () => {
    expect(VoiceService.parseCommand('previous step')).toEqual({ type: 'previous' });
  });

  it('should parse German "vorheriger schritt" as previous command', () => {
    expect(VoiceService.parseCommand('vorheriger schritt')).toEqual({ type: 'previous' });
  });

  it('should parse "repeat" as repeat command', () => {
    expect(VoiceService.parseCommand('repeat')).toEqual({ type: 'repeat' });
  });

  it('should parse "stop" as stop command', () => {
    expect(VoiceService.parseCommand('stop')).toEqual({ type: 'stop' });
  });

  it('should parse German "stopp" as stop command', () => {
    expect(VoiceService.parseCommand('stopp')).toEqual({ type: 'stop' });
  });

  it('should parse "ingredients" as ingredients command', () => {
    expect(VoiceService.parseCommand('ingredients')).toEqual({ type: 'ingredients' });
  });

  it('should parse German "zutaten" as ingredients command', () => {
    expect(VoiceService.parseCommand('zutaten')).toEqual({ type: 'ingredients' });
  });

  it('should parse "timer 5 minutes" as timer command with minutes', () => {
    expect(VoiceService.parseCommand('timer 5 minutes')).toEqual({ type: 'timer', minutes: 5 });
  });

  it('should parse German "timer 10 minuten" as timer command with minutes', () => {
    expect(VoiceService.parseCommand('timer 10 minuten')).toEqual({ type: 'timer', minutes: 10 });
  });

  it('should parse "5 minute timer" as timer command', () => {
    expect(VoiceService.parseCommand('5 minute timer')).toEqual({ type: 'timer', minutes: 5 });
  });

  it('should be case-insensitive', () => {
    expect(VoiceService.parseCommand('NEXT STEP')).toEqual({ type: 'next' });
  });

  it('should ignore surrounding whitespace', () => {
    expect(VoiceService.parseCommand('  next step  ')).toEqual({ type: 'next' });
  });

  it('should return null for unknown phrases', () => {
    expect(VoiceService.parseCommand('do something random')).toBeNull();
  });

  it('should return null for empty input', () => {
    expect(VoiceService.parseCommand('')).toBeNull();
  });
});
