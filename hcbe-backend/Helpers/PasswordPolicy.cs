namespace HcbeApi.Helpers;

using System.Security.Cryptography;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;

    public static bool IsStrong(string? password) =>
        password is { Length: >= MinimumLength }
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(character => !char.IsLetterOrDigit(character));

    public const string ValidationMessage =
        "Password must contain at least 12 characters, including uppercase, lowercase, number, and symbol";

    public static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string numbers = "23456789";
        const string symbols = "!@#$%*-_+";
        var all = upper + lower + numbers + symbols;
        var characters = new List<char>
        {
            Pick(upper), Pick(lower), Pick(numbers), Pick(symbols)
        };
        for (var index = characters.Count; index < 16; index++) characters.Add(Pick(all));
        for (var index = characters.Count - 1; index > 0; index--)
        {
            var target = RandomNumberGenerator.GetInt32(index + 1);
            (characters[index], characters[target]) = (characters[target], characters[index]);
        }
        return new string(characters.ToArray());
    }

    private static char Pick(string characters) =>
        characters[RandomNumberGenerator.GetInt32(characters.Length)];
}
