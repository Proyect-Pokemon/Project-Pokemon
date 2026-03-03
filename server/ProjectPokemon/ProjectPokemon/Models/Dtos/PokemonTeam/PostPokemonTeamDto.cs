namespace ProjectPokemon.Models.Dtos.PokemonTeam {
    public class PostPokemonTeamDto {
        public string? Nickname { get; set; }
        public bool Shiny { get; set; } = false;
        public int Slot { get; set; }
        public long TeamId { get; set; }
        public int PokemonId { get; set; }
        public int NatureId { get; set; }
        public required int MovementId1 { get; set; }
        public int? MovementId2 { get; set; } = null;
        public int? MovementId3 { get; set; } = null;
        public int? MovementId4 { get; set; } = null;
    }
}
