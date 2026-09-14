namespace CamelMailer;

/// <summary>The audience stream carried on every campaign, so a list needs no second lookup.</summary>
public sealed record CampaignStream
{
    /// <summary>The permalink of the stream.</summary>
    public string? Permalink { get; init; }

    /// <summary>The display name of the stream.</summary>
    public string? Name { get; init; }
}

/// <summary>A broadcast campaign: content plus an audience.</summary>
public sealed record Campaign
{
    /// <summary>The numeric campaign id.</summary>
    public long Id { get; init; }

    /// <summary>The display name.</summary>
    public string? Name { get; init; }

    /// <summary>The message subject.</summary>
    public string? Subject { get; init; }

    /// <summary>The sender address.</summary>
    public string? From { get; init; }

    /// <summary>The HTML part.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text part.</summary>
    public string? TextBody { get; init; }

    /// <summary>
    /// The lifecycle state: <c>draft</c>, <c>scheduled</c>, <c>sending</c>,
    /// <c>sent</c>, <c>failed</c> or <c>canceled</c>. Only draft and scheduled
    /// can be edited.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>The recipient count snapshotted when the send begins.</summary>
    public long Total { get; init; }

    /// <summary>The recipients expanded into messages so far.</summary>
    public long Sent { get; init; }

    /// <summary>The id of the audience stream.</summary>
    public long StreamId { get; init; }

    /// <summary>The permalink and name of the audience stream.</summary>
    public CampaignStream? Stream { get; init; }

    /// <summary>The send time of a scheduled campaign.</summary>
    public DateTimeOffset? ScheduledAt { get; init; }

    /// <summary>When the campaign row was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>When expansion finished (<c>sent</c> or <c>failed</c>).</summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>Per-campaign counters, attributed through the messages the campaign produced.</summary>
public sealed record CampaignStats
{
    /// <summary>The recipient count of the campaign.</summary>
    public long Total { get; init; }

    /// <summary>Messages created.</summary>
    public long Sent { get; init; }

    /// <summary>Messages delivered.</summary>
    public long Delivered { get; init; }

    /// <summary>Messages that failed.</summary>
    public long Failed { get; init; }

    /// <summary>Messages opened at least once.</summary>
    public long Opened { get; init; }

    /// <summary>Messages with at least one click.</summary>
    public long Clicked { get; init; }

    /// <summary>Resulting unsubscribes.</summary>
    public long Unsubscribed { get; init; }
}

/// <summary>A campaign together with its statistics.</summary>
public sealed record CampaignDetails
{
    /// <summary>The campaign itself.</summary>
    public Campaign Campaign { get; init; } = new();

    /// <summary>Its counters.</summary>
    public CampaignStats Stats { get; init; } = new();
}

/// <summary>
/// Fields for <see cref="ICampaignsResource.CreateDraftAsync"/>.
/// </summary>
/// <remarks>
/// The initial status follows what you pass: <see cref="SendNow"/> wins, then a
/// <see cref="ScheduledAt"/> (status <c>scheduled</c>), else a draft.
/// </remarks>
public sealed record CreateDraftCampaignRequest
{
    /// <summary>The permalink of the broadcast stream to send to.</summary>
    public required string Stream { get; init; }

    /// <summary>The bare sender address; the broadcast path authorizes its domain.</summary>
    public required string From { get; init; }

    /// <summary>The display name.</summary>
    public string? Name { get; init; }

    /// <summary>The message subject.</summary>
    public string? Subject { get; init; }

    /// <summary>The HTML part.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text part.</summary>
    public string? TextBody { get; init; }

    /// <summary>The send time; arms the campaign as <c>scheduled</c>.</summary>
    public DateTimeOffset? ScheduledAt { get; init; }

    /// <summary>Send on creation, overriding <see cref="ScheduledAt"/>.</summary>
    public bool? SendNow { get; init; }
}

/// <summary>
/// Fields for <see cref="ICampaignsResource.CreateAndSendAsync"/>. There is no
/// schedule here: the send starts before the call returns.
/// </summary>
public sealed record CreateAndSendCampaignRequest
{
    /// <summary>The display name.</summary>
    public required string Name { get; init; }

    /// <summary>The bare sender address.</summary>
    public string? From { get; init; }

    /// <summary>The message subject.</summary>
    public string? Subject { get; init; }

    /// <summary>The HTML part.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text part.</summary>
    public string? TextBody { get; init; }
}

/// <summary>
/// Fields for <see cref="ICampaignsResource.UpdateAsync"/>. Unset fields are left
/// unchanged.
/// </summary>
/// <remarks>
/// The schedule is three-valued, which is why it has two members: leaving both
/// unset keeps the current schedule, <see cref="ScheduledAt"/> moves a draft to
/// <c>scheduled</c>, and <see cref="ClearSchedule"/> sends an explicit
/// <c>null</c>, which drops the campaign back to <c>draft</c>. An omitted field
/// and a <c>null</c> mean different things to the API.
/// </remarks>
public sealed record UpdateCampaignRequest
{
    /// <summary>A new display name.</summary>
    public string? Name { get; init; }

    /// <summary>A new sender address.</summary>
    public string? From { get; init; }

    /// <summary>A new subject.</summary>
    public string? Subject { get; init; }

    /// <summary>A new HTML part.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>A new plain-text part.</summary>
    public string? TextBody { get; init; }

    /// <summary>Schedules the campaign, moving a draft to <c>scheduled</c>.</summary>
    public DateTimeOffset? ScheduledAt { get; init; }

    /// <summary>
    /// Clears the schedule, dropping the campaign back to <c>draft</c>. Sends an
    /// explicit <c>null</c>; omitting the field would leave the schedule standing.
    /// </summary>
    public bool ClearSchedule { get; init; }
}
