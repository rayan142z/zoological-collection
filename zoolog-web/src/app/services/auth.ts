import { Injectable, signal} from '@angular/core';

// Beschreibt die Nutzerdaten für eingeloggten Nutzer
export interface User {
  email: string;
  username?: string;
  role: 'user' | 'moderator' | 'admin';
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  // Mit diesem Schlüsselname speichern wir den aktuellen Nutzer im Browser
  private readonly storageKey = 'zoolog_current_user';

  // speichert den eingeloggten Nutzer. Wenn niemand eingeloggt ist, ist der Wert null
  currentUser = signal<User | null>(this.loadUserFromStorage());

  // Aktuell ist es noch nicht mit dem Backend verbunden
  login(email: string, password: string): boolean {
    if (!email || !password) {
      return false;
    }

    const user: User = {
      email: email,
      // Nur für Simulation wir der Username aus der Email abgeleitet
      username: email.split('@')[0], 
      role: 'user',
    };

    // Nutzer im Browser gespeichert
    localStorage.setItem(this.storageKey, JSON.stringify(user)); 
    // Ab hier gilt der Nutzer als eingeloggt
    this.currentUser.set(user);

    return true;
  }

  // Auch das ist aktuell nur eine Frontend-Simulation
  register(email: string, password: string, username?: string): boolean {
    if(!email || !password) {
      return false;
    }

    const user: User = {
      email: email,
      username: username,
      role: 'user',
    };

    localStorage.setItem(this.storageKey, JSON.stringify(user));
    this.currentUser.set(user);

    return true;
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.currentUser.set(null);
  }

  isLoggedIn(): boolean {
    return this.currentUser() !== null;
  }

  private loadUserFromStorage(): User | null {
    const storedUser = localStorage.getItem(this.storageKey);

    if (!storedUser) {
      return null;
    }

    try {
      return JSON.parse(storedUser) as User;
    } catch {
      // Falls die gespeicherten Daten beschädigt oder ungültig sind, werden sie gelöscht
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }
}
