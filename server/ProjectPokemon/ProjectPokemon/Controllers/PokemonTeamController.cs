using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.PokemonTeam;

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
    [Authorize]
    public async Task<IEnumerable<GetAllPokemonTeamDto>> GetAllPokemonTeams() {
        ICollection<PokemonTeam> PokemonTeams = await _unitOfWork.PokemonTeamRepository.GetAllAsync();

        IEnumerable<GetAllPokemonTeamDto> getAllPokemonTeamsDto = PokemonTeams.Select(PokemonTeam => new GetAllPokemonTeamDto {
            Id = PokemonTeam.Id,
            Nickname = PokemonTeam.Nickname,
            Sex = PokemonTeam.Sex,
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
    [Authorize]
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

        //Validar que el TeamId exista
        bool teamExists = await _unitOfWork.TeamRepository.GetQueryable().AnyAsync(t => t.Id == dto.TeamId);
        if (!teamExists) {
            return BadRequest(new { error = "El TeamId proporcionado no existe." });
        }

        // Validar que el sexo sea M, H o null
        if (dto.Sex != null && dto.Sex != 'M' && dto.Sex != 'H') {
            return BadRequest(new { error = "El campo 'Sex' debe ser 'M', 'H' o null." });
        }

        // Validar que los movimientos no sean repetidos
        HashSet<int> movimientos = new HashSet<int>();
        movimientos.Add(dto.MovementId1);

        if (dto.MovementId2 != null) {
            if (!movimientos.Add(dto.MovementId2.Value)) {
                return BadRequest(new { error = "Los movimientos no pueden ser repetidos." });
            }
        }

        if (dto.MovementId3 != null) {
            if (!movimientos.Add(dto.MovementId3.Value)) {
                return BadRequest(new { error = "Los movimientos no pueden ser repetidos." });
            }
        }

        if (dto.MovementId4 != null) {
            if (!movimientos.Add(dto.MovementId4.Value)) {
                return BadRequest(new { error = "Los movimientos no pueden ser repetidos." });
            }
        }

        PokemonTeam pokemonteam = new PokemonTeam {
            Nickname = dto.Nickname,
            Sex = dto.Sex,
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

    // DELETE
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePokemonTeam(int id) {
        PokemonTeam? pokemonteam = await _unitOfWork.PokemonTeamRepository.GetByIdAsync(id);
        if (pokemonteam == null) {
            return NotFound();
        }
        await _unitOfWork.PokemonTeamRepository.DeleteAsync(pokemonteam);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) {
            return BadRequest();
        }
        return Ok();
    }

    // PUT
    [HttpPut("{id}All")]
    [Authorize]
    public async Task<IActionResult> UpdatePokemonTeam(int id, [FromBody] PutPokemonTeamDto dto) {
        // Validar que el sexo sea M, H o null
        if (dto.Sex != null && dto.Sex != 'M' && dto.Sex != 'H') {
            return BadRequest(new { error = "El campo 'Sex' debe ser 'M', 'H' o null." });
        }

        // Validar que los movimientos no sean repetidos
        HashSet<int> movimientos = new HashSet<int>();
        movimientos.Add(dto.MovementId1);

        if (dto.MovementId2 != null) {
            if (!movimientos.Add(dto.MovementId2.Value)) {
                return BadRequest(new { error = "Los movimientos no pueden ser repetidos." });
            }
        }

        if (dto.MovementId3 != null) {
            if (!movimientos.Add(dto.MovementId3.Value)) {
                return BadRequest(new { error = "Los movimientos no pueden ser repetidos." });
            }
        }

        if (dto.MovementId4 != null) {
            if (!movimientos.Add(dto.MovementId4.Value)) {
                return BadRequest(new { error = "Los movimientos no pueden ser repetidos." });
            }
        }

        PokemonTeam? pokemonteam = await _unitOfWork.PokemonTeamRepository.GetByIdAsync(id);
        if (pokemonteam == null) {
            return NotFound();
        }
        pokemonteam.Shiny = dto.Shiny;
        pokemonteam.NatureId = dto.NatureId;
        pokemonteam.Sex = dto.Sex;
        pokemonteam.MovementId1 = dto.MovementId1;
        pokemonteam.MovementId2 = dto.MovementId2;
        pokemonteam.MovementId3 = dto.MovementId3;
        pokemonteam.MovementId4 = dto.MovementId4;
        await _unitOfWork.PokemonTeamRepository.UpdateAsync(pokemonteam);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) {
            return BadRequest();
        }
        return Ok(dto);
    }

    [HttpPut("{id}Nickname")]
    [Authorize]
    public async Task<IActionResult> UpdatePokemonTeamNickname(int id, [FromBody] PutPokemonTeamNicknameDto dto) {

        PokemonTeam? pokemonteam = await _unitOfWork.PokemonTeamRepository.GetByIdAsync(id);
        if (pokemonteam == null) {
            return NotFound();
        }
        pokemonteam.Nickname = dto.Nickname;
        await _unitOfWork.PokemonTeamRepository.UpdateAsync(pokemonteam);
        bool success = await _unitOfWork.SaveAsync();
        if (!success) {
            return BadRequest();
        }
        return Ok(dto);
    }
}
