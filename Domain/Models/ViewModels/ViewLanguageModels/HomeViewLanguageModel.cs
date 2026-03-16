namespace Domain.Models.ViewModels.ViewLanguageModels;

public record HomeViewLanguageModel : ViewLanguageModel
{
    public required string Welcome { get; set; }
}
