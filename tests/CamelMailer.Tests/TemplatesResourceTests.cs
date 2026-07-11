using System.Net;
using System.Text.Json;

namespace CamelMailer.Tests;

public class TemplatesResourceTests
{
    private const string TemplateJson = """
        {
          "template": {
            "id": 3, "uuid": "u-3", "name": "Welcome", "permalink": "welcome",
            "subject": "Welcome, {{ name }}!", "html_body": "<p>Hi {{ name }}</p>",
            "text_body": "Hi {{ name }}", "archived": false
          }
        }
        """;

    [Fact]
    public async Task ListAsync_ReturnsTemplates()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "templates": [
                    { "id": 3, "uuid": "u-3", "name": "Welcome", "permalink": "welcome",
                      "subject": "Welcome!", "html_body": null, "text_body": "Hi", "archived": false }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var templates = await client.Templates.ListAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/templates",
            handler.LastRequest!.RequestUri!.ToString());
        var template = Assert.Single(templates);
        Assert.Equal("welcome", template.Permalink);
        Assert.Null(template.HtmlBody);
    }

    [Fact]
    public async Task CreateAsync_PostsSnakeCaseBody()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success(TemplateJson),
        };
        using var client = TestClient.Create(handler);

        var template = await client.Templates.CreateAsync(new CreateTemplateRequest
        {
            Name = "Welcome",
            Subject = "Welcome, {{ name }}!",
            HtmlBody = "<p>Hi {{ name }}</p>",
            TextBody = "Hi {{ name }}",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Welcome", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("<p>Hi {{ name }}</p>", body.RootElement.GetProperty("html_body").GetString());
        Assert.Equal("Welcome, {{ name }}!", template.Subject);
        Assert.False(template.Archived);
    }

    [Fact]
    public async Task GetAsync_UsesPermalink()
    {
        var handler = new MockHttpMessageHandler { ResponseBody = TestEnvelope.Success(TemplateJson) };
        using var client = TestClient.Create(handler);

        var template = await client.Templates.GetAsync("welcome");

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/templates/welcome",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(3, template.Id);
    }

    [Fact]
    public async Task UpdateAsync_SendsPatch()
    {
        var handler = new MockHttpMessageHandler { ResponseBody = TestEnvelope.Success(TemplateJson) };
        using var client = TestClient.Create(handler);

        await client.Templates.UpdateAsync("welcome", new UpdateTemplateRequest
        {
            Subject = "Hello {{ name }}",
        });

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/templates/welcome",
            handler.LastRequest.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Hello {{ name }}", body.RootElement.GetProperty("subject").GetString());
        Assert.False(body.RootElement.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task ArchiveAsync_PostsToArchive()
    {
        var handler = new MockHttpMessageHandler { ResponseBody = TestEnvelope.Success(TemplateJson) };
        using var client = TestClient.Create(handler);

        await client.Templates.ArchiveAsync("welcome");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/templates/welcome/archive",
            handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task RenderAsync_PostsModel_AndReturnsRenderedFields()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "rendered": {
                    "subject": "Welcome, Ada!",
                    "html_body": "<p>Hi Ada</p>",
                    "text_body": "Hi Ada"
                  }
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var rendered = await client.Templates.RenderAsync(
            "welcome",
            new Dictionary<string, object?> { ["name"] = "Ada" });

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/templates/welcome/render",
            handler.LastRequest!.RequestUri!.ToString());
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Ada", body.RootElement.GetProperty("template_model").GetProperty("name").GetString());
        Assert.Equal("Welcome, Ada!", rendered.Subject);
        Assert.Equal("<p>Hi Ada</p>", rendered.HtmlBody);
        Assert.Equal("Hi Ada", rendered.TextBody);
    }

    [Fact]
    public async Task PermalinksAreUrlEscaped()
    {
        var handler = new MockHttpMessageHandler { ResponseBody = TestEnvelope.Success(TemplateJson) };
        using var client = TestClient.Create(handler);

        await client.Templates.GetAsync("wel come/x");

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/templates/wel%20come%2Fx",
            handler.LastRequest!.RequestUri!.AbsoluteUri);
    }
}
