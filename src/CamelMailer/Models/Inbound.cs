namespace CamelMailer;

/// <summary>One page of inbound and held messages.</summary>
public sealed record InboundList
{
    /// <summary>
    /// The page of messages. The API names this key <c>inbound</c>, not
    /// <c>messages</c>.
    /// </summary>
    public IReadOnlyList<Message> Inbound { get; init; } = [];

    /// <summary>The page window.</summary>
    public Pagination? Pagination { get; init; }
}

/// <summary>Filters for listing inbound and held messages. All optional.</summary>
public sealed record ListInboundOptions
{
    /// <summary>Restrict to one delivery status, e.g. <c>held</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Restrict to one message stream, by permalink.</summary>
    public string? Stream { get; init; }

    /// <summary>Substring match on subject and addresses.</summary>
    public string? Query { get; init; }

    /// <summary>The 1-based page number.</summary>
    public int? Page { get; init; }

    /// <summary>The page size, capped at 100.</summary>
    public int? PerPage { get; init; }
}

/// <summary>What a retry or bypass did.</summary>
public sealed record RequeueResult
{
    /// <summary>
    /// Whether the message went back on the delivery queue. The API names this
    /// field <c>requeued</c>.
    /// </summary>
    public bool Requeued { get; init; }

    /// <summary>The message as it now stands.</summary>
    public Message? Message { get; init; }
}
