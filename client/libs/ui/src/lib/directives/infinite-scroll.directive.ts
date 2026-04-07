import {
  Directive,
  ElementRef,
  OnDestroy,
  AfterViewInit,
  inject,
  output,
  input,
} from '@angular/core';

/**
 * Emits `loadMore` whenever the host element scrolls into view, provided
 * `enabled` is true. Designed for infinite-scroll list pagination.
 *
 * Usage:
 *   <div ynInfiniteScroll [enabled]="hasMore() && !isLoading()" (loadMore)="loadNextPage()"></div>
 */
@Directive({
  selector: '[ynInfiniteScroll]',
  standalone: true,
})
export class InfiniteScrollDirective implements AfterViewInit, OnDestroy {
  enabled = input<boolean>(true);
  loadMore = output<void>();

  private host = inject(ElementRef<HTMLElement>);
  private observer: IntersectionObserver | null = null;

  ngAfterViewInit(): void {
    this.observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting && this.enabled()) {
        this.loadMore.emit();
      }
    });
    this.observer.observe(this.host.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.observer = null;
  }
}
