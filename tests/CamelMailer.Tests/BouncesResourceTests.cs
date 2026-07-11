namespace CamelMailer.Tests;

public class BouncesResourceTests
{
    [Fact]
    public async Task ListAsync_ReturnsBouncesWithPagination()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "bounces": [
                    { "id": 9, "token": "b-9", "scope": "incoming", "rcpt_to": "billing@acme.com",
                      "mail_from": null, "subject": "Delivery failed", "message_id": null,
                      "tag": null, "status": "Processed", "bounce": true, "spam_status": null,
                      "spam_score": null, "held": false, "threat": false, "size": 900,
                      "metadata": null, "stream_id": null, "bypassed": false,
                      "created_at": "2026-07-02T08:00:00+00:00" }
                  ],
                  "pagination": { "page": 1, "per_page": 30, "total": 1, "total_pages": 1 }
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var list = await client.Bounces.ListAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/bounces",
            handler.LastRequest!.RequestUri!.ToString());
        var bounce = Assert.Single(list.Bounces);
        Assert.True(bounce.Bounce);
        Assert.Equal(1, list.Pagination!.Total);
    }

    [Fact]
    public async Task ListAsync_WithOptions_BuildsQueryString()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                { "bounces": [], "pagination": { "page": 3, "per_page": 10, "total": 0, "total_pages": 0 } }
                """),
        };
        using var client = TestClient.Create(handler);

        await client.Bounces.ListAsync(new ListBouncesOptions
        {
            Page = 3,
            PerPage = 10,
            Tag = "receipt",
        });

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("page=3", query);
        Assert.Contains("per_page=10", query);
        Assert.Contains("tag=receipt", query);
    }

    [Fact]
    public async Task GetAsync_ReturnsSingleBounce()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "bounce": {
                    "id": 9, "token": "b-9", "scope": "incoming", "rcpt_to": "billing@acme.com",
                    "mail_from": null, "subject": "Delivery failed", "message_id": null,
                    "tag": null, "status": "Processed", "bounce": true, "spam_status": null,
                    "spam_score": null, "held": false, "threat": false, "size": 900,
                    "metadata": null, "stream_id": null, "bypassed": false,
                    "created_at": "2026-07-02T08:00:00+00:00"
                  }
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var bounce = await client.Bounces.GetAsync(9);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/bounces/9",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(9, bounce.Id);
        Assert.Equal("Delivery failed", bounce.Subject);
    }
}
