import { PokemonApi } from './pokemon-api';
import { Move } from './move';

export interface GetTurnOrder {
	firstAttacker: PokemonApi;
	secondAttacker: PokemonApi;
	firstMovement: Move;
	secondMovement: Move;
	firstHp: number;
	secondHp: number;
	firstIsA: boolean;
}
