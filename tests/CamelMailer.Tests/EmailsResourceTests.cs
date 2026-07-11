using System.Net;
using System.Text;
using System.Text.Json;

namespace CamelMailer.Tests;

public class EmailsResourceTests
{
    [Fact]
    public async Task SendAsync_PostsSnakeCaseBody_AndParsesResult()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success("""
                {
                  "message_id": 42,
                  "recipients": [
                    { "rcpt_to": "ada@example.com", "message_id": 42, "token": "tok1", "status": "queued" }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var result = await client.Emails.SendAsync(new SendEmailRequest
        {
            From = "billing@acme.com",
            To = ["ada@example.com"],
            Subject = "Your receipt",
            TextBody = "Thanks for your purchase.",
            Tag = "receipt",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages",
            handler.LastRequest.RequestUri!.ToString());

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        Assert.Equal("billing@acme.com", root.GetProperty("from").GetString());
        Assert.Equal("ada@example.com", root.GetProperty("to")[0].GetString());
        Assert.Equal("Your receipt", root.GetProperty("subject").GetString());
        Assert.Equal("Thanks for your purchase.", root.GetProperty("text_body").GetString());
        Assert.Equal("receipt", root.GetProperty("tag").GetString());
        Assert.False(root.TryGetProperty("html_body", out _));

        Assert.Equal(42, result.MessageId);
        var recipient = Assert.Single(result.Recipients);
        Assert.Equal("ada@example.com", recipient.RcptTo);
        Assert.Equal(42, recipient.MessageId);
        Assert.Equal("tok1", recipient.Token);
        Assert.Equal("queued", recipient.Status);
    }

    [Fact]
    public async Task SendAsync_SerializesNamedAddressesAsObjects()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success("""{"message_id":1,"recipients":[]}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Emails.SendAsync(new SendEmailRequest
        {
            From = new EmailAddress { Email = "billing@acme.com", Name = "Acme Billing" },
            To = [new EmailAddress { Email = "ada@example.com", Name = "Ada" }, "bob@example.com"],
            Subject = "Hi",
            TextBody = "Hello",
        });

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        Assert.Equal("billing@acme.com", root.GetProperty("from").GetProperty("email").GetString());
        Assert.Equal("Acme Billing", root.GetProperty("from").GetProperty("name").GetString());
        Assert.Equal("Ada", root.GetProperty("to")[0].GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.String, root.GetProperty("to")[1].ValueKind);
    }

