import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18n } from './i18n.service';

/** {{ 'Übersicht' | t }} – impure so it re-renders when the language changes. */
@Pipe({ name: 't', pure: false })
export class TPipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(de: string, params?: Record<string, string | number>): string {
    return this.i18n.t(de, params);
  }
}
