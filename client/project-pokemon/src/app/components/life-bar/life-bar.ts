import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-life-bar',
  imports: [],
  templateUrl: './life-bar.html',
  styleUrl: './life-bar.css',
})
export class LifeBar {
  @Input() currentHp: number | null = null;
  @Input() maxHp: number | null = null;

  get hpPercent(): number {
    const max = this.maxHp ?? 0;
    const current = this.currentHp ?? max;
    if (max <= 0) return 0;
    const percent = (current / max) * 100;
    return Math.max(0, Math.min(100, percent));
  }

  get healthColor(): string {
    const percent = this.hpPercent;
    if (percent > 50) return 'green';
    if (percent > 20) return 'yellow';
    return 'red';
  }
}
