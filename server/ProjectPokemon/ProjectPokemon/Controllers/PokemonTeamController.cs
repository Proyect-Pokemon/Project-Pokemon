using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.PokemonTeam;
using ProjectPokemon.Models.Dtos.Team;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPokemon.Controllers; 
[Route("api/[controller]")]
[ApiController]
public class PokemonTeamController : ControllerBase {
    private readonly UnitOfWork _unitOfWork;
    public PokemonTeamController(UnitOfWork unitOfWork) {
        _unitOfWork = unitOfWork;
    }

    //GET: api/pokemonteam
    [HttpGet]
    public async Task<IEnumerable<GetAllPokemonTeamDto>> GetAllPokemonTeams() {
        ICollection<PokemonTeam> PokemonTeams = await _unitOfWork.PokemonTeamRepository.GetAllAsync();

        IEnumerable<GetAllPokemonTeamDto> getAllPokemonTeamsDto = PokemonTeams.Select(PokemonTeam => new GetAllPokemonTeamDto {
            Id = PokemonTeam.Id,
            Nickname = PokemonTeam.Nickname,
            Shiny = PokemonTeam.Shiny,
            Slot = PokemonTeam.Slot,
            TeamId = PokemonTeam.TeamId,
            PokemonId = PokemonTeam.PokemonId,
            NatureId = PokemonTeam.NatureId,
            MovementId1 = PokemonTeam.MovementId1,
            MovementId2 = PokemonTeam.MovementId2,
            MovementId3 = PokemonTeam.MovementId3,
            MovementId4 = PokemonTeam.MovementId4
        });
        return getAllPokemonTeamsDto;
    }

    // POST
    [HttpPost]
    public async Task<ActionResult<PostPokemonTeamDto>> AddPokemonTeam([FromBody] PostPokemonTeamDto dto) {
        // Validar rango de slot
        if (dto.Slot < 1 || dto.Slot > 6) {
            return BadRequest(new { error = "El campo 'Slot' debe estar entre 1 y 6." });
        }

        // Validar que no exista ya un PokemonTeam con el mismo TeamId y Slot
        bool existsSameSlot = await _unitOfWork.PokemonTeamRepository
            .GetQueryable()
            .AnyAsync(pt => pt.TeamId == dto.TeamId && pt.Slot == dto.Slot);
        if (existsSameSlot) {
            return BadRequest(new { error = "Ya existe un Pokémon en ese slot para el equipo indicado." });
        }

        PokemonTeam pokemonteam = new PokemonTeam {
            Nickname = dto.Nickname,
            Shiny = dto.Shiny = false,
            Slot = dto.Slot,
            TeamId = dto.TeamId,
            PokemonId = dto.PokemonId,
            NatureId = dto.NatureId,
            MovementId1 = dto.MovementId1,
            MovementId2 = dto.MovementId2,
            MovementId3 = dto.MovementId3,
            MovementId4 = dto.MovementId4
        };

        await _unitOfWork.PokemonTeamRepository.InsertAsync(pokemonteam);
        bool success = await _unitOfWork.SaveAsync();

        if (!success) {
            return BadRequest();
        }

        return Ok(dto);
    }
}
