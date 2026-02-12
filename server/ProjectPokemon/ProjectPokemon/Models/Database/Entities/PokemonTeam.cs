using ProjectPokemon.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectPokemon.Models.Database.Entities {
    public class PokemonTeam {
        public long Id { get; set; }
        public bool Shiny { get; set; } = false;
        public string? Nickname { get; set; } = null; //si es nulu usa el nombre del pokemon
        public int Slot { get; set; } // 1 a 6
        public long TeamId { get; set; } 
        public Team Team { get; set; } = null!;
        public long PokemonId { get; set; }
        public Pokemon Pokemon { get; set; } = null!;
        public required long MovementId1 { get; set; }
        public long? MovementId2 { get; set; } = null;
        public long? MovementId3 { get; set; } = null;
        public long? MovementId4 { get; set; } = null;
        public long NatureId { get; set; }
        public Nature Nature { get; set; } = null!;

        [ForeignKey(nameof(MovementId1))]
        public Movement Movement1 { get; set; } = null!;

        [ForeignKey(nameof(MovementId2))]
        public Movement? Movement2 { get; set; } = null;

        [ForeignKey(nameof(MovementId3))]
        public Movement? Movement3 { get; set; } = null;

        [ForeignKey(nameof(MovementId4))]
        public Movement? Movement4 { get; set; } = null;
    }
}
