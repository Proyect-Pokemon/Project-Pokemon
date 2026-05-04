import { BattleMove } from '../move';

export interface PokemonApi {
  name: string;
  sprite: string;
  statusCondition?: string | null;
  currentHp: number;
  maxHp: number;
  hp?: number;
  atk: number;
  def: number;
  spa: number;
  spd: number;
  spe: number;
  type1: string;
  type2: string | null;
  moves: BattleMove[];
}

export interface BattleResponse {
  pokemonA: PokemonApi;
  pokemonB: PokemonApi;
}
