namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;

public record GraphQlApiRequesModel : ApiRequestModel
{
    public required string Query { get; set; }
    public IDictionary<string, object?>? Variables { get; set; }
}
