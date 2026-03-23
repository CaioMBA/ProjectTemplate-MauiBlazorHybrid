namespace Domain.Models.ViewModels.ViewLanguageModels;

    public record LoadingViewLanguageModel : ViewLanguageModel
    {
        public required string Loading { get; set; }
    }
