using ProjectPokemon.Models.Database.Entities;
using System.Diagnostics.CodeAnalysis;

namespace ProjectPokemon.Battle.Movements {
    public abstract class BattleMovement : Movement {
        public int CurrentPp { get; set; }

        [SetsRequiredMembers]
        protected BattleMovement(Movement movement) : base() {
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

            Random random = new Random();
            int roll = random.Next(1, 101);
            return roll <= Accuracy;
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
}
