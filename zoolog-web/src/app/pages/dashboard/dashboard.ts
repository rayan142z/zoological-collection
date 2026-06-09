import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface Collection {
  id: number;
  name: string;
  description: string;
  emoji: string;
  color: string;
  objectCount: number;
  tags: string[];
  lastUpdated: string;
  owner?: string;
  views?: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  showNewCollection = false;
  activeTab = 'mine';

  quickStats = [
    { icon: '🗂️', value: '3',    label: 'Meine Sammlungen' },
    { icon: '🔬', value: '24',   label: 'Objekte gesamt' },
    { icon: '⭐', value: '5',    label: 'Favoriten' },
    { icon: '📤', value: '3',    label: 'Ausgeliehen' },
  ];

  myCollections: Collection[] = [
    {
      id: 1,
      name: 'Insekten Mitteleuropas',
      description: 'Käfer, Schmetterlinge und Hautflügler aus der Region',
      emoji: '🦋',
      color: 'linear-gradient(135deg, #e8f5e9, #c8e6c9)',
      objectCount: 12,
      tags: ['Insecta', 'Mitteleuropa'],
      lastUpdated: 'vor 2 Tagen',
    },
    {
      id: 2,
      name: 'Heimische Säugetiere',
      description: 'Präparate einheimischer Säugetierarten',
      emoji: '🐭',
      color: 'linear-gradient(135deg, #fff8e1, #ffecb3)',
      objectCount: 7,
      tags: ['Mammalia', 'Heimisch'],
      lastUpdated: 'vor 1 Woche',
    },
    {
      id: 3,
      name: 'Reptilien & Amphibien',
      description: 'Schlangen, Eidechsen und Frösche',
      emoji: '🦎',
      color: 'linear-gradient(135deg, #e0f2f1, #b2dfdb)',
      objectCount: 5,
      tags: ['Reptilia', 'Amphibia'],
      lastUpdated: 'vor 3 Wochen',
    },
  ];

  favCollections: Collection[] = [
    { id: 4,  name: 'Meeresschnecken',    description: '', emoji: '🐚', color: 'linear-gradient(135deg,#e3f2fd,#bbdefb)', objectCount: 31, tags: [], lastUpdated: '', owner: 'Prof. K. Huber' },
    { id: 5,  name: 'Vögel Deutschlands', description: '', emoji: '🐦', color: 'linear-gradient(135deg,#f3e5f5,#e1bee7)', objectCount: 18, tags: [], lastUpdated: '', owner: 'Dr. A. Berger' },
    { id: 6,  name: 'Pilze & Flechten',   description: '', emoji: '🍄', color: 'linear-gradient(135deg,#fce4ec,#f8bbd0)', objectCount: 9,  tags: [], lastUpdated: '', owner: 'L. Vogel' },
    { id: 7,  name: 'Spinnen Europas',    description: '', emoji: '🕷️', color: 'linear-gradient(135deg,#efebe9,#d7ccc8)', objectCount: 22, tags: [], lastUpdated: '', owner: 'M. Schreiber' },
    { id: 8,  name: 'Fossilien Siegen',   description: '', emoji: '🦴', color: 'linear-gradient(135deg,#e8eaf6,#c5cae9)', objectCount: 14, tags: [], lastUpdated: '', owner: 'Archiv' },
  ];

  popularCollections: Collection[] = [
    { id: 9,  name: 'Schmetterlinge Europas', description: 'Über 40 Arten aus ganz Europa — von Tagfaltern bis Nachtfaltern', emoji: '🦋', color: 'linear-gradient(135deg,#e8f5e9,#a5d6a7)', objectCount: 43, tags: [], lastUpdated: '', views: 1240 },
    { id: 10, name: 'Käfer der Welt',         description: 'Die artenreichste Tiergruppe — präzise erfasst und klassifiziert',  emoji: '🪲', color: 'linear-gradient(135deg,#fff3e0,#ffcc80)', objectCount: 67, tags: [], lastUpdated: '', views: 980  },
    { id: 11, name: 'Heimische Vögel',         description: 'Singvögel, Greifvögel und Wasservögel aus Deutschland',            emoji: '🐦', color: 'linear-gradient(135deg,#e3f2fd,#90caf9)', objectCount: 29, tags: [], lastUpdated: '', views: 754  },
    { id: 12, name: 'Meeresmuscheln',          description: 'Muschelschalen aus Nord- und Ostsee sowie dem Mittelmeer',         emoji: '🐚', color: 'linear-gradient(135deg,#fce4ec,#f48fb1)', objectCount: 18, tags: [], lastUpdated: '', views: 612  },
  ];

  totalObjects(cols: Collection[]): number {
    return cols.reduce((sum, c) => sum + c.objectCount, 0);
  }
}