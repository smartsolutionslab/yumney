import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '@yumney/shared/auth';
import { createAsyncState, ERROR_MAPS } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';
import { UserProfileApiService } from '../api';

const CONFIRMATION_TOKEN = 'DELETE';

@Component({
  selector: 'yn-danger-zone',
  standalone: true,
  imports: [FormsModule, TranslocoModule, AsyncStateComponent],
  templateUrl: './danger-zone.component.html',
  styleUrl: './danger-zone.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DangerZoneComponent {
  protected readonly confirmationToken = CONFIRMATION_TOKEN;

  private api = inject(UserProfileApiService);
  private auth = inject(AuthService);
  private deleteState = createAsyncState();

  protected confirmationInput = signal('');
  protected deleting = this.deleteState.isLoading;
  protected error = this.deleteState.serverError;

  protected canDelete = computed(
    () => this.confirmationInput().trim() === CONFIRMATION_TOKEN && !this.deleting(),
  );

  protected onDelete(): void {
    if (!this.canDelete()) return;

    this.deleteState.execute(this.api.deleteAccount(), ERROR_MAPS.account.save, () => {
      // Wipe the local session and bounce the user back to the IdP login page.
      // The Keycloak account is already gone, so the redirect lands on a fresh
      // sign-in screen.
      this.auth.logout();
    });
  }
}
