using Domain.Enums;

namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;

public record RestApiRequestModel : ApiRequestModel
{
    public required ApiRequestMethod TypeRequest { get; set; }
    public string? Body { get; set; }
}
