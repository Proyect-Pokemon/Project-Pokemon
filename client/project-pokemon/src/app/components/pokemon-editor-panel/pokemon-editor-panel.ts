import { Component, Input, Output, EventEmitter, inject, signal, effect, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PokemonService } from '../../services/pokemon-service';
import { Pokemon } from '../../models/pokemon';
import { PostPokemonTeamDto } from '../../models/pokemon-team';
import { Movement } from '../../models/move';
import { MovementService } from '../../services/movement-service';
import { PokemonStatsDialog } from '../pokemon-stats-dialog/pokemon-stats-dialog';
import { PokemonNatureSelector } from './pokemon-nature-selector/pokemon-nature-selector';
import { PokemonMovesGrid } from './pokemon-moves-grid/pokemon-moves-grid';
import { PokemonPreview } from './pokemon-preview/pokemon-preview';
import { PokemonStats } from '../../models/pokemon-stats';

@Component({
    selector: 'app-pokemon-editor-panel',
    standalone: true,
    imports: [CommonModule, PokemonStatsDialog, PokemonNatureSelector, PokemonMovesGrid, PokemonPreview],
    templateUrl: './pokemon-editor-panel.html',
    styleUrls: ['./pokemon-editor-panel.css'],
})
export class PokemonEditorPanel {
    @ViewChild('searchInput') searchInput?: HTMLInputElement;
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
            this.loadInitialPokemonList();
            // Hacer focus en el input de búsqueda
            setTimeout(() => {
                this.searchInput?.focus();
            }, 0);
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

    searchQuery = signal<string>('');
    searchResults = signal<Pokemon[]>([]);
    selectedPokemonFromSearch = signal<Pokemon | null>(null);
    searchError = signal<string | null>(null);
    allPokemonCache = signal<Pokemon[]>([]);
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
        this.loadPokemonCache();

        effect(() => {
            const query = this.searchQuery();
            const cache = this.allPokemonCache();
            
            if (cache.length === 0) {
                return;
            }

            this.searchError.set(null);

            // Si está vacío, mostrar todos los pokemon
            if (!query || query.trim() === '') {
                this.searchResults.set([...cache].sort((a, b) => a.id - b.id));
                return;
            }

            // Intentar convertir a número
            const numericId = parseInt(query);
            
            if (!isNaN(numericId)) {
                // Búsqueda por ID
                const found = cache.filter(p => p.id === numericId);
                if (found.length === 0) {
                    this.searchError.set('No se encontró Pokémon con ese número.');
                    this.searchResults.set([]);
                } else {
                    this.searchResults.set(found);
                    // Si solo hay un resultado, seleccionarlo automáticamente
                    if (found.length === 1) {
                        this.selectedPokemonFromSearch.set(found[0]);
                    }
                }
            } else {
                // Búsqueda por nombre (case-insensitive, contains)
                const normalizedQuery = query.toLowerCase().trim();
                const found = cache.filter(p => p.name.toLowerCase().includes(normalizedQuery));
                
                if (found.length === 0) {
                    this.searchError.set('No se encontraron Pokémon con ese nombre.');
                    this.searchResults.set([]);
                } else {
                    this.searchResults.set(found.sort((a, b) => a.id - b.id));
                    // Si solo hay un resultado, seleccionarlo automáticamente
                    if (found.length === 1) {
                        this.selectedPokemonFromSearch.set(found[0]);
                    }
                }
            }
        });

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

    private async loadPokemonCache() {
        if (this.allPokemonCache().length > 0) {
            return;
        }
        const allPokemon = await this.pokemonService.getAllPokemon();
        this.allPokemonCache.set(allPokemon);
    }

    private async loadInitialPokemonList() {
        // Asegurar que el cache esté cargado
        await this.loadPokemonCache();
        
        // Si la búsqueda está vacía, mostrar todos los Pokémon
        if (!this.searchQuery() || this.searchQuery().trim() === '') {
            this.searchResults.set([...this.allPokemonCache()].sort((a, b) => a.id - b.id));
        }
    }

    private async loadMovementsCache() {
        if (this.allMovementsCache().length > 0) {
            return;
        }
        const allMovements = await this.movementService.getAllMovements();
        this.allMovementsCache.set(allMovements);
    }


    onSearchInput(event: Event) {
        const target = event.target as HTMLInputElement | null;
        if (!target) {
            this.searchQuery.set('');
            return;
        }

        // Limpiar la selección cuando el usuario escribe
        if (this.selectedPokemonFromSearch()) {
            this.selectedPokemonFromSearch.set(null);
        }

        this.searchQuery.set(target.value);
    }

    selectPokemonFromGrid(pokemon: Pokemon) {
        this.selectedPokemonFromSearch.set(pokemon);
    }

    onSelectedNatureChanged(natureId: number) {
        this.selectedNatureId.set(natureId);
    }

    onCreatePokemonTeam() {
        const foundPokemon = this.selectedPokemonFromSearch();
        if (!foundPokemon) {
            return;
        }

        this.createPokemonTeam.emit({
            nickname: null,
            shiny: false,
            slot: this.slot,
            teamId: this.teamId,
            pokemonId: foundPokemon.id,
            natureId: this.selectedNatureId(),
            movementId1: 1,
            movementId2: null,
            movementId3: null,
            movementId4: null,
        });
    }

    onClose() {
        this.close.emit();
        this.resetPanelUiState();
    }

    private resetPanelUiState() {
        this.searchQuery.set('');
        this.searchResults.set([]);
        this.selectedPokemonFromSearch.set(null);
        this.searchError.set(null);
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
        let pokemon: Pokemon | null = null;

        // Si está buscando, use el Pokémon encontrado
        if (this.selectedPokemonFromSearch()) {
            pokemon = this.selectedPokemonFromSearch();
        } else if (this.pokemonId) {
            // Si tiene un Pokémon seleccionado, búscalo en el cache
            const cache = this.allPokemonCache();
            pokemon = cache.find(p => p.id === this.pokemonId) ?? null;

            // Si no está en cache, cargalo
            if (!pokemon) {
                const allPokemon = await this.pokemonService.getAllPokemon();
                pokemon = allPokemon.find(p => p.id === this.pokemonId) ?? null;
            }
        }

        // Extraer stats si el Pokémon se encontró
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

        this.showStatsDialog.set(true);
    }

    closeStatsDialog() {
        this.showStatsDialog.set(false);
        this.currentPokemonStats.set(null);
    }
}
