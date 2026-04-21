using ProjectPokemon.Enum;

// Este DTO es la información que se podrá ver del movimiento al selecionarlo durante la creación de equipo

namespace ProjectPokemon.Models.Dtos.Movement; 
public class MovementDto {
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int? Power { get; set; } = null;
    public int? Accuracy { get; set; } = null;
    public required MovementClass MovementClass { get; set; }
    public required int Pp { get; set; }
    public required PokeType Type { get; set; }
}
