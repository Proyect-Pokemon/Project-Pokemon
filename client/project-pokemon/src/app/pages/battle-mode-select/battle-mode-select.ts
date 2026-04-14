import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-battle-mode-select',
  imports: [CommonModule],
  templateUrl: './battle-mode-select.html',
  styleUrl: './battle-mode-select.css',
})
export class BattleModeSelect {
  private router = inject(Router);
  isLeaving = signal(false);

  goToVsMachine(): void {
    if (this.isLeaving()) return;

    this.isLeaving.set(true);
    setTimeout(() => {
      void this.router.navigate(['/battle-select']);
    }, 300);
  }
}
