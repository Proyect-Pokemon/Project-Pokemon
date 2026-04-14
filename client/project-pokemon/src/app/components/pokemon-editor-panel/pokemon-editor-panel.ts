import { Component, Input, Output, EventEmitter, inject, signal, effect, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Pokemon } from '../../models/pokemon';
import { PostPokemonTeamDto } from '../../models/pokemon-team';
import { Movement } from '../../models/move';
import { MovementService } from '../../services/movement-service';
import { PokemonService } from '../../services/pokemon-service';
import { PokemonStatsDialog } from '../pokemon-stats-dialog/pokemon-stats-dialog';
import { PokemonNatureSelector } from './pokemon-nature-selector/pokemon-nature-selector';
import { PokemonMovesGrid } from './pokemon-moves-grid/pokemon-moves-grid';
import { PokemonPreview } from './pokemon-preview/pokemon-preview';
import { PokemonSearchPanel } from './pokemon-search-panel/pokemon-search-panel';
import { PokemonStats } from '../../models/pokemon-stats';

@Component({
    selector: 'app-pokemon-editor-panel',
    standalone: true,
    imports: [CommonModule, PokemonStatsDialog, PokemonNatureSelector, PokemonMovesGrid, PokemonPreview, PokemonSearchPanel],
    templateUrl: './pokemon-editor-panel.html',
    styleUrls: ['./pokemon-editor-panel.css'],
})
export class PokemonEditorPanel {
    @ViewChild('searchPanel') searchPanel?: PokemonSearchPanel;
    @ViewChild('natureSelector') natureSelector?: PokemonNatureSelector;

    private readonly pokemonService = inject(PokemonService);
    private readonly movementService = inject(MovementService);
    readonly MAX_NICKNAME_LENGTH = 12;
    private readonly MIN_TEAM_SLOT = 1;
    private readonly MAX_TEAM_SLOT = 6;
    private readonly SLOT_TRANSITION_MS = 300;

    private panelOpen = false;
    private isSlotTransitioning = false;
    private pendingSlot: number | null = null;

    @Input() set isOpen(value: boolean) {
        this.panelOpen = value;
        if (!value) {
            this.resetPanelUiState();
        } else {
            // Al abrir el panel, cargar y mostrar todos los Pokémon si la búsqueda está vacía
            this.searchPanel?.loadInitialPokemonList();
            // Hacer focus en el input de búsqueda
            this.searchPanel?.focusSearchInput();
        }
    }

    get isOpen(): boolean {
        return this.panelOpen;
    }
    @Input() teamId = 0;
    @Input() slot: number = 1;
    @Input() pokemonDisplayName: string | null = null;
    @Input() pokemonSprite: string | null = null;
    @Input() pokemonId: number | null = null;
    @Input() pokemonTeamId: number | null = null;
    @Input() pokemonBaseName: string | null = null;
    @Input() pokemonNickname: string | null = null;
    @Input() natureId: number | null = null;
    @Input() set movementIds(value: (number | null)[]) {
        this.movementIdsSignal.set(value);
    }
    @Output() close = new EventEmitter<void>();
    @Output() createPokemonTeam = new EventEmitter<PostPokemonTeamDto>();
    @Output() changeSlot = new EventEmitter<number>();
    @Output() nicknameUpdated = new EventEmitter<{ pokemonTeamId: number, nickname: string | null }>();
    @Output() movementsUpdated = new EventEmitter<{ pokemonTeamId: number, movementIds: (number | null)[] }>();

    animationDirection = signal<'left' | 'right' | 'leftIn' | 'rightIn' | 'none'>('none');
    movements = signal<(Movement | null)[]>([null, null, null, null]);
    allMovementsCache = signal<Movement[]>([]);
    movementIdsSignal = signal<(number | null)[]>([null, null, null, null]);
    showStatsDialog = signal(false);
    currentPokemonStats = signal<PokemonStats | null>(null);
    selectedNatureId = signal(1);

    get isEasterEggSlot(): boolean {
        return this.slot <= 0 || this.slot >= 7;
    }

    get disablePreviousArrow(): boolean {
        const effectiveSlot = this.pendingSlot ?? this.slot;
        return effectiveSlot <= this.MIN_TEAM_SLOT;
    }

    get disableNextArrow(): boolean {
        const effectiveSlot = this.pendingSlot ?? this.slot;
        return effectiveSlot >= this.MAX_TEAM_SLOT;
    }

