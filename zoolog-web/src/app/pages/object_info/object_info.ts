import { AssetUrlPipe } from '../../utils/asset-url.pipe';
import { Component, OnInit, inject, ChangeDetectorRef, Input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { forkJoin, of } from 'rxjs';
import { catchError, timeout, first } from 'rxjs/operators';
import { Location } from '@angular/common';
import { Auth } from '../../services/auth';
import {
  FormBuilder,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';

// Importe für Leaflet
import { LeafletModule } from '@asymmetrik/ngx-leaflet';
import * as L from 'leaflet';

import { STATUS_EN_TO_DE } from '../../utils/status-map';
import { TPipe } from '../../i18n/t.pipe';
import { I18n } from '../../i18n/i18n.service';

// Marker-Icons vom CDN (die Leaflet-Bilder werden nicht mitgebündelt)
const MARKER_ICON = L.icon({
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

interface SpecimenApiResponse {
  id: number;
  name: string;
  taxonomyId: number;
  collectionId: number;
  status: string;
  dateCollected: string;
  photoPath: string;
  description?: string;
  latitude?: number | null;
  longitude?: number | null;
  size?: string | null;
  weight?: number | null;
  birthYear?: number | null;
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
  createdBy?: number;
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
  collectionCreatedBy?: number;
  description?: string;
  latitude?: number | null;
  longitude?: number | null;
  size?: string | null;
  weight?: number | null;
  birthYear?: number | null;
}

interface UserDto {
  id: number;
  username: string;
}

@Component({
  selector: 'app-object-info',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, LeafletModule, FormsModule, ReactiveFormsModule, AssetUrlPipe, TPipe],
  templateUrl: './object_info.html',
  styleUrl: './object_info.css',
})
export class ObjectInfo implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly location = inject(Location);
  private readonly auth = inject(Auth);
  private readonly fb = inject(FormBuilder);
  private readonly i18n = inject(I18n);

  specimen: ExtendedSpecimenDetail | null = null;
  isLoading = true;
  errorMessage = '';

  @Input() collection: any;
  isAdmin: boolean = false;
  currentUserId: number | null = null;
  loanForm!: FormGroup;
  showLoanModal = false;
  lightboxOpen = false;
  linkCopied = false;

  /** Ersteller der Sammlung oder Admin dürfen bearbeiten und verleihen. */
  get canManage(): boolean {
    return !!this.specimen && (this.specimen.collectionCreatedBy === this.currentUserId || this.isAdmin);
  }

  /** Gewicht wird in kg gespeichert; kleine Werte lesbarer in Gramm. */
  formatWeight(kg: number): string {
    const fmt = (n: number) => n.toLocaleString(this.i18n.locale(), { maximumFractionDigits: 2 });
    return kg < 1 ? `${fmt(kg * 1000)} g` : `${fmt(kg)} kg`;
  }

  copyLink(): void {
    navigator.clipboard?.writeText(window.location.href).then(() => {
      this.linkCopied = true;
      this.cdr.detectChanges();
      setTimeout(() => {
        this.linkCopied = false;
        this.cdr.detectChanges();
      }, 2000);
    });
  }

  // Vollständige Benutzerliste für die Zuordnung Name <-> ID
  usersList: UserDto[] = [];
  suggestedPersons: string[] = [];
  filteredPersons: string[] = [];
  showSuggestions = false;

  // Leaflet Konfiguration
  mapOptions = {
    layers: [L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 18 })],
    zoom: 13,
    center: L.latLng(0, 0),
  };
  mapLayers: L.Layer[] = [];

  ngOnInit(): void {
    this.isAdmin = this.auth.isUserAdmin();
    this.currentUserId = this.auth.getCurrentUserId();

    // Formular initialisieren
    this.loanForm = this.fb.group({
      loanedTo: ['', Validators.required], // Hier tippt der Nutzer den Namen ein
      returnDate: ['', Validators.required],
      notes: [''],
    });

    const id =
      this.route.snapshot.paramMap.get('id') || this.route.parent?.snapshot.paramMap.get('id');
    if (id) {
      this.loadSpecimenDetails(Number(id));
    } else {
      this.errorMessage = 'Keine gültige Objekt-ID übergeben.';
      this.isLoading = false;
    }
  }

  // Lädt Benutzer für die Tipp-Vorschläge (schließt den eigenen Nutzer aus)
  loadSuggestions(): void {
    this.api.get<any[]>('users').subscribe({
      next: (users) => {
        this.usersList = users.map((u) => ({ id: u.id, username: u.username }));

        this.suggestedPersons = this.usersList
          .filter((user) => user.id !== this.currentUserId)
          .map((user) => user.username);

        this.filteredPersons = [];
      },
      error: (err) => console.error('Fehler beim Laden der Benutzer', err),
    });
  }

  onSearchChange(event: Event): void {
    const input = (event.target as HTMLInputElement).value;
    if (!input || input.length < 1) {
      this.filteredPersons = [];
      this.showSuggestions = false;
      return;
    }

    const searchTerm = input.toLowerCase();
    this.filteredPersons = this.suggestedPersons.filter((p) =>
      p.toLowerCase().includes(searchTerm),
    );
    this.showSuggestions = true;
  }

  selectPerson(person: string): void {
    this.loanForm.patchValue({ loanedTo: person });
    this.showSuggestions = false;
    this.filteredPersons = [];
  }

  onBlurWithDelay(): void {
    setTimeout(() => {
      this.showSuggestions = false;
    }, 200);
  }

  private loadSpecimenDetails(id: number): void {
    forkJoin({
      specimen: this.api.get<SpecimenApiResponse>(`specimen/${id}`).pipe(
        first(),
        timeout(5000),
        catchError(() => of(null)),
      ),
      taxonomies: this.api.get<TaxonomyApiResponse[]>('taxonomy').pipe(
        first(),
        timeout(5000),
        catchError(() => of([])),
      ),
      collections: this.api.get<CollectionApiResponse[]>('collections').pipe(
        first(),
        timeout(5000),
        catchError(() => of([])),
      ),
    }).subscribe(({ specimen, taxonomies, collections }) => {
      if (!specimen) {
        this.errorMessage = 'Objekt konnte nicht geladen werden.';
        this.isLoading = false;
        return;
      }

      const taxonomy = taxonomies.find((t) => t.id === specimen.taxonomyId);
      const matchedCollection = collections.find((c) => c.id === specimen.collectionId);

      this.specimen = {
        id: specimen.id,
        name: specimen.name,
        description: specimen.description || '',
        genus: taxonomy?.genus ?? 'Unbekannt',
        species: taxonomy?.species ?? 'Unbekannt',
        status: STATUS_EN_TO_DE[specimen.status] ?? specimen.status,
        dateCollected: specimen.dateCollected,
        image: specimen.photoPath,
        collectionName: matchedCollection?.name ?? `Sammlung #${specimen.collectionId}`,
        collectionId: specimen.collectionId,
        collectionDescription: matchedCollection?.description ?? null,
        collectionCreatedBy: matchedCollection?.createdBy ?? this.collection?.createdBy,
        latitude: specimen.latitude,
        longitude: specimen.longitude,
        size: specimen.size,
        weight: specimen.weight,
        birthYear: specimen.birthYear,
      };

      this.isLoading = false;
      this.cdr.detectChanges();
    });
  }

  onMapReady(map: L.Map): void {
    if (this.specimen?.latitude && this.specimen?.longitude) {
      const point = L.latLng(this.specimen.latitude, this.specimen.longitude);
      setTimeout(() => {
        map.invalidateSize();
        map.setView(point, 13);

        const marker = L.marker(point, { icon: MARKER_ICON });
        marker.addTo(map);

        this.mapLayers = [marker];
      }, 100);
    }
  }

  goBack(): void {
    this.location.back();
    setTimeout(() => {
      window.location.reload();
    }, 50);
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      verfügbar: 'status-available',
      ausgeliehen: 'status-loaned',
      verloren: 'status-lost',
      zerstört: 'status-destroyed',
    };
    return map[status] || '';
  }

  openLoanModal(): void {
    this.showLoanModal = true;
    this.loadSuggestions();
  }

  closeLoanModal(): void {
    this.showLoanModal = false;
  }

  submitLoan(): void {
    const typedName = this.loanForm.value.loanedTo;

    // Finde die ID des eingetippten Benutzers anhand des Namens
    const matchedUser = this.usersList.find((u) => u.username === typedName);
    if (!matchedUser) {
      alert(this.i18n.t('Bitte wähle einen gültigen Benutzer aus der Vorschlagsliste aus.'));
      return;
    }

    if (this.loanForm.invalid) {
      alert(this.i18n.t('Bitte fülle alle Pflichtfelder korrekt aus.'));
      return;
    }

    const formValues = this.loanForm.value;
    const returnDateStr = formValues.returnDate;
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const tomorrowStr = tomorrow.toISOString().split('T')[0];

    if (returnDateStr < tomorrowStr) {
      alert(this.i18n.t('Das Rückgabedatum muss mindestens auf morgen oder später liegen.'));
      return;
    }

    // Payload für das Backend: Konvertiert den Namen in die ID für loanedTo
    // und übergibt die eigene ID für loanedFrom
    const payload = {
      specimenId: this.specimen?.id,
      loanedTo: matchedUser.id, // ID des ausgewählten Ausleihers
      loanedFrom: this.currentUserId, // Eigene ID als Leihender
      loanDate: new Date().toISOString().split('T')[0],
      returnDate: returnDateStr,
      notes: formValues.notes,
      fromCollection: this.specimen?.collectionId,
      status: 'active',
    };

    this.api.post('loan', payload).subscribe({
      next: () => {
        alert(this.i18n.t('Objekt erfolgreich als verliehen eingetragen!'));
        this.closeLoanModal();
        this.goBack();
      },
      error: (err) => {
        console.error('Fehler beim Speichern des Verleihs:', err);
        alert(this.i18n.t('Fehler beim Speichern.'));
      },
    });
  }
}
