using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database.Repositories; 
public class NatureRepository : BaseRepository<Nature, int> {
    public NatureRepository(PokemonDbContext context) : base(context) {
    }
}
