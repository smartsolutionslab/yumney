import { Directive, ElementRef, inject, output } from '@angular/core';

@Directive({
  selector: '[ynClickOutside]',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class ClickOutsideDirective {
  clickOutside = output<void>();
  escape = output<void>();

  private host = inject(ElementRef<HTMLElement>);

  protected onDocumentClick(event: MouseEvent): void {
    const target = event.target;
    if (target instanceof Node && !this.host.nativeElement.contains(target)) {
      this.clickOutside.emit();
    }
  }

  protected onEscape(): void {
    this.escape.emit();
  }
}
