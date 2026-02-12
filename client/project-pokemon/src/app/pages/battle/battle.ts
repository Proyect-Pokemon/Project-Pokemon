import { Component, inject, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { BattleResponse } from '../../models/pokemon-api';
import { Move } from '../../models/move';
import { MovementButton } from '../../components/movement-button/movement-button';
import { simulateBattle } from '../../services/battle-simulator';
import { CommonModule } from '@angular/common';
import { BattleLogOverlay } from '../../components/battle-log-overlay/battle-log-overlay';

@Component({
  selector: 'app-battle',
  imports: [MovementButton, BattleLogOverlay, CommonModule],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {

  battle = signal(false);
  battleInfo = signal<BattleResponse | null>(null);
  hpA = signal<number | null>(null);
  hpB = signal<number | null>(null);

  // Estado para el log y visibilidad del overlay
  battleLog = signal<string[]>([]);
  showLogOverlay = signal(false);

  private apiService = inject(BattleService);

  async ngOnInit(): Promise<void> {
    const data = await this.apiService.getBattle();
    if (!data) return;
    
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

      // Log del combate
      const log: string[] = [];
      log.push(`Turno del usuario: ${battle.pokemonA.name} usa ${result.userMovement.name} y hace ${result.userMovement.power ?? 0} daño.`);
      log.push(`Turno de la máquina: ${battle.pokemonB.name} usa ${result.opponentMovement.name} y hace ${result.opponentMovement.power ?? 0} daño.`);
      log.push(`Vida restante de ${battle.pokemonA.name}: ${result.hpA}`);
      log.push(`Vida restante de ${battle.pokemonB.name}: ${result.hpB}`);
      if (result.winner) {
        if (result.winner === "Empate") {
          log.push("Empate. Los dos Pokemon han quedado fuera de combate.");
        } else {
          log.push(`${result.winner} ha ganado el combate`);
        }
      }
      this.battleLog.set(log);
      this.showLogOverlay.set(true);
      setTimeout(() => {
        this.showLogOverlay.set(false);
      }, 3500);
      return battle;
    });
  }
}