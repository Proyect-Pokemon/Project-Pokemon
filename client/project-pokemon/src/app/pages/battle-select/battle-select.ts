import { Component, computed, effect, inject, signal } from '@angular/core';
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
import { MovementService } from '../../services/movement-service';
import { NatureService } from '../../services/nature-service';
import { Movement } from '../../models/move';
import { Nature } from '../../models/nature';

interface TeamSlotView {
  isEmpty: boolean;
  displayName: string;
  speciesName: string;
  sprite: string | null;
  types: string[];
  nature: string;
  moves: Array<{
    name: string;
    type: string;
    movementClass: string;
    isEmpty: boolean;
  }>;
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
  selectedTeam = computed(() => this.teams().find((team) => team.id === this.selectedTeamId()) ?? null);
  selectedTeamFilledCount = computed(() => this.selectedTeam()?.slots.filter((slot) => !slot.isEmpty).length ?? 0);
  isLoadingTeams = signal(true);
  isSearchingBattle = signal(false);
  searchingMessage = signal('');

  private teamService = inject(TeamService);
  private authService = inject(AuthService);
  private pokemonTeamService = inject(PokemonTeamService);
  private pokemonService = inject(PokemonService);
  private movementService = inject(MovementService);
  private natureService = inject(NatureService);
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

  openTeamConfirmation(teamId: number, event?: Event): void {
    event?.stopPropagation();
    this.selectedTeamId.set(teamId);
  }

  closeTeamConfirmation(event?: Event): void {
    event?.stopPropagation();
    this.selectedTeamId.set(null);
  }

  confirmSelectedTeam(): void {
    const teamId = this.selectedTeamId();
    if (!teamId) {
      return;
    }

    this.startBattle(teamId);
    this.selectedTeamId.set(null);
  }

