namespace Domain.Models.ApplicationConfigurationModels.ApiDefaultModels.RequestModels;

public record ApiRequestModel
{
    public required string Url { get; set; }
    public double? TimeOut { get; set; }
    public IDictionary<string, string?>? Headers { get; set; }
    public IDictionary<string, string?>? QueryParameters { get; set; }
    public AuthenticationHeaderModel? Authentication { get; set; }
}
