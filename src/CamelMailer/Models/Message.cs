using System.Text.Json;

namespace CamelMailer;

/// <summary>A message stored on the server — outgoing or incoming.</summary>
public sealed record Message
{
    /// <summary>The numeric message id.</summary>
    public long Id { get; init; }

    /// <summary>The public token of the message.</summary>
    public string? Token { get; init; }

    /// <summary><c>outgoing</c> or <c>incoming</c>.</summary>
    public string? Scope { get; init; }

    /// <summary>The recipient address.</summary>
    public string? RcptTo { get; init; }

    /// <summary>The envelope sender address.</summary>
    public string? MailFrom { get; init; }

    /// <summary>The subject line.</summary>
    public string? Subject { get; init; }

    /// <summary>The RFC 5322 <c>Message-ID</c> header value.</summary>
    public string? MessageId { get; init; }

    /// <summary>The free-form tag the message was sent with.</summary>
    public string? Tag { get; init; }

    /// <summary>The delivery status, e.g. <c>Sent</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Whether the message is a bounce.</summary>
    public bool Bounce { get; init; }

    /// <summary>The spam-check verdict, if any.</summary>
    public string? SpamStatus { get; init; }

    /// <summary>The spam score, if the message was checked.</summary>
    public double? SpamScore { get; init; }

    /// <summary>Whether the message is held for review.</summary>
    public bool Held { get; init; }

    /// <summary>Whether the message was flagged as a threat.</summary>
    public bool Threat { get; init; }

    /// <summary>The message size in bytes.</summary>
    public long? Size { get; init; }

    /// <summary>Arbitrary metadata stored with the message at send time.</summary>
    public IReadOnlyDictionary<string, JsonElement>? Metadata { get; init; }

    /// <summary>The id of the message stream the message belongs to.</summary>
    public long? StreamId { get; init; }

    /// <summary>Whether a hold was bypassed for this message.</summary>
    public bool Bypassed { get; init; }

    /// <summary>When the message was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>A message together with its delivery attempts.</summary>
public sealed record MessageDetails
{
    /// <summary>The message itself.</summary>
    public Message Message { get; init; } = new();

    /// <summary>The delivery attempts made for the message.</summary>
    public IReadOnlyList<Delivery> Deliveries { get; init; } = [];
}

/// <summary>One page of messages.</summary>
public sealed record MessageList
{
    /// <summary>The messages on this page.</summary>
    public IReadOnlyList<Message> Messages { get; init; } = [];

    /// <summary>Paging information.</summary>
    public Pagination? Pagination { get; init; }
}

/// <summary>A single delivery attempt of a message.</summary>
public sealed record Delivery
{
    /// <summary>The numeric delivery id.</summary>
    public long Id { get; init; }

    /// <summary>The outcome, e.g. <c>Sent</c> or <c>SoftFail</c>.</summary>
    public string? Status { get; init; }

    /// <summary>A human-readable description of the outcome.</summary>
    public string? Details { get; init; }

    /// <summary>The raw response of the receiving server.</summary>
    public string? Output { get; init; }

    /// <summary>Whether the connection used TLS.</summary>
    public bool SentWithSsl { get; init; }

    /// <summary>When the attempt was made.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>An open or click event recorded for a message.</summary>
public sealed record MessageEvent
{
    /// <summary>The IP address the event came from.</summary>
    public string? IpAddress { get; init; }

    /// <summary>The user agent that triggered the event.</summary>
    public string? UserAgent { get; init; }

    /// <summary>The clicked URL (clicks only; <c>null</c> for opens).</summary>
    public string? Url { get; init; }

    /// <summary>When the event happened.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Filters for <see cref="IEmailsResource.ListAsync" />.</summary>
public sealed record ListEmailsOptions
{
    /// <summary>The 1-based page to fetch.</summary>
    public int? Page { get; init; }

    /// <summary>Results per page (max 100).</summary>
    public int? PerPage { get; init; }

    /// <summary>Restrict to <c>outgoing</c> or <c>incoming</c> messages.</summary>
    public string? Scope { get; init; }

    /// <summary>Restrict to a delivery status, e.g. <c>Sent</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Restrict to messages sent with this tag.</summary>
    public string? Tag { get; init; }

    /// <summary>Substring match on subject and addresses.</summary>
    public string? Query { get; init; }

    /// <summary>Restrict to a message-stream permalink.</summary>
    public string? Stream { get; init; }
}

/// <summary>Paging information returned by list endpoints.</summary>
public sealed record Pagination
{
    /// <summary>The 1-based page number.</summary>
    public int Page { get; init; }

    /// <summary>Results per page.</summary>
    public int PerPage { get; init; }

    /// <summary>Total number of results.</summary>
    public long Total { get; init; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; init; }
}
