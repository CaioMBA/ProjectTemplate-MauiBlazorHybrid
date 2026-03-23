using Domain.Models.ViewModels.ViewLanguageModels;

namespace Domain.Models.ViewModels.ViewLanguageModels.HandlerViewLanguageModels;

public record NotFoundViewLanguageModel : ViewLanguageModel
{
    public required string Paragraph { get; set; }
    public required string RedirectButton { get; set; }
}
