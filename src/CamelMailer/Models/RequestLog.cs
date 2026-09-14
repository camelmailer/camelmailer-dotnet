namespace CamelMailer;

/// <summary>One logged API request.</summary>
public sealed record ApiRequestLog
{
    /// <summary>The numeric log id.</summary>
    public long Id { get; init; }

    /// <summary>The HTTP method.</summary>
    public string? Method { get; init; }

    /// <summary>The request path.</summary>
    public string? Path { get; init; }

    /// <summary>The status the API answered with.</summary>
    public int StatusCode { get; init; }

    /// <summary>How long the request took, in milliseconds.</summary>
    public long DurationMs { get; init; }

    /// <summary>The client's User-Agent header.</summary>
    public string? UserAgent { get; init; }

    /// <summary>When the request arrived.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>One page of logged requests.</summary>
public sealed record RequestLogList
{
    /// <summary>The page of logged requests.</summary>
    public IReadOnlyList<ApiRequestLog> Requests { get; init; } = [];

    /// <summary>The page window.</summary>
    public Pagination? Pagination { get; init; }
}

/// <summary>One tag with how often the server's recent messages used it.</summary>
public sealed record TagCount
{
    /// <summary>The tag itself.</summary>
    public string? Tag { get; init; }

    /// <summary>How many messages carry it.</summary>
    public long Count { get; init; }
}
