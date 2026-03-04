import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { GetAllPokemonTeamDto, PostPokemonTeamDto } from '../models/pokemon-team';

@Injectable({
  providedIn: 'root',
})
export class PokemonTeamService {
  private apiService = inject(ApiService);

  async getAllPokemonTeams(): Promise<GetAllPokemonTeamDto[]> {
    const result = await this.apiService.get<GetAllPokemonTeamDto[]>('pokemonteam');

    if (result.success && result.data !== undefined) {
      return result.data;
    }

    return [];
  }

  async addPokemonTeam(dto: PostPokemonTeamDto): Promise<boolean> {
    const result = await this.apiService.post<PostPokemonTeamDto>('pokemonteam', dto);
    return result.success;
  }
}
