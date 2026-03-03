import { PokemonApi } from "./pokemon-api";

export interface PokemonTeam {
    id?: number;
    nickname?: string;
    shiny: boolean;
    slot: number;
    teamId: number;
    pokemonId: number;
    natureId: number;
    movementId1: number;
    movementId2?: number;
    movementId3?: number;
    movementId4?: number;
    // Información completa del pokémon (se obtiene del API)
    pokemon?: PokemonApi;
}

export interface PostPokemonTeamDto {
    nickname?: string;
    shiny: boolean;
    slot: number;
    teamId: number;
    pokemonId: number;
    natureId: number;
    movementId1: number;
    movementId2?: number;
    movementId3?: number;
    movementId4?: number;
}
