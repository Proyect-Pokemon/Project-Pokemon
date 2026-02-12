namespace ProjectPokemon.Models.Dtos.Team; 
public class GetAllTeamDto {
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; } = null;
    public long IdUser { get; set; }
    //public string User { get; set; } = null!;
}
