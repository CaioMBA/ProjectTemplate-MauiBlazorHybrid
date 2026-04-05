using Domain.Mappings;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTest;

public class LanguageDictionaryMappingTests
{
    [Fact]
    public void ReplaceTokens_ShouldReplaceKnownToken()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<AppSettingsModel>(x => x.AppName = "My App");
        using var provider = services.BuildServiceProvider();

        var sut = new LanguageDictionaryMapping(provider);

        var result = sut.ReplaceTokens("Welcome to {AppName}");

        Assert.Equal("Welcome to My App", result);
    }

    [Fact]
    public void ReplaceTokens_ShouldKeepUnknownToken()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<AppSettingsModel>(x => x.AppName = "My App");
        using var provider = services.BuildServiceProvider();

        var sut = new LanguageDictionaryMapping(provider);

        var result = sut.ReplaceTokens("Value: {UnknownToken}");

        Assert.Equal("Value: {UnknownToken}", result);
    }
}
