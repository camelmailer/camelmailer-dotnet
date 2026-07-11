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
}
