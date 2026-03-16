using Dapper;
using Domain.Extensions;
using Newtonsoft.Json.Linq;
using System.Data;

namespace Data.DatabaseRepositories.TypeHandlers;

public class JObjectTypeHandler : SqlMapper.TypeHandler<JObject>
{
    public override void SetValue(IDbDataParameter parameter, JObject? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = (object?)value?.ToJson() ?? DBNull.Value;
    }

    public override JObject Parse(object value)
    {
        if (value is string json)
        {
            return JObject.Parse(json);
        }

        throw new DataException("Unexpected data type when parsing JSON.");
    }
}
