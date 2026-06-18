import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { Auth } from '../../services/auth';

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
  private readonly auth = inject(Auth);
  private readonly cdr = inject(ChangeDetectorRef);

  showNewCollection = false;
  activeTab = 'mine';

  // "Favoriten" und "Ausgeliehen" gibt es im Backend noch nicht (kein Favorites-Konzept,
  // kein LoanController. Als "—" markiert statt einer falschen Zahl.
  quickStats = [
    { icon: '🗂️', value: '0',  label: 'Meine Sammlungen' },
    { icon: '🔬', value: '0',  label: 'Objekte gesamt' },
    { icon: '⭐', value: '—',  label: 'Favoriten' },
    { icon: '📤', value: '—',  label: 'Ausgeliehen' },
  ];

  myCollections: Collection[] = [];

  favCollections: Collection[] = [
    { id: 4,  name: 'Meeresschnecken',    description: '', emoji: '🐚', color: 'linear-gradient(135deg,#e3f2fd,#bbdefb)', objectCount: 31, tags: [], lastUpdated: '', owner: 'Prof. K. Huber' },
    { id: 5,  name: 'Vögel Deutschlands', description: '', emoji: '🐦', color: 'linear-gradient(135deg,#f3e5f5,#e1bee7)', objectCount: 18, tags: [], lastUpdated: '', owner: 'Dr. A. Berger' },
    { id: 6,  name: 'Pilze & Flechten',   description: '', emoji: '🍄', color: 'linear-gradient(135deg,#fce4ec,#f8bbd0)', objectCount: 9,  tags: [], lastUpdated: '', owner: 'L. Vogel' },
    { id: 7,  name: 'Spinnen Europas',    description: '', emoji: '🕷️', color: 'linear-gradient(135deg,#efebe9,#d7ccc8)', objectCount: 22, tags: [], lastUpdated: '', owner: 'M. Schreiber' },
    { id: 8,  name: 'Fossilien Siegen',   description: '', emoji: '🦴', color: 'linear-gradient(135deg,#e8eaf6,#c5cae9)', objectCount: 14, tags: [], lastUpdated: '', owner: 'Archiv' },
  ];

  popularCollections: Collection[] = [
    { id: 9,  name: 'Schmetterlinge Europas', description: 'Über 40 Arten aus ganz Europa — von Tagfaltern bis Nachtfaltern', emoji: '🦋', color: 'linear-gradient(135deg,#e8f5e9,#a5d6a7)', objectCount: 43, tags: [], lastUpdated: '', views: 1240 },
    { id: 10, name: 'Käfer der Welt',         description: 'Die artenreichste Tiergruppe — präzise erfasst und klassifiziert',  emoji: '🪲', color: 'linear-gradient(135deg,#fff3e0,#ffcc80)', objectCount: 67, tags: [], lastUpdated: '', views: 980  },
    { id: 11, name: 'Heimische Vögel',         description: 'Singvögel, Greifvögel und Wasservögel aus Deutschland',            emoji: '🐦', color: 'linear-gradient(135deg,#e3f2fd,#90caf9)', objectCount: 29, tags: [], lastUpdated: '', views: 754  },
    { id: 12, name: 'Meeresmuscheln',          description: 'Muschelschalen aus Nord- und Ostsee sowie dem Mittelmeer',         emoji: '🐚', color: 'linear-gradient(135deg,#fce4ec,#f48fb1)', objectCount: 18, tags: [], lastUpdated: '', views: 612  },
  ];

  totalObjects(cols: Collection[]): number {
    return cols.reduce((sum, c) => sum + c.objectCount, 0);
  }

  ngOnInit(): void {
    this.loadMyCollections();
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
}