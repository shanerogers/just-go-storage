using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace JustGo.Grading.Tests;

public sealed class JustGoApiResilienceTests
{
    [Fact]
    public void Configure_AllowsSlowJustGoCallsLongerThanTheDefaultTenSeconds()
    {
        var options = new HttpStandardResilienceOptions();

        JustGoApiResilience.Configure(options);

        Assert.True(options.AttemptTimeout.Timeout >= TimeSpan.FromSeconds(30));
        Assert.True(options.TotalRequestTimeout.Timeout > options.AttemptTimeout.Timeout);
    }

    [Fact]
    public async Task Configure_ProducesAClientThatPassesResilienceValidation()
    {
        var services = new ServiceCollection();
#pragma warning disable EXTEXP0001
        services.AddHttpClient("JustGoApi", client => client.BaseAddress = new Uri("http://localhost"))
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(JustGoApiResilience.Configure);
#pragma warning restore EXTEXP0001
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("JustGoApi");

        Assert.NotNull(client);
    }
}
