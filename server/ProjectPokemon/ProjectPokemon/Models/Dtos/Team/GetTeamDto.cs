namespace ProjectPokemon.Models.Dtos.Team; 
public class GetTeamDto {
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; } = null;
    public long UserId { get; set; }
    //public string User { get; set; } = null!;
}
