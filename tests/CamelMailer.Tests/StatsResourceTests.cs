namespace CamelMailer.Tests;

public class StatsResourceTests
{
    [Fact]
    public async Task GetAsync_ReturnsCounters()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "stats": {
                    "total": 100, "incoming": 10, "outgoing": 90, "sent": 85,
                    "pending": 2, "held": 1, "bounced": 2, "soft_fail": 3, "hard_fail": 1,
                    "opens": 40, "clicks": 12, "unique_opens": 30, "unique_clicks": 9
                  }
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var stats = await client.Stats.GetAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/stats",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(100, stats.Total);
        Assert.Equal(85, stats.Sent);
        Assert.Equal(3, stats.SoftFail);
        Assert.Equal(30, stats.UniqueOpens);
    }

    [Fact]
    public async Task GetAsync_WithWindow_SendsFromAndToAsUtc()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"stats":{"total":0}}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Stats.GetAsync(
            from: new DateTimeOffset(2026, 7, 1, 2, 0, 0, TimeSpan.FromHours(2)),
            to: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero));

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("from=2026-07-01T00%3A00%3A00Z", query);
        Assert.Contains("to=2026-07-02T00%3A00%3A00Z", query);
    }

    [Fact]
    public async Task GetDeliveriesAsync_ReturnsQueueDepth()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "queued": 5,
                  "domains": [
                    { "domain": "example.com", "queued": 3 },
                    { "domain": "example.org", "queued": 2 }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var stats = await client.Stats.GetDeliveriesAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/stats/deliveries",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(5, stats.Queued);
        Assert.Equal(2, stats.Domains.Count);
        Assert.Equal("example.com", stats.Domains[0].Domain);
        Assert.Equal(3, stats.Domains[0].Queued);
    }
}
