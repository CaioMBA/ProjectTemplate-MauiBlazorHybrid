using Newtonsoft.Json;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Web;

namespace Domain.Extensions;

public static class ObjectExtension
{
    public static byte[] ToBytes(this object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj switch
        {
            byte[] byteArray => byteArray,
            Memory<byte> memory => memory.ToArray(),
            ReadOnlyMemory<byte> readOnlyMemory => readOnlyMemory.ToArray(),
            ArraySegment<byte> segment => segment.ToArray(),
            IEnumerable<byte> byteEnumerable => byteEnumerable.ToArray(),
            string str => Encoding.UTF8.GetBytes(str),
            char[] chars => Encoding.UTF8.GetBytes(chars),
            Stream stream => stream.ReadStreamBytes(),
            _ when obj.GetType().IsSimpleType () => Encoding.UTF8.GetBytes(Convert.ToString(obj, CultureInfo.InvariantCulture) ?? string.Empty),
            _ => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj))
        };
    }

    private static byte[] ReadStreamBytes(this Stream stream)
    {
        if (stream is MemoryStream memoryStream)
            return memoryStream.ToArray();

        var originalPosition = stream.CanSeek ? stream.Position : 0;
        if (stream.CanSeek)
            stream.Position = 0;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        if (stream.CanSeek)
            stream.Position = originalPosition;

        return ms.ToArray();
    }

    private static bool IsSimpleType(this Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(decimal)
               || type == typeof(Guid)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan);
    }

    public static T? ToObject<T>(this byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
            return default;

        if (typeof(T) == typeof(byte[]))
            return (T)(object)data;

        var raw = Encoding.UTF8.GetString(data);

        if (typeof(T) == typeof(string))
            return (T)(object)raw;

        if (string.IsNullOrWhiteSpace(raw))
            return default;

        try
        {
            return JsonConvert.DeserializeObject<T>(raw);
        }
        catch (JsonException)
        {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            object converted = targetType.IsEnum
                ? Enum.Parse(targetType, raw, ignoreCase: true)
                : targetType == typeof(Guid)
                    ? Guid.Parse(raw)
                    : Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture)!;

            return (T?)converted;
        }
    }

    public static string ToJson(this object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    public static string ToCSV<T>(this IEnumerable<T> data, char separator = ';')
    {
        ArgumentNullException.ThrowIfNull(data);

        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties();

        sb.AppendLine(string.Join(separator, properties.Select(p => EscapeCsvValue(p.Name, separator))));

        foreach (var item in data)
        {
            var values = properties.Select(p => EscapeCsvValue(item is null ? null : p.GetValue(item)?.ToString(), separator));
            sb.AppendLine(string.Join(separator, values));
        }

        return sb.ToString();
    }

    public static string ToCSV(this IEnumerable<IDictionary<string, object>> data, char separator = ';')
    {
        ArgumentNullException.ThrowIfNull(data);

        var rows = data.ToList();
        if (rows.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        var keys = rows[0].Keys.ToList();

        sb.AppendLine(string.Join(separator, keys.Select(p => EscapeCsvValue(p, separator))));

        foreach (var item in rows)
        {
            var values = keys.Select(key =>
                item.TryGetValue(key, out var value)
                    ? EscapeCsvValue(value?.ToString(), separator)
                    : string.Empty);

            sb.AppendLine(string.Join(separator, values));
        }

        return sb.ToString();
    }

    public static Expression<Func<T, bool>> ToLambdaFilter<T>(this IDictionary<string, object?> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var filter in filters)
        {
            var propertyName = filter.Key;
            var propertyValue = filter.Value;

            var propertyInfo = typeof(T).GetProperty(propertyName);

            if (propertyInfo == null)
                continue;

            var property = Expression.Property(parameter, propertyInfo);
            var propertyType = propertyInfo.PropertyType;
            var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            Expression valueExpression;

            if (propertyValue is null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(propertyType) is null)
                    continue;

                valueExpression = Expression.Constant(null, propertyType);
            }
            else
            {
                object convertedValue;

                try
                {
                    if (targetType.IsEnum)
                    {
                        convertedValue = propertyValue is string enumString
                            ? Enum.Parse(targetType, enumString, ignoreCase: true)
                            : Enum.ToObject(targetType, propertyValue);
                    }
                    else if (targetType == typeof(Guid))
                    {
                        convertedValue = propertyValue is Guid guid
                            ? guid
                            : Guid.Parse(propertyValue.ToString()!);
                    }
                    else
                    {
                        convertedValue = targetType.IsInstanceOfType(propertyValue)
                            ? propertyValue
                            : Convert.ChangeType(propertyValue, targetType, CultureInfo.InvariantCulture)!;
                    }
                }
                catch
                {
                    continue;
                }

                var constant = Expression.Constant(convertedValue, targetType);
                valueExpression = targetType == propertyType
                    ? constant
                    : Expression.Convert(constant, propertyType);
            }

            var equalityExpression = Expression.Equal(property, valueExpression);

            combinedExpression = combinedExpression == null
                                    ? equalityExpression
                                        : Expression.AndAlso(combinedExpression, equalityExpression);
        }

        if (combinedExpression == null)
        {
            throw new ArgumentException("No valid filters were provided.");
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
    }

    private static string EscapeCsvValue(string? value, char separator = ';')
    {
        return value?
            .Replace(separator.ToString(), $"\\{separator}")
            .Replace("\r", "")
            .Replace("\n", "\\n")
            ?? string.Empty;
    }

    public static string ToQueryString(this IDictionary<string, object?>? parameters)
    {
        if (parameters == null || !parameters.Any())
        {
            return string.Empty;
        }

        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in parameters)
        {
            if (param.Value != null)
            {
                query[param.Key] = param.Value.ToString();
            }
        }

        return query?.ToString() ?? string.Empty;
    }
}
