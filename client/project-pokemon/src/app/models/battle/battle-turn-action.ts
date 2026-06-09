import { BattleSideId, BattleState } from './battle-state';

export interface PlayTurnRequest {
  moveName: string;
}

export interface SwitchPokemonRequest {
  targetSlot: number;
}

export interface BattleActionResponse {
  battle: BattleState;
  messages: string[];
  requiresSwitch?: boolean;
  winnerSide?: BattleSideId | null;
}
