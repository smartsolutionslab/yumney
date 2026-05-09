import {
  Component,
  ChangeDetectionStrategy,
  ViewEncapsulation,
  computed,
  input,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export type MessageBannerTone = 'error' | 'success';

@Component({
  selector: 'yn-message-banner',
  imports: [TranslocoPipe],
  templateUrl: './message-banner.component.html',
  styleUrl: './message-banner.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageBannerComponent {
  tone = input.required<MessageBannerTone>();
  message = input.required<string>();
  params = input<Record<string, unknown> | undefined>(undefined);
  testId = input<string | undefined>(undefined);

  protected readonly bannerClass = computed(() => `${this.tone()}-banner`);
  protected readonly role = computed(() => (this.tone() === 'error' ? 'alert' : 'status'));
  protected readonly ariaLive = computed(() => (this.tone() === 'error' ? null : 'polite'));
}
