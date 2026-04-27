using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Services;
using ProjectPokemon.Networking.Messages.Battle;

namespace ProjectPokemon.Controllers {
    // [DEPRECATED] Este controlador está obsoleto.
    // Las batallas ahora se inician a través de WebSocket enviando StartBattleRequest.
    // Ver: ProjectPokemon/Networking/Messages/Battle/BattleMessage.cs
    [Route("api/[controller]")]
    [ApiController]
    [Obsolete("Use WebSocket messages instead. Send StartBattleRequest via WebSocket connection.")]
    public class BattleController : ControllerBase {
        private readonly BattleService _battleService;
        private readonly BattleSessionManager _sessionManager;

        public BattleController(BattleService battleService, BattleSessionManager sessionManager) {
            _battleService = battleService;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Inicia una nueva batalla
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartBattle([FromBody] StartBattleRequest request) {
            // TODO: Obtener userId del token JWT
            int userId = request.UserId;

            var session = await _battleService.StartBattleAsync(userId, request.TeamId);

            if (session == null) {
                return BadRequest(new { error = "No se pudo iniciar la batalla" });
            }

            var response = new {
                battleId = session.BattleId,
                playerSide = new {
                    team = session.PlayerSide.Team.Select(p => new {
                        pokemonId = p.PokemonId,
                        name = p.Name,
                        nickname = p.Nickname,
                        slot = p.Slot,
                        currentHp = p.CurrentHp,
                        maxHp = p.MaxHp,
                        isFainted = p.IsFainted()
                    }),
                    activeSlot = session.PlayerSide.ActiveSlot
                },
                opponentSide = new {
                    team = session.OpponentSide.Team.Select(p => new {
                        pokemonId = p.PokemonId,
                        name = p.Name,
                        slot = p.Slot,
                        currentHp = p.CurrentHp,
                        maxHp = p.MaxHp,
                        isFainted = p.IsFainted()
                    }),
                    activeSlot = session.OpponentSide.ActiveSlot
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Obtiene el estado actual de una batalla
        /// </summary>
        [HttpGet("{battleId}")]
        public IActionResult GetBattle(string battleId) {
            var battle = _sessionManager.GetBattle(battleId);

            if (battle == null) {
                return NotFound(new { error = "Batalla no encontrada" });
            }

            var response = new {
                battleId = battle.BattleId,
                turn = battle.Turn,
                winnerSide = battle.WinnerSide,
                playerSide = new {
                    team = battle.PlayerSide.Team.Select(p => new {
                        pokemonId = p.PokemonId,
                        name = p.Name,
                        nickname = p.Nickname,
                        slot = p.Slot,
                        currentHp = p.CurrentHp,
                        maxHp = p.MaxHp,
                        isFainted = p.IsFainted()
                    }),
                    activeSlot = battle.PlayerSide.ActiveSlot
                }
            };

            return Ok(response);
        }
    }

    public class StartBattleRequest {
        public required int UserId { get; set; }
        public required int TeamId { get; set; }
    }
}

