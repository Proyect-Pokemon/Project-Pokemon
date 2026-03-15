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

            return new Movement {
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
                // CritRate = move.Meta.CritRate,
                Type = System.Enum.Parse<PokeType>(move.Type.Name, true)
            };


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

    }
}
