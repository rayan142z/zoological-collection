import { Component, inject, computed} from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Auth } from '../../services/auth';


@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  // Zugriff auf Auth Service und Router
  private auth = inject(Auth);
  private router = inject(Router);

  // Signal mit dem aktuell eingeloggten Nutzer
  currentUser = this.auth.currentUser;

  isAdminOrMod = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    
    // Beispiel: Falls deine Rolle als String im User-Objekt liegt
    const role = (user as any).role; 
    return role === 'admin' || role === 'moderator';
  });

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
