using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.Extensions;

public static class StringExtension
{
    public static string Capitalize(this string str)
    {
        var first = str.Substring(0, 1);
        var rest = str.Substring(1);
        return $"{first.ToUpper()}{rest}";
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
        if (Limit > str.Length)
            return str;

        return str.Substring(0, Limit);
    }

    public static byte[] ToBytes(this string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    public static byte[] HexToBytes(this string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Invalid length for a hex string.");

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
    public static string ToSHA256(this string str)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(str);
        var hashBytes = sha256.ComputeHash(bytes);

        var builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2")); // Convert byte to hexadecimal string
        }
        return builder.ToString();
    }
    public static string ToBase64(this string str)
    {
        try
        {
            var textoAsBytes = UTF8Encoding.UTF8.GetBytes(str);
            var resultado = Convert.ToBase64String(textoAsBytes);
            return resultado;
        }
        catch (Exception)
        {
            throw new Exception("Error converting to base64");
        }
    }
    public static string FromBase64(this string str)
    {
        try
        {
            var textoAsBytes = Convert.FromBase64String(str);
            var resultado = UTF8Encoding.UTF8.GetString(textoAsBytes);
            return resultado;
        }
        catch (Exception)
        {
            throw new Exception("Error converting from base64");
        }
    }
    public static bool IsBase64String(this string s)
    {
        s = s.Trim();
        return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
    }

    public static T? ToObject<T>(this string str)
    {
        return JsonConvert.DeserializeObject<T>(str);
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
