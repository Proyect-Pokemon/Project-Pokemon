import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-pokemon-editor-panel',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-editor-panel.html',
    styleUrls: ['./pokemon-editor-panel.css'],
})
export class PokemonEditorPanel {
    @Input() isOpen = false;
    @Input() teamId = 0;
    @Input() slot: number = 1;
    @Output() close = new EventEmitter<void>();

    onClose() {
        this.close.emit();
    }
}
