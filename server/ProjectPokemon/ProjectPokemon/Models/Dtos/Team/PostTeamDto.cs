namespace ProjectPokemon.Models.Dtos.Team {
    public class PostTeamDto {
        public required string Name { get; set; }
        public string? Description { get; set; } = null;
        public long UserId { get; set; }
    }
}
