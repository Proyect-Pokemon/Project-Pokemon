import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
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
    @Input() allMovements: Movement[] = [];
    @Output() movementChanged = new EventEmitter<{ index: number; movementId: number | null }>();

    openPickerIndex = signal<number | null>(null);
    pickerSearchQuery = signal('');

    filteredMovements = computed(() => {
        const query = this.pickerSearchQuery().toLowerCase().trim();
        if (!query) return this.allMovements;
        return this.allMovements.filter(m =>
            m.name.toLowerCase().includes(query) ||
            m.type.toLowerCase().includes(query)
        );
    });

    onMoveButtonClick(index: number): void {
        this.pickerSearchQuery.set('');
        this.openPickerIndex.set(this.openPickerIndex() === index ? null : index);
    }

    onSelectMovement(movement: Movement | null): void {
        const index = this.openPickerIndex();
        if (index === null) return;
        this.movementChanged.emit({ index, movementId: movement?.id ?? null });
        this.openPickerIndex.set(null);
        this.pickerSearchQuery.set('');
    }

    closePicker(): void {
        this.openPickerIndex.set(null);
        this.pickerSearchQuery.set('');
    }

    getTypeClass(type: string): string {
        return 'type-' + type.toLowerCase();
    }

    getClassLabel(movementClass: string): string {
        const classMap: Record<string, string> = {
            'Physical': 'Físico',
            'Special': 'Especial',
            'Status': 'Estado',
        };
        return classMap[movementClass] ?? movementClass;
    }

    onTypeIconLoad(event: Event): void {
        (event.target as HTMLImageElement).style.display = '';
    }

    onTypeIconError(event: Event): void {
        (event.target as HTMLImageElement).style.display = 'none';
    }

    private readonly TYPE_ICON_MAP: Record<string, string> = {
        grass: 'leaf',
    };

    getTypeIconSrc(type: string): string {
        const key = type.toLowerCase();
        const filename = this.TYPE_ICON_MAP[key] ?? key;
        return `assets/icons/types/${filename}.svg`;
    }
}
