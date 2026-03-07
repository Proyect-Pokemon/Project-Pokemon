import { Component, Input, Output, EventEmitter, inject, signal, effect, computed, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PokemonService } from '../../services/pokemon-service';
import { Pokemon } from '../../models/pokemon';
import { PostPokemonTeamDto } from '../../models/pokemon-team';
import { Movement } from '../../models/move';
import { MovementService } from '../../services/movement-service';
import { PokemonStatsDialog } from '../pokemon-stats-dialog/pokemon-stats-dialog';
import { NatureService } from '../../services/nature-service';
import { Nature } from '../../models/nature';
import { PokemonTeamService } from '../../services/pokemon-team-service';

interface PokemonStats {
    hp: number;
    attack: number;
    defense: number;
    specialAttack: number;
    specialDefense: number;
    speed: number;
}

@Component({
    selector: 'app-pokemon-editor-panel',
    standalone: true,
    imports: [CommonModule, PokemonStatsDialog],
    templateUrl: './pokemon-editor-panel.html',
    styleUrls: ['./pokemon-editor-panel.css'],
})
export class PokemonEditorPanel {
    @ViewChild('nicknameInput') nicknameInput?: ElementRef<HTMLInputElement>;

    private readonly pokemonService = inject(PokemonService);
    private readonly movementService = inject(MovementService);
    private readonly natureService = inject(NatureService);
    private readonly pokemonTeamService = inject(PokemonTeamService);
    readonly MAX_NICKNAME_LENGTH = 12;

    private panelOpen = false;

    @Input() set isOpen(value: boolean) {
        this.panelOpen = value;
        if (!value) {
            this.isNatureSectionExpanded.set(false);
            this.isEditingNickname.set(false);
            this.nicknameDraft.set('');
            this.isSavingNickname.set(false);
        }
    }

    get isOpen(): boolean {
        return this.panelOpen;
    }
    @Input() teamId = 0;
    @Input() slot: number = 1;
    @Input() pokemonDisplayName: string | null = null;
    @Input() pokemonSprite: string | null = null;
    @Input() pokemonId: number | null = null;
    @Input() pokemonTeamId: number | null = null;
    @Input() pokemonBaseName: string | null = null;
    @Input() pokemonNickname: string | null = null;
    @Input() set natureId(value: number | null) {
        const normalizedNatureId = value && value > 0 ? value : 1;
        this.initialNatureId.set(normalizedNatureId);
        this.selectedNatureId.set(normalizedNatureId);
    }
    @Input() set movementIds(value: (number | null)[]) {
        this.movementIdsSignal.set(value);
    }
    @Output() close = new EventEmitter<void>();
    @Output() createPokemonTeam = new EventEmitter<PostPokemonTeamDto>();
    @Output() changeSlot = new EventEmitter<number>();
    @Output() nicknameUpdated = new EventEmitter<{ pokemonTeamId: number, nickname: string | null }>();

    showSearchControls = signal(false);
    searchPokemonNumber = signal<number | null>(null);
    searchedPokemon = signal<Pokemon | null>(null);
    searchError = signal<string | null>(null);
    allPokemonCache = signal<Pokemon[]>([]);
    animationDirection = signal<'left' | 'right' | 'leftIn' | 'rightIn' | 'none'>('none');
    movements = signal<(Movement | null)[]>([null, null, null, null]);
    allMovementsCache = signal<Movement[]>([]);
    movementIdsSignal = signal<(number | null)[]>([null, null, null, null]);
    allNaturesCache = signal<Nature[]>([]);
    selectedNatureId = signal(1);
    isLoadingNatures = signal(false);
    natureError = signal<string | null>(null);
    isNatureSectionExpanded = signal(false);
    showStatsDialog = signal(false);
    currentPokemonStats = signal<PokemonStats | null>(null);
    isEditingNickname = signal(false);
    nicknameDraft = signal('');
    isSavingNickname = signal(false);
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

    get isEasterEggSlot(): boolean {
        return this.slot <= 0 || this.slot >= 7;
    }

