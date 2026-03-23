namespace Domain.Models.ViewModels.ViewLanguageModels.HandlerViewLanguageModels;

public record ForbiddenViewLanguageModel : ViewLanguageModel
{
    public required string Paragraph { get; set; }
    public required string RedirectButton { get; set; }
}
