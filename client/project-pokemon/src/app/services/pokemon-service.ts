import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { Pokemon } from '../models/pokemon';

@Injectable({
  providedIn: 'root',
})
export class PokemonService {
  private api = inject(ApiService);

  async getAllPokemon(): Promise<Pokemon[]> {
    const result = await this.api.get<Pokemon[]>('pokemon');
    return result.success && result.data ? result.data : [];
  }
}
