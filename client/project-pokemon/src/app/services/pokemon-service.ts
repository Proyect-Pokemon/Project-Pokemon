import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { Pokemon } from '../models/pokemon';

@Injectable({
  providedIn: 'root',
})
export class PokemonService {
  private readonly api = inject(ApiService);

  async getAllPokemon(): Promise<Pokemon[]> {
    return this.api.get<Pokemon[]>('pokemon');
  }
}