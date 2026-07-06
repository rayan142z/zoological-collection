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
  photoPath?: string;
}

@Component({
  selector: 'app-edit-object',
  standalone: true,
  imports: [RouterLink, FormsModule, CommonModule],
  templateUrl: './edit_object.html',
  styleUrl: './edit_object.css' 
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

  
  private locationId = 0;
  private taxonomyId = 0;
  private collectionId = 0;
  private dateCollected: string | null = null;
  private size: string | null = null;
  private photoPath: string | null = null;

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

        
        this.locationId = rawData.locationId || rawData.LocationId || 0;
        this.taxonomyId = rawData.taxonomyId || rawData.TaxonomyId || 0;
        this.collectionId = rawData.collectionId || rawData.CollectionId || 0;
        this.dateCollected = rawData.dateCollected || rawData.DateCollected || null;
        this.size = rawData.size || rawData.Size || null;
        this.photoPath = rawData.photoPath || rawData.PhotoPath || null;
      },
      error: () => this.errorMessage.set('Fehler beim Laden des Objekts.')
    });
  }

  onSubmit(): void {
  this.isSaving.set(true);
  this.errorMessage.set('');

  
  const payload = {
    name: this.name(),
    description: this.description(),
    status: this.status(),
    dateCollected: this.dateCollected,
    size: this.size,
    photoPath: this.photoPath,
    locationId: this.locationId,
    taxonomyId: this.taxonomyId,
    collectionId: this.collectionId
  };

  this.api.put(`specimen/${this.specimenId}`, payload).subscribe({
    next: () => {
      this.isSaving.set(false);
      
      
      this.location.back(); 
    },
    error: (err) => {
      this.isSaving.set(false);
      this.errorMessage.set(err.error?.message || 'Änderungen konnten nicht gespeichert werden.');
    }
  });
}

  goBack(): void {
    this.location.back();
  }
}