using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;

namespace ProjectPokemon.Services.Internal;
// Este servicio va a llamar a los servicios que recogen los datos de la PokeApi
// y se va a encargar de insertar esos datos en la BD
public class DataLoader {

    private readonly PokemonDbContext _db;
    private readonly PokemonDataService _pokemonLoader;
    private readonly MovementDataService _movementLoader;
    public DataLoader(
    MovementDataService movementLoader,
    PokemonDbContext db,
    PokemonDataService pokemonLoader) {
        _db = db;
        _pokemonLoader = pokemonLoader;
        _movementLoader = movementLoader;
    }
    public async Task LoadAllDataAsync() {

        await _pokemonLoader.LoadGenerationAsync(1);

        await LoadPokemons();

        await LoadMovements();
    }
    private async Task LoadPokemons() {
        for (int id = 1; id <= 151; id++) {
            if (_db.Pokemons.Any(p => p.Id == id)) continue;

            var pokemon = await _pokemonLoader.LoadPokemon(id);
            _db.Pokemons.Add(pokemon);
            await _db.SaveChangesAsync();

            Console.WriteLine($"Cargado Pokemon: {pokemon.Name}");
        }
    }

    private async Task LoadMovements() {
        for (int id = 1; id <= 165; id++) {
            if (_db.Movements.Any(p => p.Id == id)) continue;

            var moves = await _movementLoader.LoadMovement(id);
            _db.Movements.Add(moves);
            await _db.SaveChangesAsync();

            Console.WriteLine($"Cargado Movimientos: {moves.Name}");
        }
    }
}
