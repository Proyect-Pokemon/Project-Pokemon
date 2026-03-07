import { Component, Input, Output, EventEmitter, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NatureService } from '../../../services/nature-service';
import { Nature } from '../../../models/nature';

@Component({
    selector: 'app-pokemon-nature-selector',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './pokemon-nature-selector.html',
    styleUrls: ['./pokemon-nature-selector.css'],
})
export class PokemonNatureSelector {
    private readonly natureService = inject(NatureService);

    @Input() set natureId(value: number | null) {
        const normalizedNatureId = value && value > 0 ? value : 1;
        this.initialNatureId.set(normalizedNatureId);
        this.selectedNatureId.set(normalizedNatureId);
    }

    @Output() natureChanged = new EventEmitter<number>();

    selectedNatureId = signal(1);
    isLoadingNatures = signal(false);
    natureError = signal<string | null>(null);
    isNatureSectionExpanded = signal(false);
    allNaturesCache = signal<Nature[]>([]);
    private initialNatureId = signal(1);

    selectedNature = computed(() => {
        const selectedNatureId = this.selectedNatureId();
        return this.allNaturesCache().find(nature => nature.id === selectedNatureId) ?? null;
    });

    private readonly statLabels: Record<string, string> = {
        hp: 'PS',
        attack: 'Ataque',
        defense: 'Defensa',
        specialattack: 'Ataque Especial',
        specialdefense: 'Defensa Especial',
        speed: 'Velocidad',
    };

    private readonly numericStatKeys: Record<number, string> = {
        0: 'hp',
        1: 'attack',
        2: 'defense',
        3: 'specialattack',
        4: 'specialdefense',
        5: 'speed',
    };

    private readonly natureNames: Record<string, string> = {
        hardy: 'Fuerte',
        bold: 'Osada',
        modest: 'Modesta',
        calm: 'Serena',
        timid: 'Miedosa',
        lonely: 'Huraña',
        docile: 'Dócil',
        mild: 'Afable',
        gentle: 'Amable',
        hasty: 'Activa',
        adamant: 'Firme',
        impish: 'Agitada',
        bashful: 'Tímida',
        careful: 'Cauta',
        jolly: 'Alegre',
        naughty: 'Pícara',
        lax: 'Floja',
        rash: 'Alocada',
        quirky: 'Rara',
        naive: 'Ingenua',
        brave: 'Audaz',
        relaxed: 'Plácida',
        quiet: 'Mansa',
        sassy: 'Grosera',
        serious: 'Seria',
    };

    constructor() {
        this.loadNaturesCache();

        effect(() => {
            const natures = this.allNaturesCache();
            const selectedNatureId = this.selectedNatureId();

            if (natures.length === 0) {
                return;
            }

            if (!natures.some(nature => nature.id === selectedNatureId)) {
                this.selectedNatureId.set(natures[0].id);
            }
        });
    }

    private async loadNaturesCache() {
        if (this.allNaturesCache().length > 0 || this.isLoadingNatures()) {
            return;
        }

        this.isLoadingNatures.set(true);
        this.natureError.set(null);

        const allNatures = await this.natureService.getAllNatures();
        this.allNaturesCache.set(allNatures);

        if (allNatures.length === 0) {
            this.natureError.set('No se pudieron cargar las naturalezas.');
        }

        this.isLoadingNatures.set(false);
    }

    onNatureChange(event: Event) {
        const target = event.target as HTMLSelectElement | null;
        if (!target) {
            return;
        }

        const value = Number(target.value);
        if (!Number.isNaN(value) && value > 0) {
            this.selectedNatureId.set(value);
            this.natureChanged.emit(value);
        }
    }

    toggleNatureSection() {
        this.isNatureSectionExpanded.update(value => !value);
    }

    getNatureDescriptor(nature: Nature): string {
        if (this.isNeutralNature(nature)) {
            return 'Sin cambios';
        }

        return `+${this.getNatureStatLabel(nature.statBoost)} / -${this.getNatureStatLabel(nature.statDrop)}`;
    }

    getNatureStatLabel(stat: string | number): string {
        const normalizedKey = this.normalizeStatKey(stat);
        return this.statLabels[normalizedKey] ?? String(stat);
    }

    getNatureName(nature: Nature): string {
        const normalizedName = nature.name.toLowerCase();
        return this.natureNames[normalizedName] ?? nature.name;
    }

    isNeutralNature(nature: Nature): boolean {
        return this.normalizeStatKey(nature.statBoost) === this.normalizeStatKey(nature.statDrop);
    }

    private normalizeStatKey(stat: string | number): string {
        if (typeof stat === 'number') {
            return this.numericStatKeys[stat] ?? String(stat);
        }

        return stat.toLowerCase().replace(/[\s_]/g, '');
    }

    resetNatureSelection() {
        this.selectedNatureId.set(this.initialNatureId());
        this.isNatureSectionExpanded.set(false);
    }
}
