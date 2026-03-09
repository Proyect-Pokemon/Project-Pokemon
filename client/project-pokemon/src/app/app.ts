import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLinkWithHref } from '@angular/router';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLinkWithHref],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {

  private readonly authService = inject(AuthService);

  isAuthenticated = this.authService.isAuthenticated;
  isAdmin = this.authService.isAdmin;
}