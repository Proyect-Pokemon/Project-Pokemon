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

    // Ahora emite el array completo de 4 ids tras aplicar deduplicación y compactación
    @Output() movementsChanged = new EventEmitter<(number | null)[]>();

    openPickerIndex = signal<number | null>(null);
    pickerSearchQuery = signal('');

    filteredMovements = computed(() => {
        const query = this.normalizeSearchText(this.pickerSearchQuery());
        if (!query) return this.allMovements;
        return this.allMovements.filter((movement) =>
            this.getMovementSearchTokens(movement).some((token) => token.includes(query))
        );
    });

    constructor() {
        effect(() => {
            if (this.openPickerIndex() === null) return;
            setTimeout(() => this.pickerSearchInput?.nativeElement.focus(), 0);
        });
    }

    onMoveButtonClick(index: number): void {
        this.pickerSearchQuery.set('');
        this.openPickerIndex.set(this.openPickerIndex() === index ? null : index);
    }

    onSelectMovement(movement: Movement | null): void {
        const index = this.openPickerIndex();
        if (index === null) return;

        // Array actual de ids (null = vacío)
        const current = this.movements.map((m) => m?.id ?? null);
        const next: (number | null)[] = [...current];

        if (movement === null) {
            // Quitar movimiento del slot
            next[index] = null;
        } else {
            // Si el movimiento ya existe en otro slot, lo quitamos de allí
            const existingSlot = next.findIndex((id) => id === movement.id);
            if (existingSlot !== -1 && existingSlot !== index) {
                next[existingSlot] = null;
            }
            next[index] = movement.id;
        }

        // Compactar: mover los no-nulos al frente, rellenar el resto con null
        const filled = next.filter((id): id is number => id !== null);
        const compacted: (number | null)[] = [
            ...filled,
            ...Array(4 - filled.length).fill(null),
        ];

        this.movementsChanged.emit(compacted);
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
            'Physical': 'Fís',
            'Special': 'Esp',
            'Status': 'Est',
        };
        return classMap[movementClass] ?? movementClass;
    }

    getTypeLabel(type: string): string {
        const normalizedType = this.normalizeTypeKey(type);
        const typeMap: Record<string, string> = {
            normal: 'Normal', fire: 'Fuego', water: 'Agua', electric: 'Eléctrico',
            grass: 'Planta', ice: 'Hielo', fighting: 'Lucha', poison: 'Veneno',
            ground: 'Tierra', flying: 'Volador', psychic: 'Psíquico', bug: 'Bicho',
            rock: 'Roca', ghost: 'Fantasma', dragon: 'Dragón', dark: 'Siniestro',
            steel: 'Acero', fairy: 'Hada',
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
        normal: 'normal', fire: 'fire', water: 'water', electric: 'electric',
        grass: 'leaf', planta: 'leaf', ice: 'ice', fighting: 'fighting',
        poison: 'poison', ground: 'ground', flying: 'flying', psychic: 'psychic',
        psiquico: 'psychic', bug: 'bug', rock: 'rock', ghost: 'ghost',
        dragon: 'dragon', dark: 'dark', steel: 'steel', fairy: 'fairy',
    };

    private readonly TYPE_SEARCH_ALIASES: Record<string, string[]> = {
        normal: ['normal'], fire: ['fire', 'fuego'], water: ['water', 'agua'],
        electric: ['electric', 'electrico'], grass: ['grass', 'planta'],
        ice: ['ice', 'hielo'], fighting: ['fighting', 'lucha'],
        poison: ['poison', 'veneno'], ground: ['ground', 'tierra'],
        flying: ['flying', 'volador'], psychic: ['psychic', 'psiquico'],
        bug: ['bug', 'bicho'], rock: ['rock', 'roca'], ghost: ['ghost', 'fantasma'],
        dragon: ['dragon', 'dragon'], dark: ['dark', 'siniestro'],
        steel: ['steel', 'acero'], fairy: ['fairy', 'hada'],
    };

    getTypeIconSrc(type: string): string {
        const key = this.normalizeTypeKey(type);
        const filename = this.TYPE_ICON_MAP[key] ?? key;
        return `/assets/icons/types/${filename}.svg`;
    }

    private getMovementSearchTokens(movement: Movement): string[] {
        const normalizedType = this.normalizeTypeKey(movement.type);
        const typeAliases = this.TYPE_SEARCH_ALIASES[normalizedType] ?? [normalizedType];
        return [
            this.normalizeSearchText(movement.name),
            this.normalizeSearchText(movement.type),
            this.normalizeSearchText(this.getTypeLabel(movement.type)),
            ...typeAliases.map((alias) => this.normalizeSearchText(alias)),
        ];
    }

    private normalizeSearchText(value: string): string {
        return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
    }

    private normalizeTypeKey(type: string): string {
        return type?.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
    }
}