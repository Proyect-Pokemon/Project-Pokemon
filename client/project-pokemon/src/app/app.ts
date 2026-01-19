import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Prueba } from './services/prueba';
import { Navbar } from "./shared/navbar/navbar";
import { Footer } from "./shared/footer/footer";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Footer],
  templateUrl: './app.html',
  styleUrl: './app.css'
})

export class App implements OnInit {
  async ngOnInit(): Promise<void> {
    const data = await this.apiService.getData();
    console.log(data)
  }

  protected readonly title = signal('project-pokemon');
  private apiService = inject(Prueba);
}
