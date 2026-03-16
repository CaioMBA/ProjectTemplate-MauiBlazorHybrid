namespace Domain.Models.ApplicationConfigurationModels;

public record AppThemeModel
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public AppTheme? Theme { get; set; }
    public string? Icon { get; set; }
}
