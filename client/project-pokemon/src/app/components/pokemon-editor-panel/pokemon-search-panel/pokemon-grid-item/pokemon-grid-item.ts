import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Pokemon } from '../../../../models/pokemon';

@Component({
    selector: 'app-pokemon-grid-item',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-grid-item.html',
    styleUrls: ['./pokemon-grid-item.css'],
})
export class PokemonGridItem {
    @Input() pokemon!: Pokemon;
    @Output() pokeSelected = new EventEmitter<Pokemon>();

    onSelect() {
        this.pokeSelected.emit(this.pokemon);
    }
}
