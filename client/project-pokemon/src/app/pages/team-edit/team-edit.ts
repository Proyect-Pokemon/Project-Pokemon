import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Team, GetTeamDto } from '../../models/team';
import { PokemonTeam, GetAllPokemonTeamDto } from '../../models/pokemon-team';
import { Movement } from '../../models/move';
import { Nature } from '../../models/nature';
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

  readonly MAX_SLOTS = 6;

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
    return n ? this.capitalize(n.name) : '—';
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

  displayName = computed<string | null>(() => {
    const pt = this.selectedPokemonTeam();
    if (!pt) return null;
    if (pt.nickname) return pt.nickname;
    return pt.pokemon ? this.capitalize(pt.pokemon.name) : null;
  });

  selectedSprite = computed<string | null>(() => {
    const pt = this.selectedPokemonTeam();
    if (!pt?.pokemon) return null;
    return pt.shiny
      ? (pt.pokemon.spriteFrontShiny ?? pt.pokemon.spriteFront)
      : pt.pokemon.spriteFront;
  });

  constructor() {
    const teamId = Number(this.route.snapshot.paramMap.get('id'));
    if (!teamId) {
      this.router.navigate(['/team-builder']);
    } else {
      this.loadData(teamId);
    }
  }

  private async loadData(teamId: number) {
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

    const firstFilledSlot = pokemons.sort((a, b) => a.slot - b.slot)[0]?.slot ?? 1;
    this.selectedSlot.set(firstFilledSlot);

    this.isLoading.set(false);
  }

  getStatValue(key: string): number | string {
    const p = this.selectedPokemonTeam()?.pokemon;
    if (!p) return '—';
    return (p as unknown as Record<string, unknown>)[key] as number ?? '—';
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
      grass: 'leaf', fire: 'fire', water: 'water', electric: 'electric',
      ice: 'ice', fighting: 'fighting', poison: 'poison', ground: 'ground',
      flying: 'flying', bug: 'bug', rock: 'rock', ghost: 'ghost',
      dark: 'dark', steel: 'steel', fairy: 'fairy', normal: 'normal',
    };
    const file = map[type?.toLowerCase()];
    return file ? `/assets/icons/types/${file}.svg` : null;
  }

  getTypeClass(type: string): string {
    return type ? `type-${type.toLowerCase()}` : '';
  }

  getTypeColor(type: string): string {
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
    return colors[type?.toLowerCase()] ?? '#aaa';
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
}
