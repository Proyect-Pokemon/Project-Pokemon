using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database.Repositories; 
public class MovementRepository : BaseRepository<Movement, int> {
    public MovementRepository(PokemonDbContext context) : base(context) {
    }
}
