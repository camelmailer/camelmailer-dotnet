namespace CamelMailer;

/// <summary>An outgoing email. The <c>from</c> domain must be a verified sending domain of the server.</summary>
public record SendEmailRequest
{
    /// <summary>The sender address.</summary>
    public required EmailAddress From { get; init; }

    /// <summary>The recipient addresses. One message is queued per recipient.</summary>
    public required IReadOnlyList<EmailAddress> To { get; init; }

    /// <summary>Carbon-copy recipients.</summary>
    public IReadOnlyList<EmailAddress>? Cc { get; init; }

    /// <summary>Blind-carbon-copy recipients.</summary>
    public IReadOnlyList<EmailAddress>? Bcc { get; init; }

    /// <summary>Reply-To addresses.</summary>
    public IReadOnlyList<EmailAddress>? ReplyTo { get; init; }

    /// <summary>The subject line.</summary>
    public string? Subject { get; init; }

    /// <summary>The HTML body.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text body.</summary>
    public string? TextBody { get; init; }

    /// <summary>Extra message headers.</summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>File attachments.</summary>
    public IReadOnlyList<Attachment>? Attachments { get; init; }

    /// <summary>A free-form tag used for filtering and stats.</summary>
    public string? Tag { get; init; }

    /// <summary>Arbitrary metadata stored with the message.</summary>
    public IDictionary<string, object?>? Metadata { get; init; }

    /// <summary>The message-stream permalink. Defaults to the server's default stream.</summary>
    public string? Stream { get; init; }
}

/// <summary>
/// An outgoing email rendered from a stored template. Fields set directly
/// (e.g. <see cref="SendEmailRequest.Subject" />) override the rendered ones.
/// </summary>
public sealed record SendTemplateEmailRequest : SendEmailRequest
{
    /// <summary>The permalink of the stored template to render.</summary>
    public required string Template { get; init; }

    /// <summary>The model the template's <c>{{ variables }}</c> are rendered against.</summary>
    public IDictionary<string, object?>? TemplateModel { get; init; }
}
