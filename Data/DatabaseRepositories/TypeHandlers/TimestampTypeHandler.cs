using Dapper;
using Google.Protobuf.WellKnownTypes;
using System.Data;
using System.Globalization;

namespace Data.DatabaseRepositories.TypeHandlers;

public class TimestampTypeHandler : SqlMapper.TypeHandler<Timestamp>
{
    public override void SetValue(IDbDataParameter parameter, Timestamp? value)
    {
        parameter.DbType = DbType.DateTime2;
        parameter.Value = value?.ToDateTime();
    }

    public override Timestamp Parse(object value)
    {
        return value switch
        {
            Timestamp timestamp => timestamp,
            DateTime dateTime => Timestamp.FromDateTime(dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime()),
            DateTimeOffset dateTimeOffset => Timestamp.FromDateTimeOffset(dateTimeOffset),
            string str when DateTime.TryParse(str, CultureInfo.InvariantCulture, out DateTime parsed) => Timestamp.FromDateTime(parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime()),
            _ => throw new DataException("Unexpected data type when parsing Timestamp.")
        };
    }
}
