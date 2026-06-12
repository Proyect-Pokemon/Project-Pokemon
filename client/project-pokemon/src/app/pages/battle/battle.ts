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
  replaySteps: Array<{
    stepIndex: number;
    message?: string | null;
    events: any[];
    delayMs?: number | null;
  }>;
  turnResolved: boolean;
  requiresSwitchSelection: boolean;
  availableSlotsForSwitch: number[];
  opponentRequiresSwitch: boolean;
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
  private readonly ONLINE_BATTLE_BOOTSTRAP_TIMEOUT_MS = 8000;
  private readonly DEFAULT_REPLAY_STEP_DELAY_MS = 1100;
  private readonly ATTACK_DASH_DURATION_MS = 220;
  private readonly WAITING_OPPONENT_MESSAGE = 'esperando al rival';
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
  requiresSwitchSelection = signal(false);
  availableSlotsForSwitch = signal<number[]>([]);
  isSubmittingForcedSwitch = signal(false);
  actionPanel = signal<BattleActionPanel>('root');

  combatChatMessages = signal<string[]>([]);
  showFinishDialog = signal(false);
  battleResult = signal<BattleResult>(null);
  showLeaveConfirmation = signal(false);
  playerAttackAnimating = signal(false);
  opponentAttackAnimating = signal(false);
  playerStatStageIndicator = signal<string | null>(null);
  opponentStatStageIndicator = signal<string | null>(null);

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
      this.isSubmittingForcedSwitch.set(false);

      const replaySteps = (battleEvent.replaySteps ?? []).filter((step) => !!step);
      const hasPlaybackContent = replaySteps.length > 0;
      const shouldPlayTurn = hasPlaybackContent;

      if (!untracked(() => this.battleInfo())) {
        this.applySnapshotToView(battleEvent.battle);
      }

      if (shouldPlayTurn) {
        this.enqueueBattlePlayback({
          battle: battleEvent.battle,
          replaySteps,
          turnResolved: battleEvent.turnResolved ?? false,
          requiresSwitchSelection: battleEvent.requiresSwitchSelection ?? false,
          availableSlotsForSwitch: (battleEvent.availableSlotsForSwitch ?? []).filter((slot: unknown) => Number.isInteger(slot)) as number[],
          opponentRequiresSwitch: battleEvent.opponentRequiresSwitch ?? false,
          winnerUserId: battleEvent.winnerUserId ?? null,
        });
        return;
      }

      this.applySnapshotToView(battleEvent.battle);
      this.syncActivePokemonAfterFaint(battleEvent.battle);
      const waitingNoPlayback = !(battleEvent.turnResolved ?? false) || (battleEvent.opponentRequiresSwitch ?? false);
      this.applyBattleEventState(battleEvent, waitingNoPlayback);
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
    if (this.attacksDisabled() || this.requiresSwitchSelection()) {
      return;
    }
    this.actionPanel.set('attack');
  }

  openSwitchPanel(): void {
    if (this.attacksDisabled() || this.requiresSwitchSelection()) {
      return;
    }
    this.actionPanel.set('switch');
  }

  backToActionMenu(): void {
    if (this.isWaitingForOpponent()) {
      return;
    }

    if (this.requiresSwitchSelection()) {
      return;
    }

    this.actionPanel.set('root');
  }

  async attack(move: BattleMove): Promise<void> {
    if (this.mode() === 'cpu' || this.requiresSwitchSelection() || this.attacksDisabled() || this.isWaitingForOpponent()) {
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

    if (this.requiresSwitchSelection()) {
      this.selectForcedSwitch(targetSlot);
      return;
    }

    const battleId = this.battleId();
    if (!battleId || this.attacksDisabled() || this.isWaitingForOpponent()) {
      return;
    }

    this.attacksDisabled.set(true);
    this.isWaitingForOpponent.set(true);
    this.socketService.switchPokemon(battleId, targetSlot);
  }

  selectForcedSwitch(targetSlot: number): void {
    if (!this.requiresSwitchSelection() || this.isSubmittingForcedSwitch()) {
      return;
    }

    if (!this.isAvailableForcedSwitchSlot(targetSlot)) {
      return;
    }

    const battleId = this.battleId();
    if (!battleId) {
      return;
    }

    this.isSubmittingForcedSwitch.set(true);
    this.attacksDisabled.set(true);
    this.isWaitingForOpponent.set(false);
    this.socketService.switchPokemon(battleId, targetSlot);
  }

  isAvailableForcedSwitchSlot(slot: number): boolean {
    if (!this.requiresSwitchSelection()) {
      return false;
    }

    return this.availableSlotsForSwitch().includes(slot);
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

      const playbackSteps = [...(event.replaySteps ?? [])].sort((a, b) => (a.stepIndex ?? 0) - (b.stepIndex ?? 0));

      if (!playbackSteps.length) {
        this.applySnapshotToView(event.battle);
        this.applyBattleEventState(event, false);
        return;
      }

      this.showFinishDialog.set(false);
      this.battleResult.set(null);
      this.attacksDisabled.set(true);
      this.isWaitingForOpponent.set(false);
      this.syncActivePokemonAfterFaint(event.battle);

      try {
        for (let stepIndex = 0; stepIndex < playbackSteps.length; stepIndex++) {
          const step = playbackSteps[stepIndex];
          if (this.isDestroyed) {
            return;
          }

          // Steps futuros (para que switch pueda leer el beforeHp del siguiente hp_change)
          const futureSteps: any[] = playbackSteps.slice(stepIndex + 1);

          const stepMessage = step.message?.trim();
          if (stepMessage) {
            this.combatChatMessages.update((current) => [...current, stepMessage]);
          }

          const stepEvents = Array.isArray(step.events) ? step.events : [];
          for (const stepEvent of stepEvents) {
            await this.processReplayEvent(stepEvent, event.battle, futureSteps);
          }

          const stepDelay = Number(step.delayMs);
          const normalizedStepDelay = Number.isFinite(stepDelay) && stepDelay > 0
            ? stepDelay
            : this.DEFAULT_REPLAY_STEP_DELAY_MS;
          await this.wait(normalizedStepDelay);
        }
      } finally {
        if (!this.isDestroyed) {
          this.applySnapshotToView(event.battle);
          // Bloquear si el turno no se resolvió (acción guardada, esperando rival)
          // o si el rival tiene switch forzoso pendiente
          const waitingAfterPlayback = !(event.turnResolved ?? false) || (event.opponentRequiresSwitch ?? false);
          this.applyBattleEventState(event, waitingAfterPlayback);
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
      this.requiresSwitchSelection.set(false);
      this.availableSlotsForSwitch.set([]);
      this.isSubmittingForcedSwitch.set(false);
      this.actionPanel.set('root');
      return;
    }

    this.showFinishDialog.set(false);
    this.battleResult.set(null);
    const resolvedForcedSwitch = this.resolveForcedSwitchState(event);
    this.requiresSwitchSelection.set(resolvedForcedSwitch.requiresSwitchSelection);
    this.availableSlotsForSwitch.set(resolvedForcedSwitch.availableSlotsForSwitch);

    if (resolvedForcedSwitch.requiresSwitchSelection) {
      this.isWaitingForOpponent.set(false);
      this.attacksDisabled.set(true);
      this.actionPanel.set('root');
      return;
    }

    // Si el rival aún necesita elegir Pokémon, mantener bloqueado aunque el turno se haya resuelto
    const opponentStillSwitching = (event as any).opponentRequiresSwitch ?? false;
    const effectiveWaiting = waitingForOpponent || opponentStillSwitching;
    this.isWaitingForOpponent.set(effectiveWaiting);
    this.attacksDisabled.set(effectiveWaiting);

    this.actionPanel.set('root');
  }

  private resolveForcedSwitchState(event: BattleStateEventPayload): {
    requiresSwitchSelection: boolean;
    availableSlotsForSwitch: number[];
  } {
    const explicitSlots = (event.availableSlotsForSwitch ?? []).filter((slot) => Number.isInteger(slot));
    if (event.requiresSwitchSelection) {
      return {
        requiresSwitchSelection: true,
        availableSlotsForSwitch: explicitSlots.length > 0
          ? explicitSlots
          : this.deriveAvailableSwitchSlotsFromSnapshot(event.battle),
      };
    }

    const activeIsFainted = this.isPlayerActivePokemonFainted(event.battle);
    if (!activeIsFainted) {
      return {
        requiresSwitchSelection: false,
        availableSlotsForSwitch: [],
      };
    }

    return {
      requiresSwitchSelection: true,
      availableSlotsForSwitch: this.deriveAvailableSwitchSlotsFromSnapshot(event.battle),
    };
  }

  private isPlayerActivePokemonFainted(snapshot: any): boolean {
    const team = snapshot?.playerSide?.team;
    const activeSlot = snapshot?.playerSide?.activeSlot ?? 0;
    if (!Array.isArray(team) || team.length === 0) {
      return false;
    }

    const activePokemon = team[activeSlot] ?? team[0];
    if (!activePokemon) {
      return false;
    }

    const currentHp = Number(activePokemon?.currentHp ?? 0);
    return !!activePokemon?.isFainted || currentHp <= 0;
  }

  private deriveAvailableSwitchSlotsFromSnapshot(snapshot: any): number[] {
    const team = snapshot?.playerSide?.team;
    const activeSlot = snapshot?.playerSide?.activeSlot ?? 0;
    if (!Array.isArray(team)) {
      return [];
    }

    return team
      .map((pokemon: any, index: number) => ({ pokemon, index }))
      .filter(({ pokemon, index }) => {
        if (index === activeSlot) {
          return false;
        }

        const currentHp = Number(pokemon?.currentHp ?? 0);
        return !pokemon?.isFainted && currentHp > 0;
      })
      .map(({ index }) => index);
  }

  private applySnapshotToView(snapshot: any): void {
    const mapped = this.mapBattleSnapshotToView(snapshot);
    this.battleInfo.set(mapped);
    this.hpA.set(mapped.pokemonA.currentHp ?? mapped.pokemonA.hp ?? 0);
    this.hpB.set(mapped.pokemonB.currentHp ?? mapped.pokemonB.hp ?? 0);
  }

  private async processReplayEvent(event: any, finalSnapshot: any, futureSteps?: any[]): Promise<void> {
    const eventType = event?.eventType ?? event?.EventType;
    if (eventType === 'hp_change') {
      await this.animateHpChange(event);
      return;
    }

    this.applyTimelineEvent(event, finalSnapshot, futureSteps);

    if (eventType === 'attack' && !this.attackEventDidMiss(event)) {
      await this.wait(this.ATTACK_DASH_DURATION_MS);
    }
  }

  private async animateHpChange(event: any): Promise<void> {
    const target = event?.target ?? event?.Target;
    const side = this.resolveEventSide(target);
    if (!side) {
      return;
    }

    const beforeHp = Number(event?.beforeHp ?? event?.BeforeHp);
    const afterHp = Number(event?.afterHp ?? event?.AfterHp);

    const startHp = Number.isFinite(beforeHp)
      ? beforeHp
      : (side === 'player' ? (this.hpA() ?? 0) : (this.hpB() ?? 0));
    const endHp = Number.isFinite(afterHp) ? afterHp : startHp;

    if (startHp === endHp) {
      this.setSideHp(side, endHp);
      return;
    }

    await new Promise<void>((resolve) => {
      const durationMs = 800;
      const startTime = Date.now();

      const animate = () => {
        if (this.isDestroyed) {
          resolve();
          return;
        }

        const elapsed = Date.now() - startTime;
        const progress = Math.min(elapsed / durationMs, 1);
        const currentHp = Math.round(startHp + (endHp - startHp) * progress);
        this.setSideHp(side, currentHp);

        if (progress < 1) {
          requestAnimationFrame(animate);
          return;
        }

        resolve();
      };

      animate();
    });
  }

  private applyTimelineEvent(event: any, finalSnapshot: any, futureSteps?: any[]): void {
    const eventType = event?.eventType ?? event?.EventType;
    switch (eventType) {
      case 'attack': {
        if (this.attackEventDidMiss(event)) {
          return;
        }

        const attacker = event?.attacker ?? event?.Attacker;
        const attackerSide = this.resolveEventSide(attacker);
        if (attackerSide === 'player' || attackerSide === 'opponent') {
          this.triggerAttackAnimation(attackerSide);
        }
        return;
      }

      case 'message':
        return;

      case 'hp_change':
        {
          const target = event?.target ?? event?.Target;
          const afterHp = event?.afterHp ?? event?.AfterHp;
          const resolvedSide = this.resolveEventSide(target);
          if ((resolvedSide === 'player' || resolvedSide === 'opponent') && Number.isFinite(Number(afterHp))) {
            this.setSideHp(resolvedSide, Number(afterHp));
          }
        }
        return;

      case 'status_change':
        {
          const target = event?.target ?? event?.Target;
          const resolvedSide = this.resolveEventSide(target);
          if (resolvedSide === 'player' || resolvedSide === 'opponent') {
            this.setSideStatus(resolvedSide, event?.afterStatus ?? event?.AfterStatus ?? 'None');
          }
        }
        return;

      case 'secondary_status_change':
        return;

      case 'faint':
        {
          const target = event?.target ?? event?.Target;
          const resolvedSide = this.resolveEventSide(target);
          if (resolvedSide === 'player' || resolvedSide === 'opponent') {
            this.setSideHp(resolvedSide, 0);
          }
        }
        return;

      case 'stat_stage_change':
        {
          const target = event?.target ?? event?.Target;
          const resolvedSide = this.resolveEventSide(target);
          if (resolvedSide === 'player' || resolvedSide === 'opponent') {
            this.showStatStageIndicator(resolvedSide, event);
          }
        }
        return;

      case 'battle_end': {
        const winnerUserId = event?.winnerUserId ?? event?.WinnerUserId ?? null;
        if (winnerUserId !== null && winnerUserId !== undefined) {
          this.battleResult.set(this.resolveBattleResult(Number(winnerUserId)));
          this.showFinishDialog.set(true);
        }
        return;
      }

      case 'switch':
        if (this.resolveEventSide(event) === 'player' || this.resolveEventSide(event) === 'opponent') {
          this.applySwitchFromTimeline(event, finalSnapshot, futureSteps);
        }
        return;

      default:
        return;
    }
  }

  private resolveEventSide(entity: any): BattleSideKey | null {
    const rawSide = entity?.side ?? entity?.Side;
    if (rawSide === 'player' || rawSide === 'opponent') {
      return rawSide;
    }

    return null;
  }

  private attackEventDidMiss(event: any): boolean {
    return event?.hit === false;
  }

  private isWaitingOpponentMessage(message: string): boolean {
    const normalized = this.normalizeName(message).replace(/[^a-z0-9\s]/g, ' ').replace(/\s+/g, ' ').trim();
    return normalized.includes(this.WAITING_OPPONENT_MESSAGE);
  }

  private triggerAttackAnimation(side: BattleSideKey): void {
    const animationSignal = side === 'player'
      ? this.playerAttackAnimating
      : this.opponentAttackAnimating;

    animationSignal.set(false);

    const restartTimeout = setTimeout(() => {
      if (this.isDestroyed) {
        this.playbackTimeouts.delete(restartTimeout);
        return;
      }

      animationSignal.set(true);

      const stopTimeout = setTimeout(() => {
        animationSignal.set(false);
        this.playbackTimeouts.delete(stopTimeout);
      }, this.ATTACK_DASH_DURATION_MS);

      this.playbackTimeouts.add(stopTimeout);
      this.playbackTimeouts.delete(restartTimeout);
    }, 0);

    this.playbackTimeouts.add(restartTimeout);
  }

  private showStatStageIndicator(side: BattleSideKey, event: any): void {
    const stat = this.formatStatLabel(event?.stat ?? event?.Stat ?? 'stat');
    const change = Number(event?.change ?? event?.Change ?? event?.stages ?? event?.Stages ?? event?.delta ?? event?.Delta ?? 0);
    const direction = change >= 0 ? '↑' : '↓';
    const amount = Math.max(1, Math.abs(Math.trunc(change || 1)));
    const newStage = Number(event?.newStage ?? event?.NewStage ?? NaN);
    const stageSuffix = Number.isFinite(newStage) ? ` (${newStage > 0 ? '+' : ''}${Math.trunc(newStage)})` : '';
    const label = `${direction}${amount} ${stat}${stageSuffix}`;

    const signalRef = side === 'player' ? this.playerStatStageIndicator : this.opponentStatStageIndicator;
    signalRef.set(label);

    const clearTimeoutId = setTimeout(() => {
      signalRef.set(null);
      this.playbackTimeouts.delete(clearTimeoutId);
    }, 900);

    this.playbackTimeouts.add(clearTimeoutId);
  }

  private formatStatLabel(rawStat: string): string {
    const key = this.normalizeName(String(rawStat));
    const labels: Record<string, string> = {
      attack: 'Ataque',
      defense: 'Defensa',
      specialattack: 'Ataque Esp.',
      specialdefense: 'Defensa Esp.',
      speed: 'Velocidad',
      accuracy: 'Precisión',
      evasion: 'Evasión',
      crit: 'Crítico',
      critical: 'Crítico',
    };

    return labels[key] ?? this.capitalize(String(rawStat));
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

  private applySwitchFromTimeline(event: any, finalSnapshot: any, remainingSteps?: any[]): void {
    const newPokemonName = event?.newPokemonName ?? event?.NewPokemonName ?? '';
    const newActiveSlot = event?.newActiveSlot ?? event?.NewActiveSlot;
    const side = this.resolveEventSide(event);
    if (!side) {
      return;
    }

    const switchedPokemon = this.getSnapshotPokemonBySlot(finalSnapshot, side, newActiveSlot, newPokemonName);
    if (!switchedPokemon) {
      return;
    }

    const nextPokemonView = this.mapSnapshotPokemonToView(switchedPokemon, side === 'player');

    // El snapshot tiene el HP final (después de recibir daño en este turno).
    // Si hay un evento hp_change posterior para este lado, usamos su beforeHp
    // para que la barra arranque desde el HP correcto antes de la animación de daño.
    let initialHp = nextPokemonView.currentHp ?? 0;
    if (remainingSteps && remainingSteps.length > 0) {
      for (const futureStep of remainingSteps) {
        const futureEvents = Array.isArray(futureStep.events) ? futureStep.events : [];
        for (const futureEvent of futureEvents) {
          if ((futureEvent?.eventType ?? futureEvent?.EventType) === 'hp_change') {
            const targetSide = this.resolveEventSide(futureEvent?.target ?? futureEvent?.Target);
            if (targetSide === side) {
              const beforeHp = Number(futureEvent?.beforeHp ?? futureEvent?.BeforeHp);
              if (Number.isFinite(beforeHp) && beforeHp > 0) {
                initialHp = beforeHp;
              }
              break;
            }
          }
        }
        if (initialHp !== (nextPokemonView.currentHp ?? 0)) break;
      }
    }

    const current = this.battleInfo();
    if (!current) {
      return;
    }

    if (side === 'player') {
      this.battleInfo.set({ ...current, pokemonA: { ...nextPokemonView, currentHp: initialHp } });
      this.hpA.set(initialHp);
      return;
    }

    this.battleInfo.set({ ...current, pokemonB: { ...nextPokemonView, currentHp: initialHp } });
    this.hpB.set(initialHp);
  }

  private syncActivePokemonAfterFaint(snapshot: any): void {
    const current = this.battleInfo();
    if (!current) {
      return;
    }

    const shouldReplacePlayer = (this.hpA() ?? 0) <= 0;
    const shouldReplaceOpponent = (this.hpB() ?? 0) <= 0;

    if (!shouldReplacePlayer && !shouldReplaceOpponent) {
      return;
    }

    if (shouldReplacePlayer) {
      const playerActive = this.getActiveSnapshotPokemon(snapshot, 'player');
      const playerActiveHp = Number(playerActive?.currentHp ?? 0);
      if (playerActive && playerActiveHp > 0) {
        const nextPlayer = this.mapSnapshotPokemonToView(playerActive, true);
        this.battleInfo.set({
          ...this.battleInfo()!,
          pokemonA: nextPlayer,
        });
        this.hpA.set(nextPlayer.currentHp ?? 0);
      }
    }

    if (shouldReplaceOpponent) {
      const opponentActive = this.getActiveSnapshotPokemon(snapshot, 'opponent');
      const opponentActiveHp = Number(opponentActive?.currentHp ?? 0);
      if (opponentActive && opponentActiveHp > 0) {
        const nextOpponent = this.mapSnapshotPokemonToView(opponentActive, false);
        this.battleInfo.set({
          ...this.battleInfo()!,
          pokemonB: nextOpponent,
        });
        this.hpB.set(nextOpponent.currentHp ?? 0);
      }
    }
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

  private getSnapshotPokemonBySlot(snapshot: any, side: BattleSideKey, slot: number, expectedName?: string): any | null {
    const sideKey = side === 'player' ? 'playerSide' : 'opponentSide';
    const team = snapshot?.[sideKey]?.team;
    if (!Array.isArray(team)) {
      return null;
    }

    const normalizedExpectedName = this.normalizeName(expectedName ?? '');
    const normalizedSlot = Number(slot);
    const isValidIndex = Number.isInteger(normalizedSlot) && normalizedSlot >= 0 && normalizedSlot < team.length;

    const byIndex = isValidIndex ? (team[normalizedSlot] ?? null) : null;
    const byOneBasedSlot = Number.isInteger(normalizedSlot)
      ? (team.find((pokemon: any) => Number(pokemon?.slot) === normalizedSlot + 1) ?? null)
      : null;
    const byExactSlot = Number.isInteger(normalizedSlot)
      ? (team.find((pokemon: any) => Number(pokemon?.slot) === normalizedSlot) ?? null)
      : null;

    const candidates = [byIndex, byOneBasedSlot, byExactSlot].filter(Boolean);
    if (normalizedExpectedName) {
      const byName = candidates.find((pokemon: any) => {
        const pokemonName = pokemon?.nickname || pokemon?.name || '';
        return this.normalizeName(pokemonName) === normalizedExpectedName;
      });
      if (byName) {
        return byName;
      }
    }

    return candidates[0] ?? null;
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

  private capitalize(value: string): string {
    if (!value) {
      return '';
    }

    return value.charAt(0).toUpperCase() + value.slice(1);
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