    [Fact]
    public async Task SendAsync_SerializesAttachmentsHeadersAndMetadata()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success("""{"message_id":1,"recipients":[]}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Emails.SendAsync(new SendEmailRequest
        {
            From = "billing@acme.com",
            To = ["ada@example.com"],
            Subject = "Invoice",
            HtmlBody = "<p>Hi</p>",
            Headers = new Dictionary<string, string> { ["X-Custom"] = "yes" },
            Metadata = new Dictionary<string, object?> { ["order_id"] = "o_123" },
            Stream = "outbound-2",
            Attachments =
            [
                Attachment.FromBytes("invoice.pdf", "application/pdf", Encoding.UTF8.GetBytes("PDF")),
            ],
        });

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        var attachment = root.GetProperty("attachments")[0];
        Assert.Equal("invoice.pdf", attachment.GetProperty("name").GetString());
        Assert.Equal("application/pdf", attachment.GetProperty("content_type").GetString());
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("PDF")),
            attachment.GetProperty("data_base64").GetString());
        Assert.Equal("yes", root.GetProperty("headers").GetProperty("X-Custom").GetString());
        Assert.Equal("o_123", root.GetProperty("metadata").GetProperty("order_id").GetString());
        Assert.Equal("outbound-2", root.GetProperty("stream").GetString());
    }

    [Fact]
    public async Task SendBatchAsync_PostsPlainJsonArray_AndParsesPerEntryResults()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "messages": [
                    { "status": "success", "data": { "message_id": 1, "recipients": [] } },
                    { "status": "error", "error": { "code": "ValidationError", "message": "To is required" } }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var results = await client.Emails.SendBatchAsync(
        [
            new SendEmailRequest { From = "a@acme.com", To = ["x@example.com"], TextBody = "1" },
            new SendEmailRequest { From = "a@acme.com", To = [], TextBody = "2" },
        ]);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/batch",
            handler.LastRequest!.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
        Assert.Equal(2, body.RootElement.GetArrayLength());

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal(1, results[0].Data!.MessageId);
        Assert.False(results[1].IsSuccess);
        Assert.Equal("ValidationError", results[1].Error!.Code);
        Assert.Equal("To is required", results[1].Error!.Message);
    }

    [Fact]
    public async Task SendWithTemplateAsync_PostsTemplateAndModel()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success("""{"message_id":7,"recipients":[]}"""),
        };
        using var client = TestClient.Create(handler);

        var result = await client.Emails.SendWithTemplateAsync(new SendTemplateEmailRequest
        {
            From = "hello@acme.com",
            To = ["ada@example.com"],
            Template = "welcome",
            TemplateModel = new Dictionary<string, object?> { ["name"] = "Ada" },
        });

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/with_template",
            handler.LastRequest!.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("welcome", body.RootElement.GetProperty("template").GetString());
        Assert.Equal("Ada", body.RootElement.GetProperty("template_model").GetProperty("name").GetString());
        Assert.Equal(7, result.MessageId);
    }

    [Fact]
    public async Task SendWithTemplateBatchAsync_PostsPlainJsonArray()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                { "messages": [ { "status": "success", "data": { "message_id": 9, "recipients": [] } } ] }
                """),
        };
        using var client = TestClient.Create(handler);

        var results = await client.Emails.SendWithTemplateBatchAsync(
        [
            new SendTemplateEmailRequest
            {
                From = "hello@acme.com",
                To = ["ada@example.com"],
                Template = "welcome",
            },
        ]);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/with_template/batch",
            handler.LastRequest!.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(JsonValueKind.Array, body.RootElement.ValueKind);
        Assert.Equal("welcome", body.RootElement[0].GetProperty("template").GetString());
        var only = Assert.Single(results);
        Assert.Equal(9, only.Data!.MessageId);
    }

    [Fact]
    public async Task GetAsync_ReturnsMessageWithDeliveries()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "message": {
                    "id": 42, "token": "abc", "scope": "outgoing",
                    "rcpt_to": "ada@example.com", "mail_from": "billing@acme.com",
                    "subject": "Your receipt", "message_id": "<m1@acme>", "tag": "receipt",
                    "status": "Sent", "bounce": false, "spam_status": null, "spam_score": 0.1,
                    "held": false, "threat": false, "size": 1204, "metadata": {"order":"o_1"},
                    "stream_id": 7, "bypassed": false, "created_at": "2026-07-01T10:00:00+00:00"
                  },
                  "deliveries": [
                    {
                      "id": 5, "status": "Sent", "details": "Accepted",
                      "output": "250 OK", "sent_with_ssl": true,
                      "created_at": "2026-07-01T10:00:05+00:00"
                    }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var details = await client.Emails.GetAsync(42);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/42",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal(42, details.Message.Id);
        Assert.Equal("outgoing", details.Message.Scope);
        Assert.Equal("<m1@acme>", details.Message.MessageId);
        Assert.Equal(0.1, details.Message.SpamScore);
        Assert.Equal(7, details.Message.StreamId);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero), details.Message.CreatedAt);
        var delivery = Assert.Single(details.Deliveries);
        Assert.Equal("250 OK", delivery.Output);
        Assert.True(delivery.SentWithSsl);
    }

    [Fact]
    public async Task ListAsync_WithoutOptions_SendsNoQueryString()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                { "messages": [], "pagination": { "page": 1, "per_page": 30, "total": 0, "total_pages": 0 } }
                """),
        };
        using var client = TestClient.Create(handler);

        var list = await client.Emails.ListAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Empty(list.Messages);
        Assert.Equal(1, list.Pagination!.Page);
    }

    [Fact]
    public async Task ListAsync_WithFilters_BuildsQueryString()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                { "messages": [], "pagination": { "page": 2, "per_page": 50, "total": 120, "total_pages": 3 } }
                """),
        };
        using var client = TestClient.Create(handler);

        await client.Emails.ListAsync(new ListEmailsOptions
        {
            Page = 2,
            PerPage = 50,
            Scope = "outgoing",
            Status = "Sent",
            Tag = "receipt",
            Query = "ada@example.com",
            Stream = "outbound 2",
        });

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("page=2", query);
        Assert.Contains("per_page=50", query);
        Assert.Contains("scope=outgoing", query);
        Assert.Contains("status=Sent", query);
        Assert.Contains("tag=receipt", query);
        Assert.Contains("query=ada%40example.com", query);
        Assert.Contains("stream=outbound%202", query);
    }

    [Fact]
    public async Task GetDeliveriesAsync_ReturnsDeliveryList()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "deliveries": [
                    { "id": 1, "status": "SoftFail", "details": "greylisted", "output": "451",
                      "sent_with_ssl": false, "created_at": "2026-07-01T10:00:00+00:00" }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var deliveries = await client.Emails.GetDeliveriesAsync(42);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/42/deliveries",
            handler.LastRequest!.RequestUri!.ToString());
        var delivery = Assert.Single(deliveries);
        Assert.Equal("SoftFail", delivery.Status);
        Assert.Equal("greylisted", delivery.Details);
    }

    [Fact]
    public async Task GetOpensAsync_ReturnsEvents()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "opens": [
                    { "ip_address": "203.0.113.9", "user_agent": "Mozilla/5.0", "url": null,
                      "created_at": "2026-07-01T11:00:00+00:00" }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var opens = await client.Emails.GetOpensAsync(42);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/42/opens",
            handler.LastRequest!.RequestUri!.ToString());
        var open = Assert.Single(opens);
        Assert.Equal("203.0.113.9", open.IpAddress);
        Assert.Null(open.Url);
    }

    [Fact]
    public async Task GetClicksAsync_ReturnsEvents()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "clicks": [
                    { "ip_address": "203.0.113.9", "user_agent": "Mozilla/5.0",
                      "url": "https://acme.com/invoice", "created_at": "2026-07-01T11:05:00+00:00" }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var clicks = await client.Emails.GetClicksAsync(42);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/42/clicks",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("https://acme.com/invoice", Assert.Single(clicks).Url);
    }

    [Fact]
    public async Task GetRawAsync_DecodesBase64Source()
    {
        var raw = "From: billing@acme.com\r\nSubject: Hi\r\n\r\nHello";
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success(
                $$"""{ "raw_message": "{{Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))}}" }"""),
        };
        using var client = TestClient.Create(handler);

        var bytes = await client.Emails.GetRawAsync(42);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/messages/42/raw",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(raw, Encoding.UTF8.GetString(bytes));
    }
}
