using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Models.Database.Repositories; 
public class TeamRepository : BaseRepository<Team, int> {
    public TeamRepository(PokemonDbContext context) : base(context) { }
}
