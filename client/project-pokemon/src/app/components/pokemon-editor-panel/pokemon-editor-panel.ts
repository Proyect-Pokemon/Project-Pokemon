import { Component, Input, Output, EventEmitter, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PokemonService } from '../../services/pokemon-service';
import { Pokemon } from '../../models/pokemon';
import { PostPokemonTeamDto } from '../../models/pokemon-team';

@Component({
    selector: 'app-pokemon-editor-panel',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-editor-panel.html',
    styleUrls: ['./pokemon-editor-panel.css'],
})
export class PokemonEditorPanel {
    private readonly pokemonService = inject(PokemonService);

    @Input() isOpen = false;
    @Input() teamId = 0;
    @Input() slot: number = 1;
    @Input() pokemonDisplayName: string | null = null;
    @Input() pokemonSprite: string | null = null;
    @Input() pokemonId: number | null = null;
    @Output() close = new EventEmitter<void>();
    @Output() createPokemonTeam = new EventEmitter<PostPokemonTeamDto>();
    @Output() changeSlot = new EventEmitter<number>();

    showSearchControls = signal(false);
    searchPokemonNumber = signal<number | null>(null);
    searchedPokemon = signal<Pokemon | null>(null);
    searchError = signal<string | null>(null);
    allPokemonCache = signal<Pokemon[]>([]);
    animationDirection = signal<'left' | 'right' | 'leftIn' | 'rightIn' | 'none'>('none');

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
    }

    private async loadPokemonCache() {
        if (this.allPokemonCache().length > 0) {
            return;
        }
        const allPokemon = await this.pokemonService.getAllPokemon();
        this.allPokemonCache.set(allPokemon);
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
}
