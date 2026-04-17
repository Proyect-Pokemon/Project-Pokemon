using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Shared;
using System.Diagnostics.CodeAnalysis;

namespace ProjectPokemon.Battle.Movements; 
public abstract class BattleMovement : IMovement {
    public int CurrentPp { get; set; }
    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int Pp { get; }
    public MovementClass MovementClass { get; }
    public int? Accuracy { get; }     // Porcentaje de acierto de un movimiento
    public int? Power { get; }        // Potencia del movimiento. Un movimiento de clase estado tendría 0
    public PokeTarget Target { get; }
    public int Priority { get; }      // <-- Consultar el documento con las prioridades en combate
    public int? EffectChance { get; } // Probabilidad que tiene el movimiento de que ocurra el efecto secundario. Ya sea de estadistica, estado u otros
    public PokeType Type { get; }
    public int CritRate { get; }
    public int FlinchChance { get; }  // Probabilidad de que el movimiento haga retroceder al objetivo
    public int? MaxHits { get; }      // Número máximo de veces que un movimiento puede golpear en un turno
    public int? MinHits { get; }      // Número mínimo de veces que un movimiento puede golpear en un turno
    public int? MaxTurns { get; }     // Número máximo de turnos que un movimiento puede durar. como bucle arena o giro fuego
    public int? MinTurns { get; }     // Número mínimo de turnos que un movimiento puede durar. como bucle arena o giro fuego
    public int StatChance { get; }    // Probabilidad de que el movimiento modifique las estadísticas del objetivo
    public int? Drain { get; }        // Porcentaje de vida que el usuario recupera con respecto al daño que inflige al objetivo
    public int? Healing { get; }      // Porcentaje de vida que el movimiento recupera al usuario
    public string Ailment { get; }    // Estado alterado que el movimiento puede causar al objetivo
    public int AilmentChance { get; } // Probabilidad de que el movimiento cause el estado alterado al objetivo
    public string Category { get; }   // Categoría del movimiento: daño, estado, unico, etc.

    protected BattleMovement(IMovement movement) : base() {
        // Copiar todas las propiedades del movimiento
        Id = movement.Id;
        Name = movement.Name;
        Description = movement.Description;
        Pp = movement.Pp;
        CurrentPp = movement.Pp;
        MovementClass = movement.MovementClass;
        Accuracy = movement.Accuracy;
        Power = movement.Power;
        Type = movement.Type;
        Target = movement.Target;
        Priority = movement.Priority;
        CritRate = movement.CritRate;
        EffectChance = movement.EffectChance;
        FlinchChance = movement.FlinchChance;
        MaxHits = movement.MaxHits;
        MinHits = movement.MinHits;
        MaxTurns = movement.MaxTurns;
        MinTurns = movement.MinTurns;
        StatChance = movement.StatChance;
        Drain = movement.Drain;
        Healing = movement.Healing;
        Ailment = movement.Ailment;
        AilmentChance = movement.AilmentChance;
        Category = movement.Category;
    }

    // Comprobar si el movimiento acierta
    public virtual bool CheckAccuracy(PokemonBattle attacker, PokemonBattle defender) {
        // Si la precisión es nula, es que siempre acierta
        if (Accuracy == null) {
            return true;
        }

        // Calcular el modificador por stages (precisión del atacante y evasión del defensor)
        int stageModifier = attacker.AccuracyStage - defender.EvasionStage;
        double accuracyMultiplier = PokemonBattle.GetAccuracyEvasionStageMultiplier(stageModifier);

        double finalAccuracy = Accuracy.Value * accuracyMultiplier;

        Random random = new Random();
        return random.Next(0, 100) < finalAccuracy;
    }

    // Realizar el movimiento. Método abstracto que cada tipo de movimiento implementará diferente
    public abstract void ExecuteMovement(PokemonBattle attacker, PokemonBattle defender);

    // Resta PP al movimiento
    public void ConsumePp() {
        if (CurrentPp > 0) {
            CurrentPp--;
        }
    }

    // Comprueba si el movimiento tiene PPs disponibles
    public bool HasPpAvailable() {
        return CurrentPp > 0;
    }
}
