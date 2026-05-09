import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ChatStateService } from '@yumney/shared/models';
import { HeaderComponent } from '../header/header.component';
import { ChatPanelComponent } from '../chat-panel/chat-panel.component';

@Component({
  selector: 'yn-app-layout',
  imports: [RouterOutlet, HeaderComponent, ChatPanelComponent],
  templateUrl: './app-layout.component.html',
  styleUrl: './app-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppLayoutComponent {
  protected readonly chatState = inject(ChatStateService);
}
