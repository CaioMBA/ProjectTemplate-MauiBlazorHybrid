namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;

public record GrpcApiRequestModel : ApiRequestModel
{
    public required string Service { get; set; }
    public required string Method { get; set; }
    public string? Body { get; set; }
}
