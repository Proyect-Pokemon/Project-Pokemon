import { Component, effect, inject, signal } from '@angular/core';
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
import { SocketService } from '../../services/websocket-service';
import { BattleService } from '../../services/battle-service';

@Component({
  selector: 'app-battle',
  imports: [MovementButton, BattleLogOverlay, FinishBattleDialog, LifeBar, BattleChat, CommonModule],
  providers: [TitleCasePipe],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {
  mode = signal<'cpu' | 'online'>('cpu');
  battleId = signal<string | null>(null);
  battleInfo = signal<BattleResponse | null>(null);
  hpA = signal<number | null>(null);
  hpB = signal<number | null>(null);
  isLoadingBattle = signal(true);
  attacksDisabled = signal(true);

  battleLog = signal<string[]>([]);
  showLogOverlay = signal(false);
  showFinishDialog = signal(false);

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private socketService = inject(SocketService);
  private battleService = inject(BattleService);

  currentUsername = this.authService.nickname;

  constructor() {
    effect(() => {
      if (this.mode() !== 'online') {
        return;
      }

      const battleEvent = this.socketService.onBattleState();
      const currentBattleId = this.battleId();

      if (!battleEvent || !currentBattleId || battleEvent.battle?.battleId !== currentBattleId) {
        return;
      }

      const mapped = this.mapBattleSnapshotToView(battleEvent.battle);
      this.battleInfo.set(mapped);
      this.hpA.set(mapped.pokemonA.hp);
      this.hpB.set(mapped.pokemonB.hp);
      this.isLoadingBattle.set(false);
      this.attacksDisabled.set(false);

      if (battleEvent.messages.length > 0) {
        this.battleLog.update((current) => [...current, ...battleEvent.messages]);
      }
    });
  }

  async ngOnInit(): Promise<void> {
    const modeParam = this.route.snapshot.queryParamMap.get('mode');
    this.mode.set(modeParam === 'online' ? 'online' : 'cpu');

    if (this.mode() === 'cpu') {
      const teamIdParam = this.route.snapshot.queryParamMap.get('teamId');
      const teamId = teamIdParam ? Number(teamIdParam) : NaN;

      if (!teamIdParam || Number.isNaN(teamId)) {
        this.isLoadingBattle.set(false);
        void this.router.navigate(['/battle']);
        return;
      }

      const data = await this.battleService.startBattle(teamId);
      this.isLoadingBattle.set(false);

      if (!data) {
        void this.router.navigate(['/battle']);
        return;
      }

      this.battleInfo.set(data);
      this.hpA.set(data.pokemonA.hp);
      this.hpB.set(data.pokemonB.hp);
      this.attacksDisabled.set(false);
      return;
    }

    const battleId = this.route.snapshot.queryParamMap.get('battleId');

    if (!battleId) {
      this.isLoadingBattle.set(false);
      void this.router.navigate(['/battle']);
      return;
    }

    this.battleId.set(battleId);
    this.socketService.setActiveBattle(battleId);
    this.attacksDisabled.set(true);
  }

  async attack(move: BattleMove): Promise<void> {
    if (this.mode() === 'cpu') {
      return;
    }

    const battleId = this.battleId();
    if (!battleId) {
      return;
    }

    this.socketService.attack(battleId, move.name);
  }

  onLineChanged(lineIndex: number): void {
    // Implementar lógica del log de combate
  }

  private mapBattleSnapshotToView(snapshot: any): BattleResponse {
    const playerSlot = snapshot.playerSide?.activeSlot ?? 0;
    const opponentSlot = snapshot.opponentSide?.activeSlot ?? 0;

    const playerPokemon = snapshot.playerSide?.team?.[playerSlot] ?? snapshot.playerSide?.team?.[0];
    const opponentPokemon = snapshot.opponentSide?.team?.[opponentSlot] ?? snapshot.opponentSide?.team?.[0];

    return {
      pokemonA: {
        name: playerPokemon?.nickname || playerPokemon?.name || 'Pokemon',
        sprite: playerPokemon?.spriteBack || playerPokemon?.spriteFront || '/assets/error/no-image.png',
        hp: playerPokemon?.currentHp ?? 0,
        atk: playerPokemon?.attack ?? 0,
        def: playerPokemon?.defense ?? 0,
        spa: playerPokemon?.specialAttack ?? 0,
        spd: playerPokemon?.specialDefense ?? 0,
        spe: playerPokemon?.speed ?? 0,
        type1: 'unknown',
        type2: null,
        moves: (playerPokemon?.movements ?? []).map((move: any) => ({
          name: move.name,
          description: '',
          power: null,
          accuracy: null,
          moveClass: '',
          pp: move.maxPp,
          currentPp: move.currentPp,
          type: move.type,
        })),
      },
      pokemonB: {
        name: opponentPokemon?.nickname || opponentPokemon?.name || 'Pokemon',
        sprite: opponentPokemon?.spriteFront || opponentPokemon?.spriteBack || '/assets/error/no-image.png',
        hp: opponentPokemon?.currentHp ?? 0,
        atk: opponentPokemon?.attack ?? 0,
        def: opponentPokemon?.defense ?? 0,
        spa: opponentPokemon?.specialAttack ?? 0,
        spd: opponentPokemon?.specialDefense ?? 0,
        spe: opponentPokemon?.speed ?? 0,
        type1: 'unknown',
        type2: null,
        moves: [],
      },
    };
  }
}