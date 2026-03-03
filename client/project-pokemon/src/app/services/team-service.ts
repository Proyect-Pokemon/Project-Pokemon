import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';
import { GetTeamDto, PostTeamDto } from '../models/team';

@Injectable({
  providedIn: 'root',
})
export class TeamService {
  private apiService = inject(ApiService);

  async getAllTeams(): Promise<GetTeamDto[]> {
    const result = await this.apiService.get<GetTeamDto[]>('team');

    if (result.success && result.data !== undefined) {
      return result.data;
    }

    return [];
  }

  async addTeam(dto: PostTeamDto): Promise<boolean> {
    const result = await this.apiService.post<PostTeamDto>('team', dto);
    return result.success;
  }
}
