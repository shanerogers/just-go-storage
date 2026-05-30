using JustGo.Api.Features.Auth;
using JustGo.Api.Features.Clubs;
using JustGo.Api.Features.Competitions;
using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Events;
using JustGo.Api.Features.Members;
using JustGo.Api.Features.Memberships;
using JustGo.Api.Features.Organisations;
using JustGo.Api.Features.Rewards;
using JustGo.Api.Features.Shops;
using JustGo.Api.Services;
using JustGo.Integrations.JustGo.Services;
using Microsoft.Extensions.Options;

namespace JustGo.Api;

internal static class JustGoClientExtensions
{
    extension(IServiceCollection services)

    {

        public IServiceCollection AddJustGoClient()
        {
            services.AddHttpClient("JustGoAuth", (sp, client) =>
           {
               var opts = sp.GetRequiredService<IOptions<JustGoOptions>>().Value;
               client.BaseAddress = new Uri(opts.BaseUrl);
           });

            services
                 .AddOptions<JustGoOptions>()
                 .BindConfiguration(JustGoOptions.SectionName)
                 .ValidateDataAnnotations()
                 .ValidateOnStart();

            services
                .AddTransient<JustGoAuthHandler>()
                .AddTransient<IJustGoTokenService, JustGoTokenService>();

            IHttpClientBuilder[] httpClientBuilders =
            [
                services.AddHttpClient<IAuthClient, AuthClient>(configureJustGoClient),
            services.AddHttpClient<ICompetitionClient, CompetitionClient>(configureJustGoClient),
            services.AddHttpClient<ICredentialClient, CredentialClient>(configureJustGoClient),
            services.AddHttpClient<IEventClient, EventClient>(configureJustGoClient),
            services.AddHttpClient<IClubClient, ClubClient>(configureJustGoClient),
            services.AddHttpClient<IShopClient, ShopClient>(configureJustGoClient),
            services.AddHttpClient<IMemberClient, MemberClient>(configureJustGoClient),
            services.AddHttpClient<IMembershipClient, MembershipClient>(configureJustGoClient),
            services.AddHttpClient<IOrganisationClient, OrganisationClient>(configureJustGoClient),
            services.AddHttpClient<IRewardClient, RewardClient>(configureJustGoClient)
            ];

            foreach (var builder in httpClientBuilders)
            {
                builder.AddHttpMessageHandler<JustGoAuthHandler>();
            }

            return services;

            static void configureJustGoClient(IServiceProvider serviceProvider, HttpClient client)
            {
                var opts = serviceProvider.GetRequiredService<IOptions<JustGoOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl);
            }
        }
    }
}
