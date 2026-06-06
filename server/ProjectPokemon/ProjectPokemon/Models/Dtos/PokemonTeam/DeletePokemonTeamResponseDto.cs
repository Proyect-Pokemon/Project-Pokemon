namespace ProjectPokemon.Models.Dtos.PokemonTeam {
    // DTO de respuesta para la eliminación de un Pokémon del equipo con compactación de slots

    public class DeletePokemonTeamResponseDto {
        // Indica si la operación fue exitosa
        public bool Success { get; set; }

        // Mensaje descriptivo del resultado
        public string? Message { get; set; }

        // Estado final del equipo después de la compactación (slots reordenados)
        public List<TeamPokemonSlotDto> RemainingPokemons { get; set; } = new();
    }

    // Representación simplificada de un Pokémon en el equipo
    public class TeamPokemonSlotDto {
        public int Id { get; set; }
        public int Slot { get; set; }
        public int PokemonId { get; set; }
        public string? Nickname { get; set; }
    }
}
