import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';

interface CollectionResponse {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  createdBy: number;
  createdAt: string;
}

@Component({
  selector: 'app-new-collection',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './new_collection.html',
  styleUrl: './new_collection.css',
})
export class NewCollection {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  collectionName = '';
  visibility = 'public';
  description = '';
  selectedCsvFile: File | null = null;

  onSubmit(): void {
    if (!this.collectionName || this.collectionName.trim().length < 2) {
      alert('Der Name muss mindestens 2 Zeichen lang sein.');
      return;
    }

    const payload = {
      name: this.collectionName.trim(),
      description: this.description.trim() || null,
      isPublic: this.visibility === 'public',
    };
    console.log('Sende Datei:', this.selectedCsvFile);
    this.api.post<CollectionResponse>('collections', payload).subscribe({
      next: (newCollection) => {
        console.log('Sammlung erfolgreich erstellt:', newCollection);

        if (this.selectedCsvFile) {
          const formData = new FormData();
          formData.append('file', this.selectedCsvFile);

          this.api.post(`specimen/import-csv/${newCollection.id}`, formData).subscribe({
            next: (res: any) => {
              console.log('CSV erfolgreich importiert:', res);
              alert(res.message || 'Sammlung erstellt und Exemplare importiert!');
              this.router.navigate(['/objects', newCollection.id]);
            },
            error: (err) => {
              console.error('CSV-Import fehlgeschlagen:', err);
              alert(
                'Sammlung wurde erstellt, aber der CSV-Import der Exemplare ist fehlgeschlagen.',
              );

              this.router.navigate(['/objects', newCollection.id]);
            },
          });
        } else {
          this.router.navigate(['/objects', newCollection.id]);
        }
      },
      error: (err) => {
        console.error('Fehler beim Erstellen der Sammlung:', err);

        if (err.status === 401) {
          alert('Du musst eingeloggt sein, um eine Sammlung zu erstellen.');
        } else {
          alert('Die Sammlung konnte nicht gespeichert werden. Bitte überprüfe deine Eingaben.');
        }
      },
    });
  }

  onImport(): void {
    console.log('Import-Button geklickt (Aktuell noch ohne Funktion)');
  }

  onCsvSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedCsvFile = input.files[0];
    }
  }
}
