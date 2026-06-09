import { Component, inject, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Team, GetTeamDto } from '../../models/team';
import { TeamService } from '../../services/team-service';
import { AuthService } from '../../services/auth';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';
import { GetAllPokemonTeamDto, PokemonTeam } from '../../models/pokemon-team';

@Component({
  selector: 'app-team-builder',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './team-builder.html',
  styleUrls: ['./team-builder.css'],
})
export class TeamBuilder {
  private teamService = inject(TeamService);
  private authService = inject(AuthService);
  private pokemonTeamService = inject(PokemonTeamService);
  private pokemonService = inject(PokemonService);
  private router = inject(Router);

  teams = signal<Team[]>([]);
  readonly MAX_TEAMS = 5;
  readonly MAX_POKEMON_PER_TEAM = 6;
  isLoadingTeams = signal(false);
  isCreatingTeam = false;
  editingTeamId: number | null = null;
  pendingDeleteTeam = signal<Team | null>(null);

  constructor() {
    effect(() => {
      const userId = this.authService.currentUserId();
      if (userId !== null) {
        this.loadUserTeams();
      }
    });
  }

  async addTeam() {
    const currentUserId = this.authService.currentUserId();
    if (!currentUserId || this.teams().length >= this.MAX_TEAMS || this.isCreatingTeam) {
      return;
    }

    this.isCreatingTeam = true;

  try {
    await this.teamService.addTeam({
      name: `Equipo ${this.teams().length + 1}`,
      description: null,
      userId: currentUserId,
    });

    await this.loadUserTeams();

  } catch (error) {
    console.error('Error creando equipo', error);

  } finally {
    this.isCreatingTeam = false;
  }

    this.isCreatingTeam = false;
  }

  promptDeleteTeam(teamId: number) {
    const team = this.teams().find(t => t.id === teamId) ?? null;
    this.pendingDeleteTeam.set(team);
  }

  cancelDeleteTeam() {
    this.pendingDeleteTeam.set(null);
  }

  async confirmDeleteTeam() {
    const team = this.pendingDeleteTeam();
    if (!team) {
      return;
    }

    const teamId = team.id;
    try {
      await this.teamService.deleteTeam(teamId);

      this.teams.update(teams =>
        teams.filter(t => t.id !== teamId)
      );

    } catch (error) {
      console.error('Error eliminando equipo', error);

    } finally {
      this.pendingDeleteTeam.set(null);
    }

    this.pendingDeleteTeam.set(null);
  }

  editTeam(teamId: number) {
    this.router.navigate(['/team-builder', teamId]);
  }

  startEditingName(teamId: number) {
    this.editingTeamId = teamId;
  }

  async saveTeamName(event: Event, teamId: number) {
    const input = event.target as HTMLInputElement;
    const newName = input.value.trim();
    if (newName) {
      try {
        await this.teamService.renameTeam(teamId, newName);

        this.teams.update(teams =>
          teams.map(t =>
            t.id === teamId ? { ...t, name: newName } : t
          )
        );

      } catch (error) {
        console.error('Error renombrando equipo', error);

      } finally {
        this.editingTeamId = null;
      }
    }
    this.editingTeamId = null;
  }

  getPokemonSlots(team: Team): (PokemonTeam | null)[] {
    const slots: (PokemonTeam | null)[] = Array(this.MAX_POKEMON_PER_TEAM).fill(null);
    team.pokemons.forEach(pokemon => {
      if (pokemon.slot >= 1 && pokemon.slot <= this.MAX_POKEMON_PER_TEAM) {
        slots[pokemon.slot - 1] = pokemon;
      }
    });
    return slots;
  }

  canAddMoreTeams(): boolean {
    return this.authService.currentUserId() !== null && this.teams().length < this.MAX_TEAMS;
  }

  private async loadUserTeams() {
    const currentUserId = this.authService.currentUserId();
    if (!currentUserId) {
      this.teams.set([]);
      return;
    }

    this.isLoadingTeams.set(true);

    const allTeams = await this.teamService.getAllTeams();
    const allPokemonTeams = await this.pokemonTeamService.getAllPokemonTeams();
    const allPokemons = await this.pokemonService.getAllPokemon();

    const pokemonMap = new Map(allPokemons.map(p => [p.id, p]));

    const userTeams = allTeams
      .filter(team => team.userId === currentUserId)
      .slice(0, this.MAX_TEAMS);

    this.teams.set(userTeams.map((team: GetTeamDto) => {
      const pokemonFromTeam = allPokemonTeams
        .filter((pokemonTeam: GetAllPokemonTeamDto) => pokemonTeam.teamId === team.id)
        .map((pokemonTeam: GetAllPokemonTeamDto) => ({
          id: pokemonTeam.id,
          nickname: pokemonTeam.nickname ?? null,
          shiny: pokemonTeam.shiny,
          sex: pokemonTeam.sex ?? null,
          slot: pokemonTeam.slot,
          teamId: pokemonTeam.teamId,
          pokemonId: pokemonTeam.pokemonId,
          natureId: pokemonTeam.natureId,
          movementId1: pokemonTeam.movementId1,
          movementId2: pokemonTeam.movementId2 ?? null,
          movementId3: pokemonTeam.movementId3 ?? null,
          movementId4: pokemonTeam.movementId4 ?? null,
          pokemon: pokemonMap.get(pokemonTeam.pokemonId) ?? null,
        }));

      return {
        id: team.id,
        name: team.name,
        description: team.description,
        userId: team.userId,
        pokemons: pokemonFromTeam,
        isExpanded: false,
      };
    }));

    this.isLoadingTeams.set(false);
  }
}
