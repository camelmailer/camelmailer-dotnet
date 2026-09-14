using System.Net;

namespace CamelMailer.Tests;

public class IdempotencyTests
{
    private static SendEmailRequest Receipt() => new()
    {
        From = "billing@acme.com",
        To = ["ada@example.com"],
        Subject = "Your receipt",
        TextBody = "Thanks.",
    };

    [Fact]
    public async Task TheKeyTravelsAsAHeaderNotInTheBody()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"message_id":1,"recipients":[]}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Emails.SendAsync(Receipt(), "order-4711");

        Assert.Equal(
            "order-4711",
            handler.LastRequest!.Headers.GetValues("Idempotency-Key").Single());
        // The body is what the server hashes for the claim, so the key must not
        // end up inside it.
        Assert.DoesNotContain("idempotency", handler.LastRequestBody!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NoHeaderWithoutAKey()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"message_id":1}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Emails.SendAsync(Receipt());

        Assert.False(handler.LastRequest!.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task EverySendEndpointCarriesTheKey()
    {
        // The API claims all four, so all four have to send it.
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"message_id":1,"messages":[]}"""),
        };
        using var client = TestClient.Create(handler);
        var template = new SendTemplateEmailRequest
        {
            From = "billing@acme.com",
            To = ["ada@example.com"],
            Template = "welcome",
        };

        await client.Emails.SendAsync(Receipt(), "k");
        AssertKeyed(handler, "/api/v2/server/messages");

        await client.Emails.SendBatchAsync([Receipt()], "k");
        AssertKeyed(handler, "/api/v2/server/messages/batch");

        await client.Emails.SendWithTemplateAsync(template, "k");
        AssertKeyed(handler, "/api/v2/server/messages/with_template");

        await client.Emails.SendWithTemplateBatchAsync([template], "k");
        AssertKeyed(handler, "/api/v2/server/messages/with_template/batch");
    }

    [Fact]
    public async Task AReusedKeyForAnotherBodyIsRefused()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Conflict,
            ResponseBody = TestEnvelope.Error(
                "InvalidIdempotentRequest",
                "The same idempotency key was used with a different request"),
        };
        using var client = TestClient.Create(handler);

        var error = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Emails.SendAsync(Receipt(), "reused"));
        Assert.Equal("InvalidIdempotentRequest", error.ErrorCode);
        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
    }

    [Fact]
    public async Task TheSendAllowanceSurfacesAsSendLimitExceeded()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            ResponseBody = TestEnvelope.Error("SendLimitExceeded", "the send allowance is used up"),
        };
        using var client = TestClient.Create(handler);

        var error = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Emails.SendAsync(Receipt()));
        Assert.Equal("SendLimitExceeded", error.ErrorCode);
    }

    [Fact]
    public async Task SendToStreamAsync_CountsQueuedAgainstSkipped()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Accepted,
            ResponseBody = TestEnvelope.Success("""{"queued":42,"skipped":3}"""),
        };
        using var client = TestClient.Create(handler);

        var result = await client.Emails.SendToStreamAsync(
            "newsletter",
            new SendToStreamRequest
            {
                From = "news@acme.com",
                Subject = "September",
                TextBody = "Hello.",
            });

        Assert.EndsWith(
            "/api/v2/server/streams/newsletter/send",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
        Assert.Equal(42, result.Queued);
        Assert.Equal(3, result.Skipped);
    }

    private static void AssertKeyed(MockHttpMessageHandler handler, string path)
    {
        Assert.EndsWith(path, handler.LastRequest!.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal("k", handler.LastRequest!.Headers.GetValues("Idempotency-Key").Single());
    }
}
