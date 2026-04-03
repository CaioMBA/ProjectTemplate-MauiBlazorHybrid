using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.Extensions;

public static class StringExtension
{
    public static string Capitalize(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        if (str.Length == 0)
            return str;

        if (str.Length == 1)
            return str.ToUpperInvariant();

        return char.ToUpperInvariant(str[0]) + str[1..];
    }

    public static bool ToInteger(this string str, out int result)
    {
        return int.TryParse(str, out result);
    }

    public static bool ToLong(this string str, out long result)
    {
        return long.TryParse(str, out result);
    }

    public static string LimitStringLength(this string str, int Limit)
    {
        ArgumentNullException.ThrowIfNull(str);

        if (Limit < 0)
            throw new ArgumentOutOfRangeException(nameof(Limit));

        if (Limit >= str.Length)
            return str;

        return str[..Limit];
    }

    public static byte[] ToBytes(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        return Encoding.UTF8.GetBytes(str);
    }

    public static byte[] HexToBytes(this string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        return Convert.FromHexString(hex);
    }
    public static string ToSHA256(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        var bytes = Encoding.UTF8.GetBytes(str);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
    public static string ToBase64(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        var textoAsBytes = UTF8Encoding.UTF8.GetBytes(str);
        var resultado = Convert.ToBase64String(textoAsBytes);
        return resultado;
    }
    public static string FromBase64(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);

        var textoAsBytes = Convert.FromBase64String(str);
        var resultado = UTF8Encoding.UTF8.GetString(textoAsBytes);
        return resultado;
    }
    public static bool IsBase64String(this string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim();
        return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
    }

    public static T? ToObject<T>(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return default;

        return JsonConvert.DeserializeObject<T>(str);
    }

    public static string? ToUriEscaped(this String str)
    {
        return Uri.EscapeDataString(str);
    }

    public static AppTheme ToAppTheme(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return AppTheme.Unspecified;
        }
        else if (str.EndsWith("light", StringComparison.OrdinalIgnoreCase))
        {
            return AppTheme.Light;
        }
        else if (str.EndsWith("dark", StringComparison.OrdinalIgnoreCase))
        {
            return AppTheme.Dark;
        }
        else
        {
            return AppTheme.Unspecified;
        }
    }
}
