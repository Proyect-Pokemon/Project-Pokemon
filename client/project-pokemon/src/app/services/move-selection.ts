import { Injectable } from '@angular/core';
import { PokemonApi } from '../models/pokemon-api';
import { BattleMove } from '../models/move';

@Injectable({
  providedIn: 'root',
})

// Este servicio se encarga de seleccionar un movimiento aleatorio para el oponente
// Se inyecta en el BattleService para ser utilizado durante la simulación del combate

export class MoveSelection {
  getRandomOpponentMove(pokemon: PokemonApi): BattleMove {
    if (!pokemon.moves || pokemon.moves.length === 0) {
      throw new Error('El Pokémon no tiene movimientos disponibles');
    }
    const idx = Math.floor(Math.random() * pokemon.moves.length);
    return pokemon.moves[idx];
  }
}
