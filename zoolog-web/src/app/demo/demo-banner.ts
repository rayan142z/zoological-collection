import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { environment } from '../../environments/environment';
import { Auth } from '../services/auth';
import { TPipe } from '../i18n/t.pipe';
import { resetDemoDb } from './demo-backend.interceptor';

const DISMISS_KEY = 'zoolog_demo_banner_hidden';

/** Thin notice shown only in the public demo build. */
@Component({
  selector: 'app-demo-banner',
  imports: [RouterLink, TPipe],
  template: `
    @if (visible()) {
      <div class="demo-banner" role="note">
        <span class="demo-pill">Demo</span>
        <span class="demo-text">{{ 'Alle Daten sind Beispieldaten und werden nur in deinem Browser gespeichert.' | t }}</span>
        @if (!auth.currentUser()) {
          <a class="demo-action" routerLink="/login">{{ 'Demo-Konto wählen' | t }}</a>
        }
        <button type="button" class="demo-action" (click)="reset()">{{ 'Demo zurücksetzen' | t }}</button>
        <button type="button" class="demo-close" (click)="dismiss()" [attr.aria-label]="'Hinweis schließen' | t">×</button>
      </div>
    }
  `,
  styles: `
    .demo-banner {
      display: flex; flex-wrap: wrap; align-items: center; gap: 8px 14px;
      padding: 8px 16px 8px 24px;
      background: var(--green-light);
      border-bottom: 1px solid var(--green-border);
      font-size: 13px; color: var(--text);
    }
    .demo-pill {
      background: var(--green-dark); color: #F0EAD6;
      font-size: 10px; font-weight: 700; letter-spacing: 1px; text-transform: uppercase;
      padding: 2px 8px; border-radius: 20px;
    }
    .demo-text { flex: 1 1 260px; }
    .demo-action {
      background: none; border: none; padding: 6px 2px; cursor: pointer;
      font: inherit; font-weight: 700; color: var(--green-dark);
      text-decoration: underline; text-underline-offset: 3px;
    }
    .demo-close {
      background: none; border: none; cursor: pointer;
      width: 32px; height: 32px; font-size: 20px; line-height: 1; color: var(--text-muted);
    }
  `,
})
export class DemoBanner {
  readonly auth = inject(Auth);
  readonly visible = signal(environment.demo && !this.isDismissed());

  reset(): void {
    resetDemoDb();
    this.auth.logout();
    window.location.reload();
  }

  dismiss(): void {
    this.visible.set(false);
    try {
      sessionStorage.setItem(DISMISS_KEY, '1');
    } catch {
      // ignore
    }
  }

  private isDismissed(): boolean {
    try {
      return sessionStorage.getItem(DISMISS_KEY) === '1';
    } catch {
      return false;
    }
  }
}
