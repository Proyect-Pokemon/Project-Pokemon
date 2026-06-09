import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-profile-avatar-modal',
  imports: [],
  templateUrl: './profile-avatar-modal.html',
  styleUrl: './profile-avatar-modal.css',
})
export class ProfileAvatarModal {
  readonly avatarOptions = input<string[]>([]);
  readonly loading = input(false);
  readonly error = input('');

  readonly select = output<string>();
  readonly close = output<void>();

  protected onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.loading()) {
      this.close.emit();
    }
  }
}