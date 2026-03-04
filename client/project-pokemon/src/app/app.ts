import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, RouterLinkWithHref } from '@angular/router';
import { AuthService } from './services/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLinkWithHref],
  templateUrl: './app.html',
  styleUrl: './app.css'
})

export class App implements OnInit{
  constructor(private authService: AuthService) {}

  ngOnInit() {
    this.authService.initializeJwtFromStorage();
  }
}
