import { Component, inject, OnInit, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { PokemonTeam } from '../../models/pokemon-team';

@Component({
  selector: 'app-battle',
  imports: [],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {

  turno = signal(0);

  pokemonA!: PokemonTeam;
  pokemonB!: PokemonTeam;

  private apiService = inject(BattleService);

  async ngOnInit(): Promise<void> {
    const data = await this.apiService.getBattle();
    console.log(data);

    this.pokemonA = {
      name: data.pokemonA.name,
      sprite: data.pokemonA.sprite,
      hp: data.pokemonA.hp,
      currentHP: signal(data.pokemonA.hp),
      atk: data.pokemonA.atk,
      def: data.pokemonA.def,
      spa: data.pokemonA.spa,
      spd: data.pokemonA.spd,
      spe: data.pokemonA.spe,
      type1: data.pokemonA.type1,
      type2: data.pokemonA.type2,
      moves: this.mapMoves(data.pokemonA.moves)
    };

    this.pokemonB = {
      name: data.pokemonB.name,
      sprite: data.pokemonB.sprite,
      hp: data.pokemonB.hp,
      currentHP: signal(data.pokemonB.hp),
      atk: data.pokemonB.atk,
      def: data.pokemonB.def,
      spa: data.pokemonB.spa,
      spd: data.pokemonB.spd,
      spe: data.pokemonB.spe,
      type1: data.pokemonB.type1,
      type2: data.pokemonB.type2,
      moves: this.mapMoves(data.pokemonB.moves)
    };
  }

  ataca(movimiento: number): void {

  }

  private mapMoves(moves: any[]) {
    return moves.map(m => ({
      ...m,
      currentPP: signal(m.pp)
    }));
  }
}
