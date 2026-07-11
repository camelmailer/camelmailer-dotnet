using Microsoft.Extensions.DependencyInjection;

namespace CamelMailer.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddCamelMailer_RegistersClientInterface()
    {
        var services = new ServiceCollection();
        services.AddCamelMailer(options => options.ApiKey = "cm_test");
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ICamelMailerClient>();

        Assert.NotNull(client.Emails);
        Assert.NotNull(client.Dmarc);
    }

    [Fact]
    public async Task AddCamelMailer_AppliesOptionsToOutgoingRequests()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"stats":{}}"""),
        };
        var services = new ServiceCollection();
        services
            .AddCamelMailer(options =>
            {
                options.ApiKey = "cm_di_key";
                options.BaseUrl = "https://mail.example.com";
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ICamelMailerClient>();
        await client.Stats.GetAsync();

        Assert.Equal(
            "https://mail.example.com/api/v2/server/stats",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(
            "cm_di_key",
            Assert.Single(handler.LastRequest.Headers.GetValues("X-Server-API-Key")));
    }

    [Fact]
    public void AddCamelMailer_WithoutApiKey_FailsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddCamelMailer(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<ICamelMailerClient>());
    }

    [Fact]
    public void AddCamelMailer_ReturnsHttpClientBuilderForCustomisation()
    {
        var services = new ServiceCollection();
        var builder = services.AddCamelMailer(options => options.ApiKey = "cm_test");

        Assert.NotNull(builder);
        Assert.IsAssignableFrom<IHttpClientBuilder>(builder);
    }
}
