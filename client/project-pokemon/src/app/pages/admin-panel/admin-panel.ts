import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { UserRole } from '../../models/get-admin-user-dto';
import { PutUserRoleDto } from '../../models/put-user-role-dto';
import { AdminUserRow } from '../../models/admin-user-row';
import { AdminUsersList } from '../../components/admin-users-list/admin-users-list';
import { AdminDeleteUserModal } from '../../components/admin-delete-user-modal/admin-delete-user-modal';
import { AdminService } from '../../services/admin-service';
import { AdminSearch } from '../../components/admin-search/admin-search';

@Component({
  selector: 'app-admin-panel',
  imports: [AdminUsersList, AdminDeleteUserModal, AdminSearch],
  templateUrl: './admin-panel.html',
  styleUrl: './admin-panel.css',
})
export class AdminPanel implements OnInit {
  private readonly adminService = inject(AdminService);

  protected readonly users = signal<AdminUserRow[]>([]);
  protected readonly loadingUsers = signal(false);
  protected readonly loadError = signal('');
  protected readonly actionMessage = signal('');
  protected readonly searchQuery = signal('');

  protected readonly selectedUserForDeletion = signal<AdminUserRow | null>(null);
  protected readonly deleteNicknameInput = signal('');
  protected readonly updatingRoleIds = signal<number[]>([]);
  protected readonly deletingUserId = signal<number | null>(null);

  protected readonly isRoleUpdatingFn = (userId: number): boolean =>
    this.updatingRoleIds().includes(userId);
  protected readonly isDeletingUserFn = (userId: number): boolean =>
    this.deletingUserId() === userId;

  protected readonly totalUsers = computed(() => this.users().length);
  protected readonly totalAdmins = computed(
    () => this.users().filter((u) => u.role === 'admin').length
  );
  protected readonly totalStandardUsers = computed(
    () => this.totalUsers() - this.totalAdmins()
  );

  protected readonly filteredUsers = computed(() => {
    const query = this.normalize(this.searchQuery());
    if (!query) return this.users();
    return this.users().filter(
      (u) =>
        this.normalize(u.nickname).includes(query) ||
        this.normalize(u.email).includes(query)
    );
  });

  protected readonly canConfirmDeletion = computed(() => {
    const user = this.selectedUserForDeletion();
    if (!user) return false;
    return this.deleteNicknameInput().trim() === user.nickname;
  });

  protected readonly deletingSelectedUser = computed(() => {
    const user = this.selectedUserForDeletion();
    if (!user) return false;
    return this.deletingUserId() === user.id;
  });

  ngOnInit(): void {
    void this.loadUsers();
  }

  protected reloadUsers(): void {
    void this.loadUsers();
  }

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected clearSearch(): void {
    this.searchQuery.set('');
  }

  protected async onRoleChange(event: { userId: number; role: UserRole }): Promise<void> {
    const { userId, role: selectedRole } = event;

    if (this.isRoleUpdatingFn(userId) || this.isDeletingUserFn(userId)) return;

    const currentUser = this.users().find((u) => u.id === userId);
    if (!currentUser || currentUser.role === selectedRole) return;

    if (currentUser.role === 'admin' && selectedRole === 'user' && this.totalAdmins() === 1) {
      this.actionMessage.set('Debe existir al menos un administrador en el sistema.');
      return;
    }

    const dto: PutUserRoleDto = { role: selectedRole };
    this.markRoleUpdating(userId, true);

    try {
      await this.adminService.updateUserRole(userId, dto);
      this.users.update((users) =>
        users.map((u) => (u.id === userId ? { ...u, role: selectedRole } : u))
      );
      this.actionMessage.set(
        `Rol actualizado: ${currentUser.nickname} ahora es ${selectedRole}.`
      );
    } catch (err: any) {
      this.actionMessage.set(this.extractError(err, 'No se pudo actualizar el rol.'));
    } finally {
      this.markRoleUpdating(userId, false);
    }
  }

  protected openDeleteModal(user: AdminUserRow): void {
    if (this.deletingUserId() !== null) return;
    this.selectedUserForDeletion.set(user);
    this.deleteNicknameInput.set('');
  }

  protected closeDeleteModal(force = false): void {
    if (!force && this.deletingUserId() !== null) return;
    this.selectedUserForDeletion.set(null);
    this.deleteNicknameInput.set('');
  }

  protected onDeleteNicknameInput(value: string): void {
    this.deleteNicknameInput.set(value);
  }

  protected async confirmDeleteUser(): Promise<void> {
    const user = this.selectedUserForDeletion();
    if (!user || !this.canConfirmDeletion() || this.deletingUserId() !== null) return;

    if (user.role === 'admin' && this.totalAdmins() === 1) {
      this.actionMessage.set('No se puede eliminar el único usuario administrador del sistema.');
      return;
    }

    this.deletingUserId.set(user.id);

    try {
      await this.adminService.deleteUser(user.id);
      this.users.update((users) => users.filter((u) => u.id !== user.id));
      this.actionMessage.set(`Usuario eliminado: ${user.nickname}.`);
      this.closeDeleteModal(true);
    } catch (err: any) {
      this.actionMessage.set(this.extractError(err, 'No se pudo eliminar el usuario.'));
    } finally {
      this.deletingUserId.set(null);
    }
  }

  protected readonly isLastAdmin = (userId: number): boolean => {
    const user = this.users().find((u) => u.id === userId);
    return !!user && user.role === 'admin' && this.totalAdmins() === 1;
  };

  private async loadUsers(): Promise<void> {
    this.loadingUsers.set(true);
    this.loadError.set('');
    this.actionMessage.set('');

    try {
      const users = await this.adminService.getAllUsers();
      this.users.set(
        users.map((u) => ({
          id: u.id,
          nickname: u.nickname,
          email: u.email,
          avatarUrl: this.adminService.buildAvatarUrl(u.avatarPath),
          role: u.role?.trim().toLowerCase() === 'admin' ? 'admin' : 'user',
          creationDate: u.creationDate,
        }))
      );
    } catch (err: any) {
      this.users.set([]);
      this.loadError.set(this.extractError(err, 'No se pudo cargar la lista de usuarios.'));
    } finally {
      this.loadingUsers.set(false);
    }
  }

  private normalize(value: string): string {
    return (value ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  private markRoleUpdating(userId: number, inProgress: boolean): void {
    this.updatingRoleIds.update((ids) => {
      if (inProgress) return ids.includes(userId) ? ids : [...ids, userId];
      return ids.filter((id) => id !== userId);
    });
  }

  private extractError(err: any, fallback: string): string {
    const status = err?.status;

    const statusMessages: Record<number, string> = {
      400: 'Datos inválidos. Comprueba la información e inténtalo de nuevo.',
      401: 'No tienes permiso para realizar esta acción. Inicia sesión de nuevo.',
      403: 'Acceso denegado. No tienes permisos suficientes.',
      404: 'El usuario no fue encontrado.',
      405: 'Operación no permitida. Contacta con el administrador del sistema.',
      409: 'Conflicto: el cambio no se pudo aplicar porque hay datos en conflicto.',
      500: 'Error interno del servidor. Inténtalo de nuevo más tarde.',
    };

    if (status && statusMessages[status]) {
      return statusMessages[status];
    }

    const msg =
      typeof err?.error === 'string'
        ? err.error
        : err?.error?.message || err?.error?.error || err?.message;

    if (!msg || typeof msg !== 'string') return fallback;
    const clean = msg.trim();
    return clean.length > 0 ? clean : fallback;
  }
}