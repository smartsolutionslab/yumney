import { Provider } from '@angular/core';
import { LUCIDE_ICONS, LucideIconProvider } from 'lucide-angular';
import { YN_ICONS } from './yn-icons';

export function provideYumneyIcons(): Provider {
  return {
    provide: LUCIDE_ICONS,
    multi: true,
    useValue: new LucideIconProvider(YN_ICONS),
  };
}
