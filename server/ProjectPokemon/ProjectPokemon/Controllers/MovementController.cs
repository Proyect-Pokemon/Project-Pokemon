using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.Movement;
using ProjectPokemon.Models.Dtos.User;

namespace ProjectPokemon.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class MovementController : ControllerBase {
        private readonly UnitOfWork _unitOfWork;

        public MovementController(UnitOfWork unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IEnumerable<MovementDto>> GetAllMoves() {
            ICollection<Movement> moves = await _unitOfWork.MovementRepository.GetAllAsync();

            IEnumerable<MovementDto> movementDto = moves.Select(moves => new MovementDto {
                Name = moves.Name,
                Description = moves.Description,
                Power = moves.Power ?? 0,
                Accuracy = moves.Accuracy ?? 0,
                Pp = moves.Pp,
                Type = moves.Type
            });
            return movementDto;
        }
    }
}
