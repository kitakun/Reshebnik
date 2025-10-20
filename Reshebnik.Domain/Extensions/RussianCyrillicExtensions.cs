using System.Text;

namespace Reshebnik.Domain.Extensions;

public static class RussianCyrillicExtensions
{
    public static string ToClickHouseKey(this string text, int maxLength = 16)
    {
        if (string.IsNullOrEmpty(text))
            return Guid.NewGuid().ToString("N")[..maxLength];

        // Transliterate Cyrillic to Latin
        var transliterated = text.TransliterateCyrillicToLatin();
        
        // Convert to lowercase and replace spaces/special chars with hyphens
        var key = transliterated.ToLower()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace(",", "-")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("`", "")
            .Replace("~", "")
            .Replace("@", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("^", "")
            .Replace("&", "")
            .Replace("*", "")
            .Replace("+", "")
            .Replace("=", "")
            .Replace("|", "")
            .Replace("\\", "")
            .Replace("/", "-");

        // Remove multiple consecutive hyphens
        while (key.Contains("--"))
        {
            key = key.Replace("--", "-");
        }

        // Remove leading/trailing hyphens
        key = key.Trim('-');

        // Limit to maxLength characters
        if (key.Length > maxLength)
        {
            key = key[..maxLength];
            // Remove trailing hyphen if it exists
            if (key.EndsWith("-"))
            {
                key = key[..^1];
            }
        }

        // If empty or too short, use GUID
        if (string.IsNullOrEmpty(key) || key.Length < 3)
        {
            return Guid.NewGuid().ToString("N")[..maxLength];
        }

        return key;
    }

    public static string TransliterateCyrillicToLatin(this string text)
    {
        var cyrillicToLatin = new Dictionary<char, string>
        {
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"}, {'е', "e"}, {'ё', "yo"},
            {'ж', "zh"}, {'з', "z"}, {'и', "i"}, {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"},
            {'н', "n"}, {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"}, {'у', "u"},
            {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "sch"},
            {'ъ', ""}, {'ы', "y"}, {'ь', ""}, {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
            {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"}, {'Е', "E"}, {'Ё', "Yo"},
            {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"}, {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"},
            {'Н', "N"}, {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"}, {'У', "U"},
            {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"}, {'Ш', "Sh"}, {'Щ', "Sch"},
            {'Ъ', ""}, {'Ы', "Y"}, {'Ь', ""}, {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"}
        };

        var result = new StringBuilder();
        foreach (char c in text)
        {
            if (cyrillicToLatin.TryGetValue(c, out string? latin))
            {
                result.Append(latin);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
