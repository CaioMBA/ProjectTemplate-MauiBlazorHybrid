namespace Domain.Models.ViewModels.ViewLanguageModels.LayoutViewLanguageModels;

public record MainLayoutViewLanguageModel : ViewLanguageModel
{
    public required string LogoutButton { get; set; }
}
