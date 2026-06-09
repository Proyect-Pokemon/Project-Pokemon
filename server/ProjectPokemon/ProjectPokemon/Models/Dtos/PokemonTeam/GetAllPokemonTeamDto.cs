namespace ProjectPokemon.Models.Dtos.PokemonTeam {
    public class GetAllPokemonTeamDto {
        public int Id { get; set; }
        public string? Nickname { get; set; }
        public char? Sex { get; set; } = null;
        public bool Shiny { get; set; } 
        public int Slot { get; set; }
        public int TeamId { get; set; }
        public int PokemonId { get; set; }
        public int NatureId { get; set; }
        public required int MovementId1 { get; set; }
        public int? MovementId2 { get; set; } = null;
        public int? MovementId3 { get; set; } = null;
        public int? MovementId4 { get; set; } = null;
    }
}
