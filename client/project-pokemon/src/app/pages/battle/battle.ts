import { Component, inject, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { BattleResponse } from '../../models/pokemon-api';
import { Move } from '../../models/move';
import { MovementButton } from '../../components/movement-button/movement-button';
import { simulateBattle } from '../../services/battle-simulator';

@Component({
  selector: 'app-battle',
  imports: [MovementButton],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {

  battle = signal(false);
  battleInfo = signal<BattleResponse | null>(null);
  hpA = signal<number | null>(null);
  hpB = signal<number | null>(null);

  private apiService = inject(BattleService);

  async ngOnInit(): Promise<void> {
    const data = await this.apiService.getBattle();
    this.battleInfo.set(data);
    this.hpA.set(data.pokemonA.hp);
    this.hpB.set(data.pokemonB.hp);
    console.log(data);
    this.battle.set(true);
  }

  attack(move: Move): void {
    this.battleInfo.update(battle => {
      if (!battle) return battle;
      move.currentPp = move.currentPp - 1;
      // Use current HP
      const hpA = this.hpA() ?? battle.pokemonA.hp;
      const hpB = this.hpB() ?? battle.pokemonB.hp;
      const result = simulateBattle(battle.pokemonA, battle.pokemonB, move, hpA, hpB);
      this.hpA.set(result.hpA);
      this.hpB.set(result.hpB);
      return battle;
    });
  }
}