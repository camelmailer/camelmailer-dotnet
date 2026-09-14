using System.Net;

namespace CamelMailer.Tests;

public class SubscribersLayoutsResourceTests
{
    [Fact]
    public async Task Subscribers_ListAsync_ReturnsTheAudience()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {"subscribers":[{"id":1,"address":"ada@example.com","status":"subscribed",
                 "created_at":"2026-09-01T10:00:00Z"}]}
                """),
        };
        using var client = TestClient.Create(handler);

        var subscribers = await client.Subscribers.ListAsync("product-news");

        Assert.EndsWith(
            "/api/v2/server/streams/product-news/subscribers",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
        Assert.Equal("subscribed", Assert.Single(subscribers).Status);
    }

    [Fact]
    public async Task Subscribers_AddAsync_UpsertsByAddress()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success(
                """{"subscriber":{"id":1,"address":"ada@example.com","status":"subscribed"}}"""),
        };
        using var client = TestClient.Create(handler);

        var subscriber = await client.Subscribers.AddAsync(
            "product-news",
            new AddSubscriberRequest { Address = "ada@example.com", Status = "subscribed" });

        // The endpoint takes an address and a status; there is no name field.
        Assert.Contains("\"address\":\"ada@example.com\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"subscribed\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal("ada@example.com", subscriber.Address);
    }

    [Fact]
    public async Task Subscribers_ImportAsync_SendsAnAddressesArray()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"added":2,"total":2}"""),
        };
        using var client = TestClient.Create(handler);

        var result = await client.Subscribers.ImportAsync(
            "product-news", ["ada@example.com", "grace@example.com"]);

        Assert.EndsWith(
            "/api/v2/server/streams/product-news/subscribers/import",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
        Assert.Contains("\"addresses\":[\"ada@example.com\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal(2, result.Added);
    }

    [Fact]
    public async Task Subscribers_RemoveAsync_EscapesTheAddress()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"deleted":true}"""),
        };
        using var client = TestClient.Create(handler);

        var result = await client.Subscribers.RemoveAsync("product-news", "ada+news@example.com");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        // The plus has to survive the path, or a different address is removed.
        Assert.EndsWith(
            "/subscribers/ada%2Bnews%40example.com",
            handler.LastRequest!.RequestUri!.AbsoluteUri,
            StringComparison.Ordinal);
        Assert.True(result.Deleted);
    }

    [Fact]
    public async Task Subscribers_ComplaintAsync_Unsubscribes()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success(
                """{"subscriber":{"id":1,"address":"ada@example.com","status":"unsubscribed"}}"""),
        };
        using var client = TestClient.Create(handler);

        var subscriber = await client.Subscribers.ComplaintAsync("product-news", "ada@example.com");

        Assert.EndsWith("/complaint", handler.LastRequest!.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal("unsubscribed", subscriber.Status);
    }

    [Fact]
    public async Task Layouts_ListAsync_HandlesAMissingTextWrapper()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {"layouts":[{"id":1,"uuid":"l-1","name":"Default","permalink":"default",
                 "html_wrapper":"<html>{{{ content }}}</html>","text_wrapper":null}]}
                """),
        };
        using var client = TestClient.Create(handler);

        var layouts = await client.Layouts.ListAsync();

        var layout = Assert.Single(layouts);
        Assert.Equal("default", layout.Permalink);
        // A layout created with only an HTML wrapper comes back with an
        // explicit null for the text one.
        Assert.Null(layout.TextWrapper);
    }

    [Fact]
    public async Task Layouts_CreateAsync_SendsTheWrapper()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success(
                """{"layout":{"id":1,"name":"Default","permalink":"default"}}"""),
        };
        using var client = TestClient.Create(handler);

        var layout = await client.Layouts.CreateAsync(new CreateLayoutRequest
        {
            Name = "Default",
            Permalink = "default",
            HtmlWrapper = "<html>{{{ content }}}</html>",
        });

        Assert.Contains("html_wrapper", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal("Default", layout.Name);
    }

    [Fact]
    public async Task Layouts_CreateAsync_WithoutThePlaceholderIsRefused()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.UnprocessableEntity,
            ResponseBody = TestEnvelope.Error(
                "ValidationError", "html_wrapper must contain the content placeholder"),
        };
        using var client = TestClient.Create(handler);

        var error = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Layouts.CreateAsync(new CreateLayoutRequest
            {
                Name = "Broken",
                HtmlWrapper = "<html></html>",
            }));
        Assert.Equal("ValidationError", error.ErrorCode);
    }

    [Fact]
    public async Task Layouts_UploadLogoAsync_ReadsTheUrlKey()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success(
                """{"url":"https://app.camelmailer.com/assets/layouts/l-1/logo"}"""),
        };
        using var client = TestClient.Create(handler);

        var logo = await client.Layouts.UploadLogoAsync(
            "default", "data:image/png;base64,iVBORw0KGgo=");

        Assert.Contains("\"data_url\"", handler.LastRequestBody, StringComparison.Ordinal);
        // The endpoint answers with "url", not "logo_url".
        Assert.Equal("https://app.camelmailer.com/assets/layouts/l-1/logo", logo.Url);
    }

    [Fact]
    public async Task Layouts_DeleteAsync()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"deleted":true}"""),
        };
        using var client = TestClient.Create(handler);

        var result = await client.Layouts.DeleteAsync("default");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.True(result.Deleted);
    }
}
