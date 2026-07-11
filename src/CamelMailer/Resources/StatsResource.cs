namespace CamelMailer;

/// <summary>Read message counters and delivery-queue statistics.</summary>
public interface IStatsResource
{
    /// <summary>Fetches message counters, optionally limited to a time window.</summary>
    Task<EmailStats> GetAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches the current delivery queue, broken down by recipient domain.</summary>
    Task<DeliveryStats> GetDeliveriesAsync(CancellationToken cancellationToken = default);
}

internal sealed class StatsResource : IStatsResource
{
    private const string BasePath = "/api/v2/server/stats";

    private readonly ApiConnection _connection;

    internal StatsResource(ApiConnection connection) => _connection = connection;

    public async Task<EmailStats> GetAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ApiQuery().Add("from", from).Add("to", to).Pairs;
        var data = await _connection
            .GetAsync<StatsData>(BasePath, query, cancellationToken)
            .ConfigureAwait(false);
        return data.Stats;
    }

    public Task<DeliveryStats> GetDeliveriesAsync(CancellationToken cancellationToken = default)
        => _connection.GetAsync<DeliveryStats>($"{BasePath}/deliveries", null, cancellationToken);

    internal sealed record StatsData
    {
        public EmailStats Stats { get; init; } = new();
    }
}
