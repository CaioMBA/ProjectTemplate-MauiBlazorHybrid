using Dapper;
using System.Data;
using System.Text.Json.Nodes;

namespace Data.DatabaseRepositories.TypeHandlers;

public class JsonObjectTypeHandler : SqlMapper.TypeHandler<JsonObject>
{
    public override void SetValue(IDbDataParameter parameter, JsonObject? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = (object?)value?.ToJsonString() ?? DBNull.Value;
    }

    public override JsonObject Parse(object value)
    {
        if (value is string json)
        {
            var node = JsonNode.Parse(json);
            if (node is JsonObject obj)
                return obj;

            throw new DataException("JSON is not an object.");
        }

        throw new DataException("Unexpected data type when parsing JSON.");
    }
}
