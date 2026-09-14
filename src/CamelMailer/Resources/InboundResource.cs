namespace CamelMailer;

/// <summary>
/// Inbound and held messages. Covers mail arriving through an inbound route as
/// well as outbound mail the spam filter put on hold, which is why a message
/// here can be either retried or released past the hold.
/// </summary>
public interface IInboundResource
{
    /// <summary>Lists inbound and held messages, newest first.</summary>
    Task<InboundList> ListAsync(
        ListInboundOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches one inbound message.</summary>
    Task<Message> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts a message back on the delivery queue, for instance after fixing the
    /// route it should have matched.
    /// </summary>
    Task<RequeueResult> RetryAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Releases a held message past the hold and delivers it.</summary>
    Task<RequeueResult> BypassAsync(long id, CancellationToken cancellationToken = default);
}

internal sealed class InboundResource : IInboundResource
{
    private const string BasePath = "/api/v2/server/inbound";

    private readonly ApiConnection _connection;

    internal InboundResource(ApiConnection connection) => _connection = connection;

    public Task<InboundList> ListAsync(
        ListInboundOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var query = options is null
            ? null
            : new ApiQuery()
                .Add("status", options.Status)
                .Add("stream", options.Stream)
                .Add("query", options.Query)
                .Add("page", options.Page)
                .Add("per_page", options.PerPage)
                .Pairs;
        return _connection.GetAsync<InboundList>(BasePath, query, cancellationToken);
    }

    public async Task<Message> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<MessageData>($"{BasePath}/{id}", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Message;
    }

    public Task<RequeueResult> RetryAsync(long id, CancellationToken cancellationToken = default)
        => _connection.PostAsync<RequeueResult>($"{BasePath}/{id}/retry", null, cancellationToken);

    public Task<RequeueResult> BypassAsync(long id, CancellationToken cancellationToken = default)
        => _connection.PostAsync<RequeueResult>($"{BasePath}/{id}/bypass", null, cancellationToken);

    internal sealed record MessageData
    {
        public Message Message { get; init; } = new();
    }
}
