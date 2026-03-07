import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Movement } from '../../../models/move';

@Component({
    selector: 'app-pokemon-moves-grid',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-moves-grid.html',
    styleUrls: ['./pokemon-moves-grid.css'],
})
export class PokemonMovesGrid {
    @Input() movements: (Movement | null)[] = [null, null, null, null];
}
