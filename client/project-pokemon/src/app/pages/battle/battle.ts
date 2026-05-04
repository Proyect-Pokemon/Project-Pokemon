import { Component, computed, effect, inject, signal } from '@angular/core';
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

type BattleActionPanel = 'root' | 'attack' | 'switch';

@Component({
  selector: 'app-battle',
  imports: [MovementButton, BattleLogOverlay, FinishBattleDialog, LifeBar, BattleChat, CommonModule],
  providers: [TitleCasePipe],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {
  private readonly FALLBACK_SPRITE = '/assets/error/missing-no.png';
  private readonly WAITING_MESSAGE_SNIPPET = 'esperando al rival';
  private readonly ONLINE_BATTLE_BOOTSTRAP_TIMEOUT_MS = 8000;
  private onlineBattleBootstrapTimeoutId: ReturnType<typeof setTimeout> | null = null;

  mode = signal<'cpu' | 'online'>('cpu');
  battleId = signal<string | null>(null);
  battleInfo = signal<BattleResponse | null>(null);
  latestBattleSnapshot = signal<any | null>(null);
  hpA = signal<number | null>(null);
  hpB = signal<number | null>(null);
  isLoadingBattle = signal(true);
  attacksDisabled = signal(true);
  isWaitingForOpponent = signal(false);
  actionPanel = signal<BattleActionPanel>('root');

  battleLog = signal<string[]>([]);
  showLogOverlay = signal(false);
  showFinishDialog = signal(false);

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private socketService = inject(SocketService);
  private battleService = inject(BattleService);

  currentUsername = this.authService.nickname;

  switchOptions = computed(() => {
    const snapshot = this.latestBattleSnapshot();
    const team = snapshot?.playerSide?.team;
    const activeSlot = snapshot?.playerSide?.activeSlot ?? 0;

    if (!Array.isArray(team)) {
      return [] as Array<{ index: number; name: string; hpLabel: string; sprite: string; isActive: boolean; isFainted: boolean }>;
    }

    return team.map((pokemon: any, index: number) => {
      const maxHp = pokemon?.maxHp ?? 0;
      const currentHp = pokemon?.currentHp ?? 0;
      return {
        index,
        name: pokemon?.nickname || pokemon?.name || `Pokemon ${index + 1}`,
        hpLabel: `${currentHp}/${maxHp}`,
        sprite: pokemon?.spriteBack || pokemon?.spriteFront || this.FALLBACK_SPRITE,
        isActive: index === activeSlot,
        isFainted: !!pokemon?.isFainted,
      };
    });
  });

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
      this.latestBattleSnapshot.set(battleEvent.battle);
      this.battleInfo.set(mapped);
      this.hpA.set(mapped.pokemonA.currentHp ?? mapped.pokemonA.hp ?? 0);
      this.hpB.set(mapped.pokemonB.currentHp ?? mapped.pokemonB.hp ?? 0);
      this.isLoadingBattle.set(false);
      this.clearOnlineBattleBootstrapTimeout();

      const waitingForOpponent = this.hasWaitingMessage(battleEvent.messages);
      this.isWaitingForOpponent.set(waitingForOpponent);
      this.attacksDisabled.set(waitingForOpponent);

      if (battleEvent.requiresSwitch) {
        this.actionPanel.set('switch');
      } else if (!waitingForOpponent) {
        this.actionPanel.set('root');
      }

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
      this.hpA.set(data.pokemonA.currentHp ?? data.pokemonA.hp ?? 0);
      this.hpB.set(data.pokemonB.currentHp ?? data.pokemonB.hp ?? 0);
      this.attacksDisabled.set(false);
      this.isWaitingForOpponent.set(false);
      this.actionPanel.set('root');
      return;
    }

    const battleId = this.route.snapshot.queryParamMap.get('battleId');

    if (!battleId) {
      this.isLoadingBattle.set(false);
      void this.router.navigate(['/battle']);
      return;
    }

    const matchedBattleId = this.socketService.onBattleMatched()?.battleId;
    if (!matchedBattleId || matchedBattleId !== battleId) {
      this.isLoadingBattle.set(false);
      void this.router.navigate(['/battle-select'], {
        queryParams: { mode: 'online' },
      });
      return;
    }

    this.battleId.set(battleId);
    this.socketService.setActiveBattle(battleId);
    this.attacksDisabled.set(true);
    this.isWaitingForOpponent.set(false);
    this.actionPanel.set('root');
    this.startOnlineBattleBootstrapTimeout();
  }

  ngOnDestroy(): void {
    this.clearOnlineBattleBootstrapTimeout();
    this.socketService.setActiveBattle(null);
  }

  openAttackPanel(): void {
    if (this.attacksDisabled() || this.showLogOverlay()) {
      return;
    }
    this.actionPanel.set('attack');
  }

  openSwitchPanel(): void {
    if (this.attacksDisabled() || this.showLogOverlay()) {
      return;
    }
    this.actionPanel.set('switch');
  }

  backToActionMenu(): void {
    if (this.isWaitingForOpponent()) {
      return;
    }

    if (this.socketService.onBattleState()?.requiresSwitch) {
      return;
    }

    this.actionPanel.set('root');
  }

  async attack(move: BattleMove): Promise<void> {
    if (this.mode() === 'cpu') {
      return;
    }

    const battleId = this.battleId();
    if (!battleId) {
      return;
    }

    this.attacksDisabled.set(true);
    this.isWaitingForOpponent.set(true);
    this.socketService.attack(battleId, move.name);
  }

  switchPokemon(targetSlot: number): void {
    if (this.mode() === 'cpu') {
      return;
    }

    const battleId = this.battleId();
    if (!battleId) {
      return;
    }

    this.attacksDisabled.set(true);
    this.isWaitingForOpponent.set(true);
    this.socketService.switchPokemon(battleId, targetSlot);
  }

  private hasWaitingMessage(messages: string[]): boolean {
    return messages.some(message =>
      message?.toLowerCase().includes(this.WAITING_MESSAGE_SNIPPET)
    );
  }

  onLineChanged(lineIndex: number): void {
    // Implementar lógica del log de combate
  }

  private startOnlineBattleBootstrapTimeout(): void {
    this.clearOnlineBattleBootstrapTimeout();

    this.onlineBattleBootstrapTimeoutId = setTimeout(() => {
      if (this.mode() !== 'online') {
        return;
      }

      if (this.battleInfo()) {
        return;
      }

      this.socketService.setActiveBattle(null);
      this.isLoadingBattle.set(false);
      void this.router.navigate(['/battle-select'], {
        queryParams: { mode: 'online' },
      });
    }, this.ONLINE_BATTLE_BOOTSTRAP_TIMEOUT_MS);
  }

  private clearOnlineBattleBootstrapTimeout(): void {
    if (!this.onlineBattleBootstrapTimeoutId) {
      return;
    }

    clearTimeout(this.onlineBattleBootstrapTimeoutId);
    this.onlineBattleBootstrapTimeoutId = null;
  }

  private mapBattleSnapshotToView(snapshot: any): BattleResponse {
    const playerSlot = snapshot.playerSide?.activeSlot ?? 0;
    const opponentSlot = snapshot.opponentSide?.activeSlot ?? 0;

    const playerPokemon = snapshot.playerSide?.team?.[playerSlot] ?? snapshot.playerSide?.team?.[0];
    const opponentPokemon = snapshot.opponentSide?.team?.[opponentSlot] ?? snapshot.opponentSide?.team?.[0];

    return {
      pokemonA: {
        name: playerPokemon?.nickname || playerPokemon?.name || 'Pokemon',
        sprite: playerPokemon?.spriteBack || playerPokemon?.spriteFront || this.FALLBACK_SPRITE,
        currentHp: playerPokemon?.currentHp ?? 0,
        maxHp: playerPokemon?.maxHp ?? 0,
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
        sprite: opponentPokemon?.spriteFront || opponentPokemon?.spriteBack || this.FALLBACK_SPRITE,
        currentHp: opponentPokemon?.currentHp ?? 0,
        maxHp: opponentPokemon?.maxHp ?? 0,
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