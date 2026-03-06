namespace ProjectPokemon.Models.Database.Entities {
    public class Team {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; } = null;
        public long UserId { get; set; }
        public User User { get; set; } = null!;
        public ICollection<PokemonTeam> PokemonsTeam { get; set; } = [];
    }
}
