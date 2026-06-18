import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../services/api.service';
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

  searchTerm = '';
  selectedStatus = '';
  viewMode: 'grid' | 'list' = 'grid';
  activeCollectionName = '';
  activeCollectionId: number | null = null;

  specimens: Specimen[] = [];

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      this.activeCollectionId = id ? Number(id) : null;
      this.loadSpecimens();
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
}