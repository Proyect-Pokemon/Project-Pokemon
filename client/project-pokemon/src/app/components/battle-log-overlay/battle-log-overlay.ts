import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-battle-log-overlay',
  standalone: true,
  templateUrl: './battle-log-overlay.html',
  styleUrls: ['./battle-log-overlay.css']
})
export class BattleLogOverlay {
  @Input() message = '';
  @Input() visible = false;

  get hasMessage(): boolean {
    return this.visible && this.message.trim().length > 0;
  }
}
