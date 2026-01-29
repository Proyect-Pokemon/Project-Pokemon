using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class PokemonController : ControllerBase {

        private readonly PokemonDbContext _context;
        public PokemonController(PokemonDbContext context) {
            _context = context;
        }
        [HttpGet]
        public async Task<IEnumerable<Pokemon>> GetAllPokemon() {
            return await _context.Pokemons.ToListAsync();
        }
    }
}
