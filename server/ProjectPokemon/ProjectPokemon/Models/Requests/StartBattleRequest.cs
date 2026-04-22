namespace ProjectPokemon.Models.Requests;

public class StartBattleRequest {
    public required int TeamId { get; set; }
    public required string ConnectionId { get; set; }
}
