// AS: Diese Komponente steuert die "Neues Objekt erfassen"-Seite und ermöglicht eine Live-Vorschau auf der rechten Seite.

import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-new-object',
  imports: [RouterLink, FormsModule],
  templateUrl: './new_object.html',
  styleUrl: './new_object.css'
})
export class NewObject {

  // AS: Diese Felder werden per [(ngModel)] an die Eingabefelder gebunden und ermöglichen eine Live-Vorschau der eingegebenen Daten.
  name = '';
  genus = '';
  species = '';
  status = '';

  // AS: Gibt die passende CSS-Klasse für das Status-Badge zurück,
  // damit die Farbe (grün, blau, rot, grau) zur Vorschau-Card passt
  getStatusClass(): string {
    const map: Record<string, string> = {
      'verfügbar':   'status-available',
      'ausgeliehen': 'status-loaned',
      'verloren':    'status-lost',
      'zerstört':    'status-destroyed',
    };
    return map[this.status] || '';
  }
}
