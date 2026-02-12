import { Injectable } from '@angular/core';
import { PokemonApi } from '../models/pokemon-api';
import { Move } from '../models/move';
import { GetTurnOrder } from '../models/get-turn-order';

@Injectable({
  providedIn: 'root',
})
export class TurnOrder {
  getTurnOrder(
    pokemonA: PokemonApi,
    pokemonB: PokemonApi,
    moveA: Move,
    moveB: Move,
    hpA: number,
    hpB: number
  ): GetTurnOrder {
    if (pokemonA.spe > pokemonB.spe) {
      return {
        firstAttacker: pokemonA,
        secondAttacker: pokemonB,
        firstMovement: moveA,
        secondMovement: moveB,
        firstHp: hpA,
        secondHp: hpB,
        firstIsA: true
      };
    } else if (pokemonB.spe > pokemonA.spe) {
      return {
        firstAttacker: pokemonB,
        secondAttacker: pokemonA,
        firstMovement: moveB,
        secondMovement: moveA,
        firstHp: hpB,
        secondHp: hpA,
        firstIsA: false
      };
    } else {
      // Si la velocidad es igual, elegir al azar
      if (Math.random() < 0.5) {
        return {
          firstAttacker: pokemonA,
          secondAttacker: pokemonB,
          firstMovement: moveA,
          secondMovement: moveB,
          firstHp: hpA,
          secondHp: hpB,
          firstIsA: true
        };
      } else {
        return {
          firstAttacker: pokemonB,
          secondAttacker: pokemonA,
          firstMovement: moveB,
          secondMovement: moveA,
          firstHp: hpB,
          secondHp: hpA,
          firstIsA: false
        };
      }
    }
  }
}
