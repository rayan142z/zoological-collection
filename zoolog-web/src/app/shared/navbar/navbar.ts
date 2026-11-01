import { Component, inject, computed, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { Auth } from '../../services/auth';
import { I18n } from '../../i18n/i18n.service';
import { TPipe } from '../../i18n/t.pipe';


@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, TPipe],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
  host: { '(document:keydown.escape)': 'closeMenu()' },
})
export class Navbar {
  // Zugriff auf Auth Service und Router
  private auth = inject(Auth);
  private router = inject(Router);
  readonly i18n = inject(I18n);

  // Signal mit dem aktuell eingeloggten Nutzer
  currentUser = this.auth.currentUser;

  // Mobiles Menü (unter 900px)
  readonly menuOpen = signal(false);

  isAdminOrMod = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'admin' || role === 'moderator';
  });

  constructor() {
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => this.closeMenu());
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.closeMenu();
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
