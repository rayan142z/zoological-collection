import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Auth } from '../services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  // Zugriff auf Auth-Service und Router
  const auth = inject(Auth);
  const router = inject(Router);

  // Zugriff erlauben, wenn ein Nutzer eingeloggt ist
  if (auth.isLoggedIn()) {
    return true;
  }

  // Wenn nicht zur Login Seite weiterleiten
  return router.createUrlTree(['/login']);
};
