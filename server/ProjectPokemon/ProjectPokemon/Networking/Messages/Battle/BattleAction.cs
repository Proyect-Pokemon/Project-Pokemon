namespace ProjectPokemon.Networking.Messages.Battle;

public enum BattleAction {
    StartBattle = 0,    // Iniciar batalla
    Attack = 1,         // Atacar con un movimiento
    Switch = 2,         // Cambiar Pokemon
    Forfeit = 3         // Rendirse
}
