import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { forkJoin, of } from 'rxjs';
import { catchError, timeout, first } from 'rxjs/operators';
import { Location } from '@angular/common'


const STATUS_EN_TO_DE: Record<string, string> = {
  'available': 'verfügbar',
  'loaned': 'ausgeliehen',
  'lost': 'verloren',
  'destroyed': 'zerstört'
};


interface SpecimenApiResponse {
  id: number;
  name: string;
  taxonomyId: number;
  collectionId: number;
  status: string;
  dateCollected: string;
  photoPath: string;
}

interface TaxonomyApiResponse {
  id: number;
  genus: string;
  species: string;
}

interface CollectionApiResponse {
  id: number;
  name: string;
  description: string | null;
}


interface ExtendedSpecimenDetail {
  id: number;
  name: string;
  genus: string;
  species: string;
  status: string;
  dateCollected: string;
  image: string;
  collectionName: string;
  collectionId: number;
  collectionDescription: string | null;
  description?: string;
}

@Component({
  selector: 'app-object-info',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './object_info.html',
  styleUrl: './object_info.css' 
})
export class ObjectInfo implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly location = inject(Location);

  specimen: ExtendedSpecimenDetail | null = null;
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.loadSpecimenDetails(Number(id));
    } else {
      
      const parentId = this.route.parent?.snapshot.paramMap.get('id');
      if (parentId) {
        this.loadSpecimenDetails(Number(parentId));
      } else {
        this.errorMessage = 'Keine gültige Objekt-ID übergeben.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    }
  }

  private loadSpecimenDetails(id: number): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      specimen: this.api.get<SpecimenApiResponse>(`specimen/${id}`).pipe(
        first(),
        timeout(5000),
        catchError(err => { console.error('Fehler bei specimen API:', err); return of(null); })
      ),
      taxonomies: this.api.get<TaxonomyApiResponse[]>('taxonomy').pipe(
        first(),
        timeout(5000),
        catchError(err => { console.error('Fehler bei taxonomy API:', err); return of([]); })
      ),
      collections: this.api.get<CollectionApiResponse[]>('collections').pipe(
        first(),
        timeout(5000),
        catchError(err => { console.error('Fehler bei collections API:', err); return of([]); })
      )
    }).subscribe({
      next: ({ specimen, taxonomies, collections }) => {
        if (!specimen) {
          this.errorMessage = 'Das gesuchte Objekt existiert nicht oder konnte nicht geladen werden.';
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        const taxonomy = taxonomies.find(t => t.id === specimen.taxonomyId);
        const collection = collections.find(c => c.id === specimen.collectionId);

        this.specimen = {
          id: specimen.id,
          name: specimen.name,
        
          description: (specimen as any).description || (specimen as any).Description || '',
          genus: taxonomy?.genus ?? 'Unbekannt',
          species: taxonomy?.species ?? 'Unbekannt',
          status: STATUS_EN_TO_DE[specimen.status] ?? specimen.status,
          dateCollected: specimen.dateCollected,
          image: specimen.photoPath,
          collectionName: collection?.name ?? `Sammlung #${specimen.collectionId}`,
          collectionId: specimen.collectionId,
          collectionDescription: collection?.description ?? null
        };
        
        this.isLoading = false;
        this.cdr.detectChanges(); 
      },
      error: (err) => {
        console.error('Kritischer Fehler im forkJoin Stream:', err);
        this.errorMessage = 'Fehler bei der Zusammenführung der Objektdaten.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack(): void {
    this.location.back();
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