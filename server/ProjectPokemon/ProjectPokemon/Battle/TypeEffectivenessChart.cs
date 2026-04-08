using ProjectPokemon.Enum;

namespace ProjectPokemon.Battle;

// Tabla de efectividad de tipos
// Contiene los multiplicadores de daño de cuánto afecta un tipo a otros.

public static class TypeEffectivenessChart {
    private static readonly Dictionary<PokeType, Dictionary<PokeType, double>> _chart = new() {
        [PokeType.Normal] = new() {
            [PokeType.Rock] = 0.5,
            [PokeType.Ghost] = 0.0,
            [PokeType.Steel] = 0.5
        },
        [PokeType.Fire] = new() {
            [PokeType.Fire] = 0.5,
            [PokeType.Water] = 0.5,
            [PokeType.Grass] = 2.0,
            [PokeType.Ice] = 2.0,
            [PokeType.Bug] = 2.0,
            [PokeType.Rock] = 0.5,
            [PokeType.Dragon] = 0.5,
            [PokeType.Steel] = 2.0
        },
        [PokeType.Water] = new() {
            [PokeType.Fire] = 2.0,
            [PokeType.Water] = 0.5,
            [PokeType.Grass] = 0.5,
            [PokeType.Ground] = 2.0,
            [PokeType.Rock] = 2.0,
            [PokeType.Dragon] = 0.5
        },
        [PokeType.Grass] = new() {
            [PokeType.Fire] = 0.5,
            [PokeType.Water] = 2.0,
            [PokeType.Grass] = 0.5,
            [PokeType.Poison] = 0.5,
            [PokeType.Ground] = 2.0,
            [PokeType.Flying] = 0.5,
            [PokeType.Bug] = 0.5,
            [PokeType.Rock] = 2.0,
            [PokeType.Dragon] = 0.5,
            [PokeType.Steel] = 0.5
        },
        [PokeType.Electric] = new() {
            [PokeType.Water] = 2.0,
            [PokeType.Grass] = 0.5,
            [PokeType.Electric] = 0.5,
            [PokeType.Ground] = 0.0,
            [PokeType.Flying] = 2.0,
            [PokeType.Dragon] = 0.5
        },
        [PokeType.Ice] = new() {
            [PokeType.Fire] = 0.5,
            [PokeType.Water] = 0.5,
            [PokeType.Grass] = 2.0,
            [PokeType.Ice] = 0.5,
            [PokeType.Ground] = 2.0,
            [PokeType.Flying] = 2.0,
            [PokeType.Dragon] = 2.0,
            [PokeType.Steel] = 0.5
        },
        [PokeType.Fighting] = new() {
            [PokeType.Normal] = 2.0,
            [PokeType.Ice] = 2.0,
            [PokeType.Poison] = 0.5,
            [PokeType.Flying] = 0.5,
            [PokeType.Psychic] = 0.5,
            [PokeType.Bug] = 0.5,
            [PokeType.Rock] = 2.0,
            [PokeType.Ghost] = 0.0,
            [PokeType.Dark] = 2.0,
            [PokeType.Steel] = 2.0,
            [PokeType.Fairy] = 0.5
        },
        [PokeType.Poison] = new() {
            [PokeType.Grass] = 2.0,
            [PokeType.Poison] = 0.5,
            [PokeType.Ground] = 0.5,
            [PokeType.Rock] = 0.5,
            [PokeType.Ghost] = 0.5,
            [PokeType.Steel] = 0.0,
            [PokeType.Fairy] = 2.0
        },
        [PokeType.Ground] = new() {
            [PokeType.Fire] = 2.0,
            [PokeType.Grass] = 0.5,
            [PokeType.Electric] = 2.0,
            [PokeType.Poison] = 2.0,
            [PokeType.Flying] = 0.0,
            [PokeType.Bug] = 0.5,
            [PokeType.Rock] = 2.0,
            [PokeType.Steel] = 2.0
        },
        [PokeType.Flying] = new() {
            [PokeType.Grass] = 2.0,
            [PokeType.Electric] = 0.5,
            [PokeType.Fighting] = 2.0,
            [PokeType.Bug] = 2.0,
            [PokeType.Rock] = 0.5,
            [PokeType.Steel] = 0.5
        },
        [PokeType.Psychic] = new() {
            [PokeType.Fighting] = 2.0,
            [PokeType.Poison] = 2.0,
            [PokeType.Psychic] = 0.5,
            [PokeType.Dark] = 0.0,
            [PokeType.Steel] = 0.5
        },
        [PokeType.Bug] = new() {
            [PokeType.Fire] = 0.5,
            [PokeType.Grass] = 2.0,
            [PokeType.Fighting] = 0.5,
            [PokeType.Poison] = 0.5,
            [PokeType.Flying] = 0.5,
            [PokeType.Psychic] = 2.0,
            [PokeType.Ghost] = 0.5,
            [PokeType.Dark] = 2.0,
            [PokeType.Steel] = 0.5,
            [PokeType.Fairy] = 0.5,
        },
        [PokeType.Rock] = new() {
            [PokeType.Fire] = 2.0,
            [PokeType.Ice] = 2.0,
            [PokeType.Fighting] = 0.5,
            [PokeType.Ground] = 0.5,
            [PokeType.Flying] = 2.0,
            [PokeType.Bug] = 2.0,
            [PokeType.Steel] = 0.5
        },
        [PokeType.Ghost] = new() {
            [PokeType.Ghost] = 0.0,
            [PokeType.Psychic] = 2.0,
            [PokeType.Ghost] = 2.0,
            [PokeType.Dark] = 0.5
        },
        [PokeType.Dragon] = new() {
            [PokeType.Dragon] = 2.0,
            [PokeType.Steel] = 0.5,
            [PokeType.Fairy] = 0.0
        },
        [PokeType.Dark] = new() {
            [PokeType.Fighting] = 0.5,
            [PokeType.Psychic] = 2.0,
            [PokeType.Ghost] = 2.0,
            [PokeType.Dark] = 0.5,
            [PokeType.Fairy] = 0.5
        },
        [PokeType.Steel] = new() {
            [PokeType.Fire] = 0.5,
            [PokeType.Water] = 0.5,
            [PokeType.Electric] = 0.5,
            [PokeType.Ice] = 2.0,
            [PokeType.Rock] = 2.0,
            [PokeType.Steel] = 0.5,
            [PokeType.Fairy] = 2.0
        },
        [PokeType.Fairy] = new() {
            [PokeType.Fire] = 0.5,
            [PokeType.Fighting] = 2.0,
            [PokeType.Poison] = 0.5,
            [PokeType.Dragon] = 2.0,
            [PokeType.Dark] = 2.0,
            [PokeType.Steel] = 0.5
        }
    };
}