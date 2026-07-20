import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common'; 
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { ChangeDetectorRef } from '@angular/core';
import { Location } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-collection-edit',
  imports: [
    CommonModule, 
    FormsModule,   // <-- 2. Hier unbedingt hinzufügen!
    RouterModule
  ],
  templateUrl: './collection-edit.html',
  styleUrls: ['./collection-edit.css']
})
export class CollectionEditComponent implements OnInit {
  collectionId!: number;
  collection: any = {
    name: '',
    description: ''
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private cdr: ChangeDetectorRef,
    private location: Location
  ) {}

  ngOnInit(): void {
    this.collectionId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadCollection();
  }

  loadCollection(): void {
    this.api.get<any>(`collections/${this.collectionId}`).subscribe({
      next: (data) => {
        this.collection = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Fehler beim Laden der Sammlung:', err)
    });
  }

  goBack(): void {
    this.location.back();
  }

  // Die Speicher-Funktion für Name & Beschreibung
  saveCollection(): void {
    if (!this.collection.name || this.collection.name.trim() === '') {
      alert('Bitte gib einen Namen für die Sammlung ein.');
      return;
    }

    const payload = {
      name: this.collection.name,
      description: this.collection.description
    };

    this.api.put(`collections/${this.collectionId}`, payload).subscribe({
      next: () => {
        console.log('Sammlung erfolgreich aktualisiert');
        // Nach dem Speichern zurück zur Detailansicht der Sammlung navigieren
        this.router.navigate(['/objects', this.collectionId]);
      },
      error: (err) => {
        console.error('Fehler beim Speichern der Sammlung:', err);
        alert('Änderungen konnten nicht gespeichert werden.');
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/objects', this.collectionId]);
  }

  deleteCollection(): void {
    const firstConfirm = confirm(`Möchtest du die Sammlung "${this.collection.name}" wirklich löschen?`);
    if (!firstConfirm) return;

    const secondConfirm = confirm(`⚠️ WICHTIG: Wenn du diese Sammlung löschst, werden eventuell auch ALLE darin enthaltenen Exemplare unwiderruflich gelöscht. Bist du absolut sicher?`);
    if (!secondConfirm) return;

    this.api.delete(`collections/${this.collectionId}`).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        console.error('Fehler beim Löschen der Sammlung:', err);
        alert('Die Sammlung konnte nicht gelöscht werden.');
      }
    });
  }
}