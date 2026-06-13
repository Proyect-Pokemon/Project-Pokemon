import { Component, inject, signal, computed, ViewChild, ElementRef, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Team, GetTeamDto } from '../../models/team';
import { PokemonTeam, GetAllPokemonTeamDto } from '../../models/pokemon-team';
import { Movement } from '../../models/move';
import { Nature } from '../../models/nature';
import { Pokemon } from '../../models/pokemon';
import { TeamService } from '../../services/team-service';
import { PokemonTeamService } from '../../services/pokemon-team-service';
import { PokemonService } from '../../services/pokemon-service';
import { MovementService } from '../../services/movement-service';
import { NatureService } from '../../services/nature-service';

@Component({
  selector: 'app-team-edit',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './team-edit.html',
  styleUrls: ['./team-edit.css'],
})
export class TeamEdit {
  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private teamService = inject(TeamService);
  private pokemonTeamService = inject(PokemonTeamService);
  private pokemonService = inject(PokemonService);
  private movementService = inject(MovementService);
  private natureService = inject(NatureService);

  team = signal<Team | null>(null);
  allNatures = signal<Nature[]>([]);
  allMovements = signal<Movement[]>([]);
  selectedSlot = signal(1);
  isLoading = signal(true);
  isDeletingPokemon = signal(false);
  showDeletePokemonModal = signal(false);
  searchQuery = signal('');
  searchResults = signal<Pokemon[]>([]);
  selectedPokemonFromSearch = signal<Pokemon | null>(null);
  searchError = signal<string | null>(null);
  allPokemonCache = signal<Pokemon[]>([]);

  readonly MAX_SLOTS = 6;

  // Drag & drop
  dragSourceSlot = signal<number | null>(null);
  dragOverSlot = signal<number | null>(null);
  isSwapping = signal(false);

  readonly statRows: { key: string; label: string; natureKey: string }[] = [
    { key: 'hp',             label: 'PS',         natureKey: 'hp' },
    { key: 'attack',         label: 'Ataque',     natureKey: 'attack' },
    { key: 'defense',        label: 'Defensa',    natureKey: 'defense' },
    { key: 'specialAttack',  label: 'Ata. Esp.',  natureKey: 'specialattack' },
    { key: 'specialDefense', label: 'Def. Esp.',  natureKey: 'specialdefense' },
    { key: 'speed',          label: 'Velocidad',  natureKey: 'speed' },
  ];

  selectedPokemonTeam = computed<PokemonTeam | null>(() =>
    this.team()?.pokemons.find(p => p.slot === this.selectedSlot()) ?? null
  );

  selectedNature = computed<Nature | null>(() => {
    const pt = this.selectedPokemonTeam();
    if (!pt) return null;
    return this.allNatures().find(n => n.id === pt.natureId) ?? null;
  });

  natureName = computed<string>(() => {
    const n = this.selectedNature();
    if (!n) return '—';
    const key = n.name.toLowerCase().replace(/[\s_\-]/g, '');
    const esNames: Record<string, string> = {
      hardy: 'Fuerte', bold: 'Osada', modest: 'Modesta', calm: 'Serena', timid: 'Miedosa',
      lonely: 'Huraña', docile: 'Dócil', mild: 'Afable', gentle: 'Amable', hasty: 'Activa',
      adamant: 'Firme', impish: 'Agitada', bashful: 'Tímida', careful: 'Cauta', jolly: 'Alegre',
      naughty: 'Pícara', lax: 'Floja', rash: 'Alocada', quirky: 'Rara', naive: 'Ingenua',
      brave: 'Audaz', relaxed: 'Plácida', quiet: 'Mansa', sassy: 'Grosera', serious: 'Seria',
    };
    return esNames[key] ?? this.capitalize(n.name);
  });

  selectedMovements = computed<(Movement | null)[]>(() => {
    const pt = this.selectedPokemonTeam();
    if (!pt) return [null, null, null, null];
    const all = this.allMovements();
    const resolve = (id: number | null) =>
      id ? (all.find(m => m.id === id) ?? null) : null;
    return [
      resolve(pt.movementId1),
      resolve(pt.movementId2),
      resolve(pt.movementId3),
      resolve(pt.movementId4),
    ];
  });

  slots = computed<(PokemonTeam | null)[]>(() => {
    const result: (PokemonTeam | null)[] = Array(this.MAX_SLOTS).fill(null);
    this.team()?.pokemons.forEach(p => {
      if (p.slot >= 1 && p.slot <= this.MAX_SLOTS) result[p.slot - 1] = p;
    });
    return result;
  });

