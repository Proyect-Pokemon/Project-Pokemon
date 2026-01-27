using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Database.Entities; 
public class Movement {
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int Pp { get; set; }
    public required MovementClass MovementClass { get; set; }
    public required int Accuracy { get; set; } = 100; // Porcentaje de acierto de un movimiento
    public required int Power { get; set; } = 0; // Potencia del movimiento. Un movimiento de clase estado tendría 0
    public required bool Contact { get; set; } // Implementar más adelante
    public required PokeTarget Target { get; set; }
    public required int Priority { get; set; } = 0; // <-- Consultar el documento con las prioridades en combate
    public required PokeType? Type { get; set; }

    // Relaciones
    public ICollection<Pokemon> Pokemons { get; set; } = new List<Pokemon>();
}
