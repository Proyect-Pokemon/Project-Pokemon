export interface PutPokemonTeamDto {
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