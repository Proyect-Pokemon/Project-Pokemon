import { TitleCasePipe } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-life-bar',
  imports: [TitleCasePipe],
  templateUrl: './life-bar.html',
  styleUrl: './life-bar.css',
})
export class LifeBar {
  @Input() pokemonName: string = '';
  @Input() pokemonSex: string | null = null;
  @Input() currentHp: number | null = null;
  @Input() maxHp: number | null = null;
  @Input() showHp: boolean = false;
  @Input() status: string = '';

  get showGenderBadge(): boolean {
    return this.genderClass !== null;
  }

  get genderClass(): 'male' | 'female' | null {
    if (!this.pokemonSex) {
      return null;
    }

    const normalized = this.pokemonSex.trim().toLowerCase();
    if (normalized === 'h' || normalized === 'f') {
      return 'female';
    }

    if (normalized === 'm') {
      return 'male';
    }

    return null;
  }

  get genderIconSrc(): string {
    return this.genderClass === 'female' ? 'assets/icons/UI/female.svg' : 'assets/icons/UI/male.svg';
  }

  get genderAlt(): string {
    return this.genderClass === 'female' ? 'Hembra' : 'Macho';
  }

  get hpPercent(): number {
    const max = this.maxHp ?? 0;
    const current = this.currentHp ?? max;
    if (max <= 0) return 0;
    const percent = (current / max) * 100;
    return Math.max(0, Math.min(100, percent));
  }

  get displayHp(): number {
    return Math.max(0, this.currentHp ?? 0);
  }

  get displayHpPercent(): number {
    return Math.trunc(this.hpPercent);
  }

  get healthColor(): string {
    const percent = this.hpPercent;
    if (percent > 50) return 'green';
    if (percent > 20) return 'yellow';
    return 'red';
  }

  get statusDisplay(): string {
    const statusLower = this.status.toLowerCase();
    if (statusLower === 'burned') return 'QUE';
    if (statusLower === 'frozen') return 'CON';
    if (statusLower === 'poison') return 'ENV';
    if (statusLower === 'sleep') return 'DOR';
    if (statusLower === 'paralyzed') return 'PAR';
    return '';
  }

  get statusClass(): string {
    const statusLower = this.status.toLowerCase();
    if (statusLower === 'burned') return 'status-fire';
    if (statusLower === 'frozen') return 'status-ice';
    if (statusLower === 'poison') return 'status-poison';
    if (statusLower === 'sleep') return 'status-flying';
    if (statusLower === 'paralyzed') return 'status-electric';
    return '';
  }
}
