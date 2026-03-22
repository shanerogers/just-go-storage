using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JustGo.Integrations.JustGo.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJustGoIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JustGoOptions>(configuration.GetSection(JustGoOptions.SectionName));

        services.AddTransient<JustGoAuthHandler>();
        services.AddSingleton<IJustGoTokenService, JustGoTokenService>();

        services.AddHttpClient("JustGoAuth", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<JustGoOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddHttpClient<IJustGoClient, JustGoClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<JustGoOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddHttpMessageHandler<JustGoAuthHandler>();

        return services;
    }
}
