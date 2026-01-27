namespace ProjectPokemon.Enum;
[Flags] // Son los bits de SecondaryStatus que puede tener un pokemon
public enum PokeSecondaryStatus {
    None = 0,               // 0000 0000
    Confuse = 1 << 0,       // 0000 0010
    Cursed = 1 << 1,        // 0000 0100
    Infatuation = 1 << 2,   // 0000 1000
    CantEscape = 1 << 3,    // 0001 0000
    Bound = 1 << 4,         // 0010 0000
    Seeded = 1 << 5,        // 0100 0000
    CountingDown = 1 << 6   // 1000 0000

    // Por ejemplo: Cursed e Infatuation --> 0000 1100
}
