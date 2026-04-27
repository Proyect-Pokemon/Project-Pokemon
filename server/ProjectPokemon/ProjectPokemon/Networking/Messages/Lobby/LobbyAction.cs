namespace ProjectPokemon.Networking.Messages.Lobby;

// Acciones disponibles en el lobby
public enum LobbyAction {
    JoinLobby = 1,
    LeaveLobby = 2,
    GetOnlineFriends = 3,
    SendFriendRequest = 4,
    SearchBattle = 5,      // Buscar combate (matchmaking)
    CancelSearch = 6       // Cancelar búsqueda
}
