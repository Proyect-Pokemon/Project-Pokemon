import { Component, inject } from '@angular/core';
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

  goToVsMachine(): void {
    void this.router.navigate(['/battle-select']);
  }
}
