namespace CamelMailer.Tests;

/// <summary>
/// Round-trip tests against a real CamelMailer instance. These are skipped
/// unless <c>CAMELMAILER_API_KEY</c> (and, for sending, <c>CAMELMAILER_FROM</c> /
/// <c>CAMELMAILER_TO</c>) are set. They never run in CI.
/// Point <c>CAMELMAILER_BASE_URL</c> at a self-hosted instance if needed.
/// </summary>
public class IntegrationTests
{
    private static CamelMailerClient CreateClient()
    {
        var options = new CamelMailerOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("CAMELMAILER_API_KEY")!,
        };
        var baseUrl = Environment.GetEnvironmentVariable("CAMELMAILER_BASE_URL");
        if (!string.IsNullOrEmpty(baseUrl))
        {
            options.BaseUrl = baseUrl;
        }

        return new CamelMailerClient(options);
    }

    [IntegrationFact]
    public async Task StatsAndMessageList_RoundTrip()
    {
        using var client = CreateClient();

        var stats = await client.Stats.GetAsync();
        Assert.True(stats.Total >= 0);

        var messages = await client.Emails.ListAsync(new ListEmailsOptions { PerPage = 5 });
        Assert.NotNull(messages.Messages);

        var streams = await client.Streams.ListAsync();
        Assert.NotNull(streams);
    }

    [IntegrationFact("CAMELMAILER_FROM", "CAMELMAILER_TO")]
    public async Task SendAndFetch_RoundTrip()
    {
        using var client = CreateClient();

        var sent = await client.Emails.SendAsync(new SendEmailRequest
        {
            From = Environment.GetEnvironmentVariable("CAMELMAILER_FROM")!,
            To = [Environment.GetEnvironmentVariable("CAMELMAILER_TO")!],
            Subject = "camelmailer-dotnet integration test",
            TextBody = $"Sent by the .NET SDK integration test at {DateTimeOffset.UtcNow:O}.",
            Tag = "sdk-integration-test",
        });

        Assert.NotNull(sent.MessageId);
        var recipient = Assert.Single(sent.Recipients);
        Assert.Equal("queued", recipient.Status);

        var details = await client.Emails.GetAsync(sent.MessageId!.Value);
        Assert.Equal("camelmailer-dotnet integration test", details.Message.Subject);
    }
}
