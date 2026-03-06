export interface Pokemon {
    id: number;
    name: string;
    hp: number;
    attack: number;
    defense: number;
    specialAttack: number;
    specialDefense: number;
    speed: number;
    weight: number;
    spriteFront: string;
    spriteBack: string;
    spriteFrontShiny: string | null;
    spriteBackShiny: string | null;
    spriteFrontFem: string | null;
    spriteBackFem: string | null;
    spriteFrontFemShiny: string | null;
    spriteBackFemShiny: string | null;
    cry: string | null;
    type1: string;
    type2: string | null;
}
