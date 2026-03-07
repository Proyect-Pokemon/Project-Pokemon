import { Component, Output, EventEmitter, ViewChild, signal, effect, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PokemonService } from '../../../services/pokemon-service';
import { Pokemon } from '../../../models/pokemon';
import { inject } from '@angular/core';

@Component({
    selector: 'app-pokemon-search-panel',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-search-panel.html',
    styleUrls: ['./pokemon-search-panel.css'],
})
export class PokemonSearchPanel {
    @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

    private readonly pokemonService = inject(PokemonService);

    @Output() pokemonSelected = new EventEmitter<Pokemon>();

    searchQuery = signal<string>('');
    searchResults = signal<Pokemon[]>([]);
    selectedPokemon = signal<Pokemon | null>(null);
    searchError = signal<string | null>(null);
    allPokemonCache = signal<Pokemon[]>([]);

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
                        this.selectedPokemon.set(found[0]);
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
                        this.selectedPokemon.set(found[0]);
                    }
                }
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

    async loadInitialPokemonList() {
        // Asegurar que el cache esté cargado
        await this.loadPokemonCache();
        
        // Si la búsqueda está vacía, mostrar todos los Pokémon
        if (!this.searchQuery() || this.searchQuery().trim() === '') {
            this.searchResults.set([...this.allPokemonCache()].sort((a, b) => a.id - b.id));
        }
    }

    onSearchInput(event: Event) {
        const target = event.target as HTMLInputElement | null;
        if (!target) {
            this.searchQuery.set('');
            return;
        }

        // Limpiar la selección cuando el usuario escribe
        if (this.selectedPokemon()) {
            this.selectedPokemon.set(null);
        }

        this.searchQuery.set(target.value);
    }

    selectPokemonFromGrid(pokemon: Pokemon) {
        this.selectedPokemon.set(pokemon);
    }

    onAddPokemon() {
        const pokemon = this.selectedPokemon();
        if (pokemon) {
            this.pokemonSelected.emit(pokemon);
        }
    }

    reset() {
        this.searchQuery.set('');
        this.searchResults.set([]);
        this.selectedPokemon.set(null);
        this.searchError.set(null);
    }

    focusSearchInput() {
        setTimeout(() => {
            this.searchInput?.nativeElement.focus();
        }, 0);
    }
}
