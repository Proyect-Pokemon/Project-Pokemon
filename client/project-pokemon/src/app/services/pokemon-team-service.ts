import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { GetAllPokemonTeamDto, PostPokemonTeamDto } from '../models/pokemon-team';
import { PutPokemonTeamNicknameDto } from '../models/put-pokemon-team-nickname-dto';
import { PutPokemonTeamDto } from '../models/put-pokemon-team-dto';

@Injectable({
  providedIn: 'root',
})
export class PokemonTeamService {
  private readonly api = inject(ApiService);

  async getAllPokemonTeams(): Promise<GetAllPokemonTeamDto[]> {
    return this.api.get<GetAllPokemonTeamDto[]>('pokemonteam');
  }

  async deletePokemonTeam(pokemonTeamId: number): Promise<void> {
    await this.api.delete<void>(`pokemonteam/${pokemonTeamId}`);
  }

  async addPokemonTeam(dto: PostPokemonTeamDto): Promise<PostPokemonTeamDto> {
    return this.api.post<PostPokemonTeamDto>('pokemonteam', {
      ...dto,
      sex: dto.sex ?? 'M',
    });
  }

  async updateNickname(
    pokemonTeamId: number,
    dto: PutPokemonTeamNicknameDto
  ): Promise<void> {
    await this.api.put<void>(
      `pokemonteam/${pokemonTeamId}Nickname`,
      dto
    );
  }

  async updatePokemonTeam(
    pokemonTeamId: number,
    dto: PutPokemonTeamDto
  ): Promise<void> {
    await this.api.put<void>(
      `pokemonteam/${pokemonTeamId}All`,
      dto
    );
  }
}