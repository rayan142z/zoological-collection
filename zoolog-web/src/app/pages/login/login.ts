import { Component, inject } from '@angular/core';
import { FormsModule  } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from './../../services/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  // Zugriff auf Auth und Router
  private auth= inject(Auth);
  private router = inject(Router);

  // Formularwerte
  email = '';
  password = '';

  // Fehlermeldung
  errorMessage = '';

  onSubmit(): void {
    const success = this.auth.login(this.email, this.password);

    if (!success) {
      this.errorMessage = 'Bitte E-Mail und Passwort eingeben';
      return;
    }
    // Erfolgreiche Login leitet Nutzer zur Profilseite weiter
    this.router.navigate(['/profile']);
  }
}
