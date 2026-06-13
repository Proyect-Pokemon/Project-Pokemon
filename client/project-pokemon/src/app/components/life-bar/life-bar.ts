import { TitleCasePipe } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-life-bar',
  imports: [TitleCasePipe],
  templateUrl: './life-bar.html',
  styleUrl: './life-bar.css',
})
export class LifeBar {
  private static readonly STATUS_BADGES: Record<string, { label: string; className: string }> = {
    burn: { label: 'QUE', className: 'status-fire' },
    freeze: { label: 'CON', className: 'status-ice' },
    sleep: { label: 'DOR', className: 'status-sleep' },
    paralysis: { label: 'PAR', className: 'status-electric' },
    poison: { label: 'ENV', className: 'status-poison' },
    badlypoisoned: { label: 'ENV', className: 'status-poison' },
  };

  @Input() pokemonName: string = '';
  @Input() pokemonSex: string | null = null;
  @Input() currentHp: number | null = null;
  @Input() maxHp: number | null = null;
  @Input() showHp: boolean = false;
  @Input() status: string = '';
  @Input() statStages: Record<string, number> = {};

  private static readonly STAT_LABELS: Record<string, string> = {
    // El backend envía los stats en español via TranslateStatName.
    // Tras normalizeName + strip de espacios quedan estas keys:
    ataque:           'Atq',
    defensa:          'Def',
    ataqueespecial:   'AtEsp',
    defensaespecial:  'DefEsp',
    velocidad:        'Vel',
    evasion:          'Eva',
    precision:        'Pre'
  };

  get statChips(): { key: string; label: string; stage: number }[] {
    return Object.entries(this.statStages)
      .filter(([, stage]) => stage !== 0)
      .map(([key, stage]) => {
        // Normalizar la clave por si llega con espacios, guiones o tildes
        const normalizedKey = key
          .toLowerCase()
          .normalize('NFD')
          .replace(/[\u0300-\u036f]/g, '')
          .replace(/[\s_\-]/g, '');
        return {
          key,
          label: LifeBar.STAT_LABELS[normalizedKey] ?? key,
          stage,
        };
      });
  }

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
    return this.statusBadge?.label ?? '';
  }

  get statusClass(): string {
    return this.statusBadge?.className ?? '';
  }

  get hasStatusBadge(): boolean {
    return !!this.statusBadge;
  }

  private get statusBadge(): { label: string; className: string } | null {
    const statusKey = this.normalizedStatusKey;
    if (!statusKey) {
      return null;
    }

    return LifeBar.STATUS_BADGES[statusKey] ?? null;
  }

  private get normalizedStatusKey(): string {
    return (this.status ?? '')
      .toString()
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[\s_-]/g, '');
  }
}