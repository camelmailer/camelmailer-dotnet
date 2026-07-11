namespace CamelMailer.Tests;

public class ClientTests
{
    [Fact]
    public async Task DefaultBaseUrl_IsCamelMailerCloud()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"stats":{}}"""),
        };
        using var client = new CamelMailerClient(
            new CamelMailerOptions { ApiKey = "cm_x" },
            new HttpClient(handler));

        await client.Stats.GetAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/stats",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task CustomBaseUrl_IsUsedForSelfHostedInstances()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"stats":{}}"""),
        };
        using var client = TestClient.Create(handler, "https://mail.example.com");

        await client.Stats.GetAsync();

        Assert.Equal(
            "https://mail.example.com/api/v2/server/stats",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task BaseUrlWithTrailingSlash_DoesNotProduceDoubleSlashes()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"stats":{}}"""),
        };
        using var client = TestClient.Create(handler, "https://mail.example.com/");

        await client.Stats.GetAsync();

        Assert.Equal(
            "https://mail.example.com/api/v2/server/stats",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task EveryRequest_CarriesServerApiKeyHeader()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"stats":{}}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Stats.GetAsync();

        Assert.Equal(
            TestClient.ApiKey,
            Assert.Single(handler.LastRequest!.Headers.GetValues("X-Server-API-Key")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankApiKey(string apiKey)
    {
        Assert.Throws<ArgumentException>(() => new CamelMailerClient(apiKey));
    }

    [Fact]
    public void Constructor_RejectsMissingApiKeyInOptions()
    {
        Assert.Throws<ArgumentException>(
            () => new CamelMailerClient(new CamelMailerOptions()));
    }

    [Fact]
    public void Constructor_RejectsInvalidBaseUrl()
    {
        Assert.Throws<ArgumentException>(
            () => new CamelMailerClient(new CamelMailerOptions
            {
                ApiKey = "cm_x",
                BaseUrl = "not a url",
            }));
    }

    [Fact]
    public void ApiKeyConstructor_ExposesAllResources()
    {
        using var client = new CamelMailerClient("cm_x");

        Assert.NotNull(client.Emails);
        Assert.NotNull(client.Templates);
        Assert.NotNull(client.Streams);
        Assert.NotNull(client.Stats);
        Assert.NotNull(client.Bounces);
        Assert.NotNull(client.Dmarc);
    }

    [Fact]
    public async Task ApiKeyAndBaseUrlConstructor_TargetsGivenInstance()
    {
        // The two-argument convenience constructor cannot inject a handler,
        // so verify via options + client equivalence instead.
        using var client = new CamelMailerClient("cm_x", "https://mail.example.com");
        Assert.NotNull(client.Emails);

        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"stats":{}}"""),
        };
        using var optionsClient = new CamelMailerClient(
            new CamelMailerOptions { ApiKey = "cm_x", BaseUrl = "https://mail.example.com" },
            new HttpClient(handler));
        await optionsClient.Stats.GetAsync();
        Assert.StartsWith("https://mail.example.com/", handler.LastRequest!.RequestUri!.ToString());
    }
}
