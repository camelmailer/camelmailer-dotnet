namespace CamelMailer;

/// <summary>
/// The server's own request log and tag index. Useful when a send did not arrive
/// and the question is whether the request ever reached the API, and with what
/// answer.
/// </summary>
public interface ILogsResource
{
    /// <summary>Lists logged API requests, newest first.</summary>
    Task<RequestLogList> ListAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the tags used by the server's recent messages, most used first.</summary>
    Task<IReadOnlyList<TagCount>> GetTagsAsync(CancellationToken cancellationToken = default);
}

internal sealed class LogsResource : ILogsResource
{
    private readonly ApiConnection _connection;

    internal LogsResource(ApiConnection connection) => _connection = connection;

    public Task<RequestLogList> ListAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ApiQuery().Add("page", page).Add("per_page", perPage).Pairs;
        return _connection.GetAsync<RequestLogList>("/api/v2/server/logs", query, cancellationToken);
    }

    public async Task<IReadOnlyList<TagCount>> GetTagsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<TagsData>("/api/v2/server/tags", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Tags;
    }

    internal sealed record TagsData
    {
        public IReadOnlyList<TagCount> Tags { get; init; } = [];
    }
}
