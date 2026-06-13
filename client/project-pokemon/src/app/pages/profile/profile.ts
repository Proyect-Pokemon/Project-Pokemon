import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth';
import { ApiService } from '../../services/api';
import { Router } from '@angular/router';
import { TeamService } from '../../services/team-service';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';
import { ProfileAvatarModal } from '../../components/profile-avatar-modal/profile-avatar-modal';
import { ProfileBiographyModal } from '../../components/profile-biography-modal/profile-biography-modal';
import { ProfilePasswordModal } from '../../components/profile-password-modal/profile-password-modal';
import { ProfileFavoriteModal, TeamOption } from '../../components/profile-favorite-modal/profile-favorite-modal';


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
  imports: [
    CommonModule,
    ProfileAvatarModal,
    ProfileBiographyModal,
    ProfilePasswordModal,
    ProfileFavoriteModal,
  ],
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
    return name ? `/assets/Images/${name}` : '/assets/Images/avatar-default.jpg';
  });

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal('');

  // Avatar
  protected readonly avatarModalOpen = signal(false);
  protected readonly avatarActionLoading = signal(false);
  protected readonly avatarActionError = signal('');
  protected readonly avatarOptions = [
    'aura.png', 'avatar-default.jpg', 'bruno.png', 'Cyntia.png',
    'down.png', 'espada.png', 'gold.png', 'Kalos.png',
    'Lizza.png', 'Lucho.png', 'N2.png', 'Red.jpg',
    'Rizzo.png', 'Sol.png', 'verde.png', 'verdeRoket.png',
  ];

  // Biografía
  protected readonly biographyModalOpen = signal(false);
  protected readonly biography = signal<string | null>(null);
  protected readonly biographyDraft = signal('');
  protected readonly biographyEditLoading = signal(false);
  protected readonly biographyEditError = signal('');

  // Contraseña
  protected readonly passwordModalOpen = signal(false);
  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly passwordChangeLoading = signal(false);
  protected readonly passwordChangeError = signal('');

  // Equipo favorito
  protected readonly favoriteTeamId = signal<number | null>(null);
  protected readonly favoriteModalOpen = signal(false);
  protected readonly favoriteActionLoading = signal(false);
  protected readonly favoriteActionError = signal('');

  protected readonly userTeams = signal<TeamDisplay[]>([]);
  protected readonly teamsLoading = signal(true);
  protected readonly teamsError = signal('');

  protected readonly favoriteTeam = computed(() =>
    this.userTeams().find((t) => t.id === this.favoriteTeamId()) ?? null
  );

  protected readonly teamOptions = computed<TeamOption[]>(() =>
    this.userTeams().map((t) => ({ id: t.id, name: t.name, pokemonCount: t.pokemonCount }))
  );

  ngOnInit(): void {
    void this.loadProfileData();
  }

  private async loadProfileData(): Promise<void> {
    const userId = this.auth.currentUserId();
    if (!userId) {
      this.errorMessage.set('No se pudo identificar el usuario autenticado.');
      this.loading.set(false);
      return;
    }
    await Promise.all([this.loadUserProfile(userId), this.loadUserTeams(userId)]);
    this.loading.set(false);
  }

  private async loadUserProfile(userId: number): Promise<void> {
    try {
      const profile = await this.api.get<UserProfileDto>(`users/all?userId=${userId}`);
      this.favoriteTeamId.set(profile.favoriteTeamId ?? null);
      this.biography.set(profile.biography?.trim() ?? null);
      this.biographyDraft.set(profile.biography?.trim() ?? '');
      this.selectedAvatarName.set(profile.avatarPath?.trim() ?? null);
    } catch {
      this.biography.set(null);
      this.biographyDraft.set('');
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
      this.userTeams.set(
        allTeams
          .filter((t) => t.userId === userId)
          .map((team): TeamDisplay => {
            const pokemons = allPokemonTeams
              .filter((pt) => pt.teamId === team.id)
              .sort((a, b) => (a.slot ?? 0) - (b.slot ?? 0))
              .map((pt) => {
                const p = pokemonMap.get(pt.pokemonId);
                return {
                  slot: pt.slot,
                  nickname: pt.nickname,
                  pokemonName: p?.name ?? 'Pokemon Desconocido',
                  pokemonId: pt.pokemonId,
                  sprite: p?.miniSprite ?? p?.spriteFront ?? '/assets/placeholder.png',
                  types: p ? [p.type1, p.type2].filter((t): t is string => !!t) : [],
                };
              });
            return { id: team.id, name: team.name, description: team.description, pokemonCount: pokemons.length, pokemons };
          })
      );
    } catch {
      this.teamsError.set('No se pudieron cargar los equipos del usuario.');
    } finally {
      this.teamsLoading.set(false);
    }
  }

  // Avatar
  protected openAvatarModal(): void {
    this.avatarModalOpen.set(true);
    this.avatarActionError.set('');
  }

  protected closeAvatarModal(): void {
    if (!this.avatarActionLoading()) {
      this.avatarModalOpen.set(false);
      this.avatarActionError.set('');
    }
  }

  protected async onAvatarSelect(filename: string): Promise<void> {
    const userId = this.auth.currentUserId();
    if (!userId) { this.avatarActionError.set('No se pudo identificar el usuario.'); return; }

    this.avatarActionLoading.set(true);
    this.avatarActionError.set('');
    try {
      await this.api.put(`users/Avatar?id=${userId}`, { avatarPath: filename });
      this.selectedAvatarName.set(filename);
      this.avatarModalOpen.set(false);
    } catch {
      this.avatarActionError.set('No se pudo actualizar la foto de perfil. Intenta de nuevo.');
    } finally {
      this.avatarActionLoading.set(false);
    }
  }

  // Biografía
  protected openBiographyModal(): void {
    this.biographyDraft.set(this.biography() ?? '');
    this.biographyEditError.set('');
    this.biographyModalOpen.set(true);
  }

  protected closeBiographyModal(): void {
    if (!this.biographyEditLoading()) {
      this.biographyModalOpen.set(false);
      this.biographyEditError.set('');
    }
  }

  protected onBiographyDraftChange(value: string): void {
    this.biographyDraft.set(value);
  }

  protected async saveBiography(): Promise<void> {
    const userId = this.auth.currentUserId();
    if (!userId) { this.biographyEditError.set('No se pudo identificar el usuario.'); return; }

    this.biographyEditLoading.set(true);
    this.biographyEditError.set('');
    try {
      await this.api.put(`users/Biography?id=${userId}`, { biography: this.biographyDraft().trim() });
      this.biography.set(this.biographyDraft().trim() || null);
      this.biographyModalOpen.set(false);
    } catch {
      this.biographyEditError.set('No se pudo actualizar la biografía. Intenta de nuevo.');
    } finally {
      this.biographyEditLoading.set(false);
    }
  }

  // Contraseña
  protected openPasswordModal(): void {
    this.currentPassword.set('');
    this.newPassword.set('');
    this.confirmPassword.set('');
    this.passwordChangeError.set('');
    this.passwordModalOpen.set(true);
  }

  protected closePasswordModal(): void {
    if (!this.passwordChangeLoading()) {
      this.passwordModalOpen.set(false);
      this.passwordChangeError.set('');
    }
  }

  protected async savePassword(): Promise<void> {
    const current = this.currentPassword().trim();
    const next = this.newPassword().trim();
    const confirm = this.confirmPassword().trim();

    if (!current) { this.passwordChangeError.set('Introduce tu contraseña actual.'); return; }
    if (!next || !confirm) { this.passwordChangeError.set('Introduce y confirma la nueva contraseña.'); return; }
    if (next !== confirm) { this.passwordChangeError.set('Las contraseñas nuevas no coinciden.'); return; }
    if (next.length < 6) { this.passwordChangeError.set('La nueva contraseña debe tener al menos 6 caracteres.'); return; }

    const userId = this.auth.currentUserId();
    if (!userId) { this.passwordChangeError.set('No se pudo identificar el usuario.'); return; }

    this.passwordChangeLoading.set(true);
    this.passwordChangeError.set('');
    try {
      await this.api.put(`users/Password?id=${userId}`, { currentPassword: current, newPassword: next });
      this.passwordModalOpen.set(false);
    } catch (err: any) {
      const msg = typeof err?.error === 'string' ? err.error : null;
      this.passwordChangeError.set(msg ?? 'Contraseña actual incorrecta o error al actualizar.');
    } finally {
      this.passwordChangeLoading.set(false);
    }
  }

  // Borrar cuenta
  protected readonly deleteModalOpen = signal(false);
  protected readonly deleteConfirmInput = signal('');
  protected readonly deleteLoading = signal(false);
  protected readonly deleteError = signal('');

  protected readonly canConfirmDelete = computed(() =>
    this.deleteConfirmInput().trim().toLowerCase() === 'borrar'
  );

  protected openDeleteModal(): void {
    this.deleteConfirmInput.set('');
    this.deleteError.set('');
    this.deleteModalOpen.set(true);
  }

  protected closeDeleteModal(): void {
    if (!this.deleteLoading()) {
      this.deleteModalOpen.set(false);
      this.deleteError.set('');
    }
  }

  protected async confirmDeleteAccount(): Promise<void> {
    if (!this.canConfirmDelete() || this.deleteLoading()) return;

    this.deleteLoading.set(true);
    this.deleteError.set('');
    try {
      await this.api.delete('users/account');
      this.auth.jwt = null;
      localStorage.removeItem('jwt');
      this.router.navigate(['/login']);
    } catch (err: any) {
      const msg = typeof err?.error === 'string' ? err.error : err?.error?.error ?? null;
      this.deleteError.set(msg ?? 'No se pudo eliminar la cuenta. Inténtalo de nuevo.');
    } finally {
      this.deleteLoading.set(false);
    }
  }

  // Equipo favorito
  protected openFavoriteModal(): void {
    this.favoriteActionError.set('');
    this.favoriteModalOpen.set(true);
  }

  protected closeFavoriteModal(): void {
    if (!this.favoriteActionLoading()) {
      this.favoriteModalOpen.set(false);
      this.favoriteActionError.set('');
    }
  }

  protected async onFavoriteSelect(teamId: number): Promise<void> {
    if (this.favoriteTeamId() === teamId) { this.favoriteModalOpen.set(false); return; }

    const userId = this.auth.currentUserId();
    if (!userId) { this.favoriteActionError.set('No se pudo identificar el usuario.'); return; }

    this.favoriteActionLoading.set(true);
    this.favoriteActionError.set('');
    try {
      await this.api.put(`users/FavoriteTeam?id=${userId}`, { favoriteTeamId: teamId });
      this.favoriteTeamId.set(teamId);
      this.favoriteModalOpen.set(false);
    } catch {
      this.favoriteActionError.set('No se pudo actualizar el equipo favorito. Intenta de nuevo.');
    } finally {
      this.favoriteActionLoading.set(false);
    }
  }

  // Navegación
  protected navigateToTeamBuilder(): void {
    this.router.navigate(['/team-builder']);
  }

  protected logout(): void {
    this.auth.jwt = null;
    localStorage.removeItem('jwt');
    this.router.navigate(['/login']);
  }
}