import { Component, Input, Output, EventEmitter, signal, computed, ViewChild, ElementRef, effect } from '@angular/core';
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
    @ViewChild('pickerSearchInput') pickerSearchInput?: ElementRef<HTMLInputElement>;
    @Input() movements: (Movement | null)[] = [null, null, null, null];
    @Input() allMovements: Movement[] = [];
    @Output() movementChanged = new EventEmitter<{ index: number; movementId: number | null }>();

    openPickerIndex = signal<number | null>(null);
    pickerSearchQuery = signal('');

    filteredMovements = computed(() => {
        const query = this.normalizeSearchText(this.pickerSearchQuery());
        if (!query) return this.allMovements;
        return this.allMovements.filter(m =>
            this.normalizeSearchText(m.name).includes(query) ||
            this.normalizeSearchText(m.type).includes(query)
        );
    });

    constructor() {
        effect(() => {
            if (this.openPickerIndex() === null) {
                return;
            }

            setTimeout(() => {
                this.pickerSearchInput?.nativeElement.focus();
            }, 0);
        });
    }

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
        return 'type-' + this.normalizeTypeKey(type);
    }

    getClassLabel(movementClass: string): string {
        const classMap: Record<string, string> = {
            'Physical': 'Físico',
            'Special': 'Especial',
            'Status': 'Estado',
        };
        return classMap[movementClass] ?? movementClass;
    }

    getTypeLabel(type: string): string {
        const normalizedType = this.normalizeTypeKey(type);
        const typeMap: Record<string, string> = {
            normal: 'Normal',
            fire: 'Fuego',
            water: 'Agua',
            electric: 'Eléctrico',
            grass: 'Planta',
            ice: 'Hielo',
            fighting: 'Lucha',
            poison: 'Veneno',
            ground: 'Tierra',
            flying: 'Volador',
            psychic: 'Psíquico',
            bug: 'Bicho',
            rock: 'Roca',
            ghost: 'Fantasma',
            dragon: 'Dragón',
            dark: 'Siniestro',
            steel: 'Acero',
            fairy: 'Hada',
        };

        return typeMap[normalizedType] ?? type;
    }

    onTypeIconLoad(event: Event): void {
        (event.target as HTMLImageElement).style.display = '';
    }

    onTypeIconError(event: Event): void {
        (event.target as HTMLImageElement).style.display = 'none';
    }

    private readonly TYPE_ICON_MAP: Record<string, string> = {
        normal: 'normal',
        fire: 'fire',
        water: 'water',
        electric: 'electric',
        grass: 'leaf',
        planta: 'leaf',
        ice: 'ice',
        fighting: 'fighting',
        poison: 'poison',
        ground: 'ground',
        flying: 'flying',
        psychic: 'psychic',
        psiquico: 'psychic',
        bug: 'bug',
        rock: 'rock',
        ghost: 'ghost',
        dragon: 'dragon',
        dark: 'dark',
        steel: 'steel',
        fairy: 'fairy',
    };

    getTypeIconSrc(type: string): string {
        const key = this.normalizeTypeKey(type);
        const filename = this.TYPE_ICON_MAP[key] ?? key;
        return `/assets/icons/types/${filename}.svg`;
    }

    private normalizeSearchText(value: string): string {
        return value
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .trim();
    }

    private normalizeTypeKey(type: string): string {
        return type
            ?.normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .trim();
    }
}
