import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AppLayoutComponent } from '@yumney/ui';

@Component({
  selector: 'yn-shopping-root',
  imports: [AppLayoutComponent],
  template: '<yn-app-layout />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
