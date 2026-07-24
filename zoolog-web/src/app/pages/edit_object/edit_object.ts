import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule, Location } from '@angular/common';
import { ApiService } from '../../services/api.service';

interface SpecimenDetail {
  id: number;
  name: string;
  description: string;
  status: string;
  dateCollected: string;
  locationId: number;
  taxonomyId: number;
  collectionId: number;
  size?: string;
  weight?: number | null; // <-- HIER ERGÄNZEN
  birthYear?: number | null; // <-- HIER ERGÄNZEN
  photoPath?: string;
}

@Component({
  selector: 'app-edit-object',
  standalone: true,
  imports: [RouterLink, FormsModule, CommonModule],
  templateUrl: './edit_object.html',
  styleUrl: './edit_object.css',
})
export class EditObject implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  specimenId!: number;

  name = signal('');
  description = signal('');
  status = signal('');
  weight = signal<number | null>(null); // <-- HIER ALS SIGNAL
  birthYear = signal<number | null>(null); // <-- HIER ALS SIGNAL
  size = signal<string | null>(null);

  private locationId = 0;
  private taxonomyId = 0;
  private collectionId = 0;
  private dateCollected: string | null = null;

  private photoPath: string | null = null;
  selectedFile: File | null = null;
  previewUrl = signal<string | null>(null);
  public existingPhotoPath: string | null = null;
  isSaving = signal(false);
  errorMessage = signal('');

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.specimenId = Number(idParam);
      this.loadSpecimen();
    }
  }

  private loadSpecimen(): void {
    this.api.get<SpecimenDetail>(`specimen/${this.specimenId}`).subscribe({
      next: (data) => {
        // Formular-Felder
        const rawData = data as any;
        this.name.set(rawData.name || rawData.Name || '');
        this.description.set(rawData.description || rawData.Description || '');
        this.status.set(rawData.status || rawData.Status || 'available');

        // --- KORRIGIERT: size.set() statt Zuweisung ---
        this.weight.set(rawData.weight !== undefined ? rawData.weight : (rawData.Weight ?? null));
        this.birthYear.set(
          rawData.birthYear !== undefined ? rawData.birthYear : (rawData.BirthYear ?? null),
        );
        this.size.set(rawData.size || rawData.Size || null);

        this.locationId = rawData.locationId || rawData.LocationId || 0;
        this.taxonomyId = rawData.taxonomyId || rawData.TaxonomyId || 0;
        this.collectionId = rawData.collectionId || rawData.CollectionId || 0;
        this.dateCollected = rawData.dateCollected || rawData.DateCollected || null;
        this.photoPath = rawData.photoPath || rawData.PhotoPath || null;
      },
      error: () => this.errorMessage.set('Fehler beim Laden des Objekts.'),
    });
  }

  // Die Lösch-Funktion
  deleteSpecimen(): void {
    const confirmDelete = confirm(`Möchtest du das Exemplar unwiderruflich löschen?`);
    if (!confirmDelete) return;

    this.api.delete(`specimen/${this.specimenId}`).subscribe({
      next: () => {
        console.log('Exemplar erfolgreich gelöscht');
        if (this.collectionId) {
          this.router.navigate(['/objects', this.collectionId]);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        console.error('Fehler beim Löschen des Exemplars:', err);
        alert('Das Exemplar konnte nicht gelöscht werden.');
      },
    });
  }

  onSubmit(): void {
    this.isSaving.set(true);
    this.errorMessage.set('');

    // 1. FormData erstellen
    const formData = new FormData();

    // 2. Alle Felder hinzufügen (Wichtig: Signale mit Klammern auslesen -> this.size())
    formData.append('Name', this.name());
    formData.append('Description', this.description() || '');
    formData.append('Status', this.status() || '');
    formData.append('DateCollected', this.dateCollected || '');
    formData.append('Size', this.size() || '');

    formData.append('Weight', this.weight()?.toString() || '');
    formData.append('BirthYear', this.birthYear()?.toString() || '');

    formData.append('LocationId', this.locationId?.toString() || '');
    formData.append('TaxonomyId', this.taxonomyId?.toString() || '');
    formData.append('CollectionId', this.collectionId?.toString() || '');

    // 3. Datei hinzufügen, falls eine neue ausgewählt wurde
    if (this.selectedFile) {
      formData.append('imageFile', this.selectedFile);
    }

    // 4. PUT Request senden
    this.api.put(`specimen/${this.specimenId}`, formData).subscribe({
      next: () => {
        this.isSaving.set(false); // Zur Sicherheit hier
        this.location.back();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Änderungen konnten nicht gespeichert werden.');
      },
      complete: () => {
        // complete oder ein globales finally garantiert, dass der Button *immer* entriegelt wird
        this.isSaving.set(false);
      },
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.selectedFile = input.files[0];
    }
  }

  triggerFileInput(): void {
    document.getElementById('photo-input')?.click();
  }

  goBack(): void {
    this.location.back();
  }
}
