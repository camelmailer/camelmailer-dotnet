namespace CamelMailer;

/// <summary>
/// The CamelMailer client. Authenticates with a server API key and talks to
/// the CamelMailer cloud by default; set <see cref="CamelMailerOptions.BaseUrl" />
/// for self-hosted instances.
/// </summary>
public sealed class CamelMailerClient : ICamelMailerClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates a client for the CamelMailer cloud.</summary>
    /// <param name="apiKey">The server API key (<c>cm_…</c>).</param>
    public CamelMailerClient(string apiKey)
        : this(new CamelMailerOptions { ApiKey = apiKey })
    {
    }

    /// <summary>Creates a client for a self-hosted CamelMailer instance.</summary>
    /// <param name="apiKey">The server API key (<c>cm_…</c>).</param>
    /// <param name="baseUrl">The base URL of the instance, e.g. <c>https://mail.example.com</c>.</param>
    public CamelMailerClient(string apiKey, string baseUrl)
        : this(new CamelMailerOptions { ApiKey = apiKey, BaseUrl = baseUrl })
    {
    }

    /// <summary>Creates a client from options, using an internally managed <see cref="HttpClient" />.</summary>
    /// <param name="options">The client configuration.</param>
    public CamelMailerClient(CamelMailerOptions options)
        : this(options, new HttpClient(), ownsHttpClient: true)
    {
    }

    /// <summary>
    /// Creates a client that sends through the given <see cref="HttpClient" />.
    /// The caller keeps ownership of the <paramref name="httpClient" />.
    /// </summary>
    /// <param name="options">The client configuration.</param>
    /// <param name="httpClient">The HTTP client used for all requests.</param>
    public CamelMailerClient(CamelMailerOptions options, HttpClient httpClient)
        : this(options, httpClient, ownsHttpClient: false)
    {
    }

    private CamelMailerClient(CamelMailerOptions options, HttpClient httpClient, bool ownsHttpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException(
                "A CamelMailer server API key is required. Pass it to the constructor "
                + $"or set {nameof(CamelMailerOptions)}.{nameof(CamelMailerOptions.ApiKey)}.",
                nameof(options));
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                $"'{options.BaseUrl}' is not a valid base URL; an absolute http(s) URL is required.",
                nameof(options));
        }

        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;

        var connection = new ApiConnection(httpClient, options.BaseUrl, options.ApiKey);
        Emails = new EmailsResource(connection);
        Templates = new TemplatesResource(connection);
        Streams = new StreamsResource(connection);
        Stats = new StatsResource(connection);
        Bounces = new BouncesResource(connection);
        Dmarc = new DmarcResource(connection);
        Campaigns = new CampaignsResource(connection);
        Subscribers = new SubscribersResource(connection);
        Layouts = new LayoutsResource(connection);
        Inbound = new InboundResource(connection);
        Logs = new LogsResource(connection);
    }

    /// <inheritdoc />
    public IEmailsResource Emails { get; }

    /// <inheritdoc />
    public ITemplatesResource Templates { get; }

    /// <inheritdoc />
    public IStreamsResource Streams { get; }

    /// <inheritdoc />
    public IStatsResource Stats { get; }

    /// <inheritdoc />
    public IBouncesResource Bounces { get; }

    /// <inheritdoc />
    public IDmarcResource Dmarc { get; }

    /// <inheritdoc />
    public ICampaignsResource Campaigns { get; }

    /// <inheritdoc />
    public ISubscribersResource Subscribers { get; }

    /// <inheritdoc />
    public ILayoutsResource Layouts { get; }

    /// <inheritdoc />
    public IInboundResource Inbound { get; }

    /// <inheritdoc />
    public ILogsResource Logs { get; }

    /// <summary>Disposes the internally managed <see cref="HttpClient" />, if any.</summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
