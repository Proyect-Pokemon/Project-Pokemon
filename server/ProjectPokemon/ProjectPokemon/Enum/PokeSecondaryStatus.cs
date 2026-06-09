namespace ProjectPokemon.Enum;
[Flags] // Son los bits de SecondaryStatus que puede tener un pokemon
public enum PokeSecondaryStatus {
    None = 0,               // 0000 0000 0000
    Confuse = 1,            // 0000 0000 0001
    Cursed = 1 << 1,        // 0000 0000 0010
    Infatuation = 1 << 2,   // 0000 0000 0100
    CantEscape = 1 << 3,    // 0000 0000 1000 (trap)
    Bound = 1 << 4,         // 0000 0001 0000 (bind, wrap, etc.)
    Seeded = 1 << 5,        // 0000 0010 0000 (leech-seed)
    CountingDown = 1 << 6,  // 0000 0100 0000 (perish-song)
    Flinch = 1 << 7,        // 0000 1000 0000
    Nightmare = 1 << 8,     // 0001 0000 0000
    Torment = 1 << 9,       // 0010 0000 0000
    Disable = 1 << 10,      // 0100 0000 0000
    Drowsy = 1 << 11        // 1000 0000 0000 (yawn)

    // Por ejemplo: Cursed e Infatuation --> 0000 0110
}
