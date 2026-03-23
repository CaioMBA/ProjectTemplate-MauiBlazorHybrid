using Domain.Models.ViewModels.ViewLanguageModels;
using Domain.Models.ViewModels.ViewLanguageModels.HandlerViewLanguageModels;
using Domain.Models.ViewModels.ViewLanguageModels.LayoutViewLanguageModels;

namespace Domain.Models.ApplicationConfigurationModels;

public record AppLanguageModel
{
    public string? Code { get; set; }
    public string? Flag { get; set; }
    public required string Name { get; set; }
    public required NavMenuViewLanguageModel NavMenu { get; set; }
    public required MainLayoutViewLanguageModel MainLayout { get; set; }
    public required NotFoundViewLanguageModel NotFound { get; set; }
    public required ForbiddenViewLanguageModel Forbidden { get; set; }
    public required LoadingViewLanguageModel Loading { get; set; }
    public required HomeViewLanguageModel Home { get; set; }
}
