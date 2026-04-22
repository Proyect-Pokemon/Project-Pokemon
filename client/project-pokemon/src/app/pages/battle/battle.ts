import { Component, inject, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { BattleResponse } from '../../models/battle/pokemon-api';
import { BattleMove } from '../../models/move';
import { MovementButton } from '../../components/movement-button/movement-button';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { BattleLogOverlay } from '../../components/battle-log-overlay/battle-log-overlay';
import { FinishBattleDialog } from '../../components/finish-battle-dialog/finish-battle-dialog';
import { LifeBar } from '../../components/life-bar/life-bar';
import { BattleChat } from '../../components/battle-chat/battle-chat';
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
  battleInfo = signal<BattleResponse | null>(null);
  hpA = signal<number | null>(null);
  hpB = signal<number | null>(null);
  isLoadingBattle = signal(true);
  attacksDisabled = signal(true);

  battleLog = signal<string[]>([]);
  showLogOverlay = signal(false);
  showFinishDialog = signal(false);

  private apiService = inject(BattleService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
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

    this.battleInfo.set(data);
    this.hpA.set(data.pokemonA.hp);
    this.hpB.set(data.pokemonB.hp);
  }

  async attack(move: BattleMove): Promise<void> {
    // Implementar lógica de combate
  }

  onLineChanged(lineIndex: number): void {
    // Implementar lógica del log de combate
  }
}