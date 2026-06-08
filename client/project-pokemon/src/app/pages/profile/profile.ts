import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth';
import { ApiService } from '../../services/api';
import { Router } from '@angular/router';
import { TeamService } from '../../services/team-service';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';

interface UserProfileDto {
  email: string;
  nickname: string;
  biography?: string | null;
  favoriteTeamId?: number | null;
  avatarPath?: string | null;
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

  protected readonly nickname = this.auth.nickname;

  // Imagen gestionada desde la BD, no desde el JWT
  protected readonly selectedAvatarName = signal<string | null>(null);
  protected readonly avatarSrc = computed(() => {
    const name = this.selectedAvatarName();
    if (name) return `/assets/Images/${name}`;
    return '/assets/Images/avatar-default.jpg';
  });

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal('');

  // Avatar: sin modal intermedio, click en foto abre/cierra el selector directamente
  protected readonly avatarSelectionOpen = signal(false);
  protected readonly avatarActionLoading = signal(false);
  protected readonly avatarActionError = signal('');

  protected readonly avatarOptions = [
    'aura.png',
    'avatar-default.jpg',
    'bruno.png',
    'Cyntia.png',
    'down.png',
    'espada.png',
    'gold.png',
    'Kalos.png',
    'Lizza.png',
    'Lucho.png',
    'N2.png',
    'Red.jpg',
    'Rizzo.png',
    'Sol.png',
    'verde.png',
    'verdeRoket.png',
  ];

  protected readonly editProfileOpen = signal(false);
  protected readonly biography = signal<string | null>(null);
  protected readonly biographyDraft = signal('');
  protected readonly biographyEditLoading = signal(false);
  protected readonly biographyEditError = signal('');
  protected readonly biographyEditSuccess = signal('');

  // Contrasena: un unico modal con los tres campos juntos
  protected readonly editPasswordOpen = signal(false);
  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly passwordChangeLoading = signal(false);
  protected readonly passwordChangeError = signal('');
  protected readonly passwordChangeSuccess = signal('');

  protected readonly favoriteTeamId = signal<number | null>(null);
  protected readonly favoriteTeam = computed(() =>
    this.userTeams().find((team) => team.id === this.favoriteTeamId()) ?? null
  );
  protected readonly favoriteSelectionOpen = signal(false);
  protected readonly favoriteActionLoading = signal(false);
  protected readonly favoriteActionError = signal('');

  protected readonly userTeams = signal<TeamDisplay[]>([]);
  protected readonly teamsLoading = signal(true);
  protected readonly teamsError = signal('');

