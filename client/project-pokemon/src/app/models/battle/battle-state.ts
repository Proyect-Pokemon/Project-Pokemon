import { BattleMove } from '../move';

export type BattleSideId = 'PLAYER' | 'OPPONENT';

export type BattleStatus = 'IN_PROGRESS' | 'WAITING_FOR_SWITCH' | 'FINISHED';

export interface BattleMoveState extends BattleMove {
  maxPp: number;
  disabled?: boolean;
}

export interface BattlePokemonState {
  battlePokemonId: number;
  pokemonId: number;
  slot: number;
  displayName: string;
  spriteFront: string;
  spriteBack: string;
  currentHp: number;
  maxHp: number;
  fainted: boolean;
  statusCondition: string | null;
  moves: BattleMoveState[];
}

export interface BattleSideState {
  trainerId: number;
  trainerName: string;
  activeSlot: number;
  team: BattlePokemonState[];
}

export interface BattleState {
  id: number;
  turnNumber: number;
  status: BattleStatus;
  winnerSide: BattleSideId | null;
  playerSide: BattleSideState;
  opponentSide: BattleSideState;
}
