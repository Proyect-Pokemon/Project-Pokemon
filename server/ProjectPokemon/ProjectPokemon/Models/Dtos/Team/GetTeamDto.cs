namespace ProjectPokemon.Models.Dtos.Team; 
    public class GetTeamDto {
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; } = null;
    public int UserId { get; set; }
}
