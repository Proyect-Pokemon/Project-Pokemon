namespace ProjectPokemon.Networking.Messages;

public interface IMessage {
    MessageType Type { get; }
}

public interface IMessage<TAction> : IMessage where TAction : System.Enum {
    TAction Action { get; }
}