  startBattle(teamId: number): void {
    this.selectedTeamId.set(teamId);

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

  getMoveChipColor(type: string): string {
    const normalizedType = type?.toLowerCase().trim();

    const colorMap: Record<string, string> = {
      normal: 'var(--normal-color)',
      fire: 'var(--fire-color)',
      water: 'var(--water-color)',
      electric: 'var(--electric-color)',
      grass: 'var(--grass-color)',
      ice: 'var(--ice-color)',
      fighting: 'var(--fighting-color)',
      poison: 'var(--poison-color)',
      ground: 'var(--ground-color)',
      flying: 'var(--flying-color)',
      psychic: 'var(--psychic-color)',
      bug: 'var(--bug-color)',
      rock: 'var(--rock-color)',
      ghost: 'var(--ghost-color)',
      dragon: 'var(--dragon-color)',
      dark: 'var(--dark-color)',
      steel: 'var(--steel-color)',
      fairy: 'var(--fairy-color)',
    };

    const typeColor = colorMap[normalizedType];
    return typeColor ? `color-mix(in srgb, ${typeColor} 18%, white)` : 'rgba(0, 63, 80, 0.06)';
  }

  getMoveChipBorderColor(type: string): string {
    const normalizedType = type?.toLowerCase().trim();

    const colorMap: Record<string, string> = {
      normal: 'var(--normal-color)',
      fire: 'var(--fire-color)',
      water: 'var(--water-color)',
      electric: 'var(--electric-color)',
      grass: 'var(--grass-color)',
      ice: 'var(--ice-color)',
      fighting: 'var(--fighting-color)',
      poison: 'var(--poison-color)',
      ground: 'var(--ground-color)',
      flying: 'var(--flying-color)',
      psychic: 'var(--psychic-color)',
      bug: 'var(--bug-color)',
      rock: 'var(--rock-color)',
      ghost: 'var(--ghost-color)',
      dragon: 'var(--dragon-color)',
      dark: 'var(--dark-color)',
      steel: 'var(--steel-color)',
      fairy: 'var(--fairy-color)',
    };

    return colorMap[normalizedType] ?? 'rgba(0, 63, 80, 0.08)';
  }

  getTypeBadgeColor(type: string): string {
    const normalizedType = type?.toLowerCase().trim();

    const colorMap: Record<string, string> = {
      normal: 'var(--normal-color)',
      fire: 'var(--fire-color)',
      water: 'var(--water-color)',
      electric: 'var(--electric-color)',
      grass: 'var(--grass-color)',
      ice: 'var(--ice-color)',
      fighting: 'var(--fighting-color)',
      poison: 'var(--poison-color)',
      ground: 'var(--ground-color)',
      flying: 'var(--flying-color)',
      psychic: 'var(--psychic-color)',
      bug: 'var(--bug-color)',
      rock: 'var(--rock-color)',
      ghost: 'var(--ghost-color)',
      dragon: 'var(--dragon-color)',
      dark: 'var(--dark-color)',
      steel: 'var(--steel-color)',
      fairy: 'var(--fairy-color)',
    };

    const typeColor = colorMap[normalizedType];
    return typeColor ? `color-mix(in srgb, ${typeColor} 20%, white)` : 'rgba(0, 63, 80, 0.08)';
  }

  getTypeBadgeBorderColor(type: string): string {
    const normalizedType = type?.toLowerCase().trim();

    const colorMap: Record<string, string> = {
      normal: 'var(--normal-color)',
      fire: 'var(--fire-color)',
      water: 'var(--water-color)',
      electric: 'var(--electric-color)',
      grass: 'var(--grass-color)',
      ice: 'var(--ice-color)',
      fighting: 'var(--fighting-color)',
      poison: 'var(--poison-color)',
      ground: 'var(--ground-color)',
      flying: 'var(--flying-color)',
      psychic: 'var(--psychic-color)',
      bug: 'var(--bug-color)',
      rock: 'var(--rock-color)',
      ghost: 'var(--ghost-color)',
      dragon: 'var(--dragon-color)',
      dark: 'var(--dark-color)',
      steel: 'var(--steel-color)',
      fairy: 'var(--fairy-color)',
    };

    return colorMap[normalizedType] ?? 'rgba(0, 63, 80, 0.12)';
  }

  private async loadUserTeams(): Promise<void> {
    this.isLoadingTeams.set(true);

    const userId = this.authService.currentUserId();
    const [allTeams, allPokemonTeams, allPokemons, allMovements, allNatures] = await Promise.all([
      this.teamService.getAllTeams(),
      this.pokemonTeamService.getAllPokemonTeams(),
      this.pokemonService.getAllPokemon(),
      this.movementService.getAllMovements(),
      this.natureService.getAllNatures(),
    ]);

    const teams = userId ? allTeams.filter((team) => team.userId === userId) : [];
    const pokemonById = new Map<number, Pokemon>(allPokemons.map((pokemon) => [pokemon.id, pokemon]));
    const movementById = new Map<number, Movement>(allMovements.map((movement) => [movement.id, movement]));
    const natureById = new Map<number, Nature>(allNatures.map((nature) => [nature.id, nature]));

    const cards = teams.map((team: GetTeamDto) => {
      const teamPokemon = allPokemonTeams.filter((pokemonTeam: GetAllPokemonTeamDto) => pokemonTeam.teamId === team.id);
      const slots = Array.from({ length: 6 }, (_, index) => {
        const slotNumber = index + 1;
        const slotPokemon = teamPokemon.find((pokemonTeam: GetAllPokemonTeamDto) => pokemonTeam.slot === slotNumber);
        if (!slotPokemon) {
          return {
            isEmpty: true,
            displayName: 'Hueco vacio',
            speciesName: '',
            sprite: null,
            types: [],
            nature: '',
            moves: [],
          };
        }

        const pokemon = pokemonById.get(slotPokemon.pokemonId);
        const nature = natureById.get(slotPokemon.natureId);
        const movementIds = [
          slotPokemon.movementId1,
          slotPokemon.movementId2,
          slotPokemon.movementId3,
          slotPokemon.movementId4,
        ];

        const moves = movementIds.map((movementId, moveIndex) => {
          if (!movementId) {
            return {
              name: 'Hueco',
              type: 'Sin tipo',
              movementClass: 'Sin movimiento',
              isEmpty: true,
            };
          }

          const movement = movementById.get(movementId);
          return {
            name: movement?.name || `Movimiento ${moveIndex + 1}`,
            type: movement?.type || 'Sin tipo',
            movementClass: movement?.movementClass || 'Sin clase',
            isEmpty: !movement,
          };
        });

        const displayName = slotPokemon.nickname?.trim() || pokemon?.name || 'Pokemon';
        const sprite = pokemon?.miniSprite ?? pokemon?.spriteFront ?? null;
        const types = [pokemon?.type1, pokemon?.type2].filter((type): type is string => !!type);

        return {
          isEmpty: false,
          displayName,
          speciesName: pokemon?.name || 'Pokemon',
          sprite,
          types,
          nature: nature?.name || 'Sin naturaleza',
          moves,
        };
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
