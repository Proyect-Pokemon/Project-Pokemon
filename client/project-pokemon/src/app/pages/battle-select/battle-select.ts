import { Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TeamService } from '../../services/team-service';
import { GetTeamDto } from '../../models/team';
import { AuthService } from '../../services/auth';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';
import { GetAllPokemonTeamDto } from '../../models/pokemon-team';
import { Pokemon } from '../../models/pokemon';
import { SocketService } from '../../services/websocket-service';

interface TeamSlotView {
  displayName: string;
  sprite: string | null;
}

interface BattleSelectTeamView {
  id: number;
  name: string;
  slots: TeamSlotView[];
}

@Component({
  selector: 'app-battle-select',
  imports: [CommonModule],
  templateUrl: './battle-select.html',
  styleUrl: './battle-select.css',
})
export class BattleSelect {
  mode = signal<'cpu' | 'online'>('cpu');
  teams = signal<BattleSelectTeamView[]>([]);
  selectedTeamId = signal<number | null>(null);
  contextMenuTeamId = signal<number | null>(null);
  isLoadingTeams = signal(true);
  isSearchingBattle = signal(false);
  searchingMessage = signal('');

  private teamService = inject(TeamService);
  private authService = inject(AuthService);
  private pokemonTeamService = inject(PokemonTeamService);
  private pokemonService = inject(PokemonService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private socketService = inject(SocketService);

  constructor() {
    effect(() => {
      if (this.mode() !== 'online') {
        this.isSearchingBattle.set(false);
        return;
      }

      this.searchingMessage.set(this.socketService.matchmakingMessage());
      this.isSearchingBattle.set(this.socketService.matchmakingState() === 'searching');
    });

    effect(() => {
      if (this.mode() !== 'online') {
        return;
      }

      const match = this.socketService.onBattleMatched();
      if (!match) {
        return;
      }

      this.isSearchingBattle.set(false);
      void this.router.navigate(['/battle/fight'], {
        queryParams: { mode: 'online', battleId: match.battleId },
      });
    });
  }

  async ngOnInit(): Promise<void> {
    const modeParam = this.route.snapshot.queryParamMap.get('mode');
    this.mode.set(modeParam === 'online' ? 'online' : 'cpu');

    if (this.mode() === 'online') {
      this.socketService.resetBattleContext();
      this.isSearchingBattle.set(false);
      this.searchingMessage.set('');
    }

    await this.loadUserTeams();
  }

  openTeamMenu(teamId: number, event: Event): void {
    event.stopPropagation();
    this.selectedTeamId.set(teamId);
    this.contextMenuTeamId.set(this.contextMenuTeamId() === teamId ? null : teamId);
  }

  closeTeamMenu(): void {
    this.contextMenuTeamId.set(null);
  }

  viewTeamDetails(teamId: number, event: Event): void {
    event.stopPropagation();
    this.selectedTeamId.set(teamId);
    this.contextMenuTeamId.set(null);
    void this.router.navigate(['/team-builder']);
  }

  startBattle(teamId: number, event?: Event): void {
    event?.stopPropagation();
    this.selectedTeamId.set(teamId);
    this.contextMenuTeamId.set(null);

    if (this.mode() === 'online') {
      this.socketService.searchBattle(teamId);
      this.isSearchingBattle.set(true);
      return;
    }

    void this.router.navigate(['/battle/fight'], {
      queryParams: { mode: 'cpu', teamId },
    });
  }

  cancelSearch(event?: Event): void {
    event?.stopPropagation();
    this.socketService.cancelSearch();
    this.isSearchingBattle.set(false);
  }

  private async loadUserTeams(): Promise<void> {
    this.isLoadingTeams.set(true);

    const userId = this.authService.currentUserId();
    const [allTeams, allPokemonTeams, allPokemons] = await Promise.all([
      this.teamService.getAllTeams(),
      this.pokemonTeamService.getAllPokemonTeams(),
      this.pokemonService.getAllPokemon(),
    ]);

    const teams = userId ? allTeams.filter((team) => team.userId === userId) : [];
    const pokemonById = new Map<number, Pokemon>(allPokemons.map((pokemon) => [pokemon.id, pokemon]));

    const cards = teams.map((team: GetTeamDto) => {
      const teamPokemon = allPokemonTeams.filter((pokemonTeam: GetAllPokemonTeamDto) => pokemonTeam.teamId === team.id);
      const slots = Array.from({ length: 6 }, (_, index) => {
        const slotNumber = index + 1;
        const slotPokemon = teamPokemon.find((pokemonTeam: GetAllPokemonTeamDto) => pokemonTeam.slot === slotNumber);
        if (!slotPokemon) {
          return { displayName: 'Vacio', sprite: null };
        }

        const pokemon = pokemonById.get(slotPokemon.pokemonId);
        const displayName = slotPokemon.nickname?.trim() || pokemon?.name || 'Pokemon';
        const sprite = pokemon?.miniSprite ?? pokemon?.spriteFront ?? null;

        return { displayName, sprite };
      });

      return {
        id: team.id,
        name: team.name,
        slots,
      };
    });

    this.teams.set(cards);
    this.selectedTeamId.set(cards[0]?.id ?? null);
    this.isLoadingTeams.set(false);
  }
}
