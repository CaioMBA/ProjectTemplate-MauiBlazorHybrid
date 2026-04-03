namespace Domain.Extensions;

public static class NumericExtension
{
    public static bool IsEven(this int number)
    {
        return number % 2 == 0;
    }
    public static bool IsOdd(this int number)
    {
        return number % 2 != 0;
    }
    public static bool IsPositive(this int number)
    {
        return number > 0;
    }
    public static bool IsNegative(this int number)
    {
        return number < 0;
    }
    public static bool IsZero(this int number)
    {
        return number == 0;
    }

    public static string ToFormattedBytes(this long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "Bytes cannot be negative.");
        }
        else if (bytes == 0)
        {
            return "0 B";
        }

        // 2. Set up units and logarithmic calculation
        const int kilobyte = 1024;
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

        // 3. Calculate the proper unit
        int i = (int)Math.Floor(Math.Log(bytes, kilobyte));

        // 4. Format the final string
        double num = bytes / Math.Pow(kilobyte, i);

        return $"{num:F2} {units[i]}";
    }

    public static string ToFormattedBytes(this long? bytes)
    {
        if (bytes == null)
        {
            return "0 B";
        }
        return bytes.Value.ToFormattedBytes();
    }
}
