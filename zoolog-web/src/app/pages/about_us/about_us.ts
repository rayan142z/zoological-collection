import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-about-us',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './about_us.html',
  styleUrl: './about_us.css',
})
export class AboutUs {
  // Stats data for the mission section
  readonly stats = signal([
    { label: 'Präparate', value: '5.000+' },
    { label: 'Jahre Geschichte', value: '150' },
    { label: 'Forschungsprojekte', value: '12' },
    { label: 'Exponate', value: '800+' },
  ]);

  // Core values
  readonly values = signal([
    {
      icon: '️🪺',
      title: 'Bewahrung',
      description: 'Wir schützen das Naturerbe durch modernste Konservierungsmethoden.',
    },
    {
      icon: '🔬',
      title: 'Forschung',
      description: 'Unsere Sammlung ist Grundlage für internationale Biodiversitätsstudien.',
    },
    {
      icon: '🌱',
      title: 'Bildung',
      description: 'Wir vermitteln Wissen über die Komplexität und Schönheit der Tierwelt.',
    },
  ]);

  // Team members
  readonly team = signal([
    {
      name: 'Dr. Elena Vogt',
      role: 'Leitung der Sammlung',
      bio: 'Elena ist Expertin für Entomologie und kuratiert seit 10 Jahren unseren Bestand.',
      specialties: ['Insekten', 'Taxonomie'],
    },
    {
      name: 'Markus Weber',
      role: 'Chef-Präparator',
      bio: 'Markus beherrscht die Kunst der Dermoplastik und sorgt für den Erhalt unserer Großsäuger.',
      specialties: ['Präparation', 'Anatomie'],
    },
  ]);

  // Historical timeline
  readonly history = signal([
    {
      year: '1874',
      title: 'Gründung',
      description:
        'Die Sammlung wurde als Grundstock für die regionale Naturkundeausbildung ins Leben gerufen.',
    },
    {
      year: '1920',
      title: 'Erweiterung',
      description: 'Übernahme bedeutender privater Sammlungen aus Übersee.',
    },
    {
      year: '2024',
      title: 'Digitalisierung',
      description: 'Start des Projekts ZoologWeb zur weltweiten wissenschaftlichen Vernetzung.',
    },
  ]);
}
