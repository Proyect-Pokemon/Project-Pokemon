import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BattleMove } from '../../models/move';

@Component({
  selector: 'app-movement-button',
  imports: [],
  templateUrl: './movement-button.html',
  styleUrl: './movement-button.css',
})
export class MovementButton {
  @Input() move!: BattleMove;
  @Input() disabled: boolean = false;
  @Output() moveClicked = new EventEmitter<BattleMove>();

  onClickMove(): void {
    this.moveClicked.emit(this.move);
  }
}
