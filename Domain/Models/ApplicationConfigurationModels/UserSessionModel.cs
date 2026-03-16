namespace Domain.Models.ApplicationConfigurationModels;

public record UserSessionModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; }
    public string? Language { get; set; }
    public string? Theme { get; set; }
    public required List<string> Roles { get; set; }
}
