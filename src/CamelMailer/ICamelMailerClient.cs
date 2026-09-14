namespace CamelMailer;

/// <summary>A client for the CamelMailer messaging (server) API.</summary>
public interface ICamelMailerClient
{
    /// <summary>Send email and inspect stored messages.</summary>
    IEmailsResource Emails { get; }

    /// <summary>Manage stored email templates.</summary>
    ITemplatesResource Templates { get; }

    /// <summary>Manage message streams.</summary>
    IStreamsResource Streams { get; }

    /// <summary>Read message counters and delivery-queue statistics.</summary>
    IStatsResource Stats { get; }

    /// <summary>Inspect bounce messages.</summary>
    IBouncesResource Bounces { get; }

    /// <summary>Read DMARC compliance summaries and aggregate reports.</summary>
    IDmarcResource Dmarc { get; }

    /// <summary>Plan and send broadcast campaigns.</summary>
    ICampaignsResource Campaigns { get; }

    /// <summary>Manage the opt-in audience of a broadcast stream.</summary>
    ISubscribersResource Subscribers { get; }

    /// <summary>Manage the wrappers shared by templates.</summary>
    ILayoutsResource Layouts { get; }

    /// <summary>Read inbound and held messages.</summary>
    IInboundResource Inbound { get; }

    /// <summary>Read the server's request log and tag index.</summary>
    ILogsResource Logs { get; }
}
