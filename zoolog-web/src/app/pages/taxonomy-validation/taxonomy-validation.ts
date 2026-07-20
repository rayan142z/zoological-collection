import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service'; // Pfad anpassen

@Component({
  selector: 'app-taxonomy-validation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './taxonomy-validation.html',
  styleUrls: ['./taxonomy-validation.css']
})
export class TaxonomyValidationComponent implements OnInit {
  unvalidatedTaxonomies: any[] = [];
  isLoading = true;

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadUnvalidated();
  }

  loadUnvalidated(): void {
    this.isLoading = true;
    this.api.get<any[]>('taxonomy/unvalidated').subscribe({
      next: (data) => {
        this.unvalidatedTaxonomies = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Fehler beim Laden unvalidierter Taxonomien:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  approveTaxonomy(id: number): void {
    this.api.put(`taxonomy/${id}/validate`, {}).subscribe({
      next: () => {
        // Erfolgreich validiert -> Direkt aus der Liste entfernen
        this.unvalidatedTaxonomies = this.unvalidatedTaxonomies.filter(t => t.id !== id);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Fehler beim Validieren:', err);
      }
    });
  }
}