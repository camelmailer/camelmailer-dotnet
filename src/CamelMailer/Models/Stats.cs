namespace CamelMailer;

/// <summary>Message counters for a server, optionally limited to a time window.</summary>
public sealed record EmailStats
{
    /// <summary>Total number of messages.</summary>
    public long Total { get; init; }

    /// <summary>Incoming messages.</summary>
    public long Incoming { get; init; }

    /// <summary>Outgoing messages.</summary>
    public long Outgoing { get; init; }

    /// <summary>Messages delivered successfully.</summary>
    public long Sent { get; init; }

    /// <summary>Messages still queued.</summary>
    public long Pending { get; init; }

    /// <summary>Messages held for review.</summary>
    public long Held { get; init; }

    /// <summary>Messages that bounced.</summary>
    public long Bounced { get; init; }

    /// <summary>Messages that soft-failed (will be retried).</summary>
    public long SoftFail { get; init; }

    /// <summary>Messages that hard-failed.</summary>
    public long HardFail { get; init; }

    /// <summary>Total open events.</summary>
    public long Opens { get; init; }

    /// <summary>Total click events.</summary>
    public long Clicks { get; init; }

    /// <summary>Messages opened at least once.</summary>
    public long UniqueOpens { get; init; }

    /// <summary>Messages clicked at least once.</summary>
    public long UniqueClicks { get; init; }
}

/// <summary>The current delivery queue, broken down by recipient domain.</summary>
public sealed record DeliveryStats
{
    /// <summary>Messages currently queued for delivery.</summary>
    public long Queued { get; init; }

    /// <summary>Queue depth per recipient domain.</summary>
    public IReadOnlyList<DomainQueue> Domains { get; init; } = [];
}

/// <summary>The queue depth for one recipient domain.</summary>
public sealed record DomainQueue
{
    /// <summary>The recipient domain.</summary>
    public string? Domain { get; init; }

    /// <summary>Messages queued for this domain.</summary>
    public long Queued { get; init; }
}
