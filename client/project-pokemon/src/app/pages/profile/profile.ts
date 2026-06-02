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
  protected readonly avatar = this.auth.avatarPath;
  protected readonly avatarPath = this.auth.avatarPath;
  protected readonly avatarSrc = computed(() =>
    this.selectedAvatarName()
      ? `/assets/Images/${this.selectedAvatarName()}`
      : this.auth.avatarPath()
  );
  protected readonly AvatarPath = this.auth.avatarPath;
  protected readonly isOwnProfile = signal(true);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal('');
  protected readonly avatarDialogOpen = signal(false);
  protected readonly avatarSelectionOpen = signal(false);
  protected readonly avatarActionLoading = signal(false);
  protected readonly avatarActionError = signal('');
  protected readonly selectedAvatarName = signal<string | null>(null);
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
  protected readonly biographyDraft = signal<string>('');
  protected readonly biographyEditLoading = signal(false);
  protected readonly biographyEditError = signal('');
  protected readonly biographyEditSuccess = signal('');

  protected readonly editPasswordOpen = signal(false);
  protected readonly passwordStep = signal<1 | 2>(1);
  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly passwordChangeLoading = signal(false);
  protected readonly passwordChangeError = signal('');
  protected readonly passwordChangeSuccess = signal('');

  protected readonly favoriteTeamId = signal<number | null>(null);
  protected readonly biography = signal<string | null>(null);
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
    void Promise.all([this.loadProfileData()]);
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

  protected promptAvatarChange(): void {
    this.avatarDialogOpen.set(true);
    this.avatarActionError.set('');
  }

  protected confirmAvatarDialog(accept: boolean): void {
    this.avatarDialogOpen.set(false);
    if (accept) {
      this.avatarSelectionOpen.set(true);
    }
  }

  protected cancelAvatarChange(): void {
    this.avatarDialogOpen.set(false);
    this.avatarSelectionOpen.set(false);
    this.avatarActionError.set('');
    this.selectedAvatarName.set(null);
  }

  protected async setAvatar(filename: string): Promise<void> {
    const currentUserId = this.auth.currentUserId();
    if (!currentUserId) {
      this.avatarActionError.set(
        'No se pudo identificar el usuario para actualizar la foto de perfil.'
      );
      return;
    }

    this.avatarActionLoading.set(true);
    this.avatarActionError.set('');

    try {
      await this.api.put(`users/Avatar?id=${currentUserId}`, { avatarPath: filename });
      this.selectedAvatarName.set(filename);
      this.avatarSelectionOpen.set(false);
      this.avatarDialogOpen.set(false);
    } catch (error) {
      console.error('Error actualizando avatar:', error);
      this.avatarActionError.set(
        'No se pudo actualizar la foto de perfil. Intenta de nuevo.'
      );
    } finally {
      this.avatarActionLoading.set(false);
    }
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
      await this.api.put(`users/FavoriteTeam?id=${currentUserId}`, { favoriteTeamId: teamId });
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
      const biographyValue = profile.biography?.trim() ?? null;
      this.biography.set(biographyValue);
      this.biographyDraft.set(biographyValue ?? '');
    } catch (error) {
      console.error('Error cargando perfil de usuario:', error);
      this.favoriteTeamId.set(null);
      this.biography.set(null);
      this.biographyDraft.set('');
    }
  }

  protected async saveBiography(): Promise<void> {
    const currentUserId = this.auth.currentUserId();
    if (!currentUserId) {
      this.biographyEditError.set('No se pudo identificar el usuario para actualizar la biografía.');
      return;
    }

    this.biographyEditLoading.set(true);
    this.biographyEditError.set('');
    this.biographyEditSuccess.set('');

    try {
      await this.api.put(`users/Biography?id=${currentUserId}`, {
        biography: this.biographyDraft()?.trim() ?? '',
      });
      const updatedBiography = this.biographyDraft()?.trim() ?? null;
      this.biography.set(updatedBiography);
      this.biographyEditSuccess.set('Biografía actualizada correctamente.');
      this.editProfileOpen.set(false);
    } catch (error) {
      console.error('Error actualizando biografía:', error);
      this.biographyEditError.set('No se pudo actualizar la biografía. Intenta de nuevo.');
    } finally {
      this.biographyEditLoading.set(false);
    }
  }

  protected onBiographyInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement | null;
    if (target) {
      const value = target.value.substring(0, 200);
      this.biographyDraft.set(value);
      target.value = value;
    }
  }

  protected onCurrentPasswordInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (target) {
      this.currentPassword.set(target.value);
    }
  }

  protected onNewPasswordInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (target) {
      this.newPassword.set(target.value);
    }
  }

  protected onConfirmPasswordInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    if (target) {
      this.confirmPassword.set(target.value);
    }
  }

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

  protected openPasswordChange(): void {
    this.editPasswordOpen.set(true);
    this.passwordStep.set(1);
    this.currentPassword.set('');
    this.newPassword.set('');
    this.confirmPassword.set('');
    this.passwordChangeError.set('');
    this.passwordChangeSuccess.set('');
  }

  protected closePasswordChange(): void {
    this.editPasswordOpen.set(false);
    this.passwordStep.set(1);
    this.currentPassword.set('');
    this.newPassword.set('');
    this.confirmPassword.set('');
    this.passwordChangeError.set('');
  }

  protected proceedPasswordStep(): void {
    if (!this.currentPassword()?.trim()) {
      this.passwordChangeError.set('Introduce tu contraseña actual para continuar.');
      return;
    }

    this.passwordChangeError.set('');
    this.passwordStep.set(2);
  }

  protected async savePassword(): Promise<void> {
    const currentPassword = this.currentPassword()?.trim();
    const newPassword = this.newPassword()?.trim();
    const confirmPassword = this.confirmPassword()?.trim();

    if (!currentPassword) {
      this.passwordChangeError.set('Introduce tu contraseña actual.');
      return;
    }

    if (!newPassword || !confirmPassword) {
      this.passwordChangeError.set('Introduce y confirma la nueva contraseña.');
      return;
    }

    if (newPassword !== confirmPassword) {
      this.passwordChangeError.set('Las contraseñas nuevas no coinciden.');
      return;
    }

    const currentUserId = this.auth.currentUserId();
    if (!currentUserId) {
      this.passwordChangeError.set('No se pudo identificar el usuario para actualizar la contraseña.');
      return;
    }

    this.passwordChangeLoading.set(true);
    this.passwordChangeError.set('');
    this.passwordChangeSuccess.set('');

    try {
      const hashedCurrentPassword = await this.hashText(currentPassword);
      const hashedNewPassword = await this.hashText(newPassword);

      await this.api.put(`users/Password?id=${currentUserId}`, {
        currentPassword: hashedCurrentPassword,
        newPassword: hashedNewPassword,
      });
      this.passwordChangeSuccess.set('Contraseña actualizada correctamente.');
      this.editPasswordOpen.set(false);
    } catch (error) {
      console.error('Error actualizando contraseña:', error);
      this.passwordChangeError.set('No se pudo actualizar la contraseña. Comprueba la contraseña actual e inténtalo de nuevo.');
    } finally {
      this.passwordChangeLoading.set(false);
    }
  }

  private async hashText(value: string): Promise<string> {
    const data = new TextEncoder().encode(value);
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    return hashArray.map((byte) => byte.toString(16).padStart(2, '0')).join('');
  }

  protected logout(): void {
    this.auth.jwt = null;
    localStorage.removeItem('jwt');
    this.router.navigate(['/login']);
  }
}