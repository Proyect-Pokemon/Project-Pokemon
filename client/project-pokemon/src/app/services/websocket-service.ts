import { Injectable, signal } from '@angular/core';

export interface BattleMatchedEvent {
  battleId: string;
  opponentUsername: string;
  opponentUserId: number;
}

export interface BattleStateEvent {
  action: number;
  battle: any;
  messages: string[];
  requiresSwitch: boolean;
  winnerUserId: number | null;
}

interface ChatMessage {
  battleId: string;
  from: string;
  content: string;
}

@Injectable({
  providedIn: 'root',
})
export class SocketService {
  private socket: WebSocket | null = null;
  private jwt?: string;
  private shouldReconnect = true;
  private reconnectAttempts = 0;
  private readonly maxReconnectDelay = 30000;

  readonly isConnected = signal(false);
  readonly matchmakingState = signal<'idle' | 'searching' | 'matched'>('idle');
  readonly matchmakingMessage = signal('');
  readonly activeBattleId = signal<string | null>(null);
  readonly onBattleMatched = signal<BattleMatchedEvent | null>(null);
  readonly onBattleState = signal<BattleStateEvent | null>(null);
  readonly onChatMessage = signal<ChatMessage | null>(null);

  constructor() {
    window.addEventListener('online', () => {
      if (this.shouldReconnect && this.jwt && !this.socket) {
        this.connect(this.jwt);
      }
    });
  }

  private getReconnectDelay(): number {
    const baseDelay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), this.maxReconnectDelay);
    const jitter = Math.random() * 1000;
    return baseDelay + jitter;
  }

  private attemptReconnect(): void {
    if (!this.shouldReconnect || !this.jwt || localStorage.getItem('jwt') !== this.jwt) {
      return;
    }

    const delay = this.getReconnectDelay();
    setTimeout(() => {
      this.reconnectAttempts++;
      this.connect(this.jwt!);
    }, delay);
  }

  private sendMessage(message: object): void {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
      return;
    }

    this.socket.send(JSON.stringify(message));
  }

  private joinLobby(): void {
    this.sendMessage({
      type: 4,
      action: 1,
    });
  }

  private handleLobbyMessage(message: any): void {
    if (message.action === 5) {
      if (typeof message.battleId === 'string') {
        console.info('[WS] Rival encontrado', {
          battleId: message.battleId,
          opponentUsername: message.opponentUsername,
        });
        this.matchmakingState.set('matched');
        this.matchmakingMessage.set('Rival encontrado');
        this.activeBattleId.set(message.battleId);
        this.onBattleMatched.set({
          battleId: message.battleId,
          opponentUsername: message.opponentUsername ?? 'Rival',
          opponentUserId: message.opponentUserId ?? 0,
        });
      } else {
        console.info('[WS] Matchmaking', message.message ?? 'Buscando rival...');
        this.matchmakingState.set('searching');
        this.matchmakingMessage.set(message.message ?? 'Buscando rival...');
      }
      return;
    }

    if (message.action === 6) {
      console.info('[WS] Búsqueda cancelada');
      this.matchmakingState.set('idle');
      this.matchmakingMessage.set('Búsqueda cancelada');
    }
  }

  private handleBattleMessage(message: any): void {
    console.info('[WS][Battle] Update', {
      action: message.action,
      battleId: message.battle?.battleId,
      requiresSwitch: message.requiresSwitch ?? false,
      winnerUserId: message.winnerUserId ?? null,
      messages: message.messages ?? [],
    });

    this.onBattleState.set({
      action: message.action ?? 0,
      battle: message.battle,
      messages: message.messages ?? [],
      requiresSwitch: message.requiresSwitch ?? false,
      winnerUserId: message.winnerUserId ?? null,
    });
  }

  private handleChatMessage(message: any): void {
    this.onChatMessage.set({
      battleId: message.battleId,
      from: message.senderName ?? 'Jugador',
      content: message.content ?? '',
    });
  }

  connect(jwt: string): void {
    this.jwt = jwt;
    this.shouldReconnect = true;

    if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
      return;
    }

    const wsUrl = `wss://${location.hostname}:7277/ws?access_token=${encodeURIComponent(jwt)}`;
    console.info('[WS] Intentando conectar', wsUrl);
    this.socket = new WebSocket(wsUrl);

    this.socket.onopen = () => {
      console.info('[WS] Conexión establecida');
      this.isConnected.set(true);
      this.reconnectAttempts = 0;
      this.joinLobby();
    };

    this.socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);

        switch (message.type) {
          case 4:
            this.handleLobbyMessage(message);
            break;
          case 1:
            this.handleBattleMessage(message);
            break;
          case 2:
            this.handleChatMessage(message);
            break;
        }
      } catch (err) {
        console.error('Error parsing WS message', err);
      }
    };

    this.socket.onclose = () => {
      console.info('[WS] Conexión cerrada');
      this.isConnected.set(false);
      this.socket = null;
      this.attemptReconnect();
    };

    this.socket.onerror = (event) => {
      console.error('[WS] Error de conexión', event);
      this.isConnected.set(false);
    };
  }

  searchBattle(teamId: number): void {
    this.matchmakingState.set('searching');
    this.matchmakingMessage.set('Buscando rival...');

    this.sendMessage({
      type: 4,
      action: 5,
      teamId,
    });
  }

  cancelSearch(): void {
    this.sendMessage({
      type: 4,
      action: 6,
    });
  }

  setActiveBattle(battleId: string | null): void {
    this.activeBattleId.set(battleId);
  }

  attack(battleId: string, moveName: string): void {
    console.info('[WS][Battle] Send Attack', { battleId, moveName });

    this.sendMessage({
      type: 1,
      action: 1,
      battleId,
      moveName,
    });
  }

  switchPokemon(battleId: string, targetSlot: number): void {
    console.info('[WS][Battle] Send Switch', { battleId, targetSlot });

    this.sendMessage({
      type: 1,
      action: 2,
      battleId,
      targetSlot,
    });
  }

  forfeit(battleId: string): void {
    this.sendMessage({
      type: 1,
      action: 3,
      battleId,
    });
  }

  sendChatMessage(content: string, battleId?: string): void {
    const resolvedBattleId = battleId ?? this.activeBattleId();
    if (!resolvedBattleId) {
      return;
    }

    this.sendMessage({
      type: 2,
      battleId: resolvedBattleId,
      content,
    });
  }

  disconnect(): void {
    this.shouldReconnect = false;
    this.isConnected.set(false);
    this.matchmakingState.set('idle');
    this.matchmakingMessage.set('');
    this.activeBattleId.set(null);
    this.onBattleMatched.set(null);
    this.onBattleState.set(null);
    this.onChatMessage.set(null);

    if (this.socket && this.socket.readyState === WebSocket.OPEN) {
      this.socket.close();
    }

    this.socket = null;
  }
}