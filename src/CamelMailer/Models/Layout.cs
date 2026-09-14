namespace CamelMailer;

/// <summary>
/// A template layout: the wrapper shared by every template that uses it, so
/// header, footer and styling live in one place instead of in each template.
/// </summary>
public sealed record Layout
{
    /// <summary>The numeric layout id.</summary>
    public long Id { get; init; }

    /// <summary>The stable UUID.</summary>
    public string? Uuid { get; init; }

    /// <summary>The display name.</summary>
    public string? Name { get; init; }

    /// <summary>The permalink used in API paths and by templates.</summary>
    public string? Permalink { get; init; }

    /// <summary>
    /// The wrapper for the HTML body; it embeds the body with
    /// <c>{{{ content }}}</c>. Required when creating a layout.
    /// </summary>
    public string? HtmlWrapper { get; init; }

    /// <summary>
    /// The wrapper for the plain-text body, or <c>null</c> when the layout has
    /// none.
    /// </summary>
    public string? TextWrapper { get; init; }
}

/// <summary>Fields for creating a layout.</summary>
public sealed record CreateLayoutRequest
{
    /// <summary>The display name.</summary>
    public required string Name { get; init; }

    /// <summary>An explicit permalink; derived from the name when unset.</summary>
    public string? Permalink { get; init; }

    /// <summary>
    /// The wrapper for the HTML body. It has to embed the body with
    /// <c>{{{ content }}}</c>; anything else is refused with
    /// <c>ValidationError</c>.
    /// </summary>
    public string? HtmlWrapper { get; init; }

    /// <summary>The wrapper for the plain-text body.</summary>
    public string? TextWrapper { get; init; }
}

/// <summary>Fields for updating a layout. Unset fields are left unchanged.</summary>
public sealed record UpdateLayoutRequest
{
    /// <summary>A new display name.</summary>
    public string? Name { get; init; }

    /// <summary>A new wrapper for the HTML body.</summary>
    public string? HtmlWrapper { get; init; }

    /// <summary>A new wrapper for the plain-text body.</summary>
    public string? TextWrapper { get; init; }
}

/// <summary>Where an uploaded logo now lives.</summary>
public sealed record LayoutLogo
{
    /// <summary>
    /// The absolute URL to reference from the wrapper. Served without
    /// authentication, because mail clients fetch it without a session.
    /// </summary>
    public string? Url { get; init; }
}
