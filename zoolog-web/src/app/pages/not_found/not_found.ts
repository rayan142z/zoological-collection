import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TPipe } from '../../i18n/t.pipe';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink, TPipe],
  template: `
    <section class="not-found">
      <p class="code">404</p>
      <h1 class="serif">{{ 'Seite nicht gefunden' | t }}</h1>
      <p>{{ 'Diese Seite existiert nicht oder wurde verschoben.' | t }}</p>
      <a class="primary-button" routerLink="/dashboard">{{ 'Zur Übersicht' | t }}</a>
    </section>
  `,
  styles: `
    .not-found { max-width: 480px; margin: 96px auto; padding: 0 16px; text-align: center; }
    .code { font-family: 'Playfair Display', serif; font-size: 64px; color: var(--green-mid); margin: 0; }
    h1 { margin: 8px 0 12px; }
    p { color: var(--text-muted); margin-bottom: 24px; }
  `,
})
export class NotFound {}
