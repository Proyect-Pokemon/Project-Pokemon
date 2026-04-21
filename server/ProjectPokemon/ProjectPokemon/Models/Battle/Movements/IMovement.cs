using ProjectPokemon.Enum;

namespace ProjectPokemon.Models.Battle.Movements;
public interface IMovement {
    int Id { get; }
    string Name { get; }
    string Description { get; }
    int Pp { get; }
    MovementClass MovementClass { get; }
    int? Accuracy { get; }     // Porcentaje de acierto de un movimiento
    int? Power { get; }        // Potencia del movimiento. Un movimiento de clase estado tendría 0
    PokeTarget Target { get; }
    int Priority { get; }      // <-- Consultar el documento con las prioridades en combate
    int? EffectChance { get; } // Probabilidad que tiene el movimiento de que ocurra el efecto secundario. Ya sea de estadistica, estado u otros
    PokeType Type { get; }
    int CritRate { get; }
    int FlinchChance { get; }  // Probabilidad de que el movimiento haga retroceder al objetivo
    int? MaxHits { get; }      // Número máximo de veces que un movimiento puede golpear en un turno
    int? MinHits { get; }      // Número mínimo de veces que un movimiento puede golpear en un turno
    int? MaxTurns { get; }     // Número máximo de turnos que un movimiento puede durar. como bucle arena o giro fuego
    int? MinTurns { get; }     // Número mínimo de turnos que un movimiento puede durar. como bucle arena o giro fuego
    int StatChance { get; }    // Probabilidad de que el movimiento modifique las estadísticas del objetivo
    int? Drain { get; }        // Porcentaje de vida que el usuario recupera con respecto al daño que inflige al objetivo
    int? Healing { get; }      // Porcentaje de vida que el movimiento recupera al usuario
    string Ailment { get; }    // Estado alterado que el movimiento puede causar al objetivo
    int AilmentChance { get; } // Probabilidad de que el movimiento cause el estado alterado al objetivo
    string Category { get; }   // Categoría del movimiento: daño, estado, unico, etc.
}
