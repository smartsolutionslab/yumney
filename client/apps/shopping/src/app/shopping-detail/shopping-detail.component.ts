import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { ShoppingApiService, ShoppingListDetail } from '@yumney/shared/api-client';
import { createAsyncState, HttpErrorMap } from '@yumney/shared/models';

@Component({
  selector: 'yn-shopping-detail',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './shopping-detail.component.html',
  styleUrl: './shopping-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingDetailComponent implements OnInit {
  private static readonly errorMap: HttpErrorMap = {
    404: 'shopping.detail.errors.notFound',
    default: 'shopping.detail.errors.generic',
  };

  private shoppingApi = inject(ShoppingApiService);
  private route = inject(ActivatedRoute);
  private async = createAsyncState(inject(DestroyRef));

  shoppingList = signal<ShoppingListDetail | null>(null);
  isLoading = this.async.isLoading;
  serverError = this.async.serverError;

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('shopping.detail.errors.notFound');
      return;
    }

    this.async.execute(
      this.shoppingApi.getShoppingListById(identifier),
      ShoppingDetailComponent.errorMap,
      (list) => this.shoppingList.set(list),
    );
  }
}
