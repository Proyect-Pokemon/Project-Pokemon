import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLinkWithHref } from '@angular/router';
import { AuthService } from './services/auth';
import { SocketService } from './services/websocket-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLinkWithHref],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {

  private readonly authService = inject(AuthService);
  private readonly socketService = inject(SocketService);

  isAuthenticated = this.authService.isAuthenticated;
  isAdmin = this.authService.isAdmin;

  constructor() {
    const jwt = this.authService.jwt;
    if (jwt) {
      this.socketService.connect(jwt);
    }
  }
}