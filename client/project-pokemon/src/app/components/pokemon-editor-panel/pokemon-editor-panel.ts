import { Component, Input, Output, EventEmitter, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PokemonService } from '../../services/pokemon-service';
import { Pokemon } from '../../models/pokemon';
import { PostPokemonTeamDto } from '../../models/pokemon-team';
import { Movement } from '../../models/move';
import { MovementService } from '../../services/movement-service';
import { PokemonStatsDialog } from '../pokemon-stats-dialog/pokemon-stats-dialog';

interface PokemonStats {
    hp: number;
    attack: number;
    defense: number;
    specialAttack: number;
    specialDefense: number;
    speed: number;
}

@Component({
    selector: 'app-pokemon-editor-panel',
    standalone: true,
    imports: [CommonModule, PokemonStatsDialog],
    templateUrl: './pokemon-editor-panel.html',
    styleUrls: ['./pokemon-editor-panel.css'],
})
export class PokemonEditorPanel {
    private readonly pokemonService = inject(PokemonService);
    private readonly movementService = inject(MovementService);

    @Input() isOpen = false;
    @Input() teamId = 0;
    @Input() slot: number = 1;
    @Input() pokemonDisplayName: string | null = null;
    @Input() pokemonSprite: string | null = null;
    @Input() pokemonId: number | null = null;
    @Input() set movementIds(value: (number | null)[]) {
        this.movementIdsSignal.set(value);
    }
    @Output() close = new EventEmitter<void>();
    @Output() createPokemonTeam = new EventEmitter<PostPokemonTeamDto>();
    @Output() changeSlot = new EventEmitter<number>();

    showSearchControls = signal(false);
    searchPokemonNumber = signal<number | null>(null);
    searchedPokemon = signal<Pokemon | null>(null);
    searchError = signal<string | null>(null);
    allPokemonCache = signal<Pokemon[]>([]);
    animationDirection = signal<'left' | 'right' | 'leftIn' | 'rightIn' | 'none'>('none');
    movements = signal<(Movement | null)[]>([null, null, null, null]);
    allMovementsCache = signal<Movement[]>([]);
    movementIdsSignal = signal<(number | null)[]>([null, null, null, null]);
    showStatsDialog = signal(false);
    currentPokemonStats = signal<PokemonStats | null>(null);

    get isEasterEggSlot(): boolean {
        return this.slot <= 0 || this.slot >= 7;
    }

    constructor() {
        effect(() => {
            if (this.showSearchControls()) {
                this.loadPokemonCache();
            }
        });

        effect(async () => {
            const pokemonNumber = this.searchPokemonNumber();
            if (!pokemonNumber) {
                this.searchedPokemon.set(null);
                this.searchError.set(null);
                return;
            }

            this.searchError.set(null);
            const cache = this.allPokemonCache();
            const found = cache.find(p => p.id === pokemonNumber) ?? null;

            if (!found) {
                this.searchedPokemon.set(null);
                this.searchError.set('No se encontró Pokémon con ese número.');
            } else {
                this.searchedPokemon.set(found);
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

    private async loadMovementsCache() {
        if (this.allMovementsCache().length > 0) {
            return;
        }
        const allMovements = await this.movementService.getAllMovements();
        this.allMovementsCache.set(allMovements);
    }

    toggleSearchControls() {
        this.showSearchControls.update(value => !value);
        this.searchError.set(null);
        if (!this.showSearchControls()) {
            this.searchPokemonNumber.set(null);
            this.searchedPokemon.set(null);
        }
    }

    onSearchNumberInput(event: Event) {
        const target = event.target as HTMLInputElement | null;
        if (!target) {
            this.searchPokemonNumber.set(null);
            return;
        }

        const value = Number(target.value);
        this.searchPokemonNumber.set(Number.isNaN(value) || value <= 0 ? null : value);
    }

    onCreatePokemonTeam() {
        const foundPokemon = this.searchedPokemon();
        if (!foundPokemon) {
            return;
        }

        this.createPokemonTeam.emit({
            nickname: null,
            shiny: false,
            slot: this.slot,
            teamId: this.teamId,
            pokemonId: foundPokemon.id,
            natureId: 1,
            movementId1: 1,
            movementId2: null,
            movementId3: null,
            movementId4: null,
        });
    }

    onClose() {
        this.close.emit();

        this.showSearchControls.set(false);
        this.searchPokemonNumber.set(null);
        this.searchedPokemon.set(null);
        this.searchError.set(null);
        this.showStatsDialog.set(false);
        this.currentPokemonStats.set(null);
    }

    onPreviousSlot() {
        if (this.slot > -3) {
            this.animationDirection.set('right');
            setTimeout(() => {
                this.changeSlot.emit(this.slot - 1);
                this.animationDirection.set('rightIn');
                setTimeout(() => {
                    this.animationDirection.set('none');
                }, 300);
            }, 300);
        }
    }

    onNextSlot() {
        if (this.slot < 10) {
            this.animationDirection.set('left');
            setTimeout(() => {
                this.changeSlot.emit(this.slot + 1);
                this.animationDirection.set('leftIn');
                setTimeout(() => {
                    this.animationDirection.set('none');
                }, 300);
            }, 300);
        }
    }

    async openStatsDialog() {
        // Obtener stats del Pokémon actual
        let pokemon: Pokemon | null = null;

        // Si está buscando, use el Pokémon encontrado
        if (this.searchedPokemon()) {
            pokemon = this.searchedPokemon();
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