  nextAvailableSlot = computed<number | null>(() => {
    const currentSlots = this.slots();
    const firstEmptyIndex = currentSlots.findIndex(slot => slot === null);
    if (firstEmptyIndex === -1) {
      return null;
    }
    return firstEmptyIndex + 1;
  });

  displayName = computed<string | null>(() => {
    const pt = this.selectedPokemonTeam();
    if (!pt) return null;
    if (pt.nickname) return pt.nickname;
    return pt.pokemon ? this.capitalize(pt.pokemon.name) : null;
  });

  selectedSprite = computed<string | null>(() => {
    const pt = this.selectedPokemonTeam();
    if (!pt?.pokemon) return null;

    const isFemale = pt.sex === 'H';
    if (pt.shiny) {
      if (isFemale && pt.pokemon.spriteFrontFemShiny) {
        return pt.pokemon.spriteFrontFemShiny;
      }

      return pt.pokemon.spriteFrontShiny ?? pt.pokemon.spriteFront;
    }

    if (isFemale && pt.pokemon.spriteFrontFem) {
      return pt.pokemon.spriteFrontFem;
    }

    return pt.pokemon.spriteFront;
  });

  constructor() {
    effect(() => {
      if (!this.isCurrentSlotAddable()) {
        return;
      }

      this.updateSearchResults();
      this.focusSearchInput();
    });

    const teamId = Number(this.route.snapshot.paramMap.get('id'));
    const slotParam = Number(this.route.snapshot.queryParamMap.get('slot'));
    const preferredSlot = Number.isNaN(slotParam) ? null : slotParam;
    if (!teamId) {
      this.router.navigate(['/team-builder']);
    } else {
      this.loadData(teamId, preferredSlot);
    }
  }

  private async loadData(teamId: number, preferredSlot: number | null = null) {
    this.isLoading.set(true);

    const [allTeams, allPokemonTeams, allPokemons, natures, movements] = await Promise.all([
      this.teamService.getAllTeams(),
      this.pokemonTeamService.getAllPokemonTeams(),
      this.pokemonService.getAllPokemon(),
      this.natureService.getAllNatures(),
      this.movementService.getAllMovements(),
    ]);

    const teamDto = allTeams.find((t: GetTeamDto) => t.id === teamId);
    if (!teamDto) {
      this.router.navigate(['/team-builder']);
      return;
    }

    const pokemonMap = new Map(allPokemons.map(p => [p.id, p]));
    this.allPokemonCache.set(allPokemons);
    const pokemons: PokemonTeam[] = allPokemonTeams
      .filter((pt: GetAllPokemonTeamDto) => pt.teamId === teamId)
      .map((pt: GetAllPokemonTeamDto): PokemonTeam => ({
        id: pt.id,
        nickname: pt.nickname ?? null,
        shiny: pt.shiny,
        sex: pt.sex ?? null,
        slot: pt.slot,
        teamId: pt.teamId,
        pokemonId: pt.pokemonId,
        natureId: pt.natureId,
        movementId1: pt.movementId1,
        movementId2: pt.movementId2 ?? null,
        movementId3: pt.movementId3 ?? null,
        movementId4: pt.movementId4 ?? null,
        pokemon: pokemonMap.get(pt.pokemonId) ?? null,
      }));

    this.team.set({ ...teamDto, pokemons, isExpanded: false });
    this.allNatures.set(natures);
    this.allMovements.set(movements);

    const firstFilledSlot = [...pokemons].sort((a, b) => a.slot - b.slot)[0]?.slot ?? 1;
    const nextSelectedSlot = preferredSlot && preferredSlot >= 1 && preferredSlot <= this.MAX_SLOTS
      ? preferredSlot
      : firstFilledSlot;
    this.selectedSlot.set(nextSelectedSlot);

    this.isLoading.set(false);

    this.tryPrepareSearchPanel();
  }

  isSlotSelectable(slotNumber: number): boolean {
    const slot = this.slots()[slotNumber - 1];
    if (slot !== null) {
      return true;
    }
    return this.nextAvailableSlot() === slotNumber;
  }

  isNextAvailableSlot(slotNumber: number): boolean {
    const slot = this.slots()[slotNumber - 1];
    return slot === null && this.nextAvailableSlot() === slotNumber;
  }

  isCurrentSlotAddable(): boolean {
    return this.selectedPokemonTeam() === null && this.nextAvailableSlot() === this.selectedSlot();
  }

