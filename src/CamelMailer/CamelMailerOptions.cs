namespace CamelMailer;

/// <summary>Configuration for a <see cref="CamelMailerClient" />.</summary>
public sealed class CamelMailerOptions
{
    /// <summary>The base URL of the CamelMailer cloud, used when <see cref="BaseUrl" /> is not set.</summary>
    public const string DefaultBaseUrl = "https://app.camelmailer.com";

    /// <summary>The server API key (sent as the <c>X-Server-API-Key</c> header).</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The base URL of the CamelMailer instance. Defaults to the CamelMailer
    /// cloud; point this at your own host for self-hosted installations.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
