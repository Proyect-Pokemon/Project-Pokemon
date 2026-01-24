namespace ProjectPokemon.Models.Database.Entities;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Name), IsUnique = true)]
public class Type {
    public int Id { get; set; }
    public PokemonTypes Name { get; set; }
}