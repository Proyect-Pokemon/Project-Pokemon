import { PokemonTeam } from "./pokemon-team";

export interface Team {
    id: number;
    name: string;
    description?: string | null;
    userId: number;
    pokemons: PokemonTeam[];
    isExpanded: boolean;
}

export interface GetTeamDto {
    id: number;
    name: string;
    description?: string | null;
    userId: number;
}

export interface PostTeamDto {
    name: string;
    description?: string | null;
    userId: number;
}

export const MAX_POKEMON_PER_TEAM = 6;
