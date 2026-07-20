import { Component, OnInit, inject, ChangeDetectorRef, NgZone} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import * as L from 'leaflet';

interface PublicCollection {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  createdBy: number;
  creator: {
    id: number;
    username: string;
    email: string;
    userRole: string;
    status: string;
  };
  createdAt: string;
  specimenCount: number; 
}

// Interface passend zu deinen Specimen/Objekten
interface SpecimenSearchResult {
  id: number;
  name: string;
  species: string;
  class: string;
  description: string;
  status: string;
  size?: string;
  weight?: number | null;
  birthYear?: number | null;
  latitude?: number | null;
  longitude?: number | null;
  collectionId: number;
  collectionName?: string;
}

@Component({
  selector: 'app-public-search',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './public_search.html',
  styleUrl: './public_search.css'
})
export class PublicSearch implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone)

  // Umschalter: 'collections' oder 'specimens'
  searchMode: 'collections' | 'specimens' = 'collections';

  // Gemeinsamer / Sammlungssuch-Term
  searchTerm = '';
  collections: PublicCollection[] = [];
  
  // Erweiterte Filter für Spezies & Taxonomie
  specimenQuery = '';
  selectedClass = '';
  selectedSpecies = '';
  selectedGenus = '';
  selectedFamily = '';
  selectedOrder = '';

  minWeight: number | null = null;
  maxWeight: number | null = null;
  minSize: number | null = null;
  maxSize: number | null = null;
  
  // Geodaten & Radius-Suche
  centerLat: number | null = null;
  centerLng: number | null = null;
  radiusKm: number | null = null;

  isMapModalOpen = false;
  tempLat: number | null = null;
  tempLng: number | null = null;
  tempRadiusKm = 10; // Standardradius

  private map: any = null;
  private marker: any = null;
  private circle: any = null;

  specimens: SpecimenSearchResult[] = [];
  isLoading = false;

  ngOnInit() {
    this.executeSearch();
  }

  setMode(mode: 'collections' | 'specimens'): void {
    this.searchMode = mode;
    this.executeSearch();
  }

  executeSearch(): void {
    if (this.searchMode === 'collections') {
      this.searchCollections();
    } else {
      this.searchSpecimens();
    }
  }

  private searchCollections(): void {
    this.isLoading = true;
    this.cdr.detectChanges();

    const path = this.searchTerm.trim() 
        ? `collections/search-public?query=${encodeURIComponent(this.searchTerm.trim())}`
        : 'collections/search-public';
    
    this.api.get<PublicCollection[]>(path).subscribe({
        next: (data) => {
          this.collections = data;
          this.isLoading = false;
          this.cdr.detectChanges(); 
        },
        error: (err) => {
          console.error('Fehler bei der Suche nach öffentlichen Sammlungen:', err);
          this.collections = [];
          this.isLoading = false;
          this.cdr.detectChanges(); 
        }
    });
  }

  private searchSpecimens(): void {
    this.isLoading = true;
    this.cdr.detectChanges();

    const params: string[] = [];
    if (this.specimenQuery.trim()) params.push(`query=${encodeURIComponent(this.specimenQuery.trim())}`);
    if (this.selectedClass.trim()) params.push(`className=${encodeURIComponent(this.selectedClass.trim())}`);
    if (this.selectedSpecies.trim()) params.push(`species=${encodeURIComponent(this.selectedSpecies.trim())}`);
    if (this.selectedGenus.trim()) params.push(`genus=${encodeURIComponent(this.selectedGenus.trim())}`);
    if (this.selectedFamily.trim()) params.push(`family=${encodeURIComponent(this.selectedFamily.trim())}`);
    if (this.selectedOrder.trim()) params.push(`order=${encodeURIComponent(this.selectedOrder.trim())}`);
    
    if (this.minWeight !== null) params.push(`minWeight=${this.minWeight}`);
    if (this.maxWeight !== null) params.push(`maxWeight=${this.maxWeight}`);
    if (this.minSize !== null) params.push(`minSize=${this.minSize}`);
    if (this.maxSize !== null) params.push(`maxSize=${this.maxSize}`);
    
    if (this.centerLat !== null && this.centerLng !== null && this.radiusKm !== null) {
      params.push(`lat=${this.centerLat}`);
      params.push(`lng=${this.centerLng}`);
      params.push(`radius=${this.radiusKm}`);
    }

    const queryString = params.length > 0 ? `?${params.join('&')}` : '';
    const path = `specimen/search-public${queryString}`;

    this.api.get<SpecimenSearchResult[]>(path).subscribe({
      next: (data) => {
        this.specimens = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Fehler bei der Spezies-Suche:', err);
        this.specimens = [];
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openMapModal(): void {
    this.isMapModalOpen = true;
    this.tempLat = this.centerLat ?? 51.02; // Fallback Zentrum (z.B. Deutschland Mitte)
    this.tempLng = this.centerLng ?? 7.88;
    this.tempRadiusKm = this.radiusKm ?? 10;

    // Karte nach dem Rendern des Modals initialisieren
    setTimeout(() => {
      this.initLeafletMap();
    }, 100);
  }

  closeMapModal(): void {
    this.isMapModalOpen = false;
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  private initLeafletMap(): void {
    // Falls eine alte Karte existiert, erst aufräumen
    if (this.map) {
      this.map.remove();
      this.map = null;
    }

    this.marker = null;
    this.circle = null;

    const initialLat = this.tempLat ?? 51.02;
    const initialLng = this.tempLng ?? 7.88;

    this.map = L.map('leafletMap').setView([initialLat, initialLng], 9);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    // Initialer Marker & Kreis (nur wenn tempLat/tempLng explizit gesetzt sind, ansonsten leer starten)
    if (this.tempLat !== null && this.tempLng !== null) {
      this.updateMarkerAndCircle(this.tempLat, this.tempLng, this.tempRadiusKm);
    }

    // Klick-Event auf die Karte, um den Punkt zu setzen/verschieben
    this.map.on('click', (e: any) => {
      this.zone.run(() => {
        this.tempLat = e.latlng.lat;
        this.tempLng = e.latlng.lng;
        this.updateMarkerAndCircle(this.tempLat!, this.tempLng!, this.tempRadiusKm);
      });
    });
  }

  updateMapCircle(): void {
    if (this.tempLat && this.tempLng) {
      this.updateMarkerAndCircle(this.tempLat, this.tempLng, this.tempRadiusKm);
    }
  }

  private updateMarkerAndCircle(lat: number, lng: number, radiusKm: number): void {
    if (!this.map) return;

    if (this.marker) {
      this.marker.setLatLng([lat, lng]);
    } else {
      this.marker = L.marker([lat, lng], { draggable: true }).addTo(this.map);
      this.marker.on('dragend', (event: any) => {
        const pos = event.target.getLatLng();
        this.zone.run(() => {
          this.tempLat = pos.lat;
          this.tempLng = pos.lng;
          this.updateMarkerAndCircle(pos.lat, pos.lng, this.tempRadiusKm);
        });
      });
    }

    const radiusMeters = radiusKm * 1000;
    if (this.circle) {
      this.circle.setLatLng([lat, lng]);
      this.circle.setRadius(radiusMeters);
    } else {
      this.circle = L.circle([lat, lng], {
        radius: radiusMeters,
        color: '#0a4b11',
        fillColor: '#0a4b11',
        fillOpacity: 0.15
      }).addTo(this.map);
    }
  }

  applyGeoSelection(): void {
    this.centerLat = this.tempLat;
    this.centerLng = this.tempLng;
    this.radiusKm = this.tempRadiusKm;
    this.closeMapModal();
    this.executeSearch();
  }

  

  

  clearGeoFilter(): void {
    this.centerLat = null;
    this.centerLng = null;
    this.radiusKm = null;
    this.tempLat = null;
    this.tempLng = null;
    
    // Marker und Kreis komplett von der Karte löschen
    this.destroyMapElements();

    if (this.isMapModalOpen) {
      this.closeMapModal();
    }
    this.executeSearch();
  }

  private destroyMapElements(): void {
    if (this.map) {
      if (this.marker) {
        this.marker.remove();
        this.marker = null;
      }
      if (this.circle) {
        this.circle.remove();
        this.circle = null;
      }
    }
  }

  private destroyMap(): void {
    this.destroyMapElements();
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }
}