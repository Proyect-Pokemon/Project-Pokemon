using ProjectPokemon.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ProjectPokemon.Models.Database.Repositories; 

public class PokemonRepository : BaseRepository<Pokemon, long> {
    public PokemonRepository(PokemonDbContext context) : base(context) {
    }

    public async Task<ICollection<Pokemon>> SearchByNameAsync(string name) {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<Pokemon>();

        string pattern = $"%{name}%";

        return await GetQueryable()
                    .Where(p => EF.Functions.Like(p.Name, pattern))
                    .ToArrayAsync();
    }
}
