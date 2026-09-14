namespace CamelMailer.Tests;

public class InboundLogsResourceTests
{
    [Fact]
    public async Task Inbound_ListAsync_ReadsTheInboundKey()
    {
        var handler = new MockHttpMessageHandler
        {
            // The page comes back under "inbound", not "messages".
            ResponseBody = TestEnvelope.Success("""
                {"inbound":[{"id":55,"status":"Held","held":true}],
                 "pagination":{"page":1,"per_page":50,"total":1,"total_pages":1}}
                """),
        };
        using var client = TestClient.Create(handler);

        var page = await client.Inbound.ListAsync(new ListInboundOptions
        {
            Status = "held",
            PerPage = 50,
        });

        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("status=held", url, StringComparison.Ordinal);
        Assert.Contains("per_page=50", url, StringComparison.Ordinal);
        Assert.Single(page.Inbound);
        Assert.Equal(1, page.Pagination!.Total);
    }

    [Fact]
    public async Task Inbound_RetryAndBypass()
    {
        var handler = new MockHttpMessageHandler
        {
            // The endpoint answers with "requeued", and carries the message.
            ResponseBody = TestEnvelope.Success(
                """{"requeued":true,"message":{"id":55,"status":"Pending"}}"""),
        };
        using var client = TestClient.Create(handler);

        var retried = await client.Inbound.RetryAsync(55);
        Assert.True(retried.Requeued);
        Assert.Equal(55, retried.Message!.Id);
        Assert.EndsWith(
            "/api/v2/server/inbound/55/retry",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);

        Assert.True((await client.Inbound.BypassAsync(55)).Requeued);
        Assert.EndsWith(
            "/api/v2/server/inbound/55/bypass",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logs_ListAndTags()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {"requests":[{"id":1,"method":"POST","path":"/api/v2/server/messages",
                  "status_code":201,"duration_ms":12,"user_agent":"camelmailer-dotnet",
                  "created_at":"2026-09-14T08:00:00Z"}],
                 "pagination":{"page":1,"per_page":25,"total":1,"total_pages":1}}
                """),
        };
        using var client = TestClient.Create(handler);

        var page = await client.Logs.ListAsync(perPage: 25);

        Assert.Contains("per_page=25", handler.LastRequest!.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal(201, page.Requests[0].StatusCode);
        Assert.Equal(12, page.Requests[0].DurationMs);

        handler.ResponseBody = TestEnvelope.Success("""{"tags":[{"tag":"receipt","count":12}]}""");
        var tags = await client.Logs.GetTagsAsync();
        Assert.Equal("receipt", Assert.Single(tags).Tag);
    }
}
