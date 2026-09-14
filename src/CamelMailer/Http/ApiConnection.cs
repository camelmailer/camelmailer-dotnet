using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CamelMailer;

/// <summary>
/// The shared HTTP layer: builds authenticated requests, serialises bodies
/// as snake_case JSON and unwraps the API's success/error envelope.
/// </summary>
internal sealed class ApiConnection
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    internal ApiConnection(HttpClient httpClient, string baseUrl, string apiKey)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
    }

    internal Task<T> GetAsync<T>(
        string path,
        IReadOnlyCollection<KeyValuePair<string, string>>? query,
        CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Get, path, query, body: null, cancellationToken);

    internal Task<T> PostAsync<T>(string path, object? body, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Post, path, query: null, body, cancellationToken);

    /// <summary>
    /// POSTs with an <c>Idempotency-Key</c>. The key is a header rather than a
    /// body field, because the body is what the server hashes to recognise the
    /// same request.
    /// </summary>
    internal Task<T> PostAsync<T>(
        string path,
        object? body,
        string? idempotencyKey,
        CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Post, path, query: null, body, cancellationToken, idempotencyKey);

    internal Task<T> DeleteAsync<T>(string path, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Delete, path, query: null, body: null, cancellationToken);

    internal Task<T> PatchAsync<T>(string path, object? body, CancellationToken cancellationToken)
        => SendAsync<T>(HttpMethod.Patch, path, query: null, body, cancellationToken);

    /// <summary>Formats a timestamp the way the API expects query parameters: UTC, second precision.</summary>
    internal static string FormatUtc(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        IReadOnlyCollection<KeyValuePair<string, string>>? query,
        object? body,
        CancellationToken cancellationToken,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(method, BuildUrl(path, query));
        request.Headers.Add("X-Server-API-Key", _apiKey);
        request.Headers.Accept.ParseAdd("application/json");
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, body.GetType(), JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            throw new CamelMailerNetworkException(
                "The request to the CamelMailer API timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new CamelMailerNetworkException(
                $"The request to the CamelMailer API failed: {exception.Message}", exception);
        }

        using (response)
        {
            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            return ParseEnvelope<T>(responseBody, response.StatusCode);
        }
    }

    private string BuildUrl(string path, IReadOnlyCollection<KeyValuePair<string, string>>? query)
    {
        var url = _baseUrl + path;
        if (query is { Count: > 0 })
        {
            url += "?" + string.Join(
                "&",
                query.Select(pair =>
                    $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        }

        return url;
    }

    private static T ParseEnvelope<T>(string body, HttpStatusCode statusCode)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            throw new CamelMailerException(
                $"The CamelMailer API returned an unexpected non-JSON response (HTTP {(int)statusCode}).",
                errorCode: null,
                statusCode,
                exception);
        }

        using (document)
        {
            var root = document.RootElement;
            var status = root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("status", out var statusElement)
                && statusElement.ValueKind == JsonValueKind.String
                    ? statusElement.GetString()
                    : null;

            if (status == "error")
            {
                string? code = null;
                string? message = null;
                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("code", out var codeElement)
                        && codeElement.ValueKind == JsonValueKind.String)
                    {
                        code = codeElement.GetString();
                    }

                    if (error.TryGetProperty("message", out var messageElement)
                        && messageElement.ValueKind == JsonValueKind.String)
                    {
                        message = messageElement.GetString();
                    }
                }

                throw new CamelMailerException(
                    message ?? $"The CamelMailer API returned an error (HTTP {(int)statusCode}).",
                    code,
                    statusCode);
            }

            if (status == "success"
                && root.TryGetProperty("data", out var data)
                && data.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                try
                {
                    return data.Deserialize<T>(JsonOptions)!;
                }
                catch (JsonException exception)
                {
                    throw new CamelMailerException(
                        "The CamelMailer API returned a response the SDK could not parse.",
                        errorCode: null,
                        statusCode,
                        exception);
                }
            }

            throw new CamelMailerException(
                $"The CamelMailer API returned an unexpected response (HTTP {(int)statusCode}).",
                errorCode: null,
                statusCode);
        }
    }
}

/// <summary>Builds query-string pairs, skipping unset values.</summary>
internal sealed class ApiQuery
{
    private readonly List<KeyValuePair<string, string>> _pairs = [];

    internal IReadOnlyCollection<KeyValuePair<string, string>> Pairs => _pairs;

    internal ApiQuery Add(string name, string? value)
    {
        if (value is not null)
        {
            _pairs.Add(new KeyValuePair<string, string>(name, value));
        }

        return this;
    }

    internal ApiQuery Add(string name, int? value)
        => Add(name, value?.ToString(CultureInfo.InvariantCulture));

    internal ApiQuery Add(string name, DateTimeOffset? value)
        => Add(name, value is null ? null : ApiConnection.FormatUtc(value.Value));
}
