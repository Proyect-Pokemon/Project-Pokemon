import { Component, inject, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { BattleResponse } from '../../models/pokemon-api';
import { Move } from '../../models/move';
import { MovementButton } from '../../components/movement-button/movement-button';
import { BattleSimulatorService } from '../../services/battle-simulator';
import { CommonModule } from '@angular/common';
import { BattleLogOverlay } from '../../components/battle-log-overlay/battle-log-overlay';
import { FinishBattleDialog } from '../../components/finish-battle-dialog/finish-battle-dialog';

@Component({
  selector: 'app-battle',
  imports: [MovementButton, BattleLogOverlay, FinishBattleDialog, CommonModule],
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
  showFinishDialog = signal(false);

  private apiService = inject(BattleService);
  private battleSimulator = inject(BattleSimulatorService);

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
      // Usa current HP
      const hpA = this.hpA() ?? battle.pokemonA.hp;
      const hpB = this.hpB() ?? battle.pokemonB.hp;
      const result = this.battleSimulator.simulateBattle(battle.pokemonA, battle.pokemonB, move, hpA, hpB);
      this.hpA.set(result.hpA);
      this.hpB.set(result.hpB);

      // LOG DE COMBATE
      const log: string[] = [];
      // Determinar quién atacó primero y segundo
      let firstName, secondName, firstMove, secondMove;
      if (
        (battle.pokemonA.spe > battle.pokemonB.spe) || (battle.pokemonA.spe === battle.pokemonB.spe && result.userMovement === move)
      ) {
        // PokemonA ataca primero
        firstName = battle.pokemonA.name;
        firstMove = result.userMovement.name;
        secondName = battle.pokemonB.name;
        secondMove = result.opponentMovement.name;
      } else {
        // PokemonB ataca primero
        firstName = battle.pokemonB.name;
        firstMove = result.opponentMovement.name;
        secondName = battle.pokemonA.name;
        secondMove = result.userMovement.name;
      }

      log.push(`Turno 1: ${firstName} usa ${firstMove} y hace ${result.damageFirst ?? 0} daño.`);
      log.push(`Vida restante de ${secondName}: ${firstName === battle.pokemonA.name ? result.hpB : result.hpA}`);
      
      // Solo se muestra el segundo ataque si el defensor sobrevive
      const secondTargetHp = firstName === battle.pokemonA.name ? result.hpA : result.hpB;
      if ((firstName === battle.pokemonA.name ? result.hpB : result.hpA) > 0) {
        log.push(`Turno 2: ${secondName} usa ${secondMove} y hace ${result.damageSecond ?? 0} daño.`);
        log.push(`Vida restante de ${firstName}: ${secondTargetHp}`);
      }

      if (result.winner) {
        if (result.winner === "Empate") {
          log.push("Empate. Los dos Pokemon han quedado fuera de combate.");
        } else {
          log.push(`${result.winner} ha ganado el combate`);
        }
        this.showFinishDialog.set(true);
      }
      
      this.battleLog.set(log);
      this.showLogOverlay.set(true);
      setTimeout(() => {
        this.showLogOverlay.set(false);
      }, 3500);
      return battle;
    });
  }
  onRetryBattle() {
    // Reiniciar el combate: recargar datos y ocultar el diálogo
    this.ngOnInit();
    this.showFinishDialog.set(false);
  }
}