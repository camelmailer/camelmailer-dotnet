namespace CamelMailer;

/// <summary>A stored email template rendered with Mustache-style <c>{{ variables }}</c>.</summary>
public sealed record Template
{
    /// <summary>The numeric template id.</summary>
    public long Id { get; init; }

    /// <summary>The stable UUID of the template.</summary>
    public string? Uuid { get; init; }

    /// <summary>The display name.</summary>
    public string? Name { get; init; }

    /// <summary>The permalink used to address the template in the API.</summary>
    public string? Permalink { get; init; }

    /// <summary>The subject line; may contain <c>{{ variables }}</c>.</summary>
    public string? Subject { get; init; }

    /// <summary>The HTML body; may contain <c>{{ variables }}</c>.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text body; may contain <c>{{ variables }}</c>.</summary>
    public string? TextBody { get; init; }

    /// <summary>Whether the template has been archived.</summary>
    public bool Archived { get; init; }
}

/// <summary>Fields for creating a template.</summary>
public sealed record CreateTemplateRequest
{
    /// <summary>The display name.</summary>
    public required string Name { get; init; }

    /// <summary>The subject line; may contain <c>{{ variables }}</c>.</summary>
    public string? Subject { get; init; }

    /// <summary>The HTML body; may contain <c>{{ variables }}</c>.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text body; may contain <c>{{ variables }}</c>.</summary>
    public string? TextBody { get; init; }
}

/// <summary>Fields for updating a template. Unset fields are left unchanged.</summary>
public sealed record UpdateTemplateRequest
{
    /// <summary>A new display name.</summary>
    public string? Name { get; init; }

    /// <summary>A new subject line.</summary>
    public string? Subject { get; init; }

    /// <summary>A new HTML body.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>A new plain-text body.</summary>
    public string? TextBody { get; init; }
}

/// <summary>The result of rendering a template against a model.</summary>
public sealed record RenderedTemplate
{
    /// <summary>The rendered subject line.</summary>
    public string? Subject { get; init; }

    /// <summary>The rendered HTML body.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The rendered plain-text body.</summary>
    public string? TextBody { get; init; }
}
