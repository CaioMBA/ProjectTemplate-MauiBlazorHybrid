using Dapper;
using System.Data;
using System.Globalization;

namespace Data.DatabaseRepositories.TypeHandlers;

public class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    public override TimeOnly Parse(object value)
    {
        return value switch
        {
            TimeOnly timeOnly => timeOnly,
            TimeSpan timeSpan => TimeOnly.FromTimeSpan(timeSpan),
            DateTime dateTime => TimeOnly.FromDateTime(dateTime),
            string str when TimeOnly.TryParse(str, CultureInfo.InvariantCulture, out TimeOnly parsed) => parsed,
            _ => throw new DataException("Unexpected data type when parsing TimeOnly.")
        };
    }
}
