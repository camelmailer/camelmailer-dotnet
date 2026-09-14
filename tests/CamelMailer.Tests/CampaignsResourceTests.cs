using System.Net;

namespace CamelMailer.Tests;

public class CampaignsResourceTests
{
    private const string CampaignJson = """
        {
          "id": 7, "name": "September", "subject": "What shipped", "from": "news@acme.com",
          "status": "draft", "total": 120, "sent": 0, "stream_id": 3,
          "stream": { "permalink": "product-news", "name": "Product news" },
          "scheduled_at": null, "created_at": "2026-09-01T10:00:00Z", "completed_at": null
        }
        """;

    [Fact]
    public async Task ListAsync_ReturnsCampaignsWithTheirStream()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success($$"""{"campaigns":[{{CampaignJson}}]}"""),
        };
        using var client = TestClient.Create(handler);

        var campaigns = await client.Campaigns.ListAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/campaigns",
            handler.LastRequest!.RequestUri!.ToString());
        var campaign = Assert.Single(campaigns);
        Assert.Equal("product-news", campaign.Stream!.Permalink);
        Assert.Null(campaign.ScheduledAt);
    }

    [Fact]
    public async Task ListForStreamAsync_UsesTheStreamPath()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"campaigns":[]}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Campaigns.ListForStreamAsync("product-news");

        Assert.EndsWith(
            "/api/v2/server/streams/product-news/campaigns",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_CarriesTheStats()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success($$"""
                {
                  "campaign": {{CampaignJson}},
                  "stats": { "total": 120, "sent": 118, "delivered": 110, "failed": 8,
                             "opened": 40, "clicked": 9, "unsubscribed": 1 }
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var detail = await client.Campaigns.GetAsync(7);

        Assert.Equal(110, detail.Stats.Delivered);
        Assert.Equal(7, detail.Campaign.Id);
    }

    [Fact]
    public async Task CreateDraftAsync_NamesTheStreamInTheBody()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success($$"""{"campaign":{{CampaignJson}}}"""),
        };
        using var client = TestClient.Create(handler);

        var campaign = await client.Campaigns.CreateDraftAsync(new CreateDraftCampaignRequest
        {
            Stream = "product-news",
            From = "news@acme.com",
            Name = "September",
        });

        // The planning route: the stream travels in the body, not the path.
        Assert.EndsWith(
            "/api/v2/server/campaigns",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
        Assert.Contains("\"stream\":\"product-news\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("scheduled_at", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal("draft", campaign.Status);
    }

    [Fact]
    public async Task CreateDraftAsync_ArmsASchedule()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success("""{"campaign":{"id":8,"status":"scheduled"}}"""),
        };
        using var client = TestClient.Create(handler);

        var campaign = await client.Campaigns.CreateDraftAsync(new CreateDraftCampaignRequest
        {
            Stream = "product-news",
            From = "news@acme.com",
            ScheduledAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero),
        });

        Assert.Contains("scheduled_at", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal("scheduled", campaign.Status);
    }

    [Fact]
    public async Task CreateAndSendAsync_UsesTheStreamRouteAndGoesOutImmediately()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success("""{"campaign":{"id":9,"status":"sending"}}"""),
        };
        using var client = TestClient.Create(handler);

        var campaign = await client.Campaigns.CreateAndSendAsync(
            "product-news",
            new CreateAndSendCampaignRequest { Name = "Status update", From = "news@acme.com" });

        Assert.EndsWith(
            "/api/v2/server/streams/product-news/campaigns",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
        // The stream-scoped route expands to the subscribers before it answers.
        Assert.Equal("sending", campaign.Status);
    }

    [Fact]
    public async Task UpdateAsync_SchedulesWithATime()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"campaign":{"id":7,"status":"scheduled"}}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Campaigns.UpdateAsync(7, new UpdateCampaignRequest
        {
            ScheduledAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero),
        });

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Contains(
            "\"scheduled_at\":\"2026-10-01T08:00:00Z\"",
            handler.LastRequestBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateAsync_ClearScheduleSendsAnExplicitNull()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"campaign":{"id":7,"status":"draft"}}"""),
        };
        using var client = TestClient.Create(handler);

        var campaign = await client.Campaigns.UpdateAsync(
            7, new UpdateCampaignRequest { ClearSchedule = true });

        // An omitted field leaves the schedule standing; only an explicit null
        // drops the campaign back to a draft.
        Assert.Contains("\"scheduled_at\":null", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal("draft", campaign.Status);
    }

    [Fact]
    public async Task UpdateAsync_AnUntouchedScheduleIsOmitted()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success($$"""{"campaign":{{CampaignJson}}}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Campaigns.UpdateAsync(7, new UpdateCampaignRequest { Subject = "Corrected" });

        Assert.Contains("\"subject\":\"Corrected\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("scheduled_at", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAndCancel()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"campaign":{"id":7,"status":"sending"}}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Campaigns.SendAsync(7);
        Assert.EndsWith(
            "/api/v2/server/campaigns/7/send",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);

        handler.ResponseBody = TestEnvelope.Success("""{"campaign":{"id":7,"status":"canceled"}}""");
        var canceled = await client.Campaigns.CancelAsync(7);
        Assert.EndsWith(
            "/api/v2/server/campaigns/7/cancel",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
        Assert.Equal("canceled", canceled.Status);
    }

    [Fact]
    public async Task UpdateAsync_ASendingCampaignIsRefused()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.UnprocessableEntity,
            ResponseBody = TestEnvelope.Error(
                "ValidationError", "a sent campaign can no longer be edited"),
        };
        using var client = TestClient.Create(handler);

        var error = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Campaigns.UpdateAsync(7, new UpdateCampaignRequest { Subject = "Too late" }));
        Assert.Equal("ValidationError", error.ErrorCode);
    }
}
