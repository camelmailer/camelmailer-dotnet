namespace CamelMailer;

/// <summary>A message stream — a named channel messages are sent through.</summary>
public sealed record MessageStream
{
    /// <summary>The numeric stream id.</summary>
    public long Id { get; init; }

    /// <summary>The stable UUID of the stream.</summary>
    public string? Uuid { get; init; }

    /// <summary>The display name.</summary>
    public string? Name { get; init; }

    /// <summary>The permalink used to address the stream in the API.</summary>
    public string? Permalink { get; init; }

    /// <summary><c>transactional</c> or <c>broadcast</c>.</summary>
    public string? StreamType { get; init; }

    /// <summary>Whether the stream has been archived.</summary>
    public bool Archived { get; init; }
}

/// <summary>Fields for creating a message stream.</summary>
public sealed record CreateStreamRequest
{
    /// <summary>The display name.</summary>
    public required string Name { get; init; }

    /// <summary><c>transactional</c> (default) or <c>broadcast</c>.</summary>
    public string? StreamType { get; init; }
}

/// <summary>Fields for updating a message stream. Unset fields are left unchanged.</summary>
public sealed record UpdateStreamRequest
{
    /// <summary>A new display name.</summary>
    public string? Name { get; init; }

    /// <summary>A new stream type: <c>transactional</c> or <c>broadcast</c>.</summary>
    public string? StreamType { get; init; }
}
