namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.ResponseModels;

public record GrpcApiResponseModel : ApiResponseModel
{
    public byte[]? Data { get; set; }
    public IDictionary<string, string>? Trailers { get; set; }
}
