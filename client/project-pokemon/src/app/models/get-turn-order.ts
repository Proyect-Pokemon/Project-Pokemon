import { PokemonApi } from './pokemon-api';
import { BattleMove } from './move';

export interface GetTurnOrder {
	firstAttacker: PokemonApi;
	secondAttacker: PokemonApi;
	firstMovement: BattleMove;
	secondMovement: BattleMove;
	firstHp: number;
	secondHp: number;
	firstIsA: boolean;
}
