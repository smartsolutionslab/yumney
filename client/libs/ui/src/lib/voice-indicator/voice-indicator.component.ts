import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'yn-voice-indicator',
  standalone: true,
  imports: [LucideAngularModule],
  template: `
    <div class="voice-indicator" [class.listening]="listening()" [attr.aria-live]="'polite'">
      <lucide-icon name="mic" [size]="24" />
    </div>
  `,
  styleUrl: './voice-indicator.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoiceIndicatorComponent {
  listening = input.required<boolean>();
}
