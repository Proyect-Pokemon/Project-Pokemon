namespace ProjectPokemon.Models.Database.Entities {
    public class PokemonTeam {
        public long Id { get; set; }
        public required bool Shiny { get; set; } = false;
        public long PokemonId { get; set; }
        public Pokemon Pokemon { get; set; } = null!;
    }
}
