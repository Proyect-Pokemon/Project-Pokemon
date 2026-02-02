import { Component, inject, OnInit, signal } from '@angular/core';
import { BattleService } from '../../services/battle-service';

@Component({
  selector: 'app-battle',
  imports: [],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle {

  turno = signal(0);

    pokemonAName = '';
    pokemonASprite = '';
    pokemonAHP = 0;
    pokemonACurrentHP = signal(0);
    pokemonAMoves: any[] = [];
    pokemonAMove1 = '';
    pokemonAMove2 = '';
    pokemonAMove3 = '';
    pokemonAMove4 = '';

    pokemonAMove1PP = 0;
    pokemonAMove2PP = 0;
    pokemonAMove3PP = 0;
    pokemonAMove4PP = 0;

    pokemonBName = '';
    pokemonBSprite = '';
    pokemonBHP = 0;
    pokemonBCurrentHP = signal(0);
    pokemonBMoves: any[] = [];

  private apiService = inject(BattleService);

  async ngOnInit(): Promise<void> {
    const data = await this.apiService.getBattle();
    console.log(data);

    this.pokemonAName = data.pokemonA.name;
    this.pokemonBName = data.pokemonB.name;
    this.pokemonASprite = data.pokemonA.sprite;
    this.pokemonBSprite = data.pokemonB.sprite;

    this.pokemonAHP = data.pokemonA.hp;
    this.pokemonBHP = data.pokemonB.hp;
    
    this.pokemonACurrentHP.set(this.pokemonAHP);
    this.pokemonBCurrentHP.set(this.pokemonBHP);

    this.pokemonAMoves = data.pokemonA.moves;
    this.pokemonBMoves = data.pokemonB.moves;

    this.pokemonAMove1 = this.pokemonAMoves[0].name;
    this.pokemonAMove2 = this.pokemonAMoves[1].name;
    this.pokemonAMove3 = this.pokemonAMoves[2].name;
    this.pokemonAMove4 = this.pokemonAMoves[3].name;

    this.pokemonAMove1PP = this.pokemonAMoves[0].pp;
    this.pokemonAMove2PP = this.pokemonAMoves[1].pp;
    this.pokemonAMove3PP = this.pokemonAMoves[2].pp;
    this.pokemonAMove4PP = this.pokemonAMoves[3].pp;
  }

  ataca(movimiento: number): void {

  }
}
