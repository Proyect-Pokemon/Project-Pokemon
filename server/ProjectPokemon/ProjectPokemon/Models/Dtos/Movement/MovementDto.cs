using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Dtos.Movement {
    public class MovementDto {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int Power { get; set; }
        public int Accuracy { get; set; }
        public required int Pp { get; set; }
        public required PokeType Type { get; set; }
    }
}
