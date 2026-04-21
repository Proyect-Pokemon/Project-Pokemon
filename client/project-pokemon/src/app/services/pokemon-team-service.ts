import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { GetAllPokemonTeamDto, PostPokemonTeamDto } from '../models/pokemon-team';
import { PutPokemonTeamNicknameDto } from '../models/put-pokemon-team-nickname-dto';
import { PutPokemonTeamDto } from '../models/put-pokemon-team-dto';

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

  async updateNickname(pokemonTeamId: number, dto: PutPokemonTeamNicknameDto): Promise<boolean> {
    return this.update<PutPokemonTeamNicknameDto>(`pokemonteam/${pokemonTeamId}Nickname`, dto);
  }

  async updatePokemonTeam(pokemonTeamId: number, dto: PutPokemonTeamDto): Promise<boolean> {
    return this.update<PutPokemonTeamDto>(`pokemonteam/${pokemonTeamId}All`, dto);
  }
}
