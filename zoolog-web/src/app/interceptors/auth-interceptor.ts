import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Auth } from '../services/auth';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);
  const router = inject(Router);
  const token = auth.getToken();

  // Token nur an unser eigenes Backend anhängen, nicht an beliebige externe
  // Requests (z.B. später Leaflet-Tiles oder eine Geocoding-API).
  const authReq = token && req.url.startsWith(environment.apiUrl)
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error) => {
      // Beim Login-Versuch selbst ist ein 401 ein falsches Passwort, keine
      // abgelaufene Sitzung - dort nicht umleiten, das übernimmt die Login-Seite.
      const isLoginRequest = req.url.endsWith('/auth/login');

      if (error.status === 401 && !isLoginRequest) {
        auth.logout();
        router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};