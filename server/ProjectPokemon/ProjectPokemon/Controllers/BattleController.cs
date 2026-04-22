using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Services;
using ProjectPokemon.Models.Requests;

namespace ProjectPokemon.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class BattleController : ControllerBase {
        private readonly BattleService _battleService;
        private readonly BattleSessionManager _sessionManager;

        public BattleController(BattleService battleService, BattleSessionManager sessionManager) {
            _battleService = battleService;
            _sessionManager = sessionManager;
        }

        // Inicia una nueva batalla
        [HttpPost("start")]
        public async Task<IActionResult> StartBattle([FromBody] StartBattleRequest request) {
            // TODO: Obtener userId del token JWT
            int userId = 1; // Placeholder

            var session = await _battleService.StartBattleAsync(userId, request.TeamId, request.ConnectionId);

            if (session == null) {
                return BadRequest(new { error = "No se pudo iniciar la batalla" });
            }

            var response = new {
                battleId = session.BattleId,
                snapshot = session.CreateSnapshot()
            };

            return Ok(response);
        }

        // Obtiene el estado actual de una batalla
        [HttpGet("{battleId}")]
        public IActionResult GetBattle(string battleId) {
            var battle = _sessionManager.GetBattle(battleId);

            if (battle == null) {
                return NotFound(new { error = "Batalla no encontrada" });
            }

            return Ok(new {
                battleId = battle.BattleId,
                snapshot = battle.CreateSnapshot()
            });
        }
    }
}
