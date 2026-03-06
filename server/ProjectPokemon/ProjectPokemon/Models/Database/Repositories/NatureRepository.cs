using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database.Repositories; 
public class NatureRepository : BaseRepository<Nature, long> {
    public NatureRepository(PokemonDbContext context) : base(context) {
    }
}
