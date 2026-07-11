namespace CamelMailer;

/// <summary>Send email and inspect stored messages.</summary>
public interface IEmailsResource
{
    /// <summary>Sends an email. One message is queued per recipient.</summary>
    Task<SendEmailResponse> SendAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sends many emails in one call. Returns one result per entry; a failed entry does not fail the batch.</summary>
    Task<IReadOnlyList<BatchSendResult>> SendBatchAsync(
        IEnumerable<SendEmailRequest> requests,
        CancellationToken cancellationToken = default);

    /// <summary>Sends an email rendered from a stored template.</summary>
    Task<SendEmailResponse> SendWithTemplateAsync(
        SendTemplateEmailRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sends many template emails in one call. Returns one result per entry.</summary>
    Task<IReadOnlyList<BatchSendResult>> SendWithTemplateBatchAsync(
        IEnumerable<SendTemplateEmailRequest> requests,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a message and its delivery attempts.</summary>
    Task<MessageDetails> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Lists messages, optionally filtered by scope, status, tag, text query or stream.</summary>
    Task<MessageList> ListAsync(
        ListEmailsOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches the delivery attempts of a message.</summary>
    Task<IReadOnlyList<Delivery>> GetDeliveriesAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches the open events of a message.</summary>
    Task<IReadOnlyList<MessageEvent>> GetOpensAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches the click events of a message.</summary>
    Task<IReadOnlyList<MessageEvent>> GetClicksAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches the raw RFC 5322 source of a message.</summary>
    Task<byte[]> GetRawAsync(long id, CancellationToken cancellationToken = default);
}

internal sealed class EmailsResource : IEmailsResource
{
    private const string BasePath = "/api/v2/server/messages";

    private readonly ApiConnection _connection;

    internal EmailsResource(ApiConnection connection) => _connection = connection;

    public Task<SendEmailResponse> SendAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _connection.PostAsync<SendEmailResponse>(BasePath, request, cancellationToken);
    }

    public async Task<IReadOnlyList<BatchSendResult>> SendBatchAsync(
        IEnumerable<SendEmailRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var data = await _connection
            .PostAsync<BatchData>($"{BasePath}/batch", AsBatchBody(requests), cancellationToken)
            .ConfigureAwait(false);
        return data.Messages;
    }

    public Task<SendEmailResponse> SendWithTemplateAsync(
        SendTemplateEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _connection.PostAsync<SendEmailResponse>(
            $"{BasePath}/with_template", request, cancellationToken);
    }

    public async Task<IReadOnlyList<BatchSendResult>> SendWithTemplateBatchAsync(
        IEnumerable<SendTemplateEmailRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var data = await _connection
            .PostAsync<BatchData>(
                $"{BasePath}/with_template/batch", AsBatchBody(requests), cancellationToken)
            .ConfigureAwait(false);
        return data.Messages;
    }

    public Task<MessageDetails> GetAsync(long id, CancellationToken cancellationToken = default)
        => _connection.GetAsync<MessageDetails>($"{BasePath}/{id}", null, cancellationToken);

    public Task<MessageList> ListAsync(
        ListEmailsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var query = options is null
            ? null
            : new ApiQuery()
                .Add("page", options.Page)
                .Add("per_page", options.PerPage)
                .Add("scope", options.Scope)
                .Add("status", options.Status)
                .Add("tag", options.Tag)
                .Add("query", options.Query)
                .Add("stream", options.Stream)
                .Pairs;
        return _connection.GetAsync<MessageList>(BasePath, query, cancellationToken);
    }

    public async Task<IReadOnlyList<Delivery>> GetDeliveriesAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<DeliveriesData>($"{BasePath}/{id}/deliveries", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Deliveries;
    }

    public async Task<IReadOnlyList<MessageEvent>> GetOpensAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<OpensData>($"{BasePath}/{id}/opens", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Opens;
    }

    public async Task<IReadOnlyList<MessageEvent>> GetClicksAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<ClicksData>($"{BasePath}/{id}/clicks", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Clicks;
    }

    public async Task<byte[]> GetRawAsync(long id, CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<RawData>($"{BasePath}/{id}/raw", null, cancellationToken)
            .ConfigureAwait(false);
        return data.RawMessage is null ? [] : Convert.FromBase64String(data.RawMessage);
    }

    /// <summary>
    /// The batch endpoints expect a bare JSON array. Elements are boxed as
    /// <see cref="object" /> so each entry serialises with its runtime type.
    /// </summary>
    private static List<object> AsBatchBody(IEnumerable<SendEmailRequest> requests)
        => [.. requests];

    internal sealed record BatchData
    {
        public IReadOnlyList<BatchSendResult> Messages { get; init; } = [];
    }

    internal sealed record DeliveriesData
    {
        public IReadOnlyList<Delivery> Deliveries { get; init; } = [];
    }

    internal sealed record OpensData
    {
        public IReadOnlyList<MessageEvent> Opens { get; init; } = [];
    }

    internal sealed record ClicksData
    {
        public IReadOnlyList<MessageEvent> Clicks { get; init; } = [];
    }

    internal sealed record RawData
    {
        public string? RawMessage { get; init; }
    }
}
