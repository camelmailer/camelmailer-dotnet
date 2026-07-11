using System.Net;
using System.Text.Json;

namespace CamelMailer.Tests;

public class StreamsResourceTests
{
    private const string StreamJson = """
        {
          "stream": {
            "id": 2, "uuid": "u-2", "name": "Broadcasts", "permalink": "broadcasts",
            "stream_type": "broadcast", "archived": false
          }
        }
        """;

    [Fact]
    public async Task ListAsync_ReturnsStreams()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "streams": [
                    { "id": 1, "uuid": "u-1", "name": "Default", "permalink": "default",
                      "stream_type": "transactional", "archived": false }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var streams = await client.Streams.ListAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/streams",
            handler.LastRequest!.RequestUri!.ToString());
        var stream = Assert.Single(streams);
        Assert.Equal("default", stream.Permalink);
        Assert.Equal("transactional", stream.StreamType);
    }

    [Fact]
    public async Task CreateAsync_PostsNameAndType()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Created,
            ResponseBody = TestEnvelope.Success(StreamJson),
        };
        using var client = TestClient.Create(handler);

        var stream = await client.Streams.CreateAsync(new CreateStreamRequest
        {
            Name = "Broadcasts",
            StreamType = "broadcast",
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Broadcasts", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("broadcast", body.RootElement.GetProperty("stream_type").GetString());
        Assert.Equal("broadcasts", stream.Permalink);
    }

    [Fact]
    public async Task GetAsync_UsesPermalink()
    {
        var handler = new MockHttpMessageHandler { ResponseBody = TestEnvelope.Success(StreamJson) };
        using var client = TestClient.Create(handler);

        var stream = await client.Streams.GetAsync("broadcasts");

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/streams/broadcasts",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(2, stream.Id);
    }

    [Fact]
    public async Task UpdateAsync_SendsPatchWithOnlySetFields()
    {
        var handler = new MockHttpMessageHandler { ResponseBody = TestEnvelope.Success(StreamJson) };
        using var client = TestClient.Create(handler);

        await client.Streams.UpdateAsync("broadcasts", new UpdateStreamRequest { Name = "News" });

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("News", body.RootElement.GetProperty("name").GetString());
        Assert.False(body.RootElement.TryGetProperty("stream_type", out _));
    }

    [Fact]
    public async Task ArchiveAsync_PostsToArchive()
    {
        var handler = new MockHttpMessageHandler { ResponseBody = TestEnvelope.Success(StreamJson) };
        using var client = TestClient.Create(handler);

        await client.Streams.ArchiveAsync("broadcasts");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/streams/broadcasts/archive",
            handler.LastRequest.RequestUri!.ToString());
    }
}
