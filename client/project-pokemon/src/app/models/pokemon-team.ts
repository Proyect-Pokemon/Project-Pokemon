import { Pokemon } from "./pokemon";

export interface GetAllPokemonTeamDto {
    id: number;
    nickname: string | null;
    shiny: boolean;
    sex: string | null;
    slot: number;
    teamId: number;
    pokemonId: number;
    natureId: number;
    movementId1: number;
    movementId2: number | null;
    movementId3: number | null;
    movementId4: number | null;
}

export interface PokemonTeam {
    id: number;
    nickname: string | null;
    shiny: boolean;
    sex: string | null;
    slot: number;
    teamId: number;
    pokemonId: number;
    natureId: number;
    movementId1: number;
    movementId2: number | null;
    movementId3: number | null;
    movementId4: number | null;
    // Información completa del pokémon (se obtiene del API)
    pokemon: Pokemon | null;
}

export interface PostPokemonTeamDto {
    nickname: string | null;
    shiny: boolean;
    sex?: string | null;
    slot: number;
    teamId: number;
    pokemonId: number;
    natureId: number;
    movementId1: number;
    movementId2: number | null;
    movementId3: number | null;
    movementId4: number | null;
}
