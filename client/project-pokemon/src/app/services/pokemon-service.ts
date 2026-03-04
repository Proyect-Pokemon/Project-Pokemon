import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { Pokemon } from '../models/pokemon';

@Injectable({
  providedIn: 'root',
})
export class PokemonService extends BaseApiService {
  async getAllPokemon(): Promise<Pokemon[]> {
    return this.getList<Pokemon>('pokemon');
  }
}
