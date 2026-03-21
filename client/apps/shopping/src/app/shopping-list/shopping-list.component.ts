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
import { ShoppingApiService, ShoppingListSummary } from '@yumney/shared/api-client';
import { createAsyncState, HttpErrorMap } from '@yumney/shared/models';

@Component({
  selector: 'yn-shopping-list',
  imports: [TranslocoModule, RouterLink, DatePipe],
  templateUrl: './shopping-list.component.html',
  styleUrl: './shopping-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingListComponent implements OnInit {
  private static readonly errorMap: HttpErrorMap = {
    default: 'shopping.list.errors.generic',
  };

  private shoppingApi = inject(ShoppingApiService);
  private async = createAsyncState(inject(DestroyRef));

  lists = signal<ShoppingListSummary[]>([]);
  isLoading = this.async.isLoading;
  serverError = this.async.serverError;

  ngOnInit(): void {
    this.async.execute(
      this.shoppingApi.getShoppingLists(),
      ShoppingListComponent.errorMap,
      (lists) => this.lists.set(lists),
    );
  }
}
