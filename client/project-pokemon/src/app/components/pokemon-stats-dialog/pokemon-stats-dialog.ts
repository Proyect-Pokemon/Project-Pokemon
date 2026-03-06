import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

interface PokemonStats {
    hp: number;
    attack: number;
    defense: number;
    specialAttack: number;
    specialDefense: number;
    speed: number;
}

@Component({
    selector: 'app-pokemon-stats-dialog',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-stats-dialog.html',
    styleUrls: ['./pokemon-stats-dialog.css'],
})
export class PokemonStatsDialog {
    @Input() isOpen = false;
    @Input() stats: PokemonStats | null = null;
    @Input() natureBoostStat: string | number | null = null;
    @Input() natureDropStat: string | number | null = null;
    @Output() close = new EventEmitter<void>();

    private readonly LEVEL = 50; // Nivel estándar competitivo
    private readonly IV = 31; // IVs máximos
    private readonly EV = 0; // Sin EVs
    private readonly NATURE_BOOST = 1.1;
    private readonly NATURE_DROP = 0.9;

    private readonly numericStatKeys: Record<number, string> = {
        0: 'hp',
        1: 'attack',
        2: 'defense',
        3: 'specialattack',
        4: 'specialdefense',
        5: 'speed',
    };

    calculateHp(baseHp: number): number {
        return Math.floor(((2 * baseHp + this.IV + (this.EV / 4.0)) * this.LEVEL) / 100) + this.LEVEL + 10;
    }

    calculateStat(baseStat: number, statKey: string): number {
        const unmodifiedStat = Math.floor((((2 * baseStat + this.IV + (this.EV / 4.0)) * this.LEVEL) / 100) + 5);
        return Math.floor(unmodifiedStat * this.getNatureMultiplier(statKey));
    }

    getNatureStatColor(statKey: string): string | null {
        const natureMultiplier = this.getNatureMultiplier(statKey);

        if (natureMultiplier > 1) {
            return '#B62024';
        }

        if (natureMultiplier < 1) {
            return '#1D4ED8';
        }

        return null;
    }

    private getNatureMultiplier(statKey: string): number {
        const normalizedStatKey = this.normalizeStatKey(statKey);
        const normalizedBoost = this.normalizeStatKey(this.natureBoostStat);
        const normalizedDrop = this.normalizeStatKey(this.natureDropStat);

        // Naturalezas neutrales (sube y baja la misma stat)
        if (normalizedBoost && normalizedBoost === normalizedDrop) {
            return 1;
        }

        if (normalizedBoost && normalizedStatKey === normalizedBoost) {
            return this.NATURE_BOOST;
        }

        if (normalizedDrop && normalizedStatKey === normalizedDrop) {
            return this.NATURE_DROP;
        }

        return 1;
    }

    private normalizeStatKey(stat: string | number | null): string {
        if (stat === null) {
            return '';
        }

        if (typeof stat === 'number') {
            return this.numericStatKeys[stat] ?? String(stat);
        }

        return stat.toLowerCase().replace(/[\s_]/g, '');
    }

    getStatColor(value: number): string {
        if (value <= 50) {
            return '#808080';
        }

        if (value <= 150) {
            return '#C85048';
        }

        if (value <= 250) {
            return '#F07F2F';
        }

        if (value <= 500) {
            return '#F9CF2E';
        }

        if (value <= 600) {
            return '#76C850';
        }

        if (value <= 1000) {
            return '#40E0D0';
        }

        return '#00FFFF';
    }

    onOverlayClick() {
        this.close.emit();
    }

    onCloseButton() {
        this.close.emit();
    }
}
