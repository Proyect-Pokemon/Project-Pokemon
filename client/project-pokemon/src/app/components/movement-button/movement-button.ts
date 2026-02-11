import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Move } from '../../models/move';

@Component({
  selector: 'app-movement-button',
  imports: [],
  templateUrl: './movement-button.html',
  styleUrl: './movement-button.css',
})
export class MovementButton {
  @Input() move!: Move;
  @Input() disabled: boolean = false;
  @Output() moveClicked = new EventEmitter<Move>();

  onClickMove(): void {
    this.moveClicked.emit(this.move);
  }
}
