namespace CamelMailer;

/// <summary>
/// The opt-in subscribers of a broadcast stream. A broadcast send to an address
/// that is not subscribed is refused, so this list is the audience.
/// </summary>
public interface ISubscribersResource
{
    /// <summary>Lists the stream's subscribers, subscribed and unsubscribed alike.</summary>
    Task<IReadOnlyList<Subscriber>> ListAsync(
        string permalink,
        CancellationToken cancellationToken = default);

    /// <summary>Adds or updates one subscriber. Upserts by address, so calling it twice is safe.</summary>
    Task<Subscriber> AddAsync(
        string permalink,
        AddSubscriberRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Adds many addresses at once, all as subscribed.</summary>
    Task<ImportSubscribersResult> ImportAsync(
        string permalink,
        IEnumerable<string> addresses,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a spam complaint: writes a stream-scoped suppression and flips the
    /// subscription to <c>unsubscribed</c>. Idempotent, so a feedback loop can
    /// replay it safely.
    /// </summary>
    Task<Subscriber> ComplaintAsync(
        string permalink,
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a subscriber from the stream entirely.</summary>
    Task<DeleteResult> RemoveAsync(
        string permalink,
        string address,
        CancellationToken cancellationToken = default);
}

internal sealed class SubscribersResource : ISubscribersResource
{
    private readonly ApiConnection _connection;

    internal SubscribersResource(ApiConnection connection) => _connection = connection;

    public async Task<IReadOnlyList<Subscriber>> ListAsync(
        string permalink,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<SubscribersData>(BasePath(permalink), null, cancellationToken)
            .ConfigureAwait(false);
        return data.Subscribers;
    }

    public async Task<Subscriber> AddAsync(
        string permalink,
        AddSubscriberRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PostAsync<SubscriberData>(BasePath(permalink), request, cancellationToken)
            .ConfigureAwait(false);
        return data.Subscriber;
    }

    public Task<ImportSubscribersResult> ImportAsync(
        string permalink,
        IEnumerable<string> addresses,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        var body = new ImportBody { Addresses = addresses.ToList() };
        return _connection.PostAsync<ImportSubscribersResult>(
            $"{BasePath(permalink)}/import", body, cancellationToken);
    }

    public async Task<Subscriber> ComplaintAsync(
        string permalink,
        string address,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .PostAsync<SubscriberData>(
                $"{BasePath(permalink)}/{Encode(address)}/complaint", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Subscriber;
    }

    public Task<DeleteResult> RemoveAsync(
        string permalink,
        string address,
        CancellationToken cancellationToken = default)
        => _connection.DeleteAsync<DeleteResult>(
            $"{BasePath(permalink)}/{Encode(address)}", cancellationToken);

    private static string BasePath(string permalink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permalink);
        return $"/api/v2/server/streams/{Uri.EscapeDataString(permalink)}/subscribers";
    }

    /// <summary>
    /// Percent-encodes an address for a path segment. <see cref="Uri.EscapeDataString"/>
    /// already escapes the plus, which has to survive or a different address is
    /// addressed.
    /// </summary>
    private static string Encode(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        return Uri.EscapeDataString(address);
    }

    internal sealed record ImportBody
    {
        public IReadOnlyList<string> Addresses { get; init; } = [];
    }

    internal sealed record SubscribersData
    {
        public IReadOnlyList<Subscriber> Subscribers { get; init; } = [];
    }

    internal sealed record SubscriberData
    {
        public Subscriber Subscriber { get; init; } = new();
    }
}
