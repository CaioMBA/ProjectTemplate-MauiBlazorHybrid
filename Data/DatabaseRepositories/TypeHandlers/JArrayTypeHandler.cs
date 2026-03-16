using Dapper;
using Domain.Extensions;
using Newtonsoft.Json.Linq;
using System.Data;

namespace Data.DatabaseRepositories.TypeHandlers;

public class JArrayTypeHandler : SqlMapper.TypeHandler<JArray>
{
    public override void SetValue(IDbDataParameter parameter, JArray? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = (object?)value?.ToJson() ?? DBNull.Value;
    }

    public override JArray Parse(object value)
    {
        if (value is string json)
        {
            return JArray.Parse(json);
        }

        throw new DataException("Unexpected data type when parsing JSON.");
    }
}
