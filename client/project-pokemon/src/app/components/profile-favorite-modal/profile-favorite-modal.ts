import { Component, input, output } from '@angular/core';

export interface TeamOption {
  id: number;
  name: string;
  pokemonCount: number;
}

@Component({
  selector: 'app-profile-favorite-modal',
  imports: [],
  templateUrl: './profile-favorite-modal.html',
  styleUrl: './profile-favorite-modal.css',
})
export class ProfileFavoriteModal {
  readonly teams = input<TeamOption[]>([]);
  readonly loading = input(false);
  readonly error = input('');

  readonly select = output<number>();
  readonly close = output<void>();

  protected onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.loading()) {
      this.close.emit();
    }
  }
}