import { Move } from "./move";

export interface PokemonApi {
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
    moves: Move[];
}

export interface BattleResponse {
    pokemonA: PokemonApi;
    pokemonB: PokemonApi;
}