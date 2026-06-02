using System.Globalization;
using System.Text;

namespace ProjectPokemon.Utils;

// Utilidad para normalizar texto eliminando tildes y caracteres especiales
// Usado para generar identificadores y mensajes para el frontend

public static class TextNormalizer {
    // Elimina tildes y caracteres diacriticos de un texto
    // Ejemplo: "Pokémon" -> "Pokemon", "Niño" -> "Nino"
    public static string RemoveDiacritics(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return text;
        }

        // Normalizar a forma NFD (Canonical Decomposition)
        string normalized = text.Normalize(NormalizationForm.FormD);

        // Filtrar solo caracteres que no sean marcas diacriticas
        StringBuilder result = new StringBuilder();
        foreach (char c in normalized) {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark) {
                result.Append(c);
            }
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    // Convierte un nombre a formato snake_case sin tildes
    // Ejemplo: "Light Screen" -> "light_screen", "Pokémon" -> "pokemon"

    public static string ToSnakeCase(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return text;
        }

        string normalized = RemoveDiacritics(text);
        return normalized.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }

    // Normaliza un nombre de Pokemon para el frontend (minusculas, sin tildes)
    // Ejemplo: "Pikachu" -> "pikachu", "Farfetch'd" -> "farfetchd"
    public static string NormalizePokemonName(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return name;
        }

        string normalized = RemoveDiacritics(name);
        // Eliminar apóstrofes y caracteres especiales
        normalized = normalized.Replace("'", "").Replace("'", "");
        return normalized.ToLowerInvariant();
    }
}
