using System.Text.Json.Serialization;

namespace CamelMailer;

/// <summary>The result of queueing one send request.</summary>
public sealed record SendEmailResponse
{
    /// <summary>The id of the queued message (shared across its recipients).</summary>
    public long? MessageId { get; init; }

    /// <summary>One entry per recipient the message was queued for.</summary>
    public IReadOnlyList<SendRecipientResult> Recipients { get; init; } = [];
}

/// <summary>The queue result for a single recipient.</summary>
public sealed record SendRecipientResult
{
    /// <summary>The recipient address.</summary>
    public string? RcptTo { get; init; }

    /// <summary>The id of the queued message.</summary>
    public long? MessageId { get; init; }

    /// <summary>The public token of the queued message.</summary>
    public string? Token { get; init; }

    /// <summary>The queue status, e.g. <c>queued</c>.</summary>
    public string? Status { get; init; }
}

/// <summary>An error object from the API envelope.</summary>
public sealed record ApiError
{
    /// <summary>The stable error code, e.g. <c>ValidationError</c>.</summary>
    public string? Code { get; init; }

    /// <summary>The human-readable error message.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// The per-entry outcome of a batch send. Batch requests return one result
/// per entry instead of failing the whole batch.
/// </summary>
public sealed record BatchSendResult
{
    /// <summary><c>success</c> or <c>error</c>.</summary>
    public string? Status { get; init; }

    /// <summary>The queue result when this entry succeeded.</summary>
    public SendEmailResponse? Data { get; init; }

    /// <summary>The error when this entry failed.</summary>
    public ApiError? Error { get; init; }

    /// <summary>Whether this entry was queued successfully.</summary>
    [JsonIgnore]
    public bool IsSuccess => string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase);
}
