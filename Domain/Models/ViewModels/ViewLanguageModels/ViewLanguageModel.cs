namespace Domain.Models.ViewModels.ViewLanguageModels;

public abstract record ViewLanguageModel
{
    public required string Title { get; set; }
    public required string Description { get; set; }
}
