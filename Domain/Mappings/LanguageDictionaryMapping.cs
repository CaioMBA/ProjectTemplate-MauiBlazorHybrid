using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Domain.Mappings;

public sealed partial class LanguageDictionaryMapping : ILanguageDictionaryMapping
{
    private readonly IReadOnlyDictionary<string, string> _replacerMap;

    public LanguageDictionaryMapping(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetService<IOptionsMonitor<AppSettingsModel>>();
        var appSettings = options!.CurrentValue;
        _replacerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AppName"] = appSettings.AppName,
            ["AppVersion"] = appSettings.AppVersion,
        };
    }

    public string ReplaceTokens(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || _replacerMap.Count == 0)
        {
            return input;
        }

        return LanguageDictionaryTokenRegex().Replace(input, match =>
        {
            var token = match.Groups["token"].Value;
            return _replacerMap.TryGetValue(token, out var value) ? value : match.Value;
        });
    }

    [GeneratedRegex(@"\{(?<token>[^{}]+)\}")]
    private static partial Regex LanguageDictionaryTokenRegex();
}
