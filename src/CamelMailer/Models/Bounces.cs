namespace CamelMailer;

/// <summary>One page of bounce messages.</summary>
public sealed record BounceList
{
    /// <summary>The bounce messages on this page.</summary>
    public IReadOnlyList<Message> Bounces { get; init; } = [];

    /// <summary>Paging information.</summary>
    public Pagination? Pagination { get; init; }
}

/// <summary>Filters for <see cref="IBouncesResource.ListAsync" />.</summary>
public sealed record ListBouncesOptions
{
    /// <summary>The 1-based page to fetch.</summary>
    public int? Page { get; init; }

    /// <summary>Results per page (max 100).</summary>
    public int? PerPage { get; init; }

    /// <summary>Restrict to bounces of messages sent with this tag.</summary>
    public string? Tag { get; init; }
}
