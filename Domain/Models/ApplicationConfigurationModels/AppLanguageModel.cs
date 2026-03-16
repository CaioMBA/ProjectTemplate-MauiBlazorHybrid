namespace Domain.Models.ApplicationConfigurationModels;

using Domain.Models.ViewModels.ViewLanguageModels;

public record AppLanguageModel
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Flag { get; set; }
    public required HomeViewLanguageModel Home { get; set; }
    public required NotFoundViewLanguageModel NotFound { get; set; }
}
