namespace CamelMailer;

/// <summary>Manage message streams.</summary>
public interface IStreamsResource
{
    /// <summary>Lists all message streams of the server.</summary>
    Task<IReadOnlyList<MessageStream>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a message stream.</summary>
    Task<MessageStream> CreateAsync(
        CreateStreamRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a message stream by permalink.</summary>
    Task<MessageStream> GetAsync(string permalink, CancellationToken cancellationToken = default);

    /// <summary>Updates a message stream. Unset fields are left unchanged.</summary>
    Task<MessageStream> UpdateAsync(
        string permalink,
        UpdateStreamRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Archives a message stream.</summary>
    Task<MessageStream> ArchiveAsync(
        string permalink,
        CancellationToken cancellationToken = default);
}

internal sealed class StreamsResource : IStreamsResource
{
    private const string BasePath = "/api/v2/server/streams";

    private readonly ApiConnection _connection;

    internal StreamsResource(ApiConnection connection) => _connection = connection;

    public async Task<IReadOnlyList<MessageStream>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<StreamsData>(BasePath, null, cancellationToken)
            .ConfigureAwait(false);
        return data.Streams;
    }

    public async Task<MessageStream> CreateAsync(
        CreateStreamRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PostAsync<StreamData>(BasePath, request, cancellationToken)
            .ConfigureAwait(false);
        return data.Stream;
    }

    public async Task<MessageStream> GetAsync(
        string permalink,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<StreamData>(PathFor(permalink), null, cancellationToken)
            .ConfigureAwait(false);
        return data.Stream;
    }

    public async Task<MessageStream> UpdateAsync(
        string permalink,
        UpdateStreamRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PatchAsync<StreamData>(PathFor(permalink), request, cancellationToken)
            .ConfigureAwait(false);
        return data.Stream;
    }

    public async Task<MessageStream> ArchiveAsync(
        string permalink,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .PostAsync<StreamData>($"{PathFor(permalink)}/archive", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Stream;
    }

    private static string PathFor(string permalink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permalink);
        return $"{BasePath}/{Uri.EscapeDataString(permalink)}";
    }

    internal sealed record StreamsData
    {
        public IReadOnlyList<MessageStream> Streams { get; init; } = [];
    }

    internal sealed record StreamData
    {
        public MessageStream Stream { get; init; } = new();
    }
}
