using Dapper;
using System.Data;
using System.Globalization;

namespace Data.DatabaseRepositories.TypeHandlers;

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        string str when DateOnly.TryParse(str, CultureInfo.InvariantCulture, out DateOnly parsed) => parsed,
        _ => throw new DataException("Unexpected data type when parsing DateOnly.")
    };
}
