import { Component, inject, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { BattleResponse } from '../../models/pokemon-api';
import { BattleMove } from '../../models/move';
import { MovementButton } from '../../components/movement-button/movement-button';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { BattleLogOverlay } from '../../components/battle-log-overlay/battle-log-overlay';
import { FinishBattleDialog } from '../../components/finish-battle-dialog/finish-battle-dialog';
import { LifeBar } from '../../components/life-bar/life-bar';
import { BattleChat } from '../../components/battle-chat/battle-chat';
import { BattleTurnResponse } from '../../models/battle-turn';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-battle',
  imports: [MovementButton, BattleLogOverlay, FinishBattleDialog, LifeBar, BattleChat, CommonModule],
  providers: [TitleCasePipe],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {

  battle = signal(false);
  battleInfo = signal<BattleResponse | null>(null);
  hpA = signal<number | null>(null);
  hpB = signal<number | null>(null);
  isLoadingBattle = signal(true);

  // Estado para el log y visibilidad del overlay
  battleLog = signal<string[]>([]);
  showLogOverlay = signal(false);
  showFinishDialog = signal(false);
  
  // Variables temporales para actualizar HP de forma sincronizada
  private pendingHpA: number | null = null;
  private pendingHpB: number | null = null;
  private logLineActions: ('update-hpA' | 'update-hpB' | 'none')[] = [];

  private apiService = inject(BattleService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private titleCasePipe = inject(TitleCasePipe);
  private authService = inject(AuthService);
  private currentTeamId: number | null = null;

  currentUsername = this.authService.nickname;

  async ngOnInit(): Promise<void> {
    const teamIdParam = this.route.snapshot.queryParamMap.get('teamId');
    const teamId = teamIdParam ? Number(teamIdParam) : NaN;

    if (!teamIdParam || Number.isNaN(teamId)) {
      this.isLoadingBattle.set(false);
      void this.router.navigate(['/battle']);
      return;
    }

    this.currentTeamId = teamId;
    const data = await this.apiService.startBattle(teamId);
    this.isLoadingBattle.set(false);

    if (!data) return;

    this.hydrateBattle(data);
  }

  async attack(move: BattleMove): Promise<void> {
    const response = await this.apiService.playTurn(move.name);
    if (!response) return;

    this.applyBattleTurn(response);
  }

  onLineChanged(lineIndex: number): void {
    const action = this.logLineActions[lineIndex];
    
    if (action === 'update-hpA') {
      this.hpA.set(this.pendingHpA!);
    } else if (action === 'update-hpB') {
      this.hpB.set(this.pendingHpB!);
    }
    // Si action === 'none', no se actualiza nada
  }

  onRetryBattle() {
    if (!this.currentTeamId) {
      this.resetBattleState();
      return;
    }

    this.apiService.startBattle(this.currentTeamId).then((data) => {
      if (!data) return;

      this.hydrateBattle(data);
    });
    this.showFinishDialog.set(false);
  }

  private hydrateBattle(data: BattleResponse): void {
    this.resetBattleState();
    this.battleInfo.set(data);
    this.hpA.set(data.pokemonA.hp);
    this.hpB.set(data.pokemonB.hp);
    this.battle.set(true);
  }

  private resetBattleState(): void {
    this.battle.set(false);
    this.battleInfo.set(null);
    this.hpA.set(null);
    this.hpB.set(null);
    this.pendingHpA = null;
    this.pendingHpB = null;
    this.battleLog.set([]);
    this.logLineActions = [];
    this.showLogOverlay.set(false);
    this.showFinishDialog.set(false);
  }

  private applyBattleTurn(result: BattleTurnResponse): void {
    if (result.battle) {
      this.battleInfo.set(result.battle);
    }

    const battle = this.battleInfo();
    if (!battle) return;

    const resolvedHpA = result.hpA ?? result.pokemonAHp ?? battle.pokemonA.hp;
    const resolvedHpB = result.hpB ?? result.pokemonBHp ?? battle.pokemonB.hp;

    this.pendingHpA = resolvedHpA;
    this.pendingHpB = resolvedHpB;
    this.hpA.set(resolvedHpA);
    this.hpB.set(resolvedHpB);

    const rawLog = result.log ?? result.battleLog ?? [];
    const normalizedLog = this.normalizeBattleLog(rawLog, result.winner);
    this.battleLog.set(normalizedLog);
    this.logLineActions = normalizedLog.map(() => 'none');

    if (normalizedLog.length > 0) {
      this.showLogOverlay.set(true);
      setTimeout(() => {
        this.showLogOverlay.set(false);
        if (result.finished || result.isFinished || !!result.winner) {
          this.showFinishDialog.set(true);
        }
      }, normalizedLog.length * 3000);
      return;
    }

    if (result.finished || result.isFinished || !!result.winner) {
      this.showFinishDialog.set(true);
    }
  }

  private normalizeBattleLog(logLines: string[], winner?: string | null): string[] {
    if (winner && winner !== 'Empate') {
      return [
        ...logLines,
        `¡${this.titleCasePipe.transform(winner)} ha ganado el combate!`,
      ];
    }

    return logLines;
  }
}