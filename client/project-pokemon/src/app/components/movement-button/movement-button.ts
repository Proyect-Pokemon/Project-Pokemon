import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BattleMove } from '../../models/move';

@Component({
  selector: 'app-movement-button',
  imports: [],
  templateUrl: './movement-button.html',
  styleUrl: './movement-button.css',
})
export class MovementButton {
  @Input() move!: BattleMove;
  @Input() disabled: boolean = false;
  @Output() moveClicked = new EventEmitter<BattleMove>();

  private readonly TYPE_ICON_MAP: Record<string, string> = {
    normal: 'normal',
    fire: 'fire',
    water: 'water',
    electric: 'electric',
    grass: 'leaf',
    ice: 'ice',
    fighting: 'fighting',
    poison: 'poison',
    ground: 'ground',
    flying: 'flying',
    psychic: 'psychic',
    bug: 'bug',
    rock: 'rock',
    ghost: 'ghost',
    dragon: 'dragon',
    dark: 'dark',
    steel: 'steel',
    fairy: 'fairy',
  };

  onClickMove(): void {
    this.moveClicked.emit(this.move);
  }

  getTypeIconSrc(type: string): string {
    const key = type.toLowerCase();
    const filename = this.TYPE_ICON_MAP[key] ?? key;
    return `/assets/icons/types/${filename}.svg`;
  }
}
