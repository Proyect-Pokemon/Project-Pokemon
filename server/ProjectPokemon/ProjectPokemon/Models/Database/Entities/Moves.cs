namespace ProjectPokemon.Models.Database.Entities; 
public class Moves {
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int PP { get; set; }
    public MoveClasses MoveClass { get; set; }
    public int Accuracy { get; set; } = 100; // Porcentaje de acierto de un movimiento
    public int? Power { get; set; } = 0; // Potencia del movimiento. Un movimiento de clase estado tendría 0
    // public bool Contact { get; set; } Implementar más adelante
    public MoveTargets Target { get; set; }
    public int Priority { get; set; } = 0; // Consultar el documento con las prioridades en combate
    public Type? MoveType { get; set; }
}
