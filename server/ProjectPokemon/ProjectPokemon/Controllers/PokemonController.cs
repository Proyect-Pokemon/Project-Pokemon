using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.Pokemon;

namespace ProjectPokemon.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class PokemonController : ControllerBase {

        private readonly UnitOfWork _unitOfWork;
        public PokemonController(UnitOfWork unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IEnumerable<Pokemon>> GetAllPokemon() {
            return await _unitOfWork.PokemonRepository.GetAllAsync();
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<Pokemon>> GetAllPokemonById(int id) {
            Pokemon? pokemon = await _unitOfWork.PokemonRepository.GetByIdAsync(id);
            if (pokemon == null) {
                return NotFound();
            }
            return Ok(pokemon);
        }


        [HttpGet("Name")]
        public async Task<IEnumerable<SearchPokemonDto>> GetPokemonByNameAsync([FromQuery] string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return Enumerable.Empty<SearchPokemonDto>();
            }

            ICollection<Pokemon> pokemons = await _unitOfWork.PokemonRepository.SearchByNameAsync(name);

            IEnumerable<SearchPokemonDto> searchPokemonDto = pokemons.Select(pokemon => new SearchPokemonDto {
                Id = pokemon.Id,
                Name = pokemon.Name,
                SpriteFront = pokemon.SpriteFront
            });
            return searchPokemonDto;
        }
    }
}
