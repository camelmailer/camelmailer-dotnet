using System.Text.Json;
using System.Text.Json.Nodes;

namespace CamelMailer;

/// <summary>
/// Broadcast campaigns. A campaign is content plus an audience.
/// </summary>
/// <remarks>
/// There are two ways to create one and they behave differently:
/// <see cref="CreateDraftAsync"/> writes it and waits, while
/// <see cref="CreateAndSendAsync"/> expands it to the stream's subscribers
/// before the call returns.
/// </remarks>
public interface ICampaignsResource
{
    /// <summary>Lists every campaign of the server, newest first.</summary>
    Task<IReadOnlyList<Campaign>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists the campaigns of one broadcast stream.</summary>
    Task<IReadOnlyList<Campaign>> ListForStreamAsync(
        string permalink,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a campaign together with its statistics.</summary>
    Task<CampaignDetails> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Fetches a campaign through its stream.</summary>
    Task<CampaignDetails> GetForStreamAsync(
        string permalink,
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a campaign without sending it. Name the audience with
    /// <see cref="CreateDraftCampaignRequest.Stream"/>.
    /// </summary>
    Task<Campaign> CreateDraftAsync(
        CreateDraftCampaignRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a campaign on a broadcast stream and sends it immediately. The
    /// send starts before this call returns, so there is no draft to review and
    /// no schedule to set; use <see cref="CreateDraftAsync"/> when the campaign
    /// should wait.
    /// </summary>
    Task<Campaign> CreateAndSendAsync(
        string permalink,
        CreateAndSendCampaignRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a draft or scheduled campaign. One that is already sending cannot
    /// be edited and the API answers <c>ValidationError</c>.
    /// </summary>
    Task<Campaign> UpdateAsync(
        long id,
        UpdateCampaignRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a campaign now, whatever its schedule said.</summary>
    Task<Campaign> SendAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled or in-flight campaign. Messages already queued are
    /// not recalled.
    /// </summary>
    Task<Campaign> CancelAsync(long id, CancellationToken cancellationToken = default);
}

internal sealed class CampaignsResource : ICampaignsResource
{
    private const string BasePath = "/api/v2/server/campaigns";
    private const string StreamsPath = "/api/v2/server/streams";

    private readonly ApiConnection _connection;

    internal CampaignsResource(ApiConnection connection) => _connection = connection;

    public async Task<IReadOnlyList<Campaign>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<CampaignsData>(BasePath, null, cancellationToken)
            .ConfigureAwait(false);
        return data.Campaigns;
    }

    public async Task<IReadOnlyList<Campaign>> ListForStreamAsync(
        string permalink,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<CampaignsData>(StreamCampaigns(permalink), null, cancellationToken)
            .ConfigureAwait(false);
        return data.Campaigns;
    }

    public Task<CampaignDetails> GetAsync(long id, CancellationToken cancellationToken = default)
        => _connection.GetAsync<CampaignDetails>($"{BasePath}/{id}", null, cancellationToken);

    public Task<CampaignDetails> GetForStreamAsync(
        string permalink,
        long id,
        CancellationToken cancellationToken = default)
        => _connection.GetAsync<CampaignDetails>(
            $"{StreamCampaigns(permalink)}/{id}", null, cancellationToken);

    public async Task<Campaign> CreateDraftAsync(
        CreateDraftCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PostAsync<CampaignData>(BasePath, request, cancellationToken)
            .ConfigureAwait(false);
        return data.Campaign;
    }

    public async Task<Campaign> CreateAndSendAsync(
        string permalink,
        CreateAndSendCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PostAsync<CampaignData>(StreamCampaigns(permalink), request, cancellationToken)
            .ConfigureAwait(false);
        return data.Campaign;
    }

    public async Task<Campaign> UpdateAsync(
        long id,
        UpdateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PatchAsync<CampaignData>($"{BasePath}/{id}", BuildUpdateBody(request), cancellationToken)
            .ConfigureAwait(false);
        return data.Campaign;
    }

    public async Task<Campaign> SendAsync(long id, CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .PostAsync<CampaignData>($"{BasePath}/{id}/send", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Campaign;
    }

    public async Task<Campaign> CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .PostAsync<CampaignData>($"{BasePath}/{id}/cancel", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Campaign;
    }

    /// <summary>
    /// Renders the update as a JSON object so a cleared schedule reaches the API
    /// as an explicit <c>null</c>. The serializer drops null properties, and an
    /// omitted <c>scheduled_at</c> leaves the schedule standing.
    /// </summary>
    private static JsonObject BuildUpdateBody(UpdateCampaignRequest request)
    {
        var body = new JsonObject();
        AddIfSet(body, "name", request.Name);
        AddIfSet(body, "from", request.From);
        AddIfSet(body, "subject", request.Subject);
        AddIfSet(body, "html_body", request.HtmlBody);
        AddIfSet(body, "text_body", request.TextBody);
        if (request.ClearSchedule)
        {
            body["scheduled_at"] = null;
        }
        else if (request.ScheduledAt is { } scheduledAt)
        {
            body["scheduled_at"] = ApiConnection.FormatUtc(scheduledAt);
        }

        return body;
    }

    private static void AddIfSet(JsonObject body, string name, string? value)
    {
        if (value is not null)
        {
            body[name] = value;
        }
    }

    private static string StreamCampaigns(string permalink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permalink);
        return $"{StreamsPath}/{Uri.EscapeDataString(permalink)}/campaigns";
    }

    internal sealed record CampaignsData
    {
        public IReadOnlyList<Campaign> Campaigns { get; init; } = [];
    }

    internal sealed record CampaignData
    {
        public Campaign Campaign { get; init; } = new();
    }
}