  onSlotSelect(slotNumber: number) {
    if (!this.isSlotSelectable(slotNumber)) {
      return;
    }

    this.selectedSlot.set(slotNumber);
    this.tryPrepareSearchPanel();
  }

  async onPokemonSelected(pokemon: Pokemon) {
    const currentTeam = this.team();
    if (!currentTeam || !this.isCurrentSlotAddable()) {
      return;
    }

    const targetSlot = this.selectedSlot();

    try {
      await this.pokemonTeamService.addPokemonTeam({
        nickname: null,
        shiny: false,
        slot: targetSlot,
        teamId: currentTeam.id,
        pokemonId: pokemon.id,
        natureId: 1,
        movementId1: 1,
        movementId2: null,
        movementId3: null,
        movementId4: null,
      });

      await this.loadData(currentTeam.id, targetSlot);

    } catch (error) {
      console.error('Error añadiendo Pokémon', error);
    }
  }

  private tryPrepareSearchPanel() {
    if (!this.isCurrentSlotAddable()) {
      return;
    }

    setTimeout(() => {
      this.updateSearchResults();
      this.focusSearchInput();
    }, 0);
  }

  onSearchInput(event: Event) {
    const target = event.target as HTMLInputElement | null;
    this.searchQuery.set(target?.value ?? '');
    this.selectedPokemonFromSearch.set(null);
    this.updateSearchResults();
  }

  selectPokemonFromGrid(pokemon: Pokemon) {
    this.selectedPokemonFromSearch.set(pokemon);
  }

  async onAddPokemonFromSearch() {
    const pokemon = this.selectedPokemonFromSearch();
    if (!pokemon) {
      return;
    }

    await this.onPokemonSelected(pokemon);
    this.resetSearchState();
  }

  showAllPokemonFromSearch(): void {
    this.selectedPokemonFromSearch.set(null);
    this.searchError.set(null);
    this.searchQuery.set('');
    this.updateSearchResults();
    this.focusSearchInput();
  }

  private updateSearchResults() {
    const cache = this.allPokemonCache();
    if (!cache.length) {
      return;
    }

    const query = this.searchQuery().trim();
    this.searchError.set(null);

    if (!query) {
      this.searchResults.set([...cache].sort((a, b) => a.id - b.id));
      return;
    }

    const numericId = Number(query);
    if (!Number.isNaN(numericId)) {
      const foundById = cache.filter(p => p.id === numericId);
      if (!foundById.length) {
        this.searchError.set('No se encontró Pokémon con ese número.');
        this.searchResults.set([]);
        return;
      }

      this.searchResults.set(foundById);
      if (foundById.length === 1) {
        this.selectedPokemonFromSearch.set(foundById[0]);
      }
      return;
    }

    const normalizedQuery = query.toLowerCase();
    const foundByName = cache
      .filter(p => p.name.toLowerCase().includes(normalizedQuery))
      .sort((a, b) => a.id - b.id);

    if (!foundByName.length) {
      this.searchError.set('No se encontraron Pokémon con ese nombre.');
      this.searchResults.set([]);
      return;
    }

    this.searchResults.set(foundByName);
    if (foundByName.length === 1) {
      this.selectedPokemonFromSearch.set(foundByName[0]);
    }
  }

  private resetSearchState() {
    this.searchQuery.set('');
    this.searchResults.set([]);
    this.selectedPokemonFromSearch.set(null);
    this.searchError.set(null);
  }

  private focusSearchInput() {
    setTimeout(() => {
      this.searchInput?.nativeElement.focus();
    }, 0);
  }

  getStatValue(key: string): number | string {
    const p = this.selectedPokemonTeam()?.pokemon;
    if (!p) return '—';

    const baseValue = (p as unknown as Record<string, unknown>)[key];
    if (typeof baseValue !== 'number') {
      return '—';
    }

    // La vida no se modifica
    if (key === 'hp') {
      return baseValue;
    }

    const statKeyToNatureKey: Record<string, string> = {
      attack: 'attack',
      defense: 'defense',
      specialAttack: 'specialattack',
      specialDefense: 'specialdefense',
      speed: 'speed',
    };

    const natureKey = statKeyToNatureKey[key];
    if (!natureKey) {
      return baseValue;
    }

    return Math.floor(baseValue * this.getNatureMultiplier(natureKey));
  }

