namespace ProjectPokemon.Models.Dtos.PokemonTeam {
    public class PutPokemonTeamDto {
        public bool Shiny { get; set; } = false;
        public char? Sex { get; set; }
        public required int Slot { get; set; }
        public int NatureId { get; set; }
        public required int MovementId1 { get; set; }
        public int? MovementId2 { get; set; } = null;
        public int? MovementId3 { get; set; } = null;
        public int? MovementId4 { get; set; } = null;
    }
}