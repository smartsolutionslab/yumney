import { AfterViewInit, Directive, ElementRef, OnDestroy, inject } from '@angular/core';
import { prefersReducedMotion, staggerFadeIn } from '../animation/gsap-utils';

@Directive({
  selector: '[ynStaggerNewItems]',
  standalone: true,
})
export class StaggerNewItemsDirective implements AfterViewInit, OnDestroy {
  private host = inject(ElementRef<HTMLElement>);
  private observer: MutationObserver | null = null;

  ngAfterViewInit(): void {
    this.observer = new MutationObserver((records) => this.onMutation(records));
    this.observer.observe(this.host.nativeElement, { childList: true });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.observer = null;
  }

  private onMutation(records: MutationRecord[]): void {
    if (prefersReducedMotion()) return;

    const added: Element[] = [];
    for (const record of records) {
      record.addedNodes.forEach((node) => {
        if (node.nodeType === Node.ELEMENT_NODE) added.push(node as Element);
      });
    }
    if (added.length === 0) return;

    requestAnimationFrame(() => staggerFadeIn(added));
  }
}
