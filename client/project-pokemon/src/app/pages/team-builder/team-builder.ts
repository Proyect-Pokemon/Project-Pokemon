import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Team, GetTeamDto } from '../../models/team';
import { TeamSlot } from '../../components/team-slot/team-slot';
import { PokemonEditorPanel } from '../../components/pokemon-editor-panel/pokemon-editor-panel';
import { TeamService } from '../../services/team-service';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-team-builder',
  standalone: true,
  imports: [CommonModule, TeamSlot, PokemonEditorPanel],
  templateUrl: './team-builder.html',
  styleUrl: './team-builder.css',
})
export class TeamBuilder {
  teams: Team[] = [];
  readonly MAX_TEAMS = 5;
  isLoadingTeams = false;
  isCreatingTeam = false;
  currentUserId: number | null = null;

  // Estado del panel editor
  isPanelOpen = false;
  selectedTeamId = 0;
  selectedSlot = 1;

  constructor(
    private teamService: TeamService,
    private authService: AuthService
  ) {}

  async ngOnInit() {
    this.currentUserId = this.authService.getUserIdFromJwt();
    await this.loadUserTeams();
  }

  async addTeam() {
    if (!this.currentUserId || this.teams.length >= this.MAX_TEAMS || this.isCreatingTeam) {
      return;
    }

    this.isCreatingTeam = true;

    const created = await this.teamService.addTeam({
      name: `Equipo ${this.teams.length + 1}`,
      description: null,
      userId: this.currentUserId,
    });

    if (created) {
      await this.loadUserTeams();
    }

    this.isCreatingTeam = false;
  }

  toggleTeamExpansion(teamId: number) {
    const team = this.teams.find(t => t.id === teamId);
    if (team) {
      team.isExpanded = !team.isExpanded;
    }
  }

  updateTeamName(data: { id: number, name: string }) {
    const team = this.teams.find(t => t.id === data.id);
    if (team) {
      team.name = data.name;
    }
  }

  handleAddPokemon(data: { teamId: number, slot: number }) {
    this.selectedTeamId = data.teamId;
    this.selectedSlot = data.slot;
    this.isPanelOpen = true;
  }

  closePanel() {
    this.isPanelOpen = false;
  }

  canAddMoreTeams(): boolean {
    return this.teams.length < this.MAX_TEAMS;
  }

  private async loadUserTeams() {
    if (!this.currentUserId) {
      this.teams = [];
      return;
    }

    this.isLoadingTeams = true;

    const allTeams = await this.teamService.getAllTeams();
    const userTeams = allTeams
      .filter(team => team.userId === this.currentUserId)
      .slice(0, this.MAX_TEAMS);

    const expandedById = new Map(this.teams.map(team => [team.id, team.isExpanded]));

    this.teams = userTeams.map((team: GetTeamDto) => ({
      id: team.id,
      name: team.name,
      description: team.description,
      userId: team.userId,
      pokemons: [],
      isExpanded: expandedById.get(team.id) ?? false,
    }));

    this.isLoadingTeams = false;
  }
}
