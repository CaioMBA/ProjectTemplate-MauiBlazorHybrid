using Dapper;
using Data.ApiRepositories;
using Data.DatabaseRepositories;
using Data.DatabaseRepositories.DatabaseContexts;
using Data.DatabaseRepositories.TypeHandlers;
using Domain;
using Domain.Interfaces.ApplicationConfigurationInterfaces;
using Domain.Mappings;
using Domain.Models.ApplicationConfigurationModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Services;
using Services.AuthenticationServices;
using System.Net;
using System.Security.Authentication;

namespace CrossCutting;

public static class InjectionSetup
{
    public static IServiceCollection AddConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<AppSettingsModel>(configuration);
        return serviceCollection;
    }

    public static IServiceCollection AddStaticFiles(this IServiceCollection serviceCollection,
                                                    IEnumerable<AppThemeModel> appThemes,
                                                    IEnumerable<AppLanguageModel> appLanguages)
    {
        serviceCollection
            .AddSingleton<IEnumerable<AppThemeModel>>(appThemes)
            .AddSingleton<IEnumerable<AppLanguageModel>>(appLanguages);

        return serviceCollection;
    }

    public static IServiceCollection AddUtilities(this IServiceCollection serviceCollection)
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include
        };
        serviceCollection.AddDistributedMemoryCache();
        serviceCollection.AddSingleton<AppUtils>();
        serviceCollection.AddAutoMapper(options =>
        {
            options.AddProfile<EntityToModelMapping>();
            options.AddProfile<ModelToDtoMapping>();
        });
        return serviceCollection;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection serviceCollection)
    {
        CookieContainer defaultCookieContainer = new();
        serviceCollection.AddHttpClient()
            .ConfigureHttpClientDefaults(c =>
            {
                c.ConfigureHttpClient(c =>
                {
                    c.DefaultRequestVersion = HttpVersion.Version30;
                    c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                    c.Timeout = Timeout.InfiniteTimeSpan;
                });
                c.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                    AllowAutoRedirect = true,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    MaxConnectionsPerServer = int.MaxValue,
                    UseCookies = true,
                    CookieContainer = defaultCookieContainer
                });
            });

        serviceCollection
            .AddScoped<RestApiAccess>()
            .AddScoped<GraphApiAccess>()
            .AddScoped<GrpcApiAccess>();

        return serviceCollection;
    }

    public static async Task<IServiceCollection> AddDatabaseClients(this IServiceCollection serviceCollection)
    {
        #region Dapper Type Handlers
        SqlMapper.AddTypeHandler(new JArrayTypeHandler());
        SqlMapper.AddTypeHandler(new JsonObjectTypeHandler());
        SqlMapper.AddTypeHandler(new JsonNodeTypeHandler());
        SqlMapper.AddTypeHandler(new JArrayTypeHandler());
        SqlMapper.AddTypeHandler(new JObjectTypeHandler());
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new TimestampTypeHandler());
        SqlMapper.AddTypeHandler(new CrontabScheduleTypeHandler());
        SqlMapper.AddTypeHandler(new DictionaryStringObjectTypeHandler());
        #endregion

        #region Entity Framework
        serviceCollection.AddDbContextFactory<AppDbContext>();
        using var serviceProvider = serviceCollection.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        using var dbContext = await dbFactory.CreateDbContextAsync();

        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database migration failed. Exception: {ex.Message}");
            throw new InvalidOperationException("Database migration failed. See inner exception for details.", ex);
        }
        #endregion

        serviceCollection.AddScoped<DatabaseAccess>();
        return serviceCollection;
    }

    public static IServiceCollection AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ISettingsServices, SettingsServices>();
        return serviceCollection;
    }

    public static IServiceCollection AddAppAuthentication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IAccountServices, AccountServices>();
        serviceCollection.AddSingleton<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
        serviceCollection.AddAuthorizationCore();
        return serviceCollection;
    }
}