    constructor() {
        // Cargar cache de movimientos al inicializar
        this.loadMovementsCache();

        // Efecto para actualizar movimientos cuando los IDs cambian
        effect(() => {
            const ids = this.movementIdsSignal();
            const cache = this.allMovementsCache();
            
            const loadedMovements = ids.map(id => {
                if (!id) return null;
                return cache.find(m => m.id === id) ?? null;
            });
            
            this.movements.set(loadedMovements);
        });
    }

    private async loadMovementsCache() {
        if (this.allMovementsCache().length > 0) {
            return;
        }
        const allMovements = await this.movementService.getAllMovements();
        this.allMovementsCache.set(allMovements);
    }

    onSelectedNatureChanged(natureId: number) {
        this.selectedNatureId.set(natureId);
    }

    onMovementChanged(data: { index: number; movementId: number | null }): void {
        const ids = [...this.movementIdsSignal()];
        ids[data.index] = data.movementId;
        this.movementIdsSignal.set(ids);

        if (this.pokemonTeamId !== null) {
            this.movementsUpdated.emit({ pokemonTeamId: this.pokemonTeamId, movementIds: ids });
        }
    }

    onPokemonSelected(pokemon: Pokemon) {
        const ids = this.movementIdsSignal();
        this.createPokemonTeam.emit({
            nickname: null,
            shiny: false,
            slot: this.slot,
            teamId: this.teamId,
            pokemonId: pokemon.id,
            natureId: this.selectedNatureId(),
            movementId1: ids[0] ?? 1,
            movementId2: ids[1] ?? null,
            movementId3: ids[2] ?? null,
            movementId4: ids[3] ?? null,
        });
    }

    onClose() {
        this.close.emit();
        this.resetPanelUiState();
    }

    private resetPanelUiState() {
        this.searchPanel?.reset();
        this.showStatsDialog.set(false);
        this.currentPokemonStats.set(null);
        this.isSlotTransitioning = false;
        this.pendingSlot = null;
        this.animationDirection.set('none');
        // Reset nature selector
        if (this.natureSelector) {
            this.natureSelector.resetNatureSelection();
        }
    }

    onPreviousSlot() {
        if (this.isSlotTransitioning) {
            return;
        }

        const currentSlot = this.pendingSlot ?? this.slot;
        const targetSlot = currentSlot - 1;

        if (targetSlot < this.MIN_TEAM_SLOT) {
            return;
        }

        this.runSlotTransition(targetSlot, 'right', 'rightIn');
    }

    onNextSlot() {
        if (this.isSlotTransitioning) {
            return;
        }

        const currentSlot = this.pendingSlot ?? this.slot;
        const targetSlot = currentSlot + 1;

        if (targetSlot > this.MAX_TEAM_SLOT) {
            return;
        }

        this.runSlotTransition(targetSlot, 'left', 'leftIn');
    }

    private runSlotTransition(
        targetSlot: number,
        outDirection: 'left' | 'right',
        inDirection: 'leftIn' | 'rightIn',
    ) {
        this.isSlotTransitioning = true;
        this.pendingSlot = targetSlot;
        this.animationDirection.set(outDirection);

        setTimeout(() => {
            this.changeSlot.emit(targetSlot);
            this.animationDirection.set(inDirection);

            setTimeout(() => {
                this.animationDirection.set('none');
                this.pendingSlot = null;
                this.isSlotTransitioning = false;
            }, this.SLOT_TRANSITION_MS);
        }, this.SLOT_TRANSITION_MS);
    }

    async openStatsDialog() {
        // Obtener stats del Pokémon actual
        if (this.pokemonId) {
            try {
                const allPokemon = await this.pokemonService.getAllPokemon();
                const pokemon = allPokemon.find(p => p.id === this.pokemonId);
                if (pokemon) {
                    this.currentPokemonStats.set({
                        hp: pokemon.hp,
                        attack: pokemon.attack,
                        defense: pokemon.defense,
                        specialAttack: pokemon.specialAttack,
                        specialDefense: pokemon.specialDefense,
                        speed: pokemon.speed,
                    });
                }
            } catch (error) {
                console.error('Error loading Pokemon stats:', error);
            }
        }

        this.showStatsDialog.set(true);
    }

    closeStatsDialog() {
        this.showStatsDialog.set(false);
        this.currentPokemonStats.set(null);
    }
}
