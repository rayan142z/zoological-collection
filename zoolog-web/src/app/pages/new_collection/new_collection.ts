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
  styleUrl: './new_collection.css'
})
export class NewCollection {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  collectionName = '';
  visibility = 'public'; 
  description = '';

  onSubmit(): void {
   
    if (!this.collectionName || this.collectionName.trim().length < 2) {
      alert('Der Name muss mindestens 2 Zeichen lang sein.');
      return;
    }

    
    const payload = {
      name: this.collectionName.trim(),
      description: this.description.trim() || null, 
      isPublic: this.visibility === 'public'        
    };

    
    this.api.post<CollectionResponse>('collections', payload).subscribe({
      next: (newCollection) => {
        console.log('Sammlung erfolgreich erstellt:', newCollection);
        
        
        this.router.navigate(['/objects', newCollection.id]);
      },
      error: (err) => {
        console.error('Fehler beim Erstellen der Sammlung:', err);
        
        if (err.status === 401) {
          alert('Du musst eingeloggt sein, um eine Sammlung zu erstellen.');
        } else {
          alert('Die Sammlung konnte nicht gespeichert werden. Bitte überprüfe deine Eingaben.');
        }
      }
    });
  }

  onImport(): void {
    console.log('Import-Button geklickt (Aktuell noch ohne Funktion)');
  }
}