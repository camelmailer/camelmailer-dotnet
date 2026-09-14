namespace CamelMailer;

/// <summary>
/// The content of a broadcast to a stream. Give either a subject with a body, or
/// a template permalink with an optional model.
/// </summary>
public sealed record SendToStreamRequest
{
    /// <summary>The sender address.</summary>
    public required string From { get; init; }

    /// <summary>The message subject.</summary>
    public string? Subject { get; init; }

    /// <summary>The HTML part.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text part.</summary>
    public string? TextBody { get; init; }

    /// <summary>A stored template to render instead of the bodies above.</summary>
    public string? Template { get; init; }

    /// <summary>The values for the template's <c>{{ variables }}</c>.</summary>
    public IReadOnlyDictionary<string, object?>? TemplateModel { get; init; }
}

/// <summary>How a broadcast to a stream was split.</summary>
public sealed record StreamSendResult
{
    /// <summary>Recipients queued.</summary>
    public long Queued { get; init; }

    /// <summary>Recipients past the per-request cap of 1000.</summary>
    public long Skipped { get; init; }
}
