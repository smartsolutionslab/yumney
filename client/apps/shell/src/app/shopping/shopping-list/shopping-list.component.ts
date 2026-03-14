import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { TranslocoModule } from '@jsverse/transloco';
import { ShoppingApiService, ShoppingListSummary } from '@yumney/shared/api-client';
import { mapHttpError, HttpErrorMap } from '@yumney/shared/models';

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
  private destroyRef = inject(DestroyRef);

  lists = signal<ShoppingListSummary[]>([]);
  isLoading = signal(false);
  serverError = signal<string | null>(null);

  ngOnInit(): void {
    this.isLoading.set(true);

    this.shoppingApi
      .getShoppingLists()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (lists) => {
          this.lists.set(lists);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.serverError.set(mapHttpError(err, ShoppingListComponent.errorMap));
        },
      });
  }
}
