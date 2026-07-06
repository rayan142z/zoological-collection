import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-borrowed-objects',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './borrowed_objects.html',
  styleUrl: './borrowed_objects.css' // Kannst du anlegen, falls du separates CSS nutzt
})
export class BorrowedObjects {
  // Hier kommt später die Ausleih-Logik rein
}