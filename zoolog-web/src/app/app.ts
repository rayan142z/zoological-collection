import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './shared/navbar/navbar';
import { DemoBanner } from './demo/demo-banner';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, DemoBanner],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('zoolog-web');
}
