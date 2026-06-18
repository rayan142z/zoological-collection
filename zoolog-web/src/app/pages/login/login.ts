import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  readonly loginData = signal({ email: '', password: '', remember: false });
  readonly showPw = signal(false);
  readonly emailFocused = signal(false);
  readonly pwFocused = signal(false);
  readonly isLoading = signal(false);
  readonly errorMsg = signal('');

  readonly specimens = signal([
    { icon: '🦋', name: 'Schwalbenschwanz', latin: 'Papilio machaon' },
    { icon: '🪲', name: 'Hirschkäfer', latin: 'Lucanus cervus' },
    { icon: '🐭', name: 'Waldmaus', latin: 'Apodemus sylvaticus' },
  ]);

  readonly panelStats = signal([
    { value: '3.4k+', label: 'Präparate' },
    { value: '18', label: 'Klassen' },
    { value: '120', label: 'Jahre' },
  ]);

  onLogin() {
    this.isLoading.set(true);
    this.errorMsg.set('');

    const { email, password } = this.loginData();

    this.auth.login(email, password).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMsg.set(err.error?.message ?? 'Anmeldung fehlgeschlagen. Bitte erneut versuchen.');
      },
    });
  }
}