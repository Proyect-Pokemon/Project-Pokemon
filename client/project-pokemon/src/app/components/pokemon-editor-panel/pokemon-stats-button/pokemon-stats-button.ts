import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-pokemon-stats-button',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-stats-button.html',
    styleUrls: ['./pokemon-stats-button.css'],
})
export class PokemonStatsButton {
    @Output() click = new EventEmitter<void>();

    onStatsClick() {
        this.click.emit();
    }
}
