using Domain.Enums;

namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;

public record AuthenticationHeaderModel
{
    public required ApiAuthorizationType Type { get; set; }
    public required string Authorization { get; set; }
}
