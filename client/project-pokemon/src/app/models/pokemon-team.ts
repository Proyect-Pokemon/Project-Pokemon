import { Signal } from "@angular/core";
import { Move } from "./move";

export interface PokemonTeam {
    name: string;
    sprite: string;
    hp: number;
    atk: number;
    def: number;
    spa: number;
    spd: number;
    spe: number;
    type1: string;
    type2?: string;
    currentHP: Signal<number>;
    moves: Move[];
}