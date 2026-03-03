import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Team, MAX_POKEMON_PER_TEAM } from '../../models/team';
import { PokemonTeam } from '../../models/pokemon-team';

@Component({
    selector: 'app-team-slot',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './team-slot.html',
    styleUrl: './team-slot.css',
})
export class TeamSlot {
    @Input() team!: Team;
    @Output() toggleExpand = new EventEmitter<number>();
    @Output() nameChange = new EventEmitter<{ id: number, name: string }>();
    @Output() addPokemon = new EventEmitter<{ teamId: number, slot: number }>();

    readonly maxSlots = MAX_POKEMON_PER_TEAM;

    onToggle() {
        this.toggleExpand.emit(this.team.id);
    }

    onNameEdit(newName: string) {
        if (newName.trim()) {
            this.nameChange.emit({ id: this.team.id, name: newName.trim() });
        }
    }

    onAddPokemon(slot: number) {
        this.addPokemon.emit({ teamId: this.team.id, slot });
    }

    getPokemonSlots(): (PokemonTeam | null)[] {
        const slots: (PokemonTeam | null)[] = Array(this.maxSlots).fill(null);
        
        this.team.pokemons.forEach(pokemon => {
            if (pokemon.slot >= 1 && pokemon.slot <= this.maxSlots) {
                slots[pokemon.slot - 1] = pokemon;
            }
        });
        
        return slots;
    }

    getSlotNumber(index: number): number {
        return index + 1;
    }
}
