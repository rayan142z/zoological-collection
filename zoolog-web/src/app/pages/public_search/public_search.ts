import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';

interface PublicCollection {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  createdBy: number;
  creator: {
    id: number;
    username: string;
    email: string;
    userRole: string;
    status: string;
  };
  createdAt: string;
  specimenCount: number; 
}

@Component({
  selector: 'app-public-search',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './public_search.html',
  styleUrl: './public_search.css'
})
export class PublicSearch implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  searchTerm = '';
  collections: PublicCollection[] = [];
  isLoading = false;

  ngOnInit() {
    // Initial alle öffentlichen Sammlungen laden
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.cdr.detectChanges(); // Zwingt Angular, sofort die Lade-Anzeige anzuzeigen

    const path = this.searchTerm.trim() 
        ? `collections/search-public?query=${encodeURIComponent(this.searchTerm.trim())}`
        : 'collections/search-public';
    
    this.api.get<PublicCollection[]>(path).subscribe({
        next: (data) => {
        this.collections = data;
        this.isLoading = false;
        
        // DAS HIER FEHLTE: Sagt Angular explizit, dass neue Daten da sind
        this.cdr.detectChanges(); 
        },
        error: (err) => {
        console.error('Fehler bei der Suche nach öffentlichen Sammlungen:', err);
        this.collections = [];
        this.isLoading = false;
        
        // Auch im Fehlerfall die UI aktualisieren (z.B. für die Empty-State-Anzeige)
        this.cdr.detectChanges(); 
        }
    });
    }
}