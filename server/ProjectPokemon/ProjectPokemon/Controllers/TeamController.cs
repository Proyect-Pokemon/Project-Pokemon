using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.Team;

namespace ProjectPokemon.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TeamController : ControllerBase {
    private readonly UnitOfWork _unitOfWork;
    public TeamController(UnitOfWork unitOfWork) {
        _unitOfWork = unitOfWork;
    }

    //GET: api/users
    [HttpGet]
    public async Task<IEnumerable<GetTeamDto>> GetAllTeams() {
        ICollection<Team> Teams = await _unitOfWork.TeamRepository.GetAllAsync();

        IEnumerable<GetTeamDto> getTeamsDto = Teams.Select(team => new GetTeamDto {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            UserId = team.UserId
        });
        return getTeamsDto;
    }
}
