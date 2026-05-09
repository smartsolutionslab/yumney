import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoModule } from '@jsverse/transloco';
import { ShoppingApiService, ShoppingListSummary } from '../api';
import { createAsyncState, ERROR_MAPS, ROUTES } from '@yumney/shared/models';
import { EmptyStateComponent, LoadingSpinnerComponent, MessageBannerComponent } from '@yumney/ui';

@Component({
  selector: 'yn-shopping-list',
  imports: [
    TranslocoModule,
    RouterLink,
    DatePipe,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    MessageBannerComponent,
  ],
  templateUrl: './shopping-list.component.html',
  styleUrl: './shopping-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private shoppingApi = inject(ShoppingApiService);
  private asyncState = createAsyncState(inject(DestroyRef));

  lists = signal<ShoppingListSummary[]>([]);
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;

  ngOnInit(): void {
    this.asyncState.execute(
      this.shoppingApi.getShoppingLists(),
      ERROR_MAPS.shopping.list,
      (lists) => this.lists.set(lists),
    );
  }
}
