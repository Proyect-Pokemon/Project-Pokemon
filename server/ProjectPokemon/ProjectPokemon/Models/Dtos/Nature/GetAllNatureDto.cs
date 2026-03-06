using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Dtos.Nature {
    public class GetAllNatureDto {
        public int Id { get; set; }
        public required PokeNature Name { get; set; }
        public required StatType StatBoost { get; set; }
        public required StatType StatDrop { get; set; }
    }
}
