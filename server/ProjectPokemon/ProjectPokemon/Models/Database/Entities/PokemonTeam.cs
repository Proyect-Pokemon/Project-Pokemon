using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPokemon.Models.Database.Entities {
    public class PokemonTeam {
        public long Id { get; set; }
        public required bool Shiny { get; set; } = false;
        public long PokemonId { get; set; }
        public Pokemon Pokemon { get; set; } = null!;
        public required long IdMovement1 { get; set; }
        public long? IdMovement2 { get; set; } = null;
        public long? IdMovement3 { get; set; } = null;
        public long? IdMovement4 { get; set; } = null;
        public long IdNature { get; set; }
        public Nature? Nature { get; set; }

        [ForeignKey(nameof(IdMovement1))]
        public Movement Movement1 { get; set; } = null!;
        [ForeignKey(nameof(IdMovement2))]
        public Movement? Movement2 { get; set; } = null;
        [ForeignKey(nameof(IdMovement3))]
        public Movement? Movement3 { get; set; } = null;
        [ForeignKey(nameof(IdMovement4))]
        public Movement? Movement4 { get; set; } = null;
    }
}
