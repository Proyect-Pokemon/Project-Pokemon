import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TeamService } from '../../services/team-service';
import { GetTeamDto } from '../../models/team';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-battle-select',
  imports: [CommonModule],
  templateUrl: './battle-select.html',
  styleUrl: './battle-select.css',
})
export class BattleSelect {
  teams = signal<GetTeamDto[]>([]);
  selectedTeamId = signal<number | null>(null);
  isLoadingTeams = signal(true);

  private teamService = inject(TeamService);
  private authService = inject(AuthService);
  private router = inject(Router);

  async ngOnInit(): Promise<void> {
    await this.loadUserTeams();
  }

  selectTeam(teamId: number): void {
    this.selectedTeamId.set(teamId);
  }

  startBattle(): void {
    const teamId = this.selectedTeamId();
    if (!teamId) return;

    void this.router.navigate(['/battle/fight'], {
      queryParams: { teamId },
    });
  }

  private async loadUserTeams(): Promise<void> {
    this.isLoadingTeams.set(true);

    const userId = this.authService.currentUserId();
    const allTeams = await this.teamService.getAllTeams();
    const teams = userId ? allTeams.filter((team) => team.userId === userId) : [];

    this.teams.set(teams);
    this.selectedTeamId.set(teams[0]?.id ?? null);
    this.isLoadingTeams.set(false);
  }
}
