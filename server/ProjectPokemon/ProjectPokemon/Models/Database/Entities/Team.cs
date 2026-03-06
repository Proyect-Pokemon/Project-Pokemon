namespace ProjectPokemon.Models.Database.Entities {
    public class Team {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; } = null;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public ICollection<PokemonTeam> PokemonsTeam { get; set; } = [];
    }
}
