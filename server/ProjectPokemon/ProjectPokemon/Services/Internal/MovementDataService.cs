using PokeApiNet;
using ProjectPokemon.Enum;
using ProjectPokemon.Models.Database.Entities;

namespace ProjectPokemon.Services.Internal {
    public class MovementDataService {
        private readonly PokeApiClient client = new PokeApiClient();

        public async Task<Movement> LoadMovement(int id) {
            var move = await client.GetResourceAsync<Move>(id);

            string description = move.FlavorTextEntries
                .FirstOrDefault(entry => entry.Language.Name == "es")?.FlavorText
                .Replace("\n", " ")
                .Replace("\f", " ") ?? "No description available.";

            string Name = move.Names
                .FirstOrDefault(entry => entry.Language.Name == "es")?.Name
                ?? move.Name;

            var movement = new Movement {
                Id = move.Id,
                Name = Name,
                Description = description,
                Pp = (int)move.Pp!,
                MovementClass = ConvertMovementClass(move.DamageClass.Name),
                Accuracy = move.Accuracy,
                Power = move.Power,
                Target = ConvertTarget(move.Target.Name),
                Priority = move.Priority,
                EffectChance = move.EffectChance ?? 0,
                CritRate = move.Meta.CritRate,
                FlinchChance = move.Meta.FlinchChance,
                MaxHits = move.Meta.MaxHits ?? null,
                MinHits = move.Meta.MinHits ?? null,
                MaxTurns = move.Meta.MaxTurns ?? null,
                MinTurns = move.Meta.MinTurns ?? null,
                StatChance = move.Meta.StatChance, // Probabilidad de que cambie una estadística
                Drain = move.Meta.Drain, // Porcentaje de vida que el usuario se cura con respecto al daño hecho
                Healing = move.Meta.Healing, // Porcentaje de vida que el usuario recupera. PARA MOVIMIENTOS QUE SOLO CURAN
                Ailment = move.Meta.Ailment.Name,
                AilmentChance = move.Meta.AilmentChance,
                Category = move.Meta.Category.Name,
                Type = System.Enum.Parse<PokeType>(move.Type.Name, true)
            };

            // Cargar stat_changes desde la API

            if (move.StatChanges != null && move.StatChanges.Count > 0) {
                foreach (var statChange in move.StatChanges) {
                    var stat = ConvertStatType(statChange.Stat.Name);
                    if (stat.HasValue) {
                        movement.StatChanges.Add(new MovementStatChange {
                            Stat = stat.Value,
                            Change = statChange.Change
                        });
                    }
                }
            }

            return movement;
        }

        private PokeTarget ConvertTarget(string apiTarget) {
            return apiTarget switch {
                "selected-pokemon" => PokeTarget.Opponent,
                "all-opponents" => PokeTarget.Opponent,
                "user" => PokeTarget.User,
                "random-opponent" => PokeTarget.Opponent,
                "users-field" => PokeTarget.UsersField,
                "all-other-pokemon" => PokeTarget.Opponent,
                "specific-move" => PokeTarget.SpecificMove,
                "entire-field" => PokeTarget.EntireField,
                "opponents-field" => PokeTarget.OpponentsField,
                "all-pokemon" => PokeTarget.AllPokemon,
                "user-and-allies" => PokeTarget.User,
                "ally" => PokeTarget.User
            };
        }

        private MovementClass ConvertMovementClass(string apiMoveClass) {
            return apiMoveClass switch {
                "physical" => MovementClass.Physical,
                "special" => MovementClass.Special,
                "status" => MovementClass.Status
            };
        }

        private StatType? ConvertStatType(string apiStatName) {
            return apiStatName switch {
                "attack" => StatType.Attack,
                "defense" => StatType.Defense,
                "special-attack" => StatType.SpecialAttack,
                "special-defense" => StatType.SpecialDefense,
                "speed" => StatType.Speed,
                "accuracy" => StatType.Accuracy,
                "evasion" => StatType.Evasion,
                _ => null // Si no es un stat reconocido, devolver null
            };
        }
    }
}