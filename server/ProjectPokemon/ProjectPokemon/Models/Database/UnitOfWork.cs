using ProjectPokemon.Models.Database.Repositories;

namespace ProjectPokemon.Models.Database {
    public class UnitOfWork {
        private readonly PokemonDbContext? _context;
        public UserRepository UserRepository => field ??= new UserRepository(_context!);
        public TeamRepository TeamRepository => field ??= new TeamRepository(_context!);
        public PokemonTeamRepository PokemonTeamRepository => field ??= new PokemonTeamRepository(_context!);

        public UnitOfWork(PokemonDbContext context) {
            _context = context;
        }

        public async Task<bool> SaveAsync() {
            return await _context!.SaveChangesAsync() > 0;
        }
    }
}
