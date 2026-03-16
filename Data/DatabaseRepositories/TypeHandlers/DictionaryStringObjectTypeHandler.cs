using Dapper;
using Domain.Extensions;
using Newtonsoft.Json;
using System.Data;

namespace Data.DatabaseRepositories.TypeHandlers;

public class DictionaryStringObjectTypeHandler : SqlMapper.TypeHandler<IDictionary<string, object?>>
{
    public override void SetValue(IDbDataParameter parameter, IDictionary<string, object?>? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value == null ? (object)DBNull.Value : value.ToJson();
    }

    public override IDictionary<string, object?>? Parse(object value)
    {
        if (value is string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object?>>(json)
                ?? throw new DataException("JSON dictionary could not be parsed.");
        }

        throw new DataException("Unexpected data type when parsing IDictionary<string, object?>.");
    }
}
