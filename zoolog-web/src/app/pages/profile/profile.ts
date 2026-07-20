import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../services/auth';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-profile',
  imports: [RouterLink, NgClass, FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile {
  private auth = inject(Auth);
  private router = inject(Router);
  private api = inject(ApiService); // Nutzt nun deinen zentralen Api-Service

  currentUser = this.auth.currentUser;
  
  // Zustand für den Bearbeitungsmodus
  isEditing = signal<boolean>(false);

  // Lokaler Zwischenspeicher für das Formular
  editForm = {
    username: '',
    email: '',
    job: '',
    description: ''
  };

  // Wechselt in den Editier-Modus und befüllt das Formular mit den aktuellen Werten
  startEditing(): void {
    const user = this.currentUser();
    if (user) {
      this.editForm = {
        username: user.username,
        email: user.email,
        job: user.job || '',
        description: user.description || ''
      };
      this.isEditing.set(true);
    }
  }

  cancelEditing(): void {
    this.isEditing.set(false);
  }

  // Sendet die aktualisierten Daten über deinen Api-Service ans Backend
  saveChanges(): void {
    const user = this.currentUser();
    if (!user) return;

    // Nutzt jetzt 'this.api.put' analog zu deinem funktionierenden Beispiel
    this.api.put(`users/${user.id}`, this.editForm).subscribe({
      next: () => {
        // Wenn erfolgreich, das globale Signal im AuthService mit den neuen Daten füttern
        const updatedUser = {
          ...user,
          username: this.editForm.username,
          name: this.editForm.username, // Alias synchron halten
          email: this.editForm.email,
          job: this.editForm.job,
          description: this.editForm.description
        };

        // Session im LocalStorage und Signal aktualisieren
        localStorage.setItem('zoolog_current_user', JSON.stringify(updatedUser));
        this.auth.currentUser.set(updatedUser);

        this.isEditing.set(false);
      },
      error: (err) => {
        console.error('Fehler beim Aktualisieren des Profils', err);
        alert('Änderungen konnten nicht gespeichert werden.');
      }
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}