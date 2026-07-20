import { Component, OnInit, inject, signal, computed, effect, AfterViewInit, OnDestroy} from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe, CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { Auth } from '../../services/auth';
import { STATUS_DE_TO_EN } from '../../utils/status-map';
import * as L from 'leaflet'; // Leaflet importieren

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

interface CollectionOption {
  id: number;
  name: string;
  createdBy: number;
}

@Component({
  selector: 'app-new-object',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe, CommonModule],
  templateUrl: './new_object.html',
  styleUrl: './new_object.css'
})
export class NewObject implements OnInit, AfterViewInit,  OnDestroy{
  private readonly api = inject(ApiService);
  private readonly auth = inject(Auth);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private selectedFile: File | null = null;

  currentStep = signal(1);

  // Formular-Signals für die Grunddaten inklusive weight, birthYear und size
  name          = signal('');
  description   = signal('');
  dateCollected = signal('');
  status        = signal('');
  weight        = signal<number | null>(null);
  birthYear     = signal<number | null>(null);
  size          = signal<number | null>(null);
  
  collectionId  = signal<number | null>(null);
  taxonomyId    = signal<number | null>(null);
  
  latitude      = signal<number | null>(null);
  longitude     = signal<number | null>(null);

  collectionLocked = false;

  taxMode = signal<'search' | 'create'>('search');
  newTaxonomy = {
    kingdom: '', phylum: '', class: '', order: '', family: '', genus: '', species: ''
  };
  toggleTaxMode() {
    this.taxMode.set(this.taxMode() === 'search' ? 'create' : 'search');
    this.taxonomyId.set(null); 
    this.taxonomySearch.set(''); 
  }

  targetCollectionName = signal<string>('');
  taxonomySearch   = signal<string>('');

  // Optionen vom Backend geladen
  taxonomyOptions   = signal<TaxonomyOption[]>([]);
  collectionOptions = signal<CollectionOption[]>([]);

  // Live gefilterte Taxonomien
  filteredTaxonomies = computed(() => {
    const query = this.taxonomySearch().toLowerCase().trim();
    if (!query) return this.taxonomyOptions();
    return this.taxonomyOptions().filter(t => 
      t.genus.toLowerCase().includes(query) || 
      t.species.toLowerCase().includes(query)
    );
  });

  // Liefert das aktuell ausgewählte Taxonomie-Objekt für die Sidebar-Vorschau
  selectedTaxonomy = computed(() =>
    this.taxonomyOptions().find((t) => t.id === this.taxonomyId()) ?? null
  );

  private map!: L.Map;
  private marker!: L.Marker;

  constructor(){
    effect(() => {
      if (this.currentStep() === 1) {
        setTimeout(() => this.initMap(), 100);
      }
    });
  }

  // Photo-Zustand
  previewUrl = signal<string | null>(null);

