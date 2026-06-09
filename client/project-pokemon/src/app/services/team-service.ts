import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { GetTeamDto, PostTeamDto } from '../models/team';

@Injectable({
  providedIn: 'root',
})
export class TeamService {
  private readonly api = inject(ApiService);

  async getAllTeams(): Promise<GetTeamDto[]> {
    return this.api.get<GetTeamDto[]>('team');
  }

  async addTeam(dto: PostTeamDto): Promise<PostTeamDto> {
    return this.api.post<PostTeamDto>('team', dto);
  }

  async deleteTeam(teamId: number): Promise<void> {
    await this.api.delete<void>(`team/${teamId}`);
  }

  async renameTeam(teamId: number, name: string): Promise<void> {
    await this.api.put<void>(`team/${teamId}`, { name });
  }
}