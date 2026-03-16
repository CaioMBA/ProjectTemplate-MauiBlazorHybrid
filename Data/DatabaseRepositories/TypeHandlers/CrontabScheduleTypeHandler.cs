using Dapper;
using NCrontab;
using System.Data;

namespace Data.DatabaseRepositories.TypeHandlers;

public class CrontabScheduleTypeHandler : SqlMapper.TypeHandler<CrontabSchedule>
{
    public override void SetValue(IDbDataParameter parameter, CrontabSchedule? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value?.ToString();
    }

    public override CrontabSchedule Parse(object value)
    {
        return value switch
        {
            string cronExpression when !string.IsNullOrWhiteSpace(cronExpression) => CrontabSchedule.Parse(cronExpression),
            _ => throw new DataException("Unexpected data type when parsing CrontabSchedule.")
        };
    }
}
