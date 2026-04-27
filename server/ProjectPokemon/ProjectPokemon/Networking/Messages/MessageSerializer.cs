using System.Text.Json;
using ProjectPokemon.Networking.Messages.Battle;
using ProjectPokemon.Networking.Messages.Lobby;

namespace ProjectPokemon.Networking.Messages;

public static class MessageSerializer {
    private static readonly JsonSerializerOptions JSON_OPTIONS = JsonSerializerOptions.Web;
    private static readonly string TYPE_PROPERTY_NAME = nameof(IMessage.Type).ToLower();
    private static readonly string ACTION_PROPERTY_NAME = "action";

    public static string Serialize<T>(T message) where T : IMessage {
        return JsonSerializer.Serialize(message, JSON_OPTIONS);
    }

    public static IMessage? Deserialize(string json) {
        // Es más eficiente que usar JsonSerialize.Deserialize<JsonElement>()
        using JsonDocument jsonDocument = JsonDocument.Parse(json);
        JsonElement jsonElement = jsonDocument.RootElement;
        MessageType messageType = GetMessageType(jsonElement);

        return messageType switch {
            MessageType.Battle => DeserializeBattleMessage(jsonElement),
            MessageType.Chat => Deserialize<ChatMessage>(jsonElement),
            MessageType.Lobby => DeserializeLobbyMessage(jsonElement),
            _ => throw new Exception("Cannot read message from client")
        };
    }

    private static BattleMessage? DeserializeBattleMessage(JsonElement jsonElement) {
        BattleAction action = GetMessageAction<BattleAction>(jsonElement);

        return action switch {
            BattleAction.StartBattle => Deserialize<StartBattleRequest>(jsonElement),
            BattleAction.Attack or BattleAction.Switch or BattleAction.Forfeit 
                => Deserialize<BattleActionRequest>(jsonElement),
            _ => throw new Exception("Cannot read battle message from client"),
        };
    }

    private static LobbyMessage? DeserializeLobbyMessage(JsonElement jsonElement) {
        LobbyAction action = GetMessageAction<LobbyAction>(jsonElement);

        return action switch {
            LobbyAction.JoinLobby => Deserialize<JoinLobbyRequest>(jsonElement),
            _ => throw new Exception("Cannot read lobby message from client"),
        };
    }

    private static MessageType GetMessageType(JsonElement jsonElement) {
        return (MessageType)jsonElement.GetProperty(TYPE_PROPERTY_NAME).GetInt32();
    }

    private static T GetMessageAction<T>(JsonElement jsonElement) where T : System.Enum {
        return (T)System.Enum.ToObject(typeof(T), jsonElement.GetProperty(ACTION_PROPERTY_NAME).GetInt32());
    }

    private static T? Deserialize<T>(JsonElement jsonElement) {
        return JsonSerializer.Deserialize<T>(jsonElement, JSON_OPTIONS);
    }
}
