using PokeApiNet;
using ProjectPokemon.Enum;
using PokemonEntity = ProjectPokemon.Models.Database.Entities.Pokemon;

namespace ProjectPokemon.Services.Internal;

public class PokemonDataService {
    private readonly PokeApiClient client = new PokeApiClient();

    //public async Task LoadGenerationAsync(int generation) {
    //    Generation gen = await client.GetResourceAsync<Generation>(generation);
    //    foreach (NamedApiResource<PokemonSpecies> specie in gen.PokemonSpecies) {
    //        client.GetResourceAsync<PokemonSpecies>(specie.Name);
    //    }
    //}

    public async Task<PokemonEntity> LoadPokemon(int id) {
        var pokemon = await client.GetResourceAsync<Pokemon>(id);

        // Recogiendo los datos de las estadísticas del pokemon
        int hp = pokemon.Stats.First(p => p.Stat.Name == "hp").BaseStat;
        int atk = pokemon.Stats.First(p => p.Stat.Name == "attack").BaseStat;
        int def = pokemon.Stats.First(p => p.Stat.Name == "defense").BaseStat;
        int spa = pokemon.Stats.First(p => p.Stat.Name == "special-attack").BaseStat;
        int spd = pokemon.Stats.First(p => p.Stat.Name == "special-defense").BaseStat;
        int spe = pokemon.Stats.First(p => p.Stat.Name == "speed").BaseStat;

        // Recogemos los tipos del pokemon
        var types = pokemon.Types.OrderBy(t => t.Slot).ToList();

        PokeType type1 = ConvertType(types[0].Type.Name); // Convierte los strings a su valor del enum correspondiente
        PokeType? type2 = types.Count > 1 ? ConvertType(types[1].Type.Name) : null;

        // Quitamos los tipo Hada por tipo normal
        if (type1 == PokeType.Fairy) {
            type1 = PokeType.Normal;
        }
        // Si el tipo2 es Hada, lo ponemos en nulo, para que no
        // haya pokemons que sean "Normal Normal"
        if (type2 == PokeType.Fairy) {
            type2 = null;
        }

        // Creamos el Pokemon con los datos que hemos recogido
        return new PokemonEntity {
            Id = pokemon.Id,
            Name = pokemon.Name,
            Hp = hp,
            Attack = atk,
            Defense = def,
            SpecialAttack = spa,
            SpecialDefense = spd,
            Speed = spe,
            Weight = pokemon.Weight / 10,
            SpriteFront = pokemon.Sprites.Versions.GenerationV.BlackWhite.Animated.FrontDefault, // TO DO --> Esto recoge una url de la PokeApi
            SpriteBack = pokemon.Sprites.Versions.GenerationV.BlackWhite.Animated.BackDefault, // Necesitamos que sea una url a nuestro wwwroot
            Type1 = type1,
            Type2 = type2
            //
            // TO DO --> Hay que añadir los atributos que faltan
            //
        };
    }

    private PokeType ConvertType(string type) {
        return type switch {
            "normal" => PokeType.Normal,
            "fire" => PokeType.Fire,
            "water" => PokeType.Water,
            "grass" => PokeType.Grass,
            "electric" => PokeType.Electric,
            "ice" => PokeType.Ice,
            "fighting" => PokeType.Fighting,
            "poison" => PokeType.Poison,
            "ground" => PokeType.Ground,
            "flying" => PokeType.Flying,
            "psychic" => PokeType.Psychic,
            "bug" => PokeType.Bug,
            "rock" => PokeType.Rock,
            "ghost" => PokeType.Ghost,
            "dragon" => PokeType.Dragon,
            "dark" => PokeType.Dark,
            "steel" => PokeType.Steel,
            "fairy" => PokeType.Fairy,
            _ => throw new Exception("Tipo desconocido: " + type)
        };
    }
}
