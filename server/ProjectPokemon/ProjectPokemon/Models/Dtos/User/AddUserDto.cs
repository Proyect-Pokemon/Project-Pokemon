using System.ComponentModel.DataAnnotations;

namespace ProjectPokemon.Models.Dtos.User {
    public class AddUserDto {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        [StringLength(254, ErrorMessage = "El correo electrónico no puede superar los 254 caracteres.")]
        public required string Email { get; set; } = null!;

        [Required(ErrorMessage = "El nickname es obligatorio.")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "El nickname debe tener minimo 3 caracteres.")]
        public required string Nickname { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener minimo 6 caracteres.")]
        public required string Password { get; set; } = null!;

        public string? AvatarPath { get; set; } = null;
        public string? Biography { get; set; }
    }
}