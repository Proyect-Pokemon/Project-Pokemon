import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PokemonStatsButton } from '../pokemon-stats-button/pokemon-stats-button';
import { PokemonTeamService } from '../../../services/pokemon-team-service';

@Component({
    selector: 'app-pokemon-preview',
    standalone: true,
    imports: [CommonModule, PokemonStatsButton],
    templateUrl: './pokemon-preview.html',
    styleUrls: ['./pokemon-preview.css'],
})
export class PokemonPreview {
    @ViewChild('nicknameInput') nicknameInput?: ElementRef<HTMLInputElement>;

    private readonly pokemonTeamService = inject(PokemonTeamService);
    readonly MAX_NICKNAME_LENGTH = 12;

    @Input() pokemonDisplayName: string | null = null;
    @Input() pokemonSprite: string | null = null;
    @Input() pokemonId: number | null = null;
    @Input() pokemonBaseName: string | null = null;
    @Input() pokemonNickname: string | null = null;
    @Input() pokemonTeamId: number | null = null;
    @Input() teamId: number = 0;
    @Input() slot: number = 1;
    @Input() isEasterEggSlot: boolean = false;
    @Input() disablePreviousArrow: boolean = false;
    @Input() disableNextArrow: boolean = false;
    @Input() animationDirection: 'left' | 'right' | 'leftIn' | 'rightIn' | 'none' = 'none';

    @Output() openStatsDialog = new EventEmitter<void>();
    @Output() previousSlot = new EventEmitter<void>();
    @Output() nextSlot = new EventEmitter<void>();
    @Output() nicknameUpdated = new EventEmitter<{ pokemonTeamId: number, nickname: string | null }>();

    isEditingNickname = false;
    nicknameDraft = '';
    isSavingNickname = false;

    canEditNickname(): boolean {
        return !this.isEasterEggSlot && !!this.pokemonDisplayName && !!this.pokemonSprite;
    }

    onPokemonDisplayNameClick() {
        if (!this.canEditNickname()) {
            return;
        }

        this.nicknameDraft = (this.pokemonNickname ?? this.pokemonBaseName ?? this.pokemonDisplayName ?? '').trim().slice(0, this.MAX_NICKNAME_LENGTH);
        this.isEditingNickname = true;
        setTimeout(() => this.focusNicknameInput(), 0);
    }

    onNicknameInput(event: Event) {
        const target = event.target as HTMLInputElement | null;
        const draftValue = target?.value ?? '';
        this.nicknameDraft = draftValue.slice(0, this.MAX_NICKNAME_LENGTH);
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

    onStatsButtonClick() {
        this.openStatsDialog.emit();
    }

    onPreviousSlotClick() {
        this.previousSlot.emit();
    }

    onNextSlotClick() {
        this.nextSlot.emit();
    }

    cancelNicknameEdit() {
        this.isEditingNickname = false;
        this.nicknameDraft = '';
    }

    private focusNicknameInput() {
        const inputElement = this.nicknameInput?.nativeElement;
        if (!inputElement || this.isSavingNickname) {
            return;
        }

        inputElement.focus();
        inputElement.select();
    }

    private async saveNickname() {
        if (!this.isEditingNickname) {
            return;
        }

        const normalizedNextNickname = this.nicknameDraft.trim().slice(0, this.MAX_NICKNAME_LENGTH);
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

        this.isSavingNickname = true;

        const success = await this.pokemonTeamService.updateNickname(pokemonTeamId, { nickname: nextNickname });

        this.isSavingNickname = false;

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
}
