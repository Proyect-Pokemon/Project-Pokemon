using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database.Repositories; 
public class MovementRepository : BaseRepository<Movement, long> {
    public MovementRepository(PokemonDbContext context) : base(context) {
    }
}
