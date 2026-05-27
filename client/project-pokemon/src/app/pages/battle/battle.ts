import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { BattleResponse } from '../../models/battle/pokemon-api';
import { BattleMove } from '../../models/move';
import { MovementButton } from '../../components/movement-button/movement-button';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { FinishBattleDialog } from '../../components/finish-battle-dialog/finish-battle-dialog';
import { LifeBar } from '../../components/life-bar/life-bar';
import { BattleChat } from '../../components/battle-chat/battle-chat';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth';
import { SocketService } from '../../services/websocket-service';
import { BattleService } from '../../services/battle-service';

type BattleActionPanel = 'root' | 'attack' | 'switch';
type BattleResult = 'victory' | 'defeat' | null;
type BattleSideKey = 'player' | 'opponent';

interface BattleStateEventPayload {
  battle: any;
  messages: string[];
  timeline: any[];
  requiresSwitch: boolean;
  winnerUserId: number | null;
}

@Component({
  selector: 'app-battle',
  imports: [MovementButton, FinishBattleDialog, LifeBar, BattleChat, CommonModule],
  providers: [TitleCasePipe],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {
  private readonly FALLBACK_SPRITE = '/assets/error/missing-no.png';
  private readonly WAITING_MESSAGE_SNIPPET = 'esperando al rival';
  private readonly ONLINE_BATTLE_BOOTSTRAP_TIMEOUT_MS = 8000;
  private readonly TURN_MESSAGE_DURATION_MS = 1000;
  private readonly TURN_EFFECT_DELAY_MS = 220;
  private onlineBattleBootstrapTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private playbackChain: Promise<void> = Promise.resolve();
  private readonly playbackTimeouts = new Set<ReturnType<typeof setTimeout>>();
  private isDestroyed = false;

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

  combatChatMessages = signal<string[]>([]);
  showFinishDialog = signal(false);
  battleResult = signal<BattleResult>(null);
  showLeaveConfirmation = signal(false);

  private leaveDecisionResolver: ((decision: boolean) => void) | null = null;
  private leaveDecisionPromise: Promise<boolean> | null = null;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private socketService = inject(SocketService);
  private battleService = inject(BattleService);

  currentUsername = this.authService.nickname;
  currentUserId = this.authService.currentUserId;

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
        sprite: this.resolveSnapshotSprite(pokemon, true),
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
      const currentBattleId = this.battleId() ?? this.route.snapshot.queryParamMap.get('battleId');

      if (!battleEvent || !currentBattleId || battleEvent.battle?.battleId !== currentBattleId) {
        return;
      }

      if (!this.battleId()) {
        this.battleId.set(currentBattleId);
      }

      this.latestBattleSnapshot.set(battleEvent.battle);
      this.isLoadingBattle.set(false);
      this.clearOnlineBattleBootstrapTimeout();

      const waitingForOpponent = this.hasWaitingMessage(battleEvent.messages);
      const backendMessages = (battleEvent.messages ?? []).filter((message) => !!message?.trim());
      const filteredTimeline = battleEvent.timeline ?? [];
      const hasPlaybackContent = backendMessages.length > 0 || filteredTimeline.length > 0;
      const shouldPlayTurn = hasPlaybackContent && !waitingForOpponent;

      if (!untracked(() => this.battleInfo())) {
        this.applySnapshotToView(battleEvent.battle);
      }

      if (shouldPlayTurn) {
        this.enqueueBattlePlayback({
          battle: battleEvent.battle,
          messages: backendMessages,
          timeline: filteredTimeline,
          requiresSwitch: battleEvent.requiresSwitch ?? false,
          winnerUserId: battleEvent.winnerUserId ?? null,
        });
        return;
      }

      this.applySnapshotToView(battleEvent.battle);
      this.applyBattleEventState(battleEvent, waitingForOpponent);
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

    this.battleId.set(battleId);

    const matchedBattleId = this.socketService.onBattleMatched()?.battleId;
    if (!matchedBattleId || matchedBattleId !== battleId) {
      this.isLoadingBattle.set(false);
      void this.router.navigate(['/battle-select'], {
        queryParams: { mode: 'online' },
      });
      return;
    }

    this.socketService.setActiveBattle(battleId);
    this.attacksDisabled.set(true);
    this.isWaitingForOpponent.set(false);
    this.actionPanel.set('root');
    this.startOnlineBattleBootstrapTimeout();
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.clearPlaybackTimeouts();
    this.clearOnlineBattleBootstrapTimeout();
    this.socketService.resetBattleContext();
  }

  canLeaveBattle(): Promise<boolean> | boolean {
    if (!this.shouldConfirmLeaveBattle()) {
      return true;
    }

    if (this.leaveDecisionPromise) {
      return this.leaveDecisionPromise;
    }

    this.showLeaveConfirmation.set(true);
    this.leaveDecisionPromise = new Promise<boolean>((resolve) => {
      this.leaveDecisionResolver = resolve;
    });

    return this.leaveDecisionPromise;
  }

  confirmLeaveBattle(): void {
    const currentBattleId = this.battleId();
    if (this.mode() === 'online' && currentBattleId) {
      this.socketService.forfeit(currentBattleId);
    }

    this.resolveLeaveDecision(true);
  }

  cancelLeaveBattle(): void {
    this.resolveLeaveDecision(false);
  }

  openAttackPanel(): void {
    if (this.attacksDisabled()) {
      return;
    }
    this.actionPanel.set('attack');
  }

  openSwitchPanel(): void {
    if (this.attacksDisabled()) {
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

  private resolveBattleResult(winnerUserId: number | null): BattleResult {
    if (!winnerUserId) {
      return 'defeat';
    }

    if (this.currentUserId() === winnerUserId) {
      return 'victory';
    }

    return 'defeat';
  }

  private shouldConfirmLeaveBattle(): boolean {
    return this.mode() === 'online' && !!this.battleId() && !this.showFinishDialog();
  }

  private resolveLeaveDecision(decision: boolean): void {
    this.showLeaveConfirmation.set(false);

    if (this.leaveDecisionResolver) {
      this.leaveDecisionResolver(decision);
    }

    this.leaveDecisionResolver = null;
    this.leaveDecisionPromise = null;
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

  private enqueueBattlePlayback(event: BattleStateEventPayload): void {
    this.playbackChain = this.playbackChain.then(async () => {
      if (this.isDestroyed) {
        return;
      }

      const backendChatMessages = event.messages
        .map((message) => message.trim())
        .filter((message): message is string => !!message);
      const playbackChatMessages = backendChatMessages;
      const playbackItems = event.timeline;

      if (!playbackItems.length && !playbackChatMessages.length) {
        this.applySnapshotToView(event.battle);
        this.applyBattleEventState(event, false);
        return;
      }

      this.showFinishDialog.set(false);
      this.battleResult.set(null);
      this.attacksDisabled.set(true);
      this.isWaitingForOpponent.set(false);

      try {
        const totalSteps = Math.max(playbackItems.length, playbackChatMessages.length);
        for (let index = 0; index < totalSteps; index++) {
          const item = playbackItems[index];
          if (this.isDestroyed) {
            return;
          }

          const combatMessage = playbackChatMessages[index] ?? '';

          if (combatMessage) {
            this.combatChatMessages.update((current) => [...current, combatMessage]);
          }

          if (item) {
            await this.wait(this.TURN_EFFECT_DELAY_MS);
            this.applyPlaybackEffect(item, event.battle);
            await this.wait(Math.max(0, this.TURN_MESSAGE_DURATION_MS - this.TURN_EFFECT_DELAY_MS));
            continue;
          }

          await this.wait(this.TURN_MESSAGE_DURATION_MS);
        }
      } finally {
        if (!this.isDestroyed) {
          this.applySnapshotToView(event.battle);
          this.applyBattleEventState(event, false);
        }
      }
    });
  }

  private applyBattleEventState(event: BattleStateEventPayload, waitingForOpponent: boolean): void {
    const isFinished = event.winnerUserId !== null && event.winnerUserId !== undefined;

    if (isFinished) {
      this.battleResult.set(this.resolveBattleResult(event.winnerUserId));
      this.showFinishDialog.set(true);
      this.isWaitingForOpponent.set(false);
      this.attacksDisabled.set(true);
      this.actionPanel.set('root');
      return;
    }

    this.showFinishDialog.set(false);
    this.battleResult.set(null);
    this.isWaitingForOpponent.set(waitingForOpponent);
    this.attacksDisabled.set(waitingForOpponent);

    if (event.requiresSwitch) {
      this.actionPanel.set('switch');
      return;
    }

    this.actionPanel.set('root');
  }

  private applySnapshotToView(snapshot: any): void {
    const mapped = this.mapBattleSnapshotToView(snapshot);
    this.battleInfo.set(mapped);
    this.hpA.set(mapped.pokemonA.currentHp ?? mapped.pokemonA.hp ?? 0);
    this.hpB.set(mapped.pokemonB.currentHp ?? mapped.pokemonB.hp ?? 0);
  }

  private applyPlaybackEffect(playbackItem: any, finalSnapshot: any): void {
    const eventType = playbackItem?.eventType;
    if (eventType) {
      this.applyTimelineEvent(playbackItem, finalSnapshot);
      return;
    }

    const message = playbackItem?.message ?? playbackItem;
    const attackMatch = message.match(/^(.+?) usa (.+?)\. Daño: (\d+)\.$/);
    if (attackMatch) {
      const side = this.resolveSideByVisibleName(attackMatch[1]);
      if (side) {
        this.applyDamageToSide(this.getOppositeSide(side), Number(attackMatch[3]));
      }
      return;
    }

    const statusDamageMatch = message.match(/^(.+?) sufre daño por .+? \((\d+) PS\)\.$/);
    if (statusDamageMatch) {
      const side = this.resolveSideByVisibleName(statusDamageMatch[1]);
      if (side) {
        this.applyDamageToSide(side, Number(statusDamageMatch[2]));
      }
      return;
    }

    const drainMatch = message.match(/^(.+?) pierde (\d+) PS por Drenadoras(?:\. (.+?) recupera (\d+) PS\.)?$/);
    if (drainMatch) {
      const damagedSide = this.resolveSideByVisibleName(drainMatch[1]);
      if (damagedSide) {
        this.applyDamageToSide(damagedSide, Number(drainMatch[2]));
      }

      const healedName = drainMatch[3];
      const healedAmount = drainMatch[4];
      if (healedName && healedAmount) {
        const healedSide = this.resolveSideByVisibleName(healedName);
        if (healedSide) {
          this.applyHealToSide(healedSide, Number(healedAmount));
        }
      }
      return;
    }

    const faintMatch = message.match(/^(.+?) se debilitó\.$/);
    if (faintMatch) {
      const side = this.resolveSideByVisibleName(faintMatch[1]);
      if (side) {
        this.setSideHp(side, 0);
      }
      return;
    }

    const switchMatch = message.match(/^(?:Cambio realizado: entra|Entra) (.+?)(?: automáticamente)?\.$/);
    if (switchMatch) {
      this.applySwitchFromSnapshot(switchMatch[1], finalSnapshot);
    }
  }

  private applyTimelineEvent(event: any, finalSnapshot: any): void {
    switch (event.eventType) {
      case 'hp_change':
        if (event.target?.side === 'player' || event.target?.side === 'opponent') {
          this.setSideHp(event.target.side, event.afterHp ?? 0);
        }
        return;

      case 'status_change':
        if (event.target?.side === 'player' || event.target?.side === 'opponent') {
          this.setSideStatus(event.target.side, event.afterStatus ?? 'None');
        }
        return;

      case 'faint':
        if (event.target?.side === 'player' || event.target?.side === 'opponent') {
          this.setSideHp(event.target.side, 0);
        }
        return;

      case 'switch':
        if (event.side === 'player' || event.side === 'opponent') {
          this.applySwitchFromTimeline(event, finalSnapshot);
        }
        return;

      default:
        return;
    }
  }

  private applyDamageToSide(side: BattleSideKey, damage: number): void {
    const currentHp = side === 'player' ? (this.hpA() ?? 0) : (this.hpB() ?? 0);
    this.setSideHp(side, Math.max(0, currentHp - Math.max(0, damage)));
  }

  private applyHealToSide(side: BattleSideKey, healing: number): void {
    const current = this.battleInfo();
    if (!current) {
      return;
    }

    const currentHp = side === 'player' ? (this.hpA() ?? 0) : (this.hpB() ?? 0);
    const maxHp = side === 'player' ? current.pokemonA.maxHp : current.pokemonB.maxHp;
    this.setSideHp(side, Math.min(maxHp, currentHp + Math.max(0, healing)));
  }

  private setSideHp(side: BattleSideKey, hp: number): void {
    const current = this.battleInfo();
    if (!current) {
      return;
    }

    if (side === 'player') {
      this.hpA.set(hp);
      this.battleInfo.set({
        ...current,
        pokemonA: {
          ...current.pokemonA,
          currentHp: hp,
        },
      });
      return;
    }

    this.hpB.set(hp);
    this.battleInfo.set({
      ...current,
      pokemonB: {
        ...current.pokemonB,
        currentHp: hp,
      },
    });
  }

  private applySwitchFromSnapshot(nextPokemonName: string, finalSnapshot: any): void {
    const side = this.resolveSwitchSide(nextPokemonName, finalSnapshot);
    if (!side) {
      return;
    }

    const activePokemon = this.getActiveSnapshotPokemon(finalSnapshot, side);
    if (!activePokemon) {
      return;
    }

    const nextPokemonView = this.mapSnapshotPokemonToView(activePokemon, side === 'player');
    const current = this.battleInfo();
    if (!current) {
      return;
    }

    if (side === 'player') {
      this.battleInfo.set({ ...current, pokemonA: nextPokemonView });
      this.hpA.set(nextPokemonView.currentHp ?? 0);
      return;
    }

    this.battleInfo.set({ ...current, pokemonB: nextPokemonView });
    this.hpB.set(nextPokemonView.currentHp ?? 0);
  }

  private applySwitchFromTimeline(event: any, finalSnapshot: any): void {
    const switchedPokemon = this.getSnapshotPokemonBySlot(finalSnapshot, event.side, event.newActiveSlot);
    if (!switchedPokemon) {
      return;
    }

    const nextPokemonView = this.mapSnapshotPokemonToView(switchedPokemon, event.side === 'player');
    const current = this.battleInfo();
    if (!current) {
      return;
    }

    if (event.side === 'player') {
      this.battleInfo.set({ ...current, pokemonA: nextPokemonView });
      this.hpA.set(nextPokemonView.currentHp ?? 0);
      return;
    }

    this.battleInfo.set({ ...current, pokemonB: nextPokemonView });
    this.hpB.set(nextPokemonView.currentHp ?? 0);
  }

  private resolveSwitchSide(nextPokemonName: string, finalSnapshot: any): BattleSideKey | null {
    const normalizedName = this.normalizeName(nextPokemonName);
    const current = this.battleInfo();
    const finalPlayer = this.getActiveSnapshotPokemon(finalSnapshot, 'player');
    const finalOpponent = this.getActiveSnapshotPokemon(finalSnapshot, 'opponent');

    const playerMatches = this.normalizeName(finalPlayer?.nickname || finalPlayer?.name || '') === normalizedName;
    const opponentMatches = this.normalizeName(finalOpponent?.nickname || finalOpponent?.name || '') === normalizedName;

    if (playerMatches && this.normalizeName(current?.pokemonA.name || '') !== normalizedName) {
      return 'player';
    }

    if (opponentMatches && this.normalizeName(current?.pokemonB.name || '') !== normalizedName) {
      return 'opponent';
    }

    if (playerMatches && !opponentMatches) {
      return 'player';
    }

    if (opponentMatches && !playerMatches) {
      return 'opponent';
    }

    return null;
  }

  private resolveSideByVisibleName(pokemonName: string): BattleSideKey | null {
    const current = this.battleInfo();
    if (!current) {
      return null;
    }

    const normalizedName = this.normalizeName(pokemonName);
    if (this.normalizeName(current.pokemonA.name) === normalizedName) {
      return 'player';
    }

    if (this.normalizeName(current.pokemonB.name) === normalizedName) {
      return 'opponent';
    }

    return null;
  }

  private getOppositeSide(side: BattleSideKey): BattleSideKey {
    return side === 'player' ? 'opponent' : 'player';
  }

  private getActiveSnapshotPokemon(snapshot: any, side: BattleSideKey): any | null {
    const sideKey = side === 'player' ? 'playerSide' : 'opponentSide';
    const team = snapshot?.[sideKey]?.team;
    const activeSlot = snapshot?.[sideKey]?.activeSlot ?? 0;
    if (!Array.isArray(team)) {
      return null;
    }

    return team[activeSlot] ?? team[0] ?? null;
  }

  private getSnapshotPokemonBySlot(snapshot: any, side: BattleSideKey, slot: number): any | null {
    const sideKey = side === 'player' ? 'playerSide' : 'opponentSide';
    const team = snapshot?.[sideKey]?.team;
    if (!Array.isArray(team)) {
      return null;
    }

    return team.find((pokemon: any) => pokemon?.slot === slot) ?? team[slot] ?? null;
  }

  private mapSnapshotPokemonToView(pokemon: any, preferBackSprite: boolean) {
    return {
      name: pokemon?.nickname || pokemon?.name || 'Pokemon',
      sex: pokemon?.sex ?? null,
      sprite: this.resolveSnapshotSprite(pokemon, preferBackSprite),
      statusCondition: pokemon?.status ?? null,
      currentHp: pokemon?.currentHp ?? 0,
      maxHp: pokemon?.maxHp ?? 0,
      atk: pokemon?.attack ?? 0,
      def: pokemon?.defense ?? 0,
      spa: pokemon?.specialAttack ?? 0,
      spd: pokemon?.specialDefense ?? 0,
      spe: pokemon?.speed ?? 0,
      type1: 'unknown',
      type2: null,
      moves: preferBackSprite
        ? (pokemon?.movements ?? []).map((move: any) => ({
            name: move.name,
            description: '',
            power: null,
            accuracy: null,
            moveClass: '',
            pp: move.maxPp,
            currentPp: move.currentPp,
            type: move.type,
          }))
        : [],
    };
  }

  private wait(milliseconds: number): Promise<void> {
    return new Promise((resolve) => {
      const timeoutId = setTimeout(() => {
        this.playbackTimeouts.delete(timeoutId);
        resolve();
      }, milliseconds);

      this.playbackTimeouts.add(timeoutId);
    });
  }

  private clearPlaybackTimeouts(): void {
    for (const timeoutId of this.playbackTimeouts) {
      clearTimeout(timeoutId);
    }

    this.playbackTimeouts.clear();
  }

  private normalizeName(value: string): string {
    return value
      .toLowerCase()
      .trim()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  private setSideStatus(side: BattleSideKey, status: string): void {
    const current = this.battleInfo();
    if (!current) {
      return;
    }

    const normalizedStatus = status === 'None' ? '' : status;
    if (side === 'player') {
      this.battleInfo.set({
        ...current,
        pokemonA: {
          ...current.pokemonA,
          statusCondition: normalizedStatus,
        },
      });
      return;
    }

    this.battleInfo.set({
      ...current,
      pokemonB: {
        ...current.pokemonB,
        statusCondition: normalizedStatus,
      },
    });
  }

  private mapBattleSnapshotToView(snapshot: any): BattleResponse {
    const playerSlot = snapshot.playerSide?.activeSlot ?? 0;
    const opponentSlot = snapshot.opponentSide?.activeSlot ?? 0;

    const playerPokemon = snapshot.playerSide?.team?.[playerSlot] ?? snapshot.playerSide?.team?.[0];
    const opponentPokemon = snapshot.opponentSide?.team?.[opponentSlot] ?? snapshot.opponentSide?.team?.[0];

    return {
      pokemonA: this.mapSnapshotPokemonToView(playerPokemon, true),
      pokemonB: this.mapSnapshotPokemonToView(opponentPokemon, false),
    };
  }

  private resolveSnapshotSprite(pokemon: any, preferBack: boolean): string {
    const isFemale = this.isFemaleSex(pokemon?.sex);
    const isShiny = !!pokemon?.shiny;

    if (preferBack) {
      if (isShiny) {
        if (isFemale && pokemon?.spriteBackFemShiny) {
          return pokemon.spriteBackFemShiny;
        }

        if (pokemon?.spriteBackShiny) {
          return pokemon.spriteBackShiny;
        }
      }

      if (isFemale && pokemon?.spriteBackFem) {
        return pokemon.spriteBackFem;
      }

      return pokemon?.spriteBack || pokemon?.spriteFront || this.FALLBACK_SPRITE;
    }

    if (isShiny) {
      if (isFemale && pokemon?.spriteFrontFemShiny) {
        return pokemon.spriteFrontFemShiny;
      }

      if (pokemon?.spriteFrontShiny) {
        return pokemon.spriteFrontShiny;
      }
    }

    if (isFemale && pokemon?.spriteFrontFem) {
      return pokemon.spriteFrontFem;
    }

    return pokemon?.spriteFront || pokemon?.spriteBack || this.FALLBACK_SPRITE;
  }

  private isFemaleSex(sex: unknown): boolean {
    if (typeof sex !== 'string') {
      return false;
    }

    const normalized = sex.trim().toLowerCase();
    return normalized === 'h' || normalized === 'f';
  }
}