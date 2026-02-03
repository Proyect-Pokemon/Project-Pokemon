import { Component, inject, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { BattleResponse } from '../../models/pokemon-api';
import { Move } from '../../models/move';

@Component({
  selector: 'app-battle',
  imports: [],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {

  turno = signal(true);
  combate = signal(false);
  battleInfo = signal<BattleResponse | null>(null);

  private apiService = inject(BattleService);

  async ngOnInit(): Promise<void> {
    const data = await this.apiService.getBattle();
    this.battleInfo.set(data);
    console.log(data);
    this.combate.set(true);
  }

  ataca(move: Move): void {
    this.turno.set(!this.turno());
    this.battleInfo.update(battle => {
      move.currentPp = move.currentPp - 1;
      return battle;
  });
  }
}