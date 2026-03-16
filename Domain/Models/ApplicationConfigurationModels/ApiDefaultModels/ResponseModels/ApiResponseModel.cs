namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.ResponseModels;

public record ApiResponseModel
{
    public int? StatusCode { get; set; }
    public string? Message { get; set; }
}
