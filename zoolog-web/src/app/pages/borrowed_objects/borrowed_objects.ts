import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Auth } from '../../services/auth';
import { forkJoin, of } from 'rxjs';
import { catchError, timeout, first } from 'rxjs/operators';

interface LoanItem {
  id: number;
  specimenId: number;
  specimenName: string;
  partnerName: string; // Entweder an wen verliehen wurde oder von wem es kommt
  loanDate: string;
  returnDate: string | null;
  status: string;
  notes: string | null;
}

@Component({
  selector: 'app-borrowed-objects',
  standalone: true,
 imports: [
    CommonModule, 
    DatePipe, // <-- Hier im imports-Array registriert
    RouterLink
  ],
  templateUrl: './borrowed_objects.html',
  styleUrl: './borrowed_objects.css' 
})
export class BorrowedObjects implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(Auth);
  private readonly cdr = inject(ChangeDetectorRef);

  currentUserId: number | null = null;
  isLoading = true;
  errorMessage = '';

  loanedOut: LoanItem[] = []; // Tiere, die ich verliehen habe
  borrowedIn: LoanItem[] = []; // Tiere, die ich geliehen habe

  ngOnInit(): void {
    this.currentUserId = this.auth.getCurrentUserId();
    this.loadLoansData();
  }

  loadLoansData(): void {
    // Lädt parallel alle Ausleihen, Exemplare und Benutzer, um die IDs in lesbare Namen aufzulösen
    forkJoin({
      loans: this.api.get<any[]>('loan').pipe(first(), timeout(5000), catchError(() => of([]))),
      specimens: this.api.get<any[]>('specimen').pipe(first(), timeout(5000), catchError(() => of([]))),
      users: this.api.get<any[]>('users').pipe(first(), timeout(5000), catchError(() => of([])))
    }).subscribe(({ loans, specimens, users }) => {
      if (!this.currentUserId) {
        this.errorMessage = 'Benutzer nicht angemeldet.';
        this.isLoading = false;
        return;
      }

      // Hilfs-Maps für schnelles Nachschlagen von Namen anhand von IDs
      const specimenMap = new Map(specimens.map(s => [s.id, s.name]));
      const userMap = new Map(users.map(u => [u.id, u.username]));

      const mappedLoans: LoanItem[] = loans.map(l => ({
        id: l.id,
        specimenId: l.specimenId,
        specimenName: specimenMap.get(l.specimenId) ?? `Exemplar #${l.specimenId}`,
        partnerName: '', // Wird gleich je nach Richtung gesetzt
        loanDate: l.loanDate,
        returnDate: l.returnDate,
        status: l.status,
        notes: l.notes,
        _loanedFrom: l.loanedFrom,
        _loanedTo: l.loanedTo
      }));

      // Aufteilung in "Verliehen von mir" vs "Geliehen an mich"
      this.loanedOut = mappedLoans
        .filter(l => (l as any)._loanedFrom === this.currentUserId && l.status !== 'returned')
        .map(l => ({
          ...l,
          partnerName: userMap.get((l as any)._loanedTo) ?? 'Unbekannter Nutzer'
        }));

      this.borrowedIn = mappedLoans
        .filter(l => (l as any)._loanedTo === this.currentUserId && l.status !== 'returned')
        .map(l => ({
          ...l,
          partnerName: userMap.get((l as any)._loanedFrom) ?? 'Unbekannter Nutzer'
        }));

      this.isLoading = false;
      this.cdr.detectChanges();
    });
  }

  returnLoan(loanId: number, specimenId: number): void {
    if (!confirm('Möchtest du dieses Exemplar wirklich als zurückgegeben markieren?')) {
      return;
    }

    this.api.post(`loan/return/${loanId}`, { specimenId }).subscribe({
      next: () => {
        alert('Exemplar erfolgreich zurückgegeben!');
        this.loadLoansData(); // Liste neu laden
      },
      error: (err) => {
        console.error('Fehler beim Zurückgeben:', err);
        alert('Fehler beim Zurückgeben des Exemplars.');
      }
    });
  }
}