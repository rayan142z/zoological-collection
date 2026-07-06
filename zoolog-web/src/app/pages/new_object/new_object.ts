import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe, CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { Auth } from '../../services/auth';
import { STATUS_DE_TO_EN } from '../../utils/status-map';

interface TaxonomyOption {
  id: number;
  kingdom: string;
  phylum: string;
  class: string;
  orders: string;
  family: string;
  genus: string;
  species: string;
}

interface LocationOption {
  id: number;
  name: string;
  region: string;
  country: string;
}

interface CollectionOption {
  id: number;
  name: string;
  createdBy: number;
}

@Component({
  selector: 'app-new-object',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe, CommonModule],
  templateUrl: './new_object.html',
  styleUrl: './new_object.css'
})
export class NewObject implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(Auth);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  currentStep = signal(1);

  // Formular-Signals für die Grunddaten
  name          = signal('');
  description   = signal('');
  dateCollected = signal('');
  status        = signal('');
  
  collectionId  = signal<number | null>(null);
  taxonomyId    = signal<number | null>(null);
  locationId    = signal<number | null>(null);
  collectionLocked = false;

  // Suchtexte für die drei Autocomplete-Eingabefelder
  collectionSearch = signal<string>('');
  taxonomySearch   = signal<string>('');
  locationSearch   = signal<string>('');

  // Optionen vom Backend geladen
  taxonomyOptions   = signal<TaxonomyOption[]>([]);
  locationOptions   = signal<LocationOption[]>([]);
  collectionOptions = signal<CollectionOption[]>([]);

  // Live gefilterte Sammlungen via computed (sucht im Namen)
  filteredCollections = computed(() => {
    const query = this.collectionSearch().toLowerCase().trim();
    if (!query) return this.collectionOptions();
    return this.collectionOptions().filter(c => 
      c.name.toLowerCase().includes(query)
    );
  });

  // Live gefilterte Taxonomien (sucht in Gattung oder Artname)
  filteredTaxonomies = computed(() => {
    const query = this.taxonomySearch().toLowerCase().trim();
    if (!query) return this.taxonomyOptions();
    return this.taxonomyOptions().filter(t => 
      t.genus.toLowerCase().includes(query) || 
      t.species.toLowerCase().includes(query)
    );
  });

  // Live gefilterte Fundorte (sucht in Name, Region oder Land)
  filteredLocations = computed(() => {
    const query = this.locationSearch().toLowerCase().trim();
    if (!query) return this.locationOptions();
    return this.locationOptions().filter(l => 
      l.name.toLowerCase().includes(query) ||
      l.region.toLowerCase().includes(query) ||
      l.country.toLowerCase().includes(query)
    );
  });

  // Liefert das aktuell ausgewählte Taxonomie-Objekt für die Sidebar-Vorschau
  selectedTaxonomy = computed(() =>
    this.taxonomyOptions().find((t) => t.id === this.taxonomyId()) ?? null
  );

  // Photo-Zustand
  previewUrl = signal<string | null>(null);

  saveError = signal('');
  isSaving = signal(false);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const routeCollectionId = params.get('id');
      if (routeCollectionId) {
        this.collectionId.set(Number(routeCollectionId));
        this.collectionLocked = true;
        
        // Falls die ID über die Route gesperrt ist, laden wir direkt den Namen für das Textfeld
        this.api.get<CollectionOption[]>('collections').subscribe((data) => {
          const matched = data.find(c => c.id === Number(routeCollectionId));
          if (matched) this.collectionSearch.set(matched.name);
        });
      }
    });

    this.api.get<TaxonomyOption[]>('taxonomy').subscribe((data) => this.taxonomyOptions.set(data));
    this.api.get<LocationOption[]>('location').subscribe((data) => this.locationOptions.set(data));

    if (!this.collectionLocked) {
      const userId = this.auth.currentUser()?.id;
      this.api.get<CollectionOption[]>('collections').subscribe((data) => {
        this.collectionOptions.set(data.filter((c) => c.createdBy === userId));
      });
    }
  }

  // Hilfsmethoden zur Auswahl aus den Autocomplete-Listen
  selectCollection(col: CollectionOption): void {
    this.collectionId.set(col.id);
    this.collectionSearch.set(col.name);
  }

  selectTaxonomy(tax: TaxonomyOption): void {
    this.taxonomyId.set(tax.id);
    this.taxonomySearch.set(`${tax.genus} ${tax.species}`);
  }

  selectLocation(loc: LocationOption): void {
    this.locationId.set(loc.id);
    this.locationSearch.set(`${loc.name} (${loc.region}, ${loc.country})`);
  }

  nextStep(): void { if (this.currentStep() < 3) this.currentStep.update(s => s + 1); }
  prevStep(): void  { if (this.currentStep() > 1) this.currentStep.update(s => s - 1); }
  goToStep(step: number): void { if (step < this.currentStep()) this.currentStep.set(step); }

  onSubmit(): void {
    const collectionId = this.collectionId();
    const taxonomyId = this.taxonomyId();
    const locationId = this.locationId();

    if (!collectionId || !taxonomyId || !locationId) {
      this.saveError.set('Bitte wähle eine Sammlung, eine Taxonomie und einen Fundort über die Suchvorschläge aus.');
      return;
    }

    this.isSaving.set(true);
    this.saveError.set('');

    const payload: Record<string, unknown> = {
      name: this.name(),
      description: this.description(),
      dateCollected: this.dateCollected() || null,
      taxonomyId,
      collectionId,
      locationId,
      photoPath: this.previewUrl() || null
    };

    if (this.status()) {
      payload['status'] = STATUS_DE_TO_EN[this.status()] ?? this.status();
    }

    this.api.post<{ id: number }>('specimen', payload).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.router.navigate(['/objects', collectionId]);
      },
      error: (err) => {
        this.isSaving.set(false);
        this.saveError.set(err.error?.message ?? 'Speichern fehlgeschlagen. Bitte erneut versuchen.');
      },
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const reader = new FileReader();
      reader.onload = (e) => this.previewUrl.set(e.target?.result as string);
      reader.readAsDataURL(input.files[0]);
    }
  }

  triggerFileInput(): void {
    document.getElementById('photo-input')?.click();
  }

  getStatusClass(): string {
    const map: Record<string, string> = {
      'verfügbar':   'status-available',
      'ausgeliehen': 'status-loaned',
      'verloren':    'status-lost',
      'zerstört':    'status-destroyed',
    };
    return map[this.status()] || '';
  }
}