import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-profile-biography-modal',
  imports: [],
  templateUrl: './profile-biography-modal.html',
  styleUrl: './profile-biography-modal.css',
})
export class ProfileBiographyModal {
  readonly draft = input('');
  readonly loading = input(false);
  readonly error = input('');

  readonly draftChange = output<string>();
  readonly save = output<void>();
  readonly close = output<void>();

  protected onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.loading()) {
      this.close.emit();
    }
  }

  protected onInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement | null;
    if (target) {
      const value = target.value.substring(0, 200);
      target.value = value;
      this.draftChange.emit(value);
    }
  }
}