    constructor() {
        effect(() => {
            if (this.showSearchControls()) {
                this.loadPokemonCache();
            }
        });

        effect(async () => {
            const pokemonNumber = this.searchPokemonNumber();
            if (!pokemonNumber) {
                this.searchedPokemon.set(null);
                this.searchError.set(null);
                return;
            }

            this.searchError.set(null);
            const cache = this.allPokemonCache();
            const found = cache.find(p => p.id === pokemonNumber) ?? null;

            if (!found) {
                this.searchedPokemon.set(null);
                this.searchError.set('No se encontró Pokémon con ese número.');
            } else {
                this.searchedPokemon.set(found);
            }
        });

        // Cargar cache de movimientos al inicializar
        this.loadMovementsCache();
        this.loadNaturesCache();

        // Efecto para actualizar movimientos cuando los IDs cambian
        effect(() => {
            const ids = this.movementIdsSignal();
            const cache = this.allMovementsCache();
            
            const loadedMovements = ids.map(id => {
                if (!id) return null;
                return cache.find(m => m.id === id) ?? null;
            });
            
            this.movements.set(loadedMovements);
        });

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

    private async loadPokemonCache() {
        if (this.allPokemonCache().length > 0) {
            return;
        }
        const allPokemon = await this.pokemonService.getAllPokemon();
        this.allPokemonCache.set(allPokemon);
    }

    private async loadMovementsCache() {
        if (this.allMovementsCache().length > 0) {
            return;
        }
        const allMovements = await this.movementService.getAllMovements();
        this.allMovementsCache.set(allMovements);
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

    toggleSearchControls() {
        this.showSearchControls.update(value => !value);
        this.searchError.set(null);
        if (!this.showSearchControls()) {
            this.searchPokemonNumber.set(null);
            this.searchedPokemon.set(null);
        }
    }

    onSearchNumberInput(event: Event) {
        const target = event.target as HTMLInputElement | null;
        if (!target) {
            this.searchPokemonNumber.set(null);
            return;
        }

        const value = Number(target.value);
        this.searchPokemonNumber.set(Number.isNaN(value) || value <= 0 ? null : value);
    }

    onNatureChange(event: Event) {
        const target = event.target as HTMLSelectElement | null;
        if (!target) {
            return;
        }

        const value = Number(target.value);
        if (!Number.isNaN(value) && value > 0) {
            this.selectedNatureId.set(value);
        }
    }

    toggleNatureSection() {
        this.isNatureSectionExpanded.update(value => !value);
    }

    canEditNickname(): boolean {
        return !this.isEasterEggSlot && !!this.pokemonDisplayName && !!this.pokemonSprite;
    }

    onPokemonDisplayNameClick() {
        if (!this.canEditNickname()) {
            return;
        }

        this.nicknameDraft.set((this.pokemonNickname ?? this.pokemonBaseName ?? this.pokemonDisplayName ?? '').trim().slice(0, this.MAX_NICKNAME_LENGTH));
        this.isEditingNickname.set(true);
        setTimeout(() => this.focusNicknameInput(), 0);
    }

    onNicknameInput(event: Event) {
        const target = event.target as HTMLInputElement | null;
        const draftValue = target?.value ?? '';
        this.nicknameDraft.set(draftValue.slice(0, this.MAX_NICKNAME_LENGTH));
    }

    onNicknameBlur() {
        this.saveNickname();
    }

    onNicknameKeydown(event: KeyboardEvent) {
        if (event.key === 'Enter') {
            event.preventDefault();
            this.saveNickname();
            return;
        }

        if (event.key === 'Escape') {
            this.cancelNicknameEdit();
        }
    }

    private cancelNicknameEdit() {
        this.isEditingNickname.set(false);
        this.nicknameDraft.set('');
    }

    private focusNicknameInput() {
        const inputElement = this.nicknameInput?.nativeElement;
        if (!inputElement || this.isSavingNickname()) {
            return;
        }

        inputElement.focus();
        inputElement.select();
    }

    private async saveNickname() {
        if (!this.isEditingNickname()) {
            return;
        }

        const normalizedNextNickname = this.nicknameDraft().trim().slice(0, this.MAX_NICKNAME_LENGTH);
        const nextNickname: string | null = normalizedNextNickname.length > 0 ? normalizedNextNickname : null;

        const normalizedCurrentNickname = (this.pokemonNickname ?? '').trim().slice(0, this.MAX_NICKNAME_LENGTH);
        const currentNickname: string | null = normalizedCurrentNickname.length > 0 ? normalizedCurrentNickname : null;

        this.cancelNicknameEdit();

        if (nextNickname === currentNickname) {
            return;
        }

        const pokemonTeamId = await this.resolvePokemonTeamIdForNicknameUpdate();
        if (pokemonTeamId === null) {
            return;
        }

        this.isSavingNickname.set(true);

        const success = await this.pokemonTeamService.updateNickname(pokemonTeamId, { nickname: nextNickname });

        this.isSavingNickname.set(false);

        if (success) {
            this.nicknameUpdated.emit({ pokemonTeamId, nickname: nextNickname });
        }
    }

    private async resolvePokemonTeamIdForNicknameUpdate(): Promise<number | null> {
        if (this.pokemonTeamId !== null) {
            return this.pokemonTeamId;
        }

        const pokemonTeams = await this.pokemonTeamService.getAllPokemonTeams();
        const selectedPokemonTeam = pokemonTeams.find(pokemonTeam =>
            pokemonTeam.teamId === this.teamId && pokemonTeam.slot === this.slot
        );

        return selectedPokemonTeam?.id ?? null;
    }

    onCreatePokemonTeam() {
        const foundPokemon = this.searchedPokemon();
        if (!foundPokemon) {
            return;
        }

        this.createPokemonTeam.emit({
            nickname: null,
            shiny: false,
            slot: this.slot,
            teamId: this.teamId,
            pokemonId: foundPokemon.id,
            natureId: this.selectedNatureId(),
            movementId1: 1,
            movementId2: null,
            movementId3: null,
            movementId4: null,
        });
    }

    onClose() {
        this.close.emit();

        this.showSearchControls.set(false);
        this.searchPokemonNumber.set(null);
        this.searchedPokemon.set(null);
        this.searchError.set(null);
        this.showStatsDialog.set(false);
        this.currentPokemonStats.set(null);
        this.selectedNatureId.set(this.initialNatureId());
        this.isNatureSectionExpanded.set(false);
        this.cancelNicknameEdit();
    }

    onPreviousSlot() {
        if (this.slot > -3) {
            this.animationDirection.set('right');
            setTimeout(() => {
                this.changeSlot.emit(this.slot - 1);
                this.animationDirection.set('rightIn');
                setTimeout(() => {
                    this.animationDirection.set('none');
                }, 300);
            }, 300);
        }
    }

    onNextSlot() {
        if (this.slot < 10) {
            this.animationDirection.set('left');
            setTimeout(() => {
                this.changeSlot.emit(this.slot + 1);
                this.animationDirection.set('leftIn');
                setTimeout(() => {
                    this.animationDirection.set('none');
                }, 300);
            }, 300);
        }
    }

    async openStatsDialog() {
        // Obtener stats del Pokémon actual
        let pokemon: Pokemon | null = null;

        // Si está buscando, use el Pokémon encontrado
        if (this.searchedPokemon()) {
            pokemon = this.searchedPokemon();
        } else if (this.pokemonId) {
            // Si tiene un Pokémon seleccionado, búscalo en el cache
            const cache = this.allPokemonCache();
            pokemon = cache.find(p => p.id === this.pokemonId) ?? null;

            // Si no está en cache, cargalo
            if (!pokemon) {
                const allPokemon = await this.pokemonService.getAllPokemon();
                pokemon = allPokemon.find(p => p.id === this.pokemonId) ?? null;
            }
        }

        // Extraer stats si el Pokémon se encontró
        if (pokemon) {
            this.currentPokemonStats.set({
                hp: pokemon.hp,
                attack: pokemon.attack,
                defense: pokemon.defense,
                specialAttack: pokemon.specialAttack,
                specialDefense: pokemon.specialDefense,
                speed: pokemon.speed,
            });
        }

        this.showStatsDialog.set(true);
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

    closeStatsDialog() {
        this.showStatsDialog.set(false);
        this.currentPokemonStats.set(null);
    }
}
