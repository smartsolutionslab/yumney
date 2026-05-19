import { Injectable, inject, signal } from '@angular/core';
import { UserPreferencesService } from './user-preferences.service';
import { parseVoiceCommand, type VoiceCommand } from './voice-command-parser';

export { type VoiceCommand };

interface SpeechRecognitionLike {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  start(): void;
  stop(): void;
  onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
  onerror: ((event: { error: string }) => void) | null;
  onend: (() => void) | null;
}

interface SpeechRecognitionConstructor {
  new (): SpeechRecognitionLike;
}

const SPEECH_RATE_BY_SPEED: Record<'slow' | 'normal' | 'fast', number> = {
  slow: 0.8,
  normal: 1.0,
  fast: 1.3,
};

@Injectable({ providedIn: 'root' })
export class VoiceService {
  private preferences = inject(UserPreferencesService);

  readonly ttsSupported = signal(this.detectTtsSupport());
  readonly sttSupported = signal(this.detectSttSupport());
  readonly isListening = signal(false);
  readonly isSpeaking = signal(false);
  readonly muted = signal(false);

  private recognition: SpeechRecognitionLike | null = null;
  private currentLang = 'en-US';

  setLanguage(lang: 'en' | 'de'): void {
    this.currentLang = lang === 'de' ? 'de-DE' : 'en-US';
    if (this.recognition) {
      this.recognition.lang = this.currentLang;
    }
  }

  speak(text: string): void {
    if (!this.ttsSupported() || this.muted() || text.trim() === '') {
      return;
    }
    // Lazy-load voice prefs the first time we speak, so a cold visit to
    // cook mode (no detour through settings) still honours enabled/speed.
    this.preferences.ensureLoaded();
    if (!this.preferences.voiceEnabled()) {
      return;
    }
    const synth = window.speechSynthesis;
    synth.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = this.currentLang;
    utterance.rate = SPEECH_RATE_BY_SPEED[this.preferences.voiceSpeed()];
    utterance.onstart = () => this.isSpeaking.set(true);
    utterance.onend = () => this.isSpeaking.set(false);
    utterance.onerror = () => this.isSpeaking.set(false);
    synth.speak(utterance);
  }

  stopSpeaking(): void {
    if (this.ttsSupported()) {
      window.speechSynthesis.cancel();
      this.isSpeaking.set(false);
    }
  }

  setMuted(muted: boolean): void {
    this.muted.set(muted);
    if (muted) {
      this.stopSpeaking();
    }
  }

  startListening(onCommand: (command: VoiceCommand) => void): void {
    this.beginSession({
      continuous: true,
      onTranscript: (transcript) => {
        const command = parseVoiceCommand(transcript);
        if (command) onCommand(command);
      },
    });
  }

  stopListening(): void {
    if (this.recognition && this.isListening()) {
      this.recognition.stop();
      this.isListening.set(false);
    }
  }

  /**
   * Cook-mode + chat hybrid capture (US-362). Listens continuously and tries to
   * match each utterance against the known cook-mode patterns first
   * (next/previous/repeat/stop/ingredients/timer). If the utterance matches,
   * <paramref name="onCommand" /> fires and the transcript is consumed. If no
   * cook-mode pattern matches, the raw transcript is handed to
   * <paramref name="onTranscript" /> so the caller can route it to the chat
   * pipeline (global commands like "add butter to the shopping list",
   * "what's for dinner tomorrow?").
   *
   * Cook-mode precedence is intentional: AC TC-362-03 — "timer 5 minutes" must
   * fire the local timer, not a chat round-trip.
   */
  startListeningWithFallback(onCommand: (command: VoiceCommand) => void, onTranscript: (transcript: string) => void): void {
    this.beginSession({
      continuous: true,
      onTranscript: (transcript) => {
        if (transcript === '') return;
        const command = parseVoiceCommand(transcript);
        if (command) {
          onCommand(command);
          return;
        }
        onTranscript(transcript);
      },
    });
  }

  /**
   * Push-to-talk capture for the command bar (US-360). Unlike the cook-mode
   * `startListening` path — which keeps the mic open for back-to-back commands
   * and only emits when the utterance matches a known pattern — this returns
   * the raw transcript of a single utterance and auto-stops on silence.
   *
   * The browser auto-ends the recognition session once the user finishes
   * speaking (continuous=false), so the caller doesn't need to call
   * `stopListening` unless cancelling mid-utterance.
   *
   * <paramref name="onNoSpeech" /> fires when the recognition session ends
   * without ever capturing a transcript (browsers signal this either by
   * firing `onerror` with `error="no-speech"`, or by firing `onend` after
   * the silence-timeout without any `onresult`). Callers use it to surface
   * a "didn't catch that" hint to the user — AC TC-360-05.
   */
  startListeningForTranscript(onTranscript: (transcript: string) => void, onNoSpeech?: () => void): void {
    let gotTranscript = false;
    this.beginSession({
      continuous: false,
      onTranscript: (transcript) => {
        if (transcript !== '') {
          gotTranscript = true;
          onTranscript(transcript);
        }
      },
      // 'no-speech' is the browser's "silence timeout" signal; surface it
      // to the caller so the UI can prompt "didn't catch that". Other
      // errors (aborted, audio-capture, not-allowed, network) are not
      // no-speech — leave them to the silent failure path the chat panel
      // already handles via lastInputWasVoice reset.
      onError: (event) => {
        if (event.error === 'no-speech' && !gotTranscript) onNoSpeech?.();
      },
      // Some browsers (Chromium on Android) fire onend without an explicit
      // 'no-speech' onerror when the silence timeout elapses. Cover that
      // path too.
      onEnd: () => {
        if (!gotTranscript) onNoSpeech?.();
      },
    });
  }

  private beginSession(config: {
    continuous: boolean;
    onTranscript: (transcript: string) => void;
    onError?: (event: { error: string }) => void;
    onEnd?: () => void;
  }): void {
    if (!this.sttSupported() || this.isListening()) return;

    const recognition = this.createRecognition();
    if (!recognition) return;

    recognition.lang = this.currentLang;
    recognition.continuous = config.continuous;
    recognition.interimResults = false;
    recognition.onresult = (event) => {
      const last = event.results[event.results.length - 1];
      const transcript = last[0]?.transcript?.trim() ?? '';
      config.onTranscript(transcript);
    };
    recognition.onerror = (event) => {
      this.isListening.set(false);
      config.onError?.(event);
    };
    recognition.onend = () => {
      this.isListening.set(false);
      config.onEnd?.();
    };

    this.recognition = recognition;
    recognition.start();
    this.isListening.set(true);
  }

  private createRecognition(): SpeechRecognitionLike | null {
    const ctor =
      (window as unknown as { SpeechRecognition?: SpeechRecognitionConstructor }).SpeechRecognition ??
      (window as unknown as { webkitSpeechRecognition?: SpeechRecognitionConstructor }).webkitSpeechRecognition;
    return ctor ? new ctor() : null;
  }

  private detectTtsSupport(): boolean {
    return typeof window !== 'undefined' && 'speechSynthesis' in window;
  }

  private detectSttSupport(): boolean {
    if (typeof window === 'undefined') return false;
    return 'SpeechRecognition' in window || 'webkitSpeechRecognition' in (window as unknown as Record<string, unknown>);
  }
}
