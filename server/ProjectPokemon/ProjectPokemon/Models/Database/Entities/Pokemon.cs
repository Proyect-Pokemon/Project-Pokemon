namespace ProjectPokemon.Models.Database.Entities {
    public class Pokemon {
        public int Id { get; set; }
        // Estadísticas
        public required int Hp { get; set; }
        public required int Atk { get; set; }
        public required int Def { get; set; }
        public required int SpAtk { get; set; }
        public required int SpDef { get; set; }
        public required int Spe {  get; set; }
        public required string? SpriteFront { get; set; }
        public required string? SpriteBack { get; set; }
        // TO DO: Añadir más adelante los sprites shiny, la forma hembra y el grito del pokemon
        //
        //public required string? SpriteFrontShiny { get; set; }
        //public required string? SpriteBackShiny { get; set; }
        //public string? SpriteFrontFem { get; set; }
        //public string? SpriteBackFem { get; set; }
        //public string? SpriteFrontFemShiny { get; set; }
        //public string? SpriteBackFemShiny { get; set; }
        //public required string? Cry { get; set; }
        public required string? Type1 { get; set; }
        public string? Type2 { get; set; }
        public ICollection<Move> Moves { get; set; } = [];
    }
}
