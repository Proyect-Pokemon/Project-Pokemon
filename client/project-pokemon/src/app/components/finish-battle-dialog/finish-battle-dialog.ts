import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-finish-battle-dialog',
  standalone: true,
  imports: [],
  templateUrl: './finish-battle-dialog.html',
  styleUrl: './finish-battle-dialog.css',
})
export class FinishBattleDialog {
  @Input() hpA: number | null = null;
  @Input() hpB: number | null = null;
  @Input() pokemonAName: string = '';
  @Input() pokemonBName: string = '';
  @Output() retry = new EventEmitter<void>();

  constructor(private router: Router) {}

  goToProfile() {
    this.router.navigate(['/profile']);
  }

  onRetry() {
    this.retry.emit();
  }

  get resultMessage(): string {
    if (this.hpA !== null && this.hpB !== null) {
      if (this.hpA > 0 && this.hpB <= 0) {
        return '¡Has ganado el combate!';
      } else if (this.hpB > 0 && this.hpA <= 0) {
        return 'Has perdido el combate';
      } else if (this.hpA <= 0 && this.hpB <= 0) {
        return 'Empate. Ambos Pokémon han caído.';
      }
    }
    return '';
  }
}
