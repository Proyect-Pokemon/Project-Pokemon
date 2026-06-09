namespace ProjectPokemon.Models.Dtos.Team {
    public class PutTeamDto {
        public required string Name { get; set; }
        public string? Description { get; set; } = null;
    }
}
