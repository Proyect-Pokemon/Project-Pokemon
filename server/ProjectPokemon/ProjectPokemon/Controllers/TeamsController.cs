using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ProjectPokemon.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        // GET: api/teams
        [HttpGet]
        public IActionResult GetDefaultTeam()
        {
            var team = new List<object>
            {
                new
                {
                    name = "Equipo Demo",
                    pokemons = new List<string> { "Charmander", "Pidgey" }
                }
            };

            return Ok(team);
        }

        // POST: api/teams
        [HttpPost]
        public IActionResult CreateTeam([FromBody] object teamRequest)
        {
            // Aquí podemos simplemente devolver siempre el equipo por defecto
            var defaultTeam = new
            {
                name = "Equipo Demo",
                pokemons = new List<string> { "Charmander", "Pidgey" }
            };

            return Ok(defaultTeam);
        }
    }
}