namespace CamelMailer;

/// <summary>One address on a broadcast stream.</summary>
public sealed record Subscriber
{
    /// <summary>The numeric subscriber id.</summary>
    public long Id { get; init; }

    /// <summary>The email address.</summary>
    public string? Address { get; init; }

    /// <summary>The optional display name.</summary>
    public string? Name { get; init; }

    /// <summary><c>subscribed</c> or <c>unsubscribed</c>.</summary>
    public string? Status { get; init; }

    /// <summary>When the subscription row was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Fields for adding or updating a subscriber. Upserts by address.</summary>
public sealed record AddSubscriberRequest
{
    /// <summary>The email address.</summary>
    public required string Address { get; init; }

    /// <summary>The optional display name.</summary>
    public string? Name { get; init; }

    /// <summary><c>subscribed</c> (default) or <c>unsubscribed</c>.</summary>
    public string? Status { get; init; }
}

/// <summary>
/// What an import wrote. Blanks and duplicates within the request are skipped,
/// so <see cref="Added"/> can be lower than the number of addresses passed.
/// </summary>
public sealed record ImportSubscribersResult
{
    /// <summary>Subscriptions written.</summary>
    public long Added { get; init; }

    /// <summary>The stream's subscriber count after the import.</summary>
    public long Total { get; init; }
}

/// <summary>What a delete removed.</summary>
public sealed record DeleteResult
{
    /// <summary>Whether the row was removed.</summary>
    public bool Deleted { get; init; }
}
