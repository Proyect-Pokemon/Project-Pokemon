import { Component, input, output } from '@angular/core';
import { UserRole } from '../../models/get-admin-user-dto';
import { AdminUserRow } from '../../models/admin-user-row';

@Component({
  selector: 'app-admin-users-list',
  imports: [],
  templateUrl: './admin-users-list.html',
  styleUrl: './admin-users-list.css',
})
export class AdminUsersList {
  readonly users = input<AdminUserRow[]>([]);
  readonly isRoleUpdating = input<(userId: number) => boolean>(() => false);
  readonly isDeletingUser = input<(userId: number) => boolean>(() => false);
  readonly isLastAdmin = input<(userId: number) => boolean>(() => false);

  readonly roleChange = output<{ userId: number; role: UserRole }>();
  readonly deleteUser = output<AdminUserRow>();

  protected onRoleSelectChange(userId: number, event: Event): void {
    const target = event.target as HTMLSelectElement | null;
    if (!target) return;

    this.roleChange.emit({
      userId,
      role: target.value.trim().toLowerCase() === 'admin' ? 'admin' : 'user',
    });
  }

  protected formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('es-ES', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  }
}