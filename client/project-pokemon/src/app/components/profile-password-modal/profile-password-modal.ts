import { Component, computed, input, output, signal } from '@angular/core';
import { strongPasswordValidator } from '../../pages/register/register';

@Component({
  selector: 'app-profile-password-modal',
  imports: [],
  templateUrl: './profile-password-modal.html',
  styleUrl: './profile-password-modal.css',
})
export class ProfilePasswordModal {
  readonly currentPassword = input('');
  readonly newPassword = input('');
  readonly confirmPassword = input('');
  readonly loading = input(false);
  readonly error = input('');

  readonly currentPasswordChange = output<string>();
  readonly newPasswordChange = output<string>();
  readonly confirmPasswordChange = output<string>();
  readonly save = output<void>();
  readonly close = output<void>();

  // Requisitos de la nueva contraseña
  readonly reqLength   = computed(() => this.newPassword().length >= 6);
  readonly reqUppercase = computed(() => /[A-Z]/.test(this.newPassword()));
  readonly reqNumber   = computed(() => /[0-9]/.test(this.newPassword()));
  readonly reqSpecial  = computed(() => /[^A-Za-z0-9]/.test(this.newPassword()));
  readonly showReqs    = computed(() => this.newPassword().length > 0);

  protected onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.loading()) {
      this.close.emit();
    }
  }

  protected onInput(event: Event, field: 'current' | 'new' | 'confirm'): void {
    const value = (event.target as HTMLInputElement | null)?.value ?? '';
    if (field === 'current') this.currentPasswordChange.emit(value);
    else if (field === 'new') this.newPasswordChange.emit(value);
    else this.confirmPasswordChange.emit(value);
  }
}