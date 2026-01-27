using Microsoft.EntityFrameworkCore;

namespace ProjectPokemon.Models.Database.Entities {
    [PrimaryKey(nameof(PokemonId), nameof(MovementId))]
    public class PokemonMovement {
        public int PokemonId { get; set; }
        public int MovementId { get; set; }
        public Pokemon Pokemon { get; set; } = null!;
        public Movement Movement { get; set; } = null!;
    }
}
