import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';
import { MovementService } from '../../services/movement-service';
import { NatureService } from '../../services/nature-service';
import { PokemonTeam, GetAllPokemonTeamDto } from '../../models/pokemon-team';
import { Pokemon } from '../../models/pokemon';
import { Movement } from '../../models/move';
import { Nature } from '../../models/nature';
import { PokemonMovesGrid } from '../../components/pokemon-editor-panel/pokemon-moves-grid/pokemon-moves-grid';
import { PokemonNatureSelector } from '../../components/pokemon-editor-panel/pokemon-nature-selector/pokemon-nature-selector';

@Component({
  selector: 'app-pokemon-edit',
  standalone: true,
  imports: [CommonModule, PokemonMovesGrid, PokemonNatureSelector],
  templateUrl: './pokemon-edit.html',
  styleUrls: ['./pokemon-edit.css'],
})
export class PokemonEdit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private pokemonTeamService = inject(PokemonTeamService);
  private pokemonService = inject(PokemonService);
  private movementService = inject(MovementService);
  private natureService = inject(NatureService);

  teamId = signal<number | null>(null);
  pokemonTeam = signal<PokemonTeam | null>(null);
  allMovements = signal<Movement[]>([]);

  isLoading = signal(true);
  isSaving = signal(false);
  saveMessage = signal<string | null>(null);

  selectedNatureId = signal<number>(1);
  selectedSex = signal<'M' | 'H' | null>(null);
  isShiny = signal(false);
  movementIds = signal<(number | null)[]>([null, null, null, null]);

  selectedPokemon = computed<Pokemon | null>(() => this.pokemonTeam()?.pokemon ?? null);

  selectedMovements = computed<(Movement | null)[]>(() => {
    const ids = this.movementIds();
    const moves = this.allMovements();
    const resolve = (id: number | null) => (id ? (moves.find(m => m.id === id) ?? null) : null);
    return [resolve(ids[0]), resolve(ids[1]), resolve(ids[2]), resolve(ids[3])];
  });

  spriteUrl = computed<string | null>(() => {
    const pokemon = this.selectedPokemon();
    if (!pokemon) return null;
    return this.isShiny() ? (pokemon.spriteFrontShiny ?? pokemon.spriteFront) : pokemon.spriteFront;
  });

  displayName = computed(() => {
    const current = this.pokemonTeam();
    if (!current) return 'Pokemon';
    if (current.nickname) return current.nickname;
    return this.capitalize(current.pokemon?.name ?? 'Pokemon');
  });

  constructor() {
    const teamId = Number(this.route.snapshot.paramMap.get('teamId'));
    const pokemonTeamId = Number(this.route.snapshot.paramMap.get('pokemonTeamId'));

    if (!teamId || !pokemonTeamId) {
      this.router.navigate(['/team-builder']);
      return;
    }

    this.teamId.set(teamId);
    this.loadData(teamId, pokemonTeamId);
  }

  private async loadData(teamId: number, pokemonTeamId: number) {
    this.isLoading.set(true);
    this.saveMessage.set(null);

    const [allPokemonTeams, allPokemons, movements] = await Promise.all([
      this.pokemonTeamService.getAllPokemonTeams(),
      this.pokemonService.getAllPokemon(),
      this.movementService.getAllMovements(),
      this.natureService.getAllNatures(),
    ]);

    const dto = allPokemonTeams.find((pt: GetAllPokemonTeamDto) => pt.id === pokemonTeamId && pt.teamId === teamId);

    if (!dto) {
      this.router.navigate(['/team-builder', teamId]);
      return;
    }

    const pokemon = allPokemons.find(p => p.id === dto.pokemonId) ?? null;
    const mapped: PokemonTeam = {
      id: dto.id,
      nickname: dto.nickname ?? null,
      shiny: dto.shiny,
      sex: dto.sex ?? null,
      slot: dto.slot,
      teamId: dto.teamId,
      pokemonId: dto.pokemonId,
      natureId: dto.natureId,
      movementId1: dto.movementId1,
      movementId2: dto.movementId2 ?? null,
      movementId3: dto.movementId3 ?? null,
      movementId4: dto.movementId4 ?? null,
      pokemon,
    };

    this.pokemonTeam.set(mapped);
    this.allMovements.set(movements);

    this.selectedNatureId.set(mapped.natureId || 1);
    this.selectedSex.set(mapped.sex === 'M' || mapped.sex === 'H' ? mapped.sex : null);
    this.isShiny.set(mapped.shiny);
    this.movementIds.set([
      mapped.movementId1,
      mapped.movementId2,
      mapped.movementId3,
      mapped.movementId4,
    ]);

    this.isLoading.set(false);
  }

  onNatureChanged(nature: Nature) {
    this.selectedNatureId.set(nature.id);
  }

  onMovementChanged(change: { index: number; movementId: number | null }) {
    const next = [...this.movementIds()];
    next[change.index] = change.movementId;
    this.movementIds.set(next);
  }

  setSex(value: 'M' | 'H' | null) {
    this.selectedSex.set(value);
  }

  async savePokemonSettings() {
    const current = this.pokemonTeam();
    if (!current || this.isSaving()) {
      return;
    }

    const ids = this.movementIds();
    const movementId1 = ids[0] ?? 1;

    this.isSaving.set(true);
    this.saveMessage.set(null);

    const ok = await this.pokemonTeamService.updatePokemonTeam(current.id, {
      nickname: current.nickname,
      shiny: this.isShiny(),
      sex: this.selectedSex(),
      slot: current.slot,
      teamId: current.teamId,
      pokemonId: current.pokemonId,
      natureId: this.selectedNatureId(),
      movementId1,
      movementId2: ids[1],
      movementId3: ids[2],
      movementId4: ids[3],
    });

    this.isSaving.set(false);

    if (!ok) {
      this.saveMessage.set('No se pudieron guardar los cambios.');
      return;
    }

    this.router.navigate(['/team-builder', current.teamId]);
  }

  goBack() {
    const currentTeamId = this.teamId();
    if (!currentTeamId) {
      this.router.navigate(['/team-builder']);
      return;
    }

    this.router.navigate(['/team-builder', currentTeamId]);
  }

  private capitalize(value: string): string {
    if (!value) return '';
    return value.charAt(0).toUpperCase() + value.slice(1);
  }
}