  saveError = signal('');
  isSaving = signal(false);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const routeCollectionId = params.get('id');
      if (routeCollectionId) {
        const colId = Number(routeCollectionId);
        this.collectionId.set(colId);
        this.collectionLocked = true;
        
        this.api.get<CollectionOption[]>('collections').subscribe((data) => {
          const matched = data.find(c => c.id === colId);
          if (matched) {
            this.targetCollectionName.set(matched.name);
          }
        });
      } else {
        this.router.navigate(['/dashboard']);
      }
    });

    this.api.get<TaxonomyOption[]>('taxonomy').subscribe((data) => this.taxonomyOptions.set(data));

    if (!this.collectionLocked) {
      const userId = this.auth.currentUser()?.id;
      this.api.get<CollectionOption[]>('collections').subscribe((data) => {
        this.collectionOptions.set(data.filter((c) => c.createdBy === userId));
      });
    }
  }

  private initMap(): void {
    const mapContainer = document.getElementById('map');
    if (!mapContainer) return;

    if (this.map) {
      this.map.remove();
    }

    const startLat = this.latitude() ?? 50.8748;
    const startLng = this.longitude() ?? 8.0243;

    this.map = L.map('map').setView([startLat, startLng], 10);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(this.map);

    const defaultIcon = L.icon({
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
      iconSize: [25, 41],
      iconAnchor: [12, 41]
    });

    this.marker = L.marker([startLat, startLng], { 
      icon: defaultIcon,
      draggable: true 
    }).addTo(this.map);

    this.marker.on('dragend', () => {
      const position = this.marker.getLatLng();
      this.latitude.set(Number(position.lat.toFixed(6)));
      this.longitude.set(Number(position.lng.toFixed(6)));
    });

    this.map.on('click', (e: L.LeafletMouseEvent) => {
      this.marker.setLatLng(e.latlng);
      this.latitude.set(Number(e.latlng.lat.toFixed(6)));
      this.longitude.set(Number(e.latlng.lng.toFixed(6)));
    });

    if (this.latitude() === null || this.longitude() === null) {
      this.latitude.set(startLat);
      this.longitude.set(startLng);
    }
  }

  useCurrentLocation(): void {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          const lat = Number(position.coords.latitude.toFixed(6));
          const lng = Number(position.coords.longitude.toFixed(6));
          this.latitude.set(lat);
          this.longitude.set(lng);
          
          if (this.map && this.marker) {
            this.map.setView([lat, lng], 14);
            this.marker.setLatLng([lat, lng]);
          }
        },
        () => alert('Standortzugriff verweigert oder nicht verfügbar.')
      );
    }
  }

  selectTaxonomy(tax: TaxonomyOption): void {
    this.taxonomyId.set(tax.id);
    this.taxonomySearch.set(`${tax.genus} ${tax.species}`);
  }

  nextStep(): void { if (this.currentStep() < 3) this.currentStep.update(s => s + 1); }
  prevStep(): void  { if (this.currentStep() > 1) this.currentStep.update(s => s - 1); }
  goToStep(step: number): void { if (step < this.currentStep()) this.currentStep.set(step); }

  onSubmit(): void {
    const collectionId = this.collectionId();
    const taxonomyId = this.taxonomyId();
    const lat = this.latitude();
    const lng = this.longitude();
  
    const newTaxonomy = this.newTaxonomy.genus && this.newTaxonomy.species && this.newTaxonomy.kingdom && this.newTaxonomy.phylum && this.newTaxonomy.class && this.newTaxonomy.family;

    if (!collectionId || !(taxonomyId || newTaxonomy ) || lat === null || lng === null) {
      this.saveError.set('Fehlende Daten');
      return;
    }

    this.isSaving.set(true);
    this.saveError.set('');

    console.log("Gewicht vor Senden:", this.weight());
    console.log("Geburtsjahr vor Senden:", this.birthYear());

    const formData = new FormData();
    formData.append('Name', this.name());
    formData.append('Description', this.description() || '');
    formData.append('DateCollected', this.dateCollected() || '');
    
    // Übergabe der neuen Spalten weight, birthYear und size
    if (this.weight() !== null) formData.append('Weight', this.weight()!.toString());
    if (this.birthYear() !== null) formData.append('BirthYear', this.birthYear()!.toString());
    if (this.size() !== null) formData.append('Size', this.size()!.toString());

    formData.append('CollectionId', collectionId.toString());
    formData.append('Latitude', lat.toString());
    formData.append('Longitude', lng.toString());
    
    if (this.taxMode() === 'search') {
      formData.append('TaxonomyId', this.taxonomyId()!.toString());
    } else {
      formData.append('IsNewTaxonomy', 'true');
      formData.append('Genus', this.newTaxonomy.genus);
      formData.append('Species', this.newTaxonomy.species);
      formData.append('Kingdom', this.newTaxonomy.kingdom);
      formData.append('Phylum', this.newTaxonomy.phylum);
      formData.append('Class', this.newTaxonomy.class);
      formData.append('Family', this.newTaxonomy.family);
      formData.append('Orders', this.newTaxonomy.order);
    }

   
    

    if (this.selectedFile) {
      formData.append('imageFile', this.selectedFile);
    }

    if (this.status()) {
      formData.append('Status', STATUS_DE_TO_EN[this.status()] ?? this.status());
    }

    this.api.post<{ id: number }>('specimen', formData).subscribe({
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
      const file = input.files[0];
      this.selectedFile = file;

      const reader = new FileReader();
      reader.onload = (e) => this.previewUrl.set(e.target?.result as string);
      reader.readAsDataURL(file);
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

  ngAfterViewInit(): void {
    this.initMap();
  }

  private updateMarker(lat: number, lng: number): void {
    const formattedLat = parseFloat(lat.toFixed(6));
    const formattedLng = parseFloat(lng.toFixed(6));

    this.latitude.set(formattedLat);
    this.longitude.set(formattedLng);

    if (this.marker) {
      this.marker.setLatLng([formattedLat, formattedLng]);
    } else {
      this.marker = L.marker([formattedLat, formattedLng], {
        draggable: true 
      }).addTo(this.map);

      this.marker.on('dragend', () => {
        const position = this.marker.getLatLng();
        this.updateMarker(position.lat, position.lng);
      });
    }

    this.map.panTo([formattedLat, formattedLng]);
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }
}