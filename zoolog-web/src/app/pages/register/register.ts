import { Component, inject } from '@angular/core';
import { FormsModule  } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from './../../services/auth';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  // Zugriff auf Auth und Router
  private auth= inject(Auth);
  private router = inject(Router);

  // Formularwerte
  username = '';
  email = '';
  password = '';

  // Fehlermeldung
  errorMessage = '';

  onSubmit(): void {
    const success = this.auth.register(this.email, this.password, this.username);

    if (!success) {
      this.errorMessage = 'Bitte alle Pflichtfelder ausfüllen';
      return;
    }
    // Erfolgreiche Registrierung leitet Nutzer zur Profilseite weiter
    this.router.navigate(['/profile']);
  }
}
