import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
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
  imports: [RouterLink, FormsModule, DatePipe],
  templateUrl: './new_object.html',
  styleUrl: './new_object.css'
})
export class NewObject implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(Auth);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  currentStep = signal(1);

  name        = signal('');
  description = signal('');
  dateCollected = signal('');
  status      = signal('');
  locationId  = signal<number | null>(null);
  collectionId = signal<number | null>(null);
  collectionLocked = false;

  //Auswahl aus existierenden Taxonomien statt Freitext: das Backend
  // erwartet eine TaxonomyId zu einer bestehenden Zeile, keine neue Klassifikation.
  // Eine neue Art anzulegen läuft über den Artenantrag, nicht hier.
  taxonomyId = signal<number | null>(null);

  taxonomyOptions = signal<TaxonomyOption[]>([]);
  locationOptions = signal<LocationOption[]>([]);
  collectionOptions = signal<CollectionOption[]>([]);

  selectedTaxonomy = computed(() =>
    this.taxonomyOptions().find((t) => t.id === this.taxonomyId()) ?? null
  );

  // Photo
  previewUrl = signal<string | null>(null);

  saveError = signal('');
  isSaving = signal(false);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const routeCollectionId = params.get('id');
      if (routeCollectionId) {
        this.collectionId.set(Number(routeCollectionId));
        this.collectionLocked = true;
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

  nextStep(): void { if (this.currentStep() < 3) this.currentStep.update(s => s + 1); }
  prevStep(): void  { if (this.currentStep() > 1) this.currentStep.update(s => s - 1); }
  goToStep(step: number): void { if (step < this.currentStep()) this.currentStep.set(step); }

  onSubmit(): void {
    const collectionId = this.collectionId();
    const locationId = this.locationId();
    const taxonomyId = this.taxonomyId();

    if (!collectionId || !locationId || !taxonomyId) {
      this.saveError.set('Bitte Sammlung, Fundort und Taxonomie auswählen.');
      return;
    }

    this.isSaving.set(true);
    this.saveError.set('');

    const payload: Record<string, unknown> = {
      name: this.name(),
      description: this.description(),
      dateCollected: this.dateCollected() || null,
      locationId,
      taxonomyId,
      collectionId,
    };

    // Leeren Status nicht mitsenden - das Backend setzt sonst seinen eigenen
    // Default; ein leerer String würde an der Status-Validierung scheitern.
    // Das <select> zeigt deutsche Werte, das Backend speichert sie auf Englisch
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