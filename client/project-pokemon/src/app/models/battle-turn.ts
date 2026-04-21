import { BattleResponse } from './pokemon-api';

export interface BattleTurnRequest {
    moveName: string;
}

export interface BattleTurnResponse {
    battle?: BattleResponse;
    pokemonAHp?: number;
    pokemonBHp?: number;
    hpA?: number;
    hpB?: number;
    battleLog?: string[];
    log?: string[];
    winner?: string | null;
    finished?: boolean;
    isFinished?: boolean;
}
