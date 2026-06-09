import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-new-object',
  imports: [RouterLink, FormsModule, DatePipe],
  templateUrl: './new_object.html',
  styleUrl: './new_object.css'
})
export class NewObject {
  currentStep = signal(1);

  // Step 1
  name        = signal('');
  description = signal('');
  dateCollected = signal('');
  status      = signal('');

  // Step 2
  reich   = signal('');
  stamm   = signal('');
  klasse  = signal('');
  ordnung = signal('');
  familie = signal('');
  genus   = signal('');
  species = signal('');

  // Photo
  previewUrl = signal<string | null>(null);

  nextStep(): void { if (this.currentStep() < 3) this.currentStep.update(s => s + 1); }
  prevStep(): void  { if (this.currentStep() > 1) this.currentStep.update(s => s - 1); }
  goToStep(step: number): void { if (step < this.currentStep()) this.currentStep.set(step); }

  onSubmit(): void { console.log('Objekt gespeichert'); }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const reader = new FileReader();
      reader.onload = (e) => this.previewUrl.set(e.target?.result as string);
      reader.readAsDataURL(input.files[0]);
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
}