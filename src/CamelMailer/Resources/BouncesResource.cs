namespace CamelMailer;

/// <summary>Inspect bounce messages.</summary>
public interface IBouncesResource
{
    /// <summary>Lists bounce messages.</summary>
    Task<BounceList> ListAsync(
        ListBouncesOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a single bounce message.</summary>
    Task<Message> GetAsync(long id, CancellationToken cancellationToken = default);
}

internal sealed class BouncesResource : IBouncesResource
{
    private const string BasePath = "/api/v2/server/bounces";

    private readonly ApiConnection _connection;

    internal BouncesResource(ApiConnection connection) => _connection = connection;

    public Task<BounceList> ListAsync(
        ListBouncesOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var query = options is null
            ? null
            : new ApiQuery()
                .Add("page", options.Page)
                .Add("per_page", options.PerPage)
                .Add("tag", options.Tag)
                .Pairs;
        return _connection.GetAsync<BounceList>(BasePath, query, cancellationToken);
    }

    public async Task<Message> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<BounceData>($"{BasePath}/{id}", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Bounce;
    }

    internal sealed record BounceData
    {
        public Message Bounce { get; init; } = new();
    }
}
