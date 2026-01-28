namespace ProjectPokemon.Enum;
[Flags] // Son los bits de SecondaryStatus que puede tener un pokemon
public enum PokeSecondaryStatus {
    None = 0,               // 0000 0000
    Confuse = 1,            // 0000 0001
    Cursed = 1 << 1,        // 0000 0010
    Infatuation = 1 << 2,   // 0000 0100
    CantEscape = 1 << 3,    // 0000 1000
    Bound = 1 << 4,         // 0001 0000
    Seeded = 1 << 5,        // 0010 0000
    CountingDown = 1 << 6   // 0100 0000

    // Por ejemplo: Cursed e Infatuation --> 0000 1100
}
