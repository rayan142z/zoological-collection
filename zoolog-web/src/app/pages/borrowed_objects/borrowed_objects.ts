import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Auth } from '../../services/auth';
import { forkJoin, of } from 'rxjs';
import { catchError, timeout, first } from 'rxjs/operators';
import { TPipe } from '../../i18n/t.pipe';
import { I18n } from '../../i18n/i18n.service';

interface LoanItem {
  id: number;
  specimenId: number;
  specimenName: string;
  partnerName: string;
  loanDate: string;
  returnDate: string | null;
  status: string;
  notes: string | null;
}

@Component({
  selector: 'app-borrowed-objects',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, TPipe],
  templateUrl: './borrowed_objects.html',
  styleUrl: './borrowed_objects.css',
})
export class BorrowedObjects implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(Auth);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly i18n = inject(I18n);

  currentUserId: number | null = null;
  isLoading = true;
  errorMessage = '';

  loanedOut: LoanItem[] = [];
  borrowedIn: LoanItem[] = [];

  groups() {
    return [
      { title: 'Von mir verliehen', partner: 'Verliehen an', items: this.loanedOut, empty: 'Du hast aktuell keine Exemplare verliehen.', canReturn: false },
      { title: 'An mich verliehen', partner: 'Verliehen von', items: this.borrowedIn, empty: 'Du hast aktuell keine fremden Exemplare ausgeliehen.', canReturn: true },
    ];
  }

  /** Aktive Leihe mit Rückgabedatum in der Vergangenheit */
  isOverdue(item: LoanItem): boolean {
    return !!item.returnDate && item.returnDate < new Date().toISOString().split('T')[0];
  }

  ngOnInit(): void {
    this.currentUserId = this.auth.getCurrentUserId();
    this.loadLoansData();
  }

  loadLoansData(): void {
    forkJoin({
      loans: this.api.get<any[]>('loan').pipe(
        first(),
        timeout(5000),
        catchError(() => of([])),
      ),
      specimens: this.api.get<any[]>('specimen').pipe(
        first(),
        timeout(5000),
        catchError(() => of([])),
      ),
      users: this.api.get<any[]>('users').pipe(
        first(),
        timeout(5000),
        catchError(() => of([])),
      ),
    }).subscribe(({ loans, specimens, users }) => {
      if (!this.currentUserId) {
        this.errorMessage = 'Benutzer nicht angemeldet.';
        this.isLoading = false;
        return;
      }

      const specimenMap = new Map(specimens.map((s) => [s.id, s.name]));
      const userMap = new Map(users.map((u) => [u.id, u.username]));

      const mappedLoans: LoanItem[] = loans.map((l) => ({
        id: l.id,
        specimenId: l.specimenId,
        specimenName: specimenMap.get(l.specimenId) ?? `Exemplar #${l.specimenId}`,
        partnerName: '',
        loanDate: l.loanDate,
        returnDate: l.returnDate,
        status: l.status,
        notes: l.notes,
        _loanedFrom: l.loanedFrom,
        _loanedTo: l.loanedTo,
      }));

      this.loanedOut = mappedLoans
        .filter((l) => (l as any)._loanedFrom === this.currentUserId && l.status !== 'returned')
        .map((l) => ({
          ...l,
          partnerName: userMap.get((l as any)._loanedTo) ?? 'Unbekannter Nutzer',
        }));

      this.borrowedIn = mappedLoans
        .filter((l) => (l as any)._loanedTo === this.currentUserId && l.status !== 'returned')
        .map((l) => ({
          ...l,
          partnerName: userMap.get((l as any)._loanedFrom) ?? 'Unbekannter Nutzer',
        }));

      this.isLoading = false;
      this.cdr.detectChanges();
    });
  }

  returnLoan(loanId: number): void {
    if (!confirm(this.i18n.t('Möchtest du dieses Exemplar wirklich als zurückgegeben markieren?'))) {
      return;
    }

    this.api.put(`loan/${loanId}/return`, {}).subscribe({
      next: () => this.loadLoansData(),
      error: (err) => {
        console.error('Fehler beim Zurückgeben:', err);
        alert(this.i18n.t('Fehler beim Zurückgeben des Exemplars.'));
      },
    });
  }
}
