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

        Assert.NotNull(await client.Campaigns.ListAsync());
        Assert.NotNull(await client.Layouts.ListAsync());
        Assert.NotNull((await client.Inbound.ListAsync(new ListInboundOptions { PerPage = 1 })).Inbound);
        Assert.NotNull((await client.Logs.ListAsync(perPage: 1)).Requests);
        Assert.NotNull(await client.Logs.GetTagsAsync());
    }

    [IntegrationFact("CAMELMAILER_FROM")]
    public async Task Broadcast_RoundTrip()
    {
        using var client = CreateClient();
        var stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var permalink = $"dotnet-bc-{stamp}";
        var from = Environment.GetEnvironmentVariable("CAMELMAILER_FROM")!;

        var stream = await client.Streams.CreateAsync(new CreateStreamRequest
        {
            Name = $"dotnet live {stamp}",
            Permalink = permalink,
            StreamType = "broadcast",
        });
        Assert.Equal(permalink, stream.Permalink);

        await client.Subscribers.AddAsync(
            permalink, new AddSubscriberRequest { Address = "ada@example.test", Name = "Ada" });
        var imported = await client.Subscribers.ImportAsync(
            permalink, ["grace@example.test", "alan@example.test", "grace@example.test", ""]);
        Assert.Equal(2, imported.Added);

        var complained = await client.Subscribers.ComplaintAsync(permalink, "alan@example.test");
        Assert.Equal("unsubscribed", complained.Status);

        // The two create routes behave differently, which is the whole reason
        // they are separate methods.
        var draft = await client.Campaigns.CreateDraftAsync(new CreateDraftCampaignRequest
        {
            Stream = permalink,
            From = from,
            Name = $"dotnet draft {stamp}",
            Subject = "D",
            TextBody = "d",
        });
        Assert.Equal("draft", draft.Status);

        var scheduled = await client.Campaigns.CreateDraftAsync(new CreateDraftCampaignRequest
        {
            Stream = permalink,
            From = from,
            Name = $"dotnet scheduled {stamp}",
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(1),
        });
        Assert.Equal("scheduled", scheduled.Status);

        var sending = await client.Campaigns.CreateAndSendAsync(
            permalink,
            new CreateAndSendCampaignRequest
            {
                Name = $"dotnet now {stamp}",
                From = from,
                Subject = "N",
                TextBody = "n",
            });
        Assert.Equal("sending", sending.Status);

        Assert.Equal(
            "scheduled",
            (await client.Campaigns.UpdateAsync(
                draft.Id,
                new UpdateCampaignRequest { ScheduledAt = DateTimeOffset.UtcNow.AddDays(2) })).Status);
        Assert.Equal(
            "draft",
            (await client.Campaigns.UpdateAsync(
                draft.Id, new UpdateCampaignRequest { ClearSchedule = true })).Status);
        Assert.Equal("canceled", (await client.Campaigns.CancelAsync(draft.Id)).Status);

        var detail = await client.Campaigns.GetAsync(draft.Id);
        Assert.Equal(draft.Id, detail.Campaign.Id);
        Assert.NotEmpty(await client.Campaigns.ListForStreamAsync(permalink));

        var broadcast = await client.Emails.SendToStreamAsync(
            permalink,
            new SendToStreamRequest { From = from, Subject = $"Broadcast {stamp}", TextBody = "hello" });
        Assert.Equal(0, broadcast.Skipped);

        Assert.True((await client.Subscribers.RemoveAsync(permalink, "grace@example.test")).Deleted);
        await client.Streams.ArchiveAsync(permalink);
    }

    [IntegrationFact("CAMELMAILER_FROM", "CAMELMAILER_TO")]
    public async Task IdempotentSend_ReplaysInsteadOfSendingTwice()
    {
        using var client = CreateClient();
        var key = $"dotnet-live-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var request = new SendEmailRequest
        {
            From = Environment.GetEnvironmentVariable("CAMELMAILER_FROM")!,
            To = [Environment.GetEnvironmentVariable("CAMELMAILER_TO")!],
            Subject = "camelmailer-dotnet idempotency",
            TextBody = "once",
        };

        var first = await client.Emails.SendAsync(request, key);
        var replayed = await client.Emails.SendAsync(request, key);
        Assert.Equal(first.MessageId, replayed.MessageId);

        var other = request with { Subject = "camelmailer-dotnet idempotency (different)" };
        var error = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Emails.SendAsync(other, key));
        Assert.Equal("InvalidIdempotentRequest", error.ErrorCode);
    }

    [IntegrationFact]
    public async Task Layout_RoundTrip()
    {
        using var client = CreateClient();
        var permalink = $"dotnet-layout-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        var created = await client.Layouts.CreateAsync(new CreateLayoutRequest
        {
            Name = permalink,
            Permalink = permalink,
            HtmlWrapper = "<html><body>{{{ content }}}</body></html>",
        });
        Assert.Equal(permalink, created.Permalink);
        // A layout created with only an HTML wrapper has no text one.
        Assert.Null(created.TextWrapper);

        var logo = await client.Layouts.UploadLogoAsync(
            permalink,
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJ"
                + "AAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        // The endpoint answers with "url"; reading "logo_url" would yield null.
        Assert.False(string.IsNullOrEmpty(logo.Url));

        Assert.True((await client.Layouts.DeleteAsync(permalink)).Deleted);
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
