import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  readonly regData = signal({
    username: '',
    email: '',
    password: '',
    terms: false
  });

  readonly showPw = signal(false);

  readonly userFocused = signal(false);
  readonly emailFocused = signal(false);
  readonly pwFocused = signal(false);

  readonly usernameOk = signal(false);
  readonly isLoading = signal(false);
  readonly errorMsg = signal('');

  readonly features = signal([
    { icon: '🛡️', title: 'Sammlung verwalten', desc: 'Erfasse und organisiere Exponate effizient.' },
    { icon: '🌿', title: 'Digitales Archiv', desc: 'Sicherer Zugriff auf alle Präparate.' },
    { icon: '🔍', title: 'Präzise Suche', desc: 'Filtere nach Taxonomie und Fundort.' },
    { icon: '📊', title: 'Statistiken', desc: 'Echtzeit-Einblicke in den Sammlungsbestand.' }
  ]);

  readonly strengthPct = computed(() => {
    const pw = this.regData().password;
    if (!pw) return 0;
    let score = 0;
    if (pw.length >= 8) score += 25;
    if (pw.length >= 12) score += 30;
    if (/[A-Z]/.test(pw)) score += 20;
    if (/[0-9]/.test(pw)) score += 25;
    return score;
  });

  readonly strengthLevel = computed(() => {
    const pct = this.strengthPct();
    if (pct < 40) return 'weak';
    if (pct < 80) return 'fair';
    return 'strong';
  });

  readonly strengthText = computed(() => {
    const level = this.strengthLevel();
    if (this.regData().password === '') return '';
    if (level === 'weak') return 'Schwach';
    if (level === 'fair') return 'Mittel';
    return 'Stark';
  });

  validateUsername() {
    this.usernameOk.set(this.regData().username.length >= 3);
  }

  onRegister() {
    if (!this.regData().terms) return;
    this.isLoading.set(true);
    this.errorMsg.set('');

    const { username, email, password } = this.regData();

    this.auth.register(username, email, password).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMsg.set(err.error?.message ?? 'Registrierung fehlgeschlagen. Bitte erneut versuchen.');
      },
    });
  }
}