  ngOnInit(): void {
    void this.loadProfileData();
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

  private async loadUserProfile(userId: number): Promise<void> {
    try {
      const profile = await this.api.get<UserProfileDto>(`users/all?userId=${userId}`);
      this.favoriteTeamId.set(profile.favoriteTeamId ?? null);
      this.biography.set(profile.biography?.trim() ?? null);
      this.biographyDraft.set(profile.biography?.trim() ?? '');
      // Inicializar imagen desde la BD para que sea correcta independientemente del JWT
      this.selectedAvatarName.set(profile.avatarPath?.trim() ?? null);
    } catch (error) {
      console.error('Error cargando perfil de usuario:', error);
      this.favoriteTeamId.set(null);
      this.biography.set(null);
      this.biographyDraft.set('');
      // Fallback al JWT si la BD falla
      const pathFromJwt = this.auth.avatarPath();
      this.selectedAvatarName.set(pathFromJwt.split('/').pop() ?? null);
    }
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

      const pokemonMap = new Map(allPokemons.map((p) => [p.id, p]));

      const userTeams = allTeams
        .filter((team) => team.userId === userId)
        .map((team): TeamDisplay => {
          const teamPokemon = allPokemonTeams
            .filter((pt) => pt.teamId === team.id)
            .sort((a, b) => (a.slot ?? 0) - (b.slot ?? 0))
            .map((pt) => {
              const pokemon = pokemonMap.get(pt.pokemonId);
              const types = pokemon
                ? [pokemon.type1, pokemon.type2].filter((t): t is string => !!t)
                : [];

              return {
                slot: pt.slot,
                nickname: pt.nickname,
                pokemonName: pokemon?.name ?? 'Pokemon Desconocido',
                pokemonId: pt.pokemonId,
                sprite:
                  pokemon?.miniSprite ??
                  pokemon?.spriteFront ??
                  '/assets/placeholder.png',
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

  // Avatar

  protected toggleAvatarSelection(): void {
    this.avatarSelectionOpen.update((open) => !open);
    this.avatarActionError.set('');
  }

  protected cancelAvatarChange(): void {
    this.avatarSelectionOpen.set(false);
    this.avatarActionError.set('');
  }

  protected async setAvatar(filename: string): Promise<void> {
    const currentUserId = this.auth.currentUserId();
    if (!currentUserId) {
      this.avatarActionError.set('No se pudo identificar el usuario.');
      return;
    }

    this.avatarActionLoading.set(true);
    this.avatarActionError.set('');

    try {
      await this.api.put(`users/Avatar?id=${currentUserId}`, { avatarPath: filename });
      this.selectedAvatarName.set(filename);
      this.avatarSelectionOpen.set(false);
    } catch (error) {
      console.error('Error actualizando avatar:', error);
      this.avatarActionError.set('No se pudo actualizar la foto de perfil. Intenta de nuevo.');
    } finally {
      this.avatarActionLoading.set(false);
    }
  }

  // Biografia

  protected openEditProfile(): void {
    this.editProfileOpen.set(true);
    this.biographyEditError.set('');
    this.biographyEditSuccess.set('');
    this.biographyDraft.set(this.biography() ?? '');
  }

  protected closeEditProfile(): void {
    this.editProfileOpen.set(false);
    this.biographyEditError.set('');
  }

  protected onBiographyInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement | null;
    if (target) {
      const value = target.value.substring(0, 200);
      this.biographyDraft.set(value);
      target.value = value;
    }
  }

  protected async saveBiography(): Promise<void> {
    const currentUserId = this.auth.currentUserId();
    if (!currentUserId) {
      this.biographyEditError.set('No se pudo identificar el usuario.');
      return;
    }

    this.biographyEditLoading.set(true);
    this.biographyEditError.set('');
    this.biographyEditSuccess.set('');

    try {
      await this.api.put(`users/Biography?id=${currentUserId}`, {
        biography: this.biographyDraft().trim(),
      });
      this.biography.set(this.biographyDraft().trim() || null);
      this.biographyEditSuccess.set('Biografia actualizada correctamente.');
      this.editProfileOpen.set(false);
    } catch (error) {
      console.error('Error actualizando biografia:', error);
      this.biographyEditError.set('No se pudo actualizar la biografia. Intenta de nuevo.');
    } finally {
      this.biographyEditLoading.set(false);
    }
  }

  // Contrasena

  protected openPasswordChange(): void {
    this.editPasswordOpen.set(true);
    this.currentPassword.set('');
    this.newPassword.set('');
    this.confirmPassword.set('');
    this.passwordChangeError.set('');
    this.passwordChangeSuccess.set('');
  }

  protected closePasswordChange(): void {
    this.editPasswordOpen.set(false);
    this.currentPassword.set('');
    this.newPassword.set('');
    this.confirmPassword.set('');
    this.passwordChangeError.set('');
  }

  protected onCurrentPasswordInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (target) this.currentPassword.set(target.value);
  }

  protected onNewPasswordInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (target) this.newPassword.set(target.value);
  }

  protected onConfirmPasswordInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (target) this.confirmPassword.set(target.value);
  }

  protected async savePassword(): Promise<void> {
    const currentPassword = this.currentPassword().trim();
    const newPassword = this.newPassword().trim();
    const confirmPassword = this.confirmPassword().trim();

    if (!currentPassword) {
      this.passwordChangeError.set('Introduce tu contrasena actual.');
      return;
    }

    if (!newPassword || !confirmPassword) {
      this.passwordChangeError.set('Introduce y confirma la nueva contrasena.');
      return;
    }

    if (newPassword !== confirmPassword) {
      this.passwordChangeError.set('Las contrasenas nuevas no coinciden.');
      return;
    }

    if (newPassword.length < 6) {
      this.passwordChangeError.set('La nueva contrasena debe tener al menos 6 caracteres.');
      return;
    }

    const currentUserId = this.auth.currentUserId();
    if (!currentUserId) {
      this.passwordChangeError.set('No se pudo identificar el usuario.');
      return;
    }

    this.passwordChangeLoading.set(true);
    this.passwordChangeError.set('');
    this.passwordChangeSuccess.set('');

    try {
      await this.api.put(`users/Password?id=${currentUserId}`, {
        currentPassword,
        newPassword,
      });
      this.passwordChangeSuccess.set('Contrasena actualizada correctamente.');
      this.closePasswordChange();
    } catch (error: any) {
      const backendMsg = typeof error?.error === 'string' ? error.error : null;
      this.passwordChangeError.set(
        backendMsg ?? 'Contrasena actual incorrecta o error al actualizar.'
      );
    } finally {
      this.passwordChangeLoading.set(false);
    }
  }

  // Equipo favorito

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
      this.favoriteActionError.set('No se pudo identificar el usuario.');
      return;
    }

    this.favoriteActionLoading.set(true);
    this.favoriteActionError.set('');

    try {
      await this.api.put(`users/FavoriteTeam?id=${currentUserId}`, { favoriteTeamId: teamId });
      this.favoriteTeamId.set(teamId);
      this.favoriteSelectionOpen.set(false);
    } catch (error) {
      console.error('Error actualizando equipo favorito:', error);
      this.favoriteActionError.set('No se pudo actualizar el equipo favorito. Intenta de nuevo.');
    } finally {
      this.favoriteActionLoading.set(false);
    }
  }

  // Navegacion / sesion

  protected navigateToTeamEdit(teamId: number): void {
    this.router.navigate(['/team-edit', teamId]);
  }

  protected navigateToTeamBuilder(): void {
    this.router.navigate(['/team-builder']);
  }

  protected logout(): void {
    this.auth.jwt = null;
    localStorage.removeItem('jwt');
    this.router.navigate(['/login']);
  }
}