  private getNatureMultiplier(natureKey: string): number {
    const nature = this.selectedNature();
    if (!nature) {
      return 1;
    }

    const numericMap: Record<number, string> = {
      0: 'hp',
      1: 'attack',
      2: 'defense',
      3: 'specialattack',
      4: 'specialdefense',
      5: 'speed',
    };

    const normalize = (v: string | number): string => {
      if (typeof v === 'number') {
        return numericMap[v] ?? '';
      }

      return v.toLowerCase().replace(/[\s_]/g, '');
    };

    const boost = normalize(nature.statBoost);
    const drop = normalize(nature.statDrop);

    if (boost === drop) {
      return 1;
    }

    if (boost === natureKey) {
      return 1.1;
    }

    if (drop === natureKey) {
      return 0.9;
    }

    return 1;
  }

  getNatureArrow(natureKey: string): '▲' | '▼' | null {
    const nature = this.selectedNature();
    if (!nature) return null;
    const numericMap: Record<number, string> = {
      0: 'hp', 1: 'attack', 2: 'defense',
      3: 'specialattack', 4: 'specialdefense', 5: 'speed',
    };
    const normalize = (v: string | number): string => {
      if (typeof v === 'number') return numericMap[v] ?? '';
      return v.toLowerCase().replace(/[\s_]/g, '');
    };
    const boost = normalize(nature.statBoost);
    const drop = normalize(nature.statDrop);
    if (boost === drop) return null;
    if (boost === natureKey) return '▲';
    if (drop === natureKey) return '▼';
    return null;
  }

  getTypeIconSrc(type: string): string | null {
    const map: Record<string, string> = {
      grass: 'leaf', planta: 'leaf', fire: 'fire', water: 'water', electric: 'electric',
      ice: 'ice', fighting: 'fighting', poison: 'poison', ground: 'ground',
      flying: 'flying', bug: 'bug', rock: 'rock', ghost: 'ghost',
      dark: 'dark', steel: 'steel', fairy: 'fairy', normal: 'normal',
      psychic: 'psychic', psiquico: 'psychic', dragon: 'dragon',
    };
    const normalizedType = type
      ?.normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
    const file = map[normalizedType];
    return file ? `/assets/icons/types/${file}.svg` : null;
  }

  getTypeClass(type: string): string {
    const normalizedType = type
      ?.normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
    return normalizedType ? `type-${normalizedType}` : '';
  }

  getTypeColor(type: string): string {
    const normalizedType = type
      ?.normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
    const colors: Record<string, string> = {
      grass: 'var(--grass-color)',     fire: 'var(--fire-color)',
      water: 'var(--water-color)',     electric: 'var(--electric-color)',
      normal: 'var(--normal-color)',   poison: 'var(--poison-color)',
      ice: 'var(--ice-color)',         ground: 'var(--ground-color)',
      rock: 'var(--rock-color)',       fighting: 'var(--fighting-color)',
      flying: 'var(--flying-color)',   dark: 'var(--dark-color)',
      psychic: 'var(--psychic-color)', ghost: 'var(--ghost-color)',
      steel: 'var(--steel-color)',     bug: 'var(--bug-color)',
      dragon: 'var(--dragon-color)',   fairy: 'var(--fairy-color)',
    };
    return colors[normalizedType] ?? '#aaa';
  }

  capitalize(s: string): string {
    if (!s) return '';
    return s.charAt(0).toUpperCase() + s.slice(1);
  }

  padId(id: number | null | undefined): string {
    if (id == null) return '???';
    return String(id).padStart(3, '0');
  }

  goBack() {
    this.router.navigate(['/team-builder']);
  }

  goToPokemonEdit() {
    const currentTeam = this.team();
    const selectedPokemonTeam = this.selectedPokemonTeam();
    if (!currentTeam || !selectedPokemonTeam) {
      return;
    }

    this.router.navigate(['/team-builder', currentTeam.id, 'pokemon', selectedPokemonTeam.id, 'edit']);
  }

  requestDeleteSelectedPokemon() {
    if (!this.selectedPokemonTeam() || this.isDeletingPokemon()) {
      return;
    }

    this.showDeletePokemonModal.set(true);
  }

  cancelDeleteSelectedPokemon() {
    this.showDeletePokemonModal.set(false);
  }

  async onDeleteSelectedPokemon() {
    const currentTeam = this.team();
    const selectedPokemonTeam = this.selectedPokemonTeam();

    if (!currentTeam || !selectedPokemonTeam || this.isDeletingPokemon()) {
      return;
    }

    this.isDeletingPokemon.set(true);
    this.showDeletePokemonModal.set(false);

    const deletedSlot = selectedPokemonTeam.slot;
    const remainingCount = currentTeam.pokemons.length - 1;

    try {
      await this.pokemonTeamService.deletePokemonTeam(selectedPokemonTeam.id);
      const nextSlot = Math.min(deletedSlot, remainingCount + 1);
      await this.loadData(currentTeam.id, nextSlot);
    } catch (error) {
      console.error('Error eliminando Pokémon', error);
    } finally {
      this.isDeletingPokemon.set(false);
    }
  }

