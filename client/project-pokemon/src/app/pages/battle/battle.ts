import { Component, inject, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { BattleResponse } from '../../models/pokemon-api';
import { Move } from '../../models/move';
import { MovementButton } from '../../components/movement-button/movement-button';
import { BattleSimulatorService } from '../../services/battle-simulator';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { BattleLogOverlay } from '../../components/battle-log-overlay/battle-log-overlay';
import { FinishBattleDialog } from '../../components/finish-battle-dialog/finish-battle-dialog';
import { LifeBar } from '../../components/life-bar/life-bar';

@Component({
  selector: 'app-battle',
  imports: [MovementButton, BattleLogOverlay, FinishBattleDialog, LifeBar, CommonModule],
  providers: [TitleCasePipe],
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
  private titleCasePipe = inject(TitleCasePipe);

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
      let firstIsUser = false;
      let firstName, secondName, firstMove, secondMove;
      if (
        (battle.pokemonA.spe > battle.pokemonB.spe) || (battle.pokemonA.spe === battle.pokemonB.spe && result.userMovement === move)
      ) {
        // PokemonA ataca primero
        firstName = this.titleCasePipe.transform(battle.pokemonA.name);
        firstMove = result.userMovement.name;
        secondName = this.titleCasePipe.transform(battle.pokemonB.name);
        secondMove = result.opponentMovement.name;
        firstIsUser = true;
      } else {
        // PokemonB ataca primero
        firstName = this.titleCasePipe.transform(battle.pokemonB.name);
        firstMove = result.opponentMovement.name;
        secondName = this.titleCasePipe.transform(battle.pokemonA.name);
        secondMove = result.userMovement.name;
        firstIsUser = false;
      }

      // Mensaje del primer ataque
      if (firstIsUser) {
        log.push(`¡${firstName} ha usado ${firstMove}!`);
      } else {
        log.push(`¡El ${firstName} enemigo ha usado ${firstMove}!`);
      }
      
      // Verificar si el segundo pokemon se debilitó
      const secondHp = firstName === battle.pokemonA.name ? result.hpB : result.hpA;
      if (secondHp <= 0) {
        if (!firstIsUser) {
          log.push(`¡${secondName} se ha debilitado!`);
        } else {
          log.push(`¡El ${secondName} enemigo se ha debilitado!`);
        }
      } else {
        // Solo se muestra el segundo ataque si el defensor sobrevive
        if (!firstIsUser) {
          log.push(`¡${secondName} ha usado ${secondMove}!`);
        } else {
          log.push(`¡El ${secondName} enemigo ha usado ${secondMove}!`);
        }
        
        // Verificar si el primer pokemon se debilitó tras el contraataque
        const firstHp = firstName === battle.pokemonA.name ? result.hpA : result.hpB;
        if (firstHp <= 0) {
          if (firstIsUser) {
            log.push(`¡${firstName} se ha debilitado!`);
          } else {
            log.push(`¡El ${firstName} enemigo se ha debilitado!`);
          }
        }
      }

      if (result.winner) {
        if (result.winner !== "Empate") {
          log.push(`¡${result.winner} ha ganado el combate!`);
        }
        this.showFinishDialog.set(true);
      }
      
      this.battleLog.set(log);
      this.showLogOverlay.set(true);
      // Ocultar overlay después de mostrar todas las líneas (cada línea dura 3 segundos)
      setTimeout(() => {
        this.showLogOverlay.set(false);
      }, log.length * 3000);
      return battle;
    });
  }
  onRetryBattle() {
    // Reiniciar el combate: recargar datos y ocultar el diálogo
    this.ngOnInit();
    this.showFinishDialog.set(false);
  }
}