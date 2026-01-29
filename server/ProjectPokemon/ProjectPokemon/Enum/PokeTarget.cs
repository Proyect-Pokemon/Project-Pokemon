namespace ProjectPokemon.Enum {
    // Aquí se almacenan los posibles objetivos de los movimientos
    public enum PokeTarget {
        SpecificMove,
        // selected-pokemon-me-first no existe porque es de gen4 (Yo primero)
        // all-allies no existe porque es de gen8 (Plegaria Lunar)
        // fainting-pokemon no existe porque es de gen9 "Plegaria Vital"
        Ally, // Si es este, fallará el movimiento
        UsersField, // Campo aliado
        OpponentsField, // Campo del oponente
        EntireField, // Ambos campos
        User, // Aquí entra --> user-and-allies, user-or-ally
        Opponent, // Aquí entra --> random-oponent, all-other-pokemon, all-opponents, selected-pokemon
        AllPokemon // Solo lo tiene el movimiento "Canto Mortal" (Perish Song)
    }
}
