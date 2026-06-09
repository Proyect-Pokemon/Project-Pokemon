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
    spriteFrontShiny: string;
    spriteBackShiny: string;
    spriteFrontFem: string | null;
    spriteBackFem: string | null;
    spriteFrontFemShiny: string | null;
    spriteBackFemShiny: string | null;
    miniSprite: string;
    type1: string;
    type2: string | null;
}
