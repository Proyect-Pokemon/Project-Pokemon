import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { GetAllPokemonTeamDto, PostPokemonTeamDto } from '../models/pokemon-team';

@Injectable({
  providedIn: 'root',
})
export class PokemonTeamService extends BaseApiService {
  async getAllPokemonTeams(): Promise<GetAllPokemonTeamDto[]> {
    return this.getList<GetAllPokemonTeamDto>('pokemonteam');
  }

  async addPokemonTeam(dto: PostPokemonTeamDto): Promise<boolean> {
    return this.create<PostPokemonTeamDto>('pokemonteam', dto);
  }
}
