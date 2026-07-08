import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, switchMap } from 'rxjs';
import { environment } from '../../environments/environment';

// Beschreibt die Nutzerdaten für eingeloggten Nutzer
export interface User {
  id: number;
  email: string;
  username: string;
  role: 'user' | 'moderator' | 'admin';
  status: string;
}

interface LoginResponse {
  message: string;
  token: string;
  expiresInMinutes: number;
  user: {
    id: number;
    username: string;
    email: string;
    role: string;
    status: string;
  };
}

interface RegisterResponse {
  id: number;
  username: string;
  email: string;
  userRole: string;
  status: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly http = inject(HttpClient);

  // Schlüsselnamen für die Daten im Browser
  private readonly userStorageKey = 'zoolog_current_user';
  private readonly tokenStorageKey = 'zoolog_token';

  // speichert den eingeloggten Nutzer. Wenn niemand eingeloggt ist, ist der Wert null
  currentUser = signal<User | null>(this.loadUserFromStorage());

  login(usernameOrEmail: string, password: string): Observable<User> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { usernameOrEmail, password })
      .pipe(map((response) => this.persistSession(response.token, response.user)));
  }

  // Die Registrierung selbst liefert kein JWT zurück (siehe UsersController.Create),
  // deshalb direkt im Anschluss einloggen, um das bisherige
  // "Registrieren = sofort eingeloggt"-Verhalten zu erhalten.
  register(username: string, email: string, password: string): Observable<User> {
    return this.http
      .post<RegisterResponse>(`${environment.apiUrl}/users`, { username, email, pass: password })
      .pipe(switchMap(() => this.login(username, password)));
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.userStorageKey);
    this.currentUser.set(null);
  }

  isLoggedIn(): boolean {
    return this.currentUser() !== null;
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  private persistSession(token: string, rawUser: LoginResponse['user']): User {
    const user: User = {
      id: rawUser.id,
      email: rawUser.email,
      username: rawUser.username,
      role: rawUser.role as User['role'],
      status: rawUser.status,
    };

    localStorage.setItem(this.tokenStorageKey, token);
    localStorage.setItem(this.userStorageKey, JSON.stringify(user));
    this.currentUser.set(user);

    return user;
  }

  private loadUserFromStorage(): User | null {
    const storedUser = localStorage.getItem(this.userStorageKey);
    const token = localStorage.getItem(this.tokenStorageKey);

    if (!storedUser || !token || !this.isTokenValid(token)) {
      localStorage.removeItem(this.tokenStorageKey);
      localStorage.removeItem(this.userStorageKey);
      return null;
    }

    try {
      return JSON.parse(storedUser) as User;
    } catch {
      // Falls die gespeicherten Daten beschädigt oder ungültig sind, werden sie gelöscht
      localStorage.removeItem(this.tokenStorageKey);
      localStorage.removeItem(this.userStorageKey);
      return null;
    }
  }

  private isTokenValid(token: string): boolean {
    try {
      const payloadSegment = token.split('.')[1];
      const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(atob(base64)) as { exp?: number };

      return typeof payload.exp === 'number' && payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }
}
