import { Injectable } from '@angular/core';
import { PokemonApi } from '../models/pokemon-api';
import { BattleMove } from '../models/move';
import { GetTurnOrder } from '../models/get-turn-order';

@Injectable({
  providedIn: 'root',
})
export class TurnOrder {
  getTurnOrder(
    pokemonA: PokemonApi,
    pokemonB: PokemonApi,
    moveA: BattleMove,
    moveB: BattleMove,
    hpA: number,
    hpB: number
  ): GetTurnOrder {
    const isAPrimero = pokemonA.spe > pokemonB.spe || (pokemonA.spe === pokemonB.spe && Math.random() < 0.5);
    return isAPrimero
      ? this.createTurnOrder(pokemonA, pokemonB, moveA, moveB, hpA, hpB, true)
      : this.createTurnOrder(pokemonB, pokemonA, moveB, moveA, hpB, hpA, false);
  }

  private createTurnOrder(
    firstAttacker: PokemonApi,
    secondAttacker: PokemonApi,
    firstMovement: BattleMove,
    secondMovement: BattleMove,
    firstHp: number,
    secondHp: number,
    firstIsA: boolean
  ): GetTurnOrder {
    return {
      firstAttacker,
      secondAttacker,
      firstMovement,
      secondMovement,
      firstHp,
      secondHp,
      firstIsA
    };
  }
}
