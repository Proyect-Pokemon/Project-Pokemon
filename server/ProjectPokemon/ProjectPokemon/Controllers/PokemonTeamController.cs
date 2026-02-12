using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.PokemonTeam;
using ProjectPokemon.Models.Dtos.Team;

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
}
