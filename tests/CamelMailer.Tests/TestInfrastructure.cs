using System.Net;
using System.Text;

namespace CamelMailer.Tests;

/// <summary>Records every request and replays a canned response. No network involved.</summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    public int CallCount { get; private set; }

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string ResponseBody { get; set; } = TestEnvelope.Success("{}");

    public string ResponseContentType { get; set; } = "application/json";

    public Exception? ExceptionToThrow { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequest = request;
        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(CancellationToken.None);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return new HttpResponseMessage(StatusCode)
        {
            RequestMessage = request,
            Content = new StringContent(ResponseBody, Encoding.UTF8, ResponseContentType),
        };
    }
}

public static class TestEnvelope
{
    public static string Success(string dataJson)
        => $$"""{"status":"success","time":0.002,"data":{{dataJson}}}""";

    public static string Error(string code, string message)
        => $$"""{"status":"error","time":0.002,"error":{"code":"{{code}}","message":"{{message}}" } }""";
}

public static class TestClient
{
    public const string ApiKey = "cm_test_key";

    public static CamelMailerClient Create(MockHttpMessageHandler handler, string? baseUrl = null)
    {
        var options = new CamelMailerOptions { ApiKey = ApiKey };
        if (baseUrl is not null)
        {
            options.BaseUrl = baseUrl;
        }

        return new CamelMailerClient(options, new HttpClient(handler));
    }
}

/// <summary>An integration test that is skipped unless the given environment variables are set.</summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute(params string[] additionalEnvironmentVariables)
    {
        var required = new List<string> { "CAMELMAILER_API_KEY" };
        required.AddRange(additionalEnvironmentVariables);
        var missing = required
            .Where(name => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
            .ToList();
        if (missing.Count > 0)
        {
            Skip = $"Integration test skipped; set {string.Join(", ", missing)} to run it.";
        }
    }
}
