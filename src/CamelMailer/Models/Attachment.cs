namespace CamelMailer;

/// <summary>A file attached to an outgoing email.</summary>
public sealed record Attachment
{
    /// <summary>The file name shown to the recipient, e.g. <c>invoice.pdf</c>.</summary>
    public required string Name { get; init; }

    /// <summary>The MIME type, e.g. <c>application/pdf</c>.</summary>
    public required string ContentType { get; init; }

    /// <summary>The Base64-encoded file content.</summary>
    public required string DataBase64 { get; init; }

    /// <summary>Builds an attachment from raw bytes, Base64-encoding them for transport.</summary>
    public static Attachment FromBytes(string name, string contentType, byte[] content) => new()
    {
        Name = name,
        ContentType = contentType,
        DataBase64 = Convert.ToBase64String(content),
    };
}
