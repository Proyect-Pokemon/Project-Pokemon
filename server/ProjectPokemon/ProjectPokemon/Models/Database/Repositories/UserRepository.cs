using ProjectPokemon.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ProjectPokemon.Models.Database.Repositories; 
public class UserRepository : BaseRepository<User, long> {
    public UserRepository(PokemonDbContext context) : base(context) {
    }
    public async Task<User?> GetUserByNicknameAsync(string nickname) {
        return await GetQueryable()
            .Where(u => u.Nickname == nickname)
            .FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await GetQueryable()
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();
    }
}
