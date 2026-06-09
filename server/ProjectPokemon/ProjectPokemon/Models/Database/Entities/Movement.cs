using ProjectPokemon.Enum;
using ProjectPokemon.Models.Battle.Movements;

namespace ProjectPokemon.Models.Database.Entities;

public class Movement : IMovement {
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int Pp { get; set; }
    public required MovementClass MovementClass { get; set; }
    public int? Accuracy { get; set; } = null; // Porcentaje de acierto de un movimiento
    public int? Power { get; set; } = null; // Potencia del movimiento. Un movimiento de clase estado tendría 0
    public required PokeTarget Target { get; set; }
    public required int Priority { get; set; } // <-- Consultar el documento con las prioridades en combate
    public int? EffectChance { get; set; } // Probabilidad que tiene el movimiento de que ocurra el efecto secundario. Ya sea de estadistica, estado u otros
    public required PokeType Type { get; set; }
    public int CritRate { get; set; } = 0;
    public int FlinchChance { get; set; } = 0; // Probabilidad de que el movimiento haga retroceder al objetivo
    public int? MaxHits { get; set; } = null; // Número máximo de veces que un movimiento puede golpear en un turno
    public int? MinHits { get; set; } = null; // Número mínimo de veces que un movimiento puede golpear en un turno
    public int? MaxTurns { get; set; } = null; // Número máximo de turnos que un movimiento puede durar. como bucle arena o giro fuego
    public int? MinTurns { get; set; } = null; // Número mínimo de turnos que un movimiento puede durar. como bucle arena o giro fuego
    public int StatChance { get; set; } = 0; // Probabilidad de que el movimiento modifique las estadísticas del objetivo
    public int? Drain { get; set; } = null; // Porcentaje de vida que el usuario recupera con respecto al daño que inflige al objetivo
    public int? Healing { get; set; } = null; // Porcentaje de vida que el movimiento recupera al usuario
    public string Ailment { get; set; } = "none"; // Estado alterado que el movimiento puede causar al objetivo
    public int AilmentChance { get; set; } = 0; // Probabilidad de que el movimiento cause el estado alterado al objetivo
    public string Category { get; set; } = "damage"; // Categoría del movimiento: daño, estado, unico, etc.

    // Relaciones
    public ICollection<PokemonMovement> PokemonMovements { get; set; } = new List<PokemonMovement>();
    public ICollection<MovementStatChange> StatChanges { get; set; } = new List<MovementStatChange>();
}