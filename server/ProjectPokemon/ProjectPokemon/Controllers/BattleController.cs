using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using System.Reflection.Emit;

// Este controlador devolverá Pokemon A y Pokemon B, con sus respectivos movimientos para el combate
namespace ProjectPokemon.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class BattleController : ControllerBase {

        // Inyectar la BD
        private readonly PokemonDbContext _context;
        public BattleController(PokemonDbContext context) {
            _context = context; 
        }

        [HttpGet]
        public async Task<IActionResult> GetBattle() {
            // Pokemon A, el pokemon del usuario
            var usersPokemon = await _context.Pokemons.FirstAsync(p => p.Id == 1);
            var opponentsPokemon = await _context.Pokemons.FirstAsync(p => p.Id == 23);

            var usersMoves = await _context.Movements
                .Where(m => new[] { 15, 53, 12, 34 }.Contains(m.Id))
                .ToListAsync();

            var opponentsMoves = await _context.Movements
                .Where(m => new[] { 16, 52, 11, 32 }.Contains(m.Id))
                .ToListAsync();

            var user = new {
                name = usersPokemon.Name,
                sprite = usersPokemon.SpriteBack,
                maxHp = CalculateHp(usersPokemon.Hp),
                currentHp = CalculateHp(usersPokemon.Hp),
                moves = usersMoves.Select(m => new {
                    name = m.Name,
                    description = m.Description,
                    pp = m.Pp   
                })
            };

            var opponent = new {
                name = opponentsPokemon.Name,
                sprite = opponentsPokemon.SpriteBack,
                maxHp = CalculateHp(opponentsPokemon.Hp),
                currentHp = CalculateHp(opponentsPokemon.Hp),
                moves = opponentsMoves.Select(m => new {
                    name = m.Name,
                    description = m.Description,
                    pp = m.Pp
                })
            };

            // Devuelve los datos de los pokemon de usuario y el contrincante
            return Ok(new { PokemonA = user, PokemonB = opponent });
        }

        private int CalculateHp(int baseHp) {
            int level = 50; // Es el nivel estándar en competitivo
            int iv = 31; // IV máximos
            int ev = 0; // Sin EV o podemos cambiar a 252, que es el máximo

            return (int)Math.Floor((2 * baseHp + iv + ev / 4.0 * level) / 100) + level + 10;
        }
    }
}
