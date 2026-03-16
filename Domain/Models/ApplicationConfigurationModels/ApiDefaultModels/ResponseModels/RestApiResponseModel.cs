namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.ResponseModels;

public record RestApiResponseModel : ApiResponseModel
{
    public string? Data { get; set; }
    public IDictionary<string, string>? Headers { get; set; }
}
