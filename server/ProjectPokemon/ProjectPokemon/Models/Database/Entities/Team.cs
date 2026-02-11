namespace ProjectPokemon.Models.Database.Entities {
    public class Team {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; } = null;
        public long IdUser { get; set; }
        public User User { get; set; } = null!;
    }
}
