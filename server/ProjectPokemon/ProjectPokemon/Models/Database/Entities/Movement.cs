using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities; 
public class Movement {
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int Pp { get; set; }
    public required MovementClass MovementClass { get; set; }
    public int? Accuracy { get; set; } = null; // Porcentaje de acierto de un movimiento
    public int? Power { get; set; } = null; // Potencia del movimiento. Un movimiento de clase estado tendría 0
    public required PokeTarget Target { get; set; }
    public required int Priority { get; set; } // <-- Consultar el documento con las prioridades en combate
    public required int EffectChance { get; set; } // Probabilidad que tiene el movimiento de que ocurra el efecto secundario. Es el num en %
    public required PokeType Type { get; set; }

    // Relaciones
    public ICollection<PokemonMovement> PokemonMovements { get; set; } = new List<PokemonMovement>();
}
