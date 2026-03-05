using Microsoft.AspNetCore.Authorization;
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

    //GET: api/team
    [HttpGet]
    [Authorize]
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


    // POST
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PostTeamDto>> AddTeam([FromBody] PostTeamDto dto) {
        Team team = new Team {
            Name = dto.Name,
            Description = dto.Description,
            UserId = dto.UserId
        };

        await _unitOfWork.TeamRepository.InsertAsync(team);
        bool success = await _unitOfWork.SaveAsync();

        if (!success) {
            return BadRequest();
        }

        return Ok(dto);
    }

    // PUT
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<PutTeamDto>> UpdateTeam(int id, [FromBody] PutTeamDto dto) {
        Team? team = await _unitOfWork.TeamRepository.GetByIdAsync(id);
        if (team == null) {
            return NotFound();
        }
        team.Name = dto.Name;
        team.Description = dto.Description;
        await _unitOfWork.TeamRepository.UpdateAsync(team);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) {
            return BadRequest();
        }
        return Ok(dto);
    }
    // DELETE
}
