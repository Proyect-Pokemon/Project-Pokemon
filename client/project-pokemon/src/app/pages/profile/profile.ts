import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth';
import { ApiService } from '../../services/api';
import { Router } from '@angular/router';
import { TeamService } from '../../services/team-service';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';
import { Team } from '../../models/team';
import { Pokemon } from '../../models/pokemon';
import { GetAllPokemonTeamDto } from '../../models/pokemon-team';

interface UserProfileDto {
  email: string;
  nickname: string;
  biography?: string | null;
  favoriteTeamId?: number | null;
}

interface TeamDisplay {
  id: number;
  name: string;
  description?: string | null;
  pokemonCount: number;
  pokemons: Array<{
    slot: number;
    nickname: string | null;
    pokemonName: string;
    pokemonId: number;
    sprite: string;
    types: string[];
  }>;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly teamService = inject(TeamService);
  private readonly pokemonTeamService = inject(PokemonTeamService);
  private readonly pokemonService = inject(PokemonService);

  // User info
  protected readonly nickname = this.auth.nickname;
  protected readonly isOwnProfile = signal(true);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal('');

  // Favorite team state
  protected readonly favoriteTeamId = signal<number | null>(null);
  protected readonly biography = signal<string | null>(null);
  protected readonly favoriteTeam = computed(() =>
    this.userTeams().find((team) => team.id === this.favoriteTeamId()) ?? null
  );
  protected readonly favoriteSelectionOpen = signal(false);
  protected readonly favoriteActionLoading = signal(false);
  protected readonly favoriteActionError = signal('');

  // Teams
  protected readonly userTeams = signal<TeamDisplay[]>([]);
  protected readonly teamsLoading = signal(true);
  protected readonly teamsError = signal('');

  ngOnInit(): void {
    this.loadProfileData();
  }

  private async loadProfileData(): Promise<void> {
    const currentUserId = this.auth.currentUserId();

    if (!currentUserId) {
      this.errorMessage.set('No se pudo identificar el usuario autenticado.');
      this.loading.set(false);
      return;
    }

    await Promise.all([
      this.loadUserProfile(currentUserId),
      this.loadUserTeams(currentUserId),
    ]);

    this.loading.set(false);
  }

  private async loadUserTeams(userId: number): Promise<void> {
    this.teamsLoading.set(true);
    this.teamsError.set('');

    try {
      const [allTeams, allPokemonTeams, allPokemons] = await Promise.all([
        this.teamService.getAllTeams(),
        this.pokemonTeamService.getAllPokemonTeams(),
        this.pokemonService.getAllPokemon(),
      ]);

      const pokemonMap = new Map(allPokemons.map(p => [p.id, p]));

      const userTeams = allTeams
        .filter(team => team.userId === userId)
        .map((team): TeamDisplay => {
          const teamPokemon = allPokemonTeams
            .filter(pt => pt.teamId === team.id)
            .sort((a, b) => (a.slot ?? 0) - (b.slot ?? 0))
            .map(pt => {
              const pokemon = pokemonMap.get(pt.pokemonId);
              const types = pokemon
                ? [pokemon.type1, pokemon.type2].filter((t): t is string => !!t)
                : [];

              return {
                slot: pt.slot,
                nickname: pt.nickname,
                pokemonName: pokemon?.name ?? 'Pokemon Desconocido',
                pokemonId: pt.pokemonId,
                sprite: pokemon?.miniSprite ?? pokemon?.spriteFront ?? '/assets/placeholder.png',
                types,
              };
            });

          return {
            id: team.id,
            name: team.name,
            description: team.description,
            pokemonCount: teamPokemon.length,
            pokemons: teamPokemon,
          };
        });

      this.userTeams.set(userTeams);
    } catch (error) {
      this.teamsError.set('No se pudieron cargar los equipos del usuario.');
      console.error('Error loading teams:', error);
    } finally {
      this.teamsLoading.set(false);
    }
  }

  protected navigateToTeamEdit(teamId: number): void {
    this.router.navigate(['/team-edit', teamId]);
  }

  protected navigateToTeamBuilder(): void {
    this.router.navigate(['/team-builder']);
  }

  protected openFavoriteSelection(): void {
    this.favoriteSelectionOpen.set(true);
    this.favoriteActionError.set('');
  }

  protected closeFavoriteSelection(): void {
    this.favoriteSelectionOpen.set(false);
    this.favoriteActionError.set('');
  }

  protected async setFavoriteTeam(teamId: number): Promise<void> {
    if (this.favoriteTeamId() === teamId) {
      this.favoriteSelectionOpen.set(false);
      return;
    }

    const currentUserId = this.auth.currentUserId();
    if (!currentUserId) {
      this.favoriteActionError.set(
        'No se pudo identificar el usuario para actualizar el equipo favorito.'
      );
      return;
    }

    this.favoriteActionLoading.set(true);
    this.favoriteActionError.set('');

    try {
      await this.api.put(`users?id=${currentUserId}`, { favoriteTeamId: teamId });
      this.favoriteTeamId.set(teamId);
      this.favoriteSelectionOpen.set(false);
    } catch (error) {
      console.error('Error actualizando equipo favorito:', error);
      this.favoriteActionError.set(
        'No se pudo actualizar el equipo favorito. Intenta de nuevo.'
      );
    } finally {
      this.favoriteActionLoading.set(false);
    }
  }

  private async loadUserProfile(userId: number): Promise<void> {
    try {
      const profile = await this.api.get<UserProfileDto>(`users/all?userId=${userId}`);
      this.favoriteTeamId.set(profile.favoriteTeamId ?? null);
      this.biography.set(profile.biography?.trim() ?? null);
    } catch (error) {
      console.error('Error cargando perfil de usuario:', error);
      this.favoriteTeamId.set(null);
      this.biography.set(null);
    }
  }

  protected logout(): void {
    this.auth.jwt = null;
    localStorage.removeItem('jwt');
    this.router.navigate(['/login']);
  }
}