  // Drag & drop

  onDragStart(event: DragEvent, slotNumber: number): void {
    const slot = this.slots()[slotNumber - 1];
    if (!slot) return;
    this.dragSourceSlot.set(slotNumber);
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', String(slotNumber));
    }
  }

  onDragOver(event: DragEvent, slotNumber: number): void {
    const source = this.dragSourceSlot();
    if (source === null || source === slotNumber) return;
    const targetSlot = this.slots()[slotNumber - 1];
    if (!targetSlot) return; // sólo se puede soltar sobre slots con pokémon
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    this.dragOverSlot.set(slotNumber);
  }

  onDragLeave(slotNumber: number): void {
    if (this.dragOverSlot() === slotNumber) {
      this.dragOverSlot.set(null);
    }
  }

  onDragEnd(): void {
    this.dragSourceSlot.set(null);
    this.dragOverSlot.set(null);
  }

  async onDrop(event: DragEvent, targetSlotNumber: number): Promise<void> {
    event.preventDefault();
    const sourceSlotNumber = this.dragSourceSlot();
    this.dragSourceSlot.set(null);
    this.dragOverSlot.set(null);

    if (sourceSlotNumber === null || sourceSlotNumber === targetSlotNumber) return;

    const currentTeam = this.team();
    if (!currentTeam || this.isSwapping()) return;

    const sourceEntry = this.slots()[sourceSlotNumber - 1];
    const targetEntry = this.slots()[targetSlotNumber - 1];
    if (!sourceEntry || !targetEntry) return;

    this.isSwapping.set(true);

    try {
      // Swap optimista en local para feedback inmediato
      const updatedPokemons = currentTeam.pokemons.map(p => {
        if (p.slot === sourceSlotNumber) return { ...p, slot: targetSlotNumber };
        if (p.slot === targetSlotNumber) return { ...p, slot: sourceSlotNumber };
        return p;
      });
      this.team.set({ ...currentTeam, pokemons: updatedPokemons });
      // Mover el foco al slot destino
      this.selectedSlot.set(targetSlotNumber);

      // Persistir: se intercambia usando un slot temporal (999) para evitar conflicto unique
      await this.pokemonTeamService.updatePokemonTeam(sourceEntry.id, {
        nickname: sourceEntry.nickname,
        shiny: sourceEntry.shiny,
        sex: sourceEntry.sex,
        slot: 999,
        teamId: sourceEntry.teamId,
        pokemonId: sourceEntry.pokemonId,
        natureId: sourceEntry.natureId,
        movementId1: sourceEntry.movementId1,
        movementId2: sourceEntry.movementId2,
        movementId3: sourceEntry.movementId3,
        movementId4: sourceEntry.movementId4,
      });

      await this.pokemonTeamService.updatePokemonTeam(targetEntry.id, {
        nickname: targetEntry.nickname,
        shiny: targetEntry.shiny,
        sex: targetEntry.sex,
        slot: sourceSlotNumber,
        teamId: targetEntry.teamId,
        pokemonId: targetEntry.pokemonId,
        natureId: targetEntry.natureId,
        movementId1: targetEntry.movementId1,
        movementId2: targetEntry.movementId2,
        movementId3: targetEntry.movementId3,
        movementId4: targetEntry.movementId4,
      });

      await this.pokemonTeamService.updatePokemonTeam(sourceEntry.id, {
        nickname: sourceEntry.nickname,
        shiny: sourceEntry.shiny,
        sex: sourceEntry.sex,
        slot: targetSlotNumber,
        teamId: sourceEntry.teamId,
        pokemonId: sourceEntry.pokemonId,
        natureId: sourceEntry.natureId,
        movementId1: sourceEntry.movementId1,
        movementId2: sourceEntry.movementId2,
        movementId3: sourceEntry.movementId3,
        movementId4: sourceEntry.movementId4,
      });

    } catch (error) {
      console.error('Error al intercambiar slots', error);
      // Recargar para restaurar estado real
      await this.loadData(currentTeam.id, this.selectedSlot());
    } finally {
      this.isSwapping.set(false);
    }
  }
}