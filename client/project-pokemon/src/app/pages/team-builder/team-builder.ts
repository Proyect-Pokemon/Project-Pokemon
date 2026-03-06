import { Component, inject, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Team, GetTeamDto } from '../../models/team';
import { TeamService } from '../../services/team-service';
import { AuthService } from '../../services/auth';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';
import { GetAllPokemonTeamDto, PostPokemonTeamDto } from '../../models/pokemon-team';
import { TeamSlot } from '../../components/team-slot/team-slot';
import { PokemonEditorPanel } from '../../components/pokemon-editor-panel/pokemon-editor-panel';

@Component({
  selector: 'app-team-builder',
  standalone: true,
  imports: [CommonModule, TeamSlot, PokemonEditorPanel],
  templateUrl: './team-builder.html',
  styleUrls: ['./team-builder.css'],
})
export class TeamBuilder {
  private teamService = inject(TeamService);
  authService = inject(AuthService);
  private pokemonTeamService = inject(PokemonTeamService);
  private pokemonService = inject(PokemonService);

  teams = signal<Team[]>([]);
  readonly MAX_TEAMS = 5;
  isLoadingTeams = false;
  isCreatingTeam = false;

  // Estado del panel editor
  isPanelOpen = false;
  selectedTeamId = 0;
  selectedSlot = 1;
  selectedPokemonDisplayName: string | null = null;
  selectedPokemonSprite: string | null = null;
  selectedPokemonId: number | null = null;
  selectedNatureId = 1;
  selectedMovementIds: (number | null)[] = [null, null, null, null];

  constructor() {
    effect(() => {
      const userId = this.authService.currentUserId();
      if (userId !== null) {
        this.loadUserTeams();
      }
    });
  }

  // ngOnInit no es necesario ya que el JWT se carga en app.ts

  async addTeam() {
    const currentUserId = this.authService.currentUserId();
    if (!currentUserId || this.teams().length >= this.MAX_TEAMS || this.isCreatingTeam) {
      return;
    }

    this.isCreatingTeam = true;

    const created = await this.teamService.addTeam({
      name: `Equipo ${this.teams().length + 1}`,
      description: null,
      userId: currentUserId,
    });

    if (created) {
      await this.loadUserTeams();
    }

    this.isCreatingTeam = false;
  }

  toggleTeamExpansion(teamId: number) {
    const team = this.teams().find(t => t.id === teamId);
    if (team) {
      team.isExpanded = !team.isExpanded;
    }
  }

  updateTeamName(data: { id: number, name: string }) {
    const team = this.teams().find(t => t.id === data.id);
    if (team) {
      team.name = data.name;
    }
  }

  handleAddPokemon(data: { teamId: number, slot: number }) {
    this.selectedTeamId = data.teamId;
    this.selectedSlot = data.slot;

    // Easter egg: MissingNo for slots -3 to 0 and 7 to 10
    if (data.slot <= 0 || data.slot >= 7) {
      this.selectedPokemonDisplayName = 'MissingNo';
      this.selectedPokemonSprite = 'assets/error/missing-no.png';
      this.selectedPokemonId = 0;
      this.selectedNatureId = 1;
      this.selectedMovementIds = [null, null, null, null];
      this.isPanelOpen = true;
      return;
    }

    const team = this.teams().find(t => t.id === data.teamId);
    const selectedPokemon = team?.pokemons.find(p => p.slot === data.slot) ?? null;

    if (selectedPokemon?.pokemon) {
      this.selectedPokemonDisplayName = selectedPokemon.nickname ?? selectedPokemon.pokemon.name;
      this.selectedPokemonSprite = selectedPokemon.shiny
        ? (selectedPokemon.pokemon.spriteFrontShiny ?? selectedPokemon.pokemon.spriteFront)
        : selectedPokemon.pokemon.spriteFront;
      this.selectedPokemonId = selectedPokemon.pokemon.id;
      this.selectedNatureId = selectedPokemon.natureId;
      this.selectedMovementIds = [
        selectedPokemon.movementId1,
        selectedPokemon.movementId2,
        selectedPokemon.movementId3,
        selectedPokemon.movementId4
      ];
    } else {
      this.selectedPokemonDisplayName = null;
      this.selectedPokemonSprite = null;
      this.selectedPokemonId = null;
      this.selectedNatureId = 1;
      this.selectedMovementIds = [null, null, null, null];
    }

    this.isPanelOpen = true;
  }

  closePanel() {
    this.isPanelOpen = false;
  }

  handleChangeSlot(newSlot: number) {
    this.handleAddPokemon({ teamId: this.selectedTeamId, slot: newSlot });
  }

  async handleCreatePokemonTeam(dto: PostPokemonTeamDto) {
    const created = await this.pokemonTeamService.addPokemonTeam(dto);
    if (!created) {
      return;
    }

    await this.loadUserTeams();
    this.closePanel();
  }

  canAddMoreTeams(): boolean {
    return this.authService.currentUserId() !== null && this.teams().length < this.MAX_TEAMS;
  }


  private async loadUserTeams() {
    const currentUserId = this.authService.currentUserId();
    if (!currentUserId) {
      console.log('currentUserId is null, skipping team loading');
      this.teams.set([]);
      return;
    }

    this.isLoadingTeams = true;

    const allTeams = await this.teamService.getAllTeams();
    const allPokemonTeams = await this.pokemonTeamService.getAllPokemonTeams();
    const allPokemons = await this.pokemonService.getAllPokemon();
    
    // Crear un mapa de pokémonId -> Pokemon para búsqueda rápida
    const pokemonMap = new Map(allPokemons.map(p => [p.id, p]));
    
    const userTeams = allTeams
      .filter(team => team.userId === currentUserId)
      .slice(0, this.MAX_TEAMS);

    const expandedById = new Map(this.teams().map(team => [team.id, team.isExpanded]));

    this.teams.set(userTeams.map((team: GetTeamDto) => {
      const pokemonFromTeam = allPokemonTeams
        .filter((pokemonTeam: GetAllPokemonTeamDto) => pokemonTeam.teamId === team.id)
        .map((pokemonTeam: GetAllPokemonTeamDto) => ({
          id: pokemonTeam.id,
          nickname: pokemonTeam.nickname ?? null,
          shiny: pokemonTeam.shiny,
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
        isExpanded: expandedById.get(team.id) ?? false,
      };
    }));

    this.isLoadingTeams = false;
  }
}
