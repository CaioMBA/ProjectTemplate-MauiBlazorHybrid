namespace Domain.Models.ViewModels.ViewLanguageModels;

public record NotFoundViewLanguageModel : ViewLanguageModel
{
    public required string Paragraph { get; set; }
}
