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
    @Output() close = new EventEmitter<void>();

    private readonly LEVEL = 50; // Nivel estándar competitivo
    private readonly IV = 31; // IVs máximos
    private readonly EV = 0; // Sin EVs
    private readonly NATURE = 100; // Sin naturaleza

    calculateHp(baseHp: number): number {
        return Math.floor(((2 * baseHp + this.IV + (this.EV / 4.0)) * this.LEVEL) / 100) + this.LEVEL + 10;
    }

    calculateStat(baseStat: number): number {
        return Math.floor((((2 * baseStat + this.IV + (this.EV / 4.0)) * this.LEVEL) / 100) + 5) * (this.NATURE / 100);
    }

    private hexToRgb(hex: string): [number, number, number] {
        // Remove '#' if present
        hex = hex.replace('#', '');
        const r = parseInt(hex.substring(0, 2), 16);
        const g = parseInt(hex.substring(2, 4), 16);
        const b = parseInt(hex.substring(4, 6), 16);
        return [r, g, b];
    }

    private rgbToHex(r: number, g: number, b: number): string {
        return '#' + [r, g, b].map(x => {
            const hex = Math.round(x).toString(16);
            return hex.length === 1 ? '0' + hex : hex;
        }).join('').toUpperCase();
    }

    private interpolateColor(color1: string, color2: string, t: number): string {
        // t should be between 0 and 1
        const [r1, g1, b1] = this.hexToRgb(color1);
        const [r2, g2, b2] = this.hexToRgb(color2);
        
        const r = r1 + (r2 - r1) * t;
        const g = g1 + (g2 - g1) * t;
        const b = b1 + (b2 - b1) * t;
        
        return this.rgbToHex(r, g, b);
    }

    private getCSSVariableColor(varName: string): string {
        // Get computed color from CSS variable
        const root = document.documentElement;
        return getComputedStyle(root).getPropertyValue(varName).trim();
    }

    getStatColor(value: number): string {
        // Define color ranges with smooth transitions
        const colors = [
            { min: 0, max: 50, color1: '#808080', color2: '#C85048' },      // gray to fighting-color
            { min: 50, max: 150, color1: '#C85048', color2: '#F07F2F' },    // fighting to fire
            { min: 150, max: 250, color1: '#F07F2F', color2: '#F9CF2E' },   // fire to electric
            { min: 250, max: 500, color1: '#F9CF2E', color2: '#76C850' },   // electric to grass
            { min: 500, max: 600, color1: '#76C850', color2: '#40E0D0' },   // grass to turquoise
            { min: 600, max: 1000, color1: '#40E0D0', color2: '#00FFFF' }   // turquoise to aqua
        ];

        // Find the appropriate range and calculate interpolation
        for (const range of colors) {
            if (value >= range.min && value <= range.max) {
                const rangeSize = range.max - range.min;
                const positionInRange = value - range.min;
                const t = rangeSize === 0 ? 0 : positionInRange / rangeSize;
                return this.interpolateColor(range.color1, range.color2, t);
            }
        }

        // Fallback for values > 1000
        return '#00FFFF';
    }

    onOverlayClick() {
        this.close.emit();
    }

    onCloseButton() {
        this.close.emit();
    }
}
