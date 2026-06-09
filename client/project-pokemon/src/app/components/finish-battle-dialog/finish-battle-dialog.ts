import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';

type BattleResult = 'victory' | 'defeat' | null;

@Component({
  selector: 'app-finish-battle-dialog',
  standalone: true,
  imports: [],
  templateUrl: './finish-battle-dialog.html',
  styleUrl: './finish-battle-dialog.css',
})
export class FinishBattleDialog {
  @Input() result: BattleResult = null;

  constructor(private router: Router) {}

  goToProfile() {
    this.router.navigate(['/profile']);
  }

  get resultTitle(): string {
    if (this.result === 'victory') {
      return 'Victoria';
    }

    return 'Derrota';
  }
}
