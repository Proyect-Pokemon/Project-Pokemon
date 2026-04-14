import { Injectable } from '@angular/core';
import { BaseApiService } from './base-api.service';
import { GetTeamDto, PostTeamDto } from '../models/team';

@Injectable({
  providedIn: 'root',
})
export class TeamService extends BaseApiService {
  async getAllTeams(): Promise<GetTeamDto[]> {
    return this.getList<GetTeamDto>('team');
  }

  async addTeam(dto: PostTeamDto): Promise<boolean> {
    return this.create<PostTeamDto>('team', dto);
  }

  async deleteTeam(teamId: number): Promise<boolean> {
    return this.delete(`team/${teamId}`);
  }

  async renameTeam(teamId: number, name: string): Promise<boolean> {
    return this.update(`team/${teamId}`, { name });
  }
}
