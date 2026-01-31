import { Component, OnInit } from '@angular/core';
import { BattleService } from '../../services/battle-service';
import { CommonModule, NgIf } from '@angular/common';

@Component({
  selector: 'app-battle',
  imports: [NgIf, CommonModule],
  templateUrl: './battle.html',
  styleUrl: './battle.css',
})
export class Battle implements OnInit {
  battle: any;

  constructor(private battleService: BattleService) {}

  async ngOnInit() {
    this.battle = await this.battleService.getBattle();
    console.log(this.battle);
  }
}
