import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { Auth } from '../../services/auth';
import { STATUS_EN_TO_DE } from '../../utils/status-map';

interface Specimen {
  id: number;
  name: string;
  genus: string;
  species: string;
  status: string;
  dateCollected: string;
  image: string | null;
  collectionId: number;
}

interface SpecimenApiResponse {
  id: number;
  name: string;
  dateCollected: string;
  status: string;
  photoPath: string | null;
  taxonomyId: number;
  collectionId: number;
}

interface TaxonomyApiResponse {
  id: number;
  genus: string;
  species: string;
}

interface CollectionApiResponse {
  id: number;
  name: string;
}

@Component({
  selector: 'app-objects',
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './objects.html',
  styleUrl: './objects.css',
})
export class Objects implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly auth = inject(Auth);

  searchTerm = '';
  selectedStatus = '';
  viewMode: 'grid' | 'list' = 'grid';
  activeCollectionName = '';
  activeCollectionId: number | null = null;
  isFavorited: boolean = false;
  collection: any = null;
  specimens: Specimen[] = [];

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      this.activeCollectionId = id ? Number(id) : null;
      this.loadSpecimens();
      this.checkIfFavorited();
    });
    const collectionId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadCollectionDetails(collectionId); 
  }

    private loadCollectionDetails(id: number): void {
    this.api.get<any>(`collections/${id}`).subscribe({
      next: (data) => {
        this.collection = data; // <-- Zuweisung an die Klassen-Property!
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Fehler beim Laden der Sammlung:', err)
    });
  }

  private loadSpecimens(): void {
    forkJoin({
      specimens: this.api.get<SpecimenApiResponse[]>('specimen'),
      taxonomies: this.api.get<TaxonomyApiResponse[]>('taxonomy'),
      collections: this.api.get<CollectionApiResponse[]>('collections'),
    }).subscribe({
      next: ({ specimens, taxonomies, collections }) => {
        const taxonomyById = new Map(taxonomies.map((t) => [t.id, t]));
        const collectionById = new Map(collections.map((c) => [c.id, c]));

        this.specimens = specimens.map((s) => {
          const taxonomy = taxonomyById.get(s.taxonomyId);
          return {
            id: s.id,
            name: s.name,
            genus: taxonomy?.genus ?? '',
            species: taxonomy?.species ?? '',
            status: STATUS_EN_TO_DE[s.status] ?? s.status,
            dateCollected: s.dateCollected,
            image: s.photoPath,
            collectionId: s.collectionId,
          };
        });

        this.activeCollectionName =
          this.activeCollectionId !== null
            ? collectionById.get(this.activeCollectionId)?.name ?? `Sammlung #${this.activeCollectionId}`
            : '';

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Fehler beim Laden der Objekte:', err);
        this.specimens = [];
        this.cdr.detectChanges();
      },
    });
  }

  get currentUserId(): number | undefined {
    return this.auth.currentUser()?.id;
  } 

  private checkIfFavorited(): void {
    // Signal aufrufen mit ()
    const userId = this.auth.currentUser()?.id; 
    
    if (!userId || this.activeCollectionId === null) {
      this.isFavorited = false;
      return;
    }

    this.api.get<number[]>(`collections/favorites/user/${userId}`).subscribe({
      next: (favoriteIds) => {
        this.isFavorited = favoriteIds.includes(this.activeCollectionId!);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Fehler beim Laden der Favoriten:', err);
      }
    });
  }

  toggleFavorite(): void {
    // Signal aufrufen mit ()
    const userId = this.auth.currentUser()?.id;
    
    if (!userId) {
      alert('Du musst eingeloggt sein, um Sammlungen zu favorisieren.');
      return;
    }

    if (this.activeCollectionId === null) return;

    if (this.isFavorited) {
      this.api.delete(`collections/${this.activeCollectionId}/favorite/user/${userId}`).subscribe({
        next: () => {
          this.isFavorited = false;
          this.cdr.detectChanges();
        },
        error: (err) => console.error('Fehler beim Entfernen des Favoriten:', err)
      });
    } else {
      this.api.post(`collections/${this.activeCollectionId}/favorite`, userId).subscribe({
        next: () => {
          this.isFavorited = true;
          this.cdr.detectChanges();
        },
        error: (err) => console.error('Fehler beim Hinzufügen des Favoriten:', err)
      });
    }
  }

  private get scopedToCollection(): Specimen[] {
    return this.activeCollectionId === null
      ? this.specimens
      : this.specimens.filter((s) => s.collectionId === this.activeCollectionId);
  }

  get filteredSpecimens() {
    return this.scopedToCollection.filter((s) => {
      const matchesSearch =
        s.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        s.genus.toLowerCase().includes(this.searchTerm.toLowerCase());
      const matchesStatus = !this.selectedStatus || s.status === this.selectedStatus;
      return matchesSearch && matchesStatus;
    });
  }

  countByStatus(status: string): number {
    return this.scopedToCollection.filter((s) => s.status === status).length;
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'verfügbar':   'status-available',
      'ausgeliehen': 'status-loaned',
      'verloren':    'status-lost',
      'zerstört':    'status-destroyed',
    };
    return map[status] || '';
  }

  exportCollection(): void {
    if (this.activeCollectionId === null) {
      alert('Es ist keine Sammlung zum Exportieren ausgewählt.');
      return;
    }

    const id = this.activeCollectionId;

    this.api.getBlob(`specimen/export-csv/${id}`).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        
        const nameSanitized = this.activeCollectionName ? this.activeCollectionName.replace(/[^a-zA-Z0-9]/g, '_') : id;
        a.download = `sammlung_${nameSanitized}_export.csv`;
        
        document.body.appendChild(a);
        a.click();
        
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('CSV-Export fehlgeschlagen:', err);
        alert('Die CSV-Datei konnte nicht exportiert werden.');
      }
    });
  }
}