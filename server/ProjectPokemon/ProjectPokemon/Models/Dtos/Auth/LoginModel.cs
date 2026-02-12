using System.ComponentModel.DataAnnotations;

namespace ProjectPokemon.Models.Dtos.Auth;

public class LoginModel
{
    [Required]
    public required string Nickname { get; set; }
    [Required]
    public required string Password { get; set; }
}
