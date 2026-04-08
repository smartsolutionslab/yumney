import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'yn-voice-indicator',
  standalone: true,
  template: `
    <div class="voice-indicator" [class.listening]="listening()" [attr.aria-live]="'polite'">
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M12 14a3 3 0 0 0 3-3V5a3 3 0 0 0-6 0v6a3 3 0 0 0 3 3z" fill="currentColor" />
        <path
          d="M19 11a1 1 0 0 0-2 0 5 5 0 0 1-10 0 1 1 0 0 0-2 0 7 7 0 0 0 6 6.92V21h-2a1 1 0 0 0 0 2h6a1 1 0 0 0 0-2h-2v-3.08A7 7 0 0 0 19 11z"
          fill="currentColor"
        />
      </svg>
    </div>
  `,
  styleUrl: './voice-indicator.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoiceIndicatorComponent {
  listening = input.required<boolean>();
}
