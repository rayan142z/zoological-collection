import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { Auth } from '../../services/auth';
import { HttpClient } from '@angular/common/http';

interface Collection {
  id: number;
  name: string;
  description: string;
  emoji: string;
  color: string;
  objectCount: number;
  tags: string[];
  lastUpdated: string;
  owner?: string;
  views?: number;
}

interface CollectionApiResponse {
  id: number;
  name: string;
  description: string | null;
  createdBy: number;
  createdAt: string;
}

interface SpecimenApiResponse {
  collectionId: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  private readonly api = inject(ApiService);
  readonly auth = inject(Auth);
  private readonly cdr = inject(ChangeDetectorRef);

  showNewCollection = false;
  activeTab = 'mine';
  favCollections: any[] = [];
  favoriteCount: number = 0;

  portalStats = {
    totalCollections: 124,
    totalObjects: 3.450,
    totalUsers: 48
  };

  // "Favoriten" und "Ausgeliehen" gibt es im Backend noch nicht (kein Favorites-Konzept,
  // kein LoanController. Als "—" markiert statt einer falschen Zahl.
  quickStats = [
    { icon: '🗂️', value: '0',  label: 'Meine Sammlungen' },
    { icon: '🔬', value: '0',  label: 'Objekte gesamt' },
    { icon: '⭐', value: '0',  label: 'Favoriten' },
    
  ];

  myCollections: Collection[] = [];

  

  totalObjects(cols: Collection[]): number {
    return cols.reduce((sum, c) => sum + c.objectCount, 0);
  }

  ngOnInit(): void {
    this.loadMyCollections();
    this.loadFavorites();
    this.loadPortalStats();
  }

  private loadPortalStats(): void {
  this.api.get<any>('collections/public-stats').subscribe({
    next: (stats) => {
      this.portalStats = {
        totalCollections: stats.totalCollections,
        totalObjects: stats.totalObjects,
        totalUsers: stats.totalUsers,
      };
      this.cdr.detectChanges();
    },
    error: (err) => {
      console.error('Fehler beim Laden der Portal-Statistiken:', err);
    },
  });
}

  private loadMyCollections(): void {
    const userId = this.auth.currentUser()?.id;
    if (!userId) return;

    forkJoin({
      collections: this.api.get<CollectionApiResponse[]>('collections'),
      specimens: this.api.get<SpecimenApiResponse[]>('specimen'),
    }).subscribe({
      next: ({ collections, specimens }) => {
        const mine = collections.filter((c) => c.createdBy === userId);
        this.myCollections = mine.map((c) => ({
          id: c.id,
          name: c.name,
          description: c.description ?? '',
          emoji: '🗂️',
          color: 'linear-gradient(135deg, #e8f5e9, #c8e6c9)',
          objectCount: specimens.filter((s) => s.collectionId === c.id).length,
          tags: [],
          lastUpdated: new Date(c.createdAt).toLocaleDateString('de-DE'),
        }));
        this.quickStats[0].value = String(this.myCollections.length);
        this.quickStats[1].value = String(
          this.myCollections.reduce((sum, col) => sum + col.objectCount, 0)
        );
        
        console.log(this.favCollections.length);
        // Erzwingt das Re-Rendern nach der asynchronen Antwort - sonst bleibt
        // die Anzeige bei "0", bis irgendein anderes Browser-Event zufällig
        // ein Re-Render auslöst (z.B. Klick auf einen Navigationslink).
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Fehler beim Laden der Sammlungen:', err);
      },
    });
  }

  private loadFavorites(): void {
    const userId = this.auth.currentUser()?.id;
    if (!userId) return;

    this.api.get<any[]>(`collections/favorites/user/${userId}`).subscribe({
      next: (favoriteIds) => {
        if (!favoriteIds || favoriteIds.length === 0) {
          this.favCollections = [];
          this.favoriteCount = 0;
          this.cdr.detectChanges();
          return;
        }

        // Alle IDs rigoros in Nummern umwandeln
        const numericIds = favoriteIds.map(id => Number(id));
        console.log('Aus Backend gelesene Favoriten-Nummern:', numericIds);

        forkJoin({
          collections: this.api.get<any[]>('collections'),
          specimens: this.api.get<any[]>('specimen'),
        }).subscribe({
          next: ({ collections, specimens }) => {
            console.log('Rohe Collections aus der Datenbank:', collections);

            // Filtern mit Absicherung gegen unterschiedliche ID-Schreibweisen (id vs Id)
            const rawFavorites = collections.filter((c) => {
              const collectionId = c.id ?? c.Id ?? c.collectionId;
              const isMatch = numericIds.includes(Number(collectionId));
              return isMatch;
            });

            

            // Mappen für das HTML-Template
            this.favCollections = rawFavorites.map((c) => {
              const cId = c.id ?? c.Id ?? c.collectionId;
              return {
                id: Number(cId),
                name: c.name,
                description: c.description ?? '',
                emoji: '⭐',
                color: 'linear-gradient(135deg, #fffde7, #fff9c4)',
                objectCount: specimens.filter((s) => (s.collectionId ?? s.CollectionId) === cId).length,
                tags: [],
                lastUpdated: c.createdAt ? new Date(c.createdAt).toLocaleDateString('de-DE') : 'Unbekannt',
              };
            });

            this.favoriteCount = this.favCollections.length;
            this.quickStats[2].value = String(this.favCollections.length);
            // Erzwinge das Zeichnen der UI
            this.cdr.detectChanges();

          },
          error: (err) => console.error('Fehler im forkJoin der Favoriten:', err),
        });
      },
      error: (err) => console.error('Fehler beim Laden der Favoriten-IDs:', err),
    });
  }
}