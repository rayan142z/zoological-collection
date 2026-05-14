import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

interface Specimen {
  id: number;
  name: string;
  genus: string;
  species: string;
  status: 'verfügbar' | 'ausgeliehen' | 'verloren' | 'zerstört';
  dateCollected: string;
  image: string;
}

@Component({
  selector: 'app-objects',
  imports: [FormsModule, DatePipe],
  templateUrl: './objects.html',
  styleUrl: './objects.css'
})
export class Objects {
  searchTerm = '';
  selectedStatus = '';

  specimens: Specimen[] = [
    { id: 1, name: 'Hirschkäfer', genus: 'Lucanus', species: 'cervus', status: 'verfügbar', dateCollected: '2019-06-12', image: 'images/Hirschkaefer.png' },
    { id: 2, name: 'Schwalbenschwanz', genus: 'Papilio', species: 'machaon', status: 'ausgeliehen', dateCollected: '2021-08-03', image: 'images/Schwalbenschwanz_Schmetterling.png' },
    { id: 3, name: 'Waldmaus', genus: 'Apodemus', species: 'sylvaticus', status: 'verfügbar', dateCollected: '2020-10-17', image: 'images/Maus.png' },
    { id: 4, name: 'Ringelnatter', genus: 'Natrix', species: 'natrix', status: 'verloren', dateCollected: '2018-04-25', image: 'images/Schlange.png' },
    { id: 5, name: 'Zilpzalp', genus: 'Phylloscopus', species: 'collybita', status: 'verfügbar', dateCollected: '2022-02-09', image: 'images/Zilpzalp.png' },
    { id: 6, name: 'Schneckenhaus', genus: 'Helix', species: 'pomatia', status: 'zerstört', dateCollected: '2017-09-30', image: 'images/Schneckenhaus.png' },
];

  get filteredSpecimens() {
    return this.specimens.filter(s => {
      const matchesSearch =
        s.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        s.genus.toLowerCase().includes(this.searchTerm.toLowerCase());
      const matchesStatus = !this.selectedStatus || s.status === this.selectedStatus;
      return matchesSearch && matchesStatus;
    });
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      'verfügbar': 'status-available',
      'ausgeliehen': 'status-loaned',
      'verloren': 'status-lost',
      'zerstört': 'status-destroyed'
    };
    return map[status] || '';
  }
}
