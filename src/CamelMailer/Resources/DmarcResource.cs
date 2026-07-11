namespace CamelMailer;

/// <summary>Read DMARC compliance summaries and stored aggregate reports.</summary>
public interface IDmarcResource
{
    /// <summary>Fetches the aggregated DMARC compliance summary.</summary>
    Task<DmarcSummary> GetSummaryAsync(
        DmarcQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists stored DMARC aggregate reports, newest report range first.</summary>
    Task<DmarcReportList> ListReportsAsync(
        DmarcQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a DMARC aggregate report together with its records.</summary>
    Task<DmarcReportDetails> GetReportAsync(
        long id,
        CancellationToken cancellationToken = default);
}

internal sealed class DmarcResource : IDmarcResource
{
    private const string BasePath = "/api/v2/server/dmarc";

    private readonly ApiConnection _connection;

    internal DmarcResource(ApiConnection connection) => _connection = connection;

    public async Task<DmarcSummary> GetSummaryAsync(
        DmarcQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<SummaryData>($"{BasePath}/summary", QueryFor(options), cancellationToken)
            .ConfigureAwait(false);
        return data.Summary;
    }

    public Task<DmarcReportList> ListReportsAsync(
        DmarcQueryOptions? options = null,
        CancellationToken cancellationToken = default)
        => _connection.GetAsync<DmarcReportList>(
            $"{BasePath}/reports", QueryFor(options), cancellationToken);

    public Task<DmarcReportDetails> GetReportAsync(
        long id,
        CancellationToken cancellationToken = default)
        => _connection.GetAsync<DmarcReportDetails>(
            $"{BasePath}/reports/{id}", null, cancellationToken);

    private static IReadOnlyCollection<KeyValuePair<string, string>>? QueryFor(
        DmarcQueryOptions? options)
        => options is null
            ? null
            : new ApiQuery()
                .Add("domain", options.Domain)
                .Add("from", options.From)
                .Add("to", options.To)
                .Add("page", options.Page)
                .Add("per_page", options.PerPage)
                .Pairs;

    internal sealed record SummaryData
    {
        public DmarcSummary Summary { get; init; } = new();
    }
}
