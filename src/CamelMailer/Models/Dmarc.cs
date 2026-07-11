namespace CamelMailer;

/// <summary>An aggregated DMARC compliance summary built from stored aggregate reports.</summary>
public sealed record DmarcSummary
{
    /// <summary>Messages covered by the reports.</summary>
    public long Total { get; init; }

    /// <summary>Messages with both DKIM and SPF aligned.</summary>
    public long Pass { get; init; }

    /// <summary>Messages failing DMARC alignment.</summary>
    public long Fail { get; init; }

    /// <summary><see cref="Pass" /> divided by <see cref="Total" /> (0.0–1.0).</summary>
    public double PassRate { get; init; }

    /// <summary>The top sending sources by volume.</summary>
    public IReadOnlyList<DmarcSource> BySource { get; init; } = [];

    /// <summary>Message counts per disposition (<c>none</c>, <c>quarantine</c>, <c>reject</c>).</summary>
    public IReadOnlyDictionary<string, long> ByDisposition { get; init; } =
        new Dictionary<string, long>();
}

/// <summary>Per-source alignment statistics within a <see cref="DmarcSummary" />.</summary>
public sealed record DmarcSource
{
    /// <summary>The sending IP address.</summary>
    public string? SourceIp { get; init; }

    /// <summary>Messages reported for this source.</summary>
    public long Count { get; init; }

    /// <summary>The percentage of messages with SPF aligned.</summary>
    public double SpfAlignedPct { get; init; }

    /// <summary>The percentage of messages with DKIM aligned.</summary>
    public double DkimAlignedPct { get; init; }

    /// <summary>Message counts per disposition for this source.</summary>
    public IReadOnlyDictionary<string, long> DispositionCounts { get; init; } =
        new Dictionary<string, long>();
}

/// <summary>Filters for the DMARC summary and report list.</summary>
public sealed record DmarcQueryOptions
{
    /// <summary>Restrict to reports for this domain.</summary>
    public string? Domain { get; init; }

    /// <summary>Match reports whose date range ends at or after this instant.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Match reports whose date range begins at or before this instant.</summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>The 1-based page to fetch (report list only).</summary>
    public int? Page { get; init; }

    /// <summary>Results per page, max 100 (report list only).</summary>
    public int? PerPage { get; init; }
}

/// <summary>A stored DMARC aggregate report.</summary>
public sealed record DmarcReport
{
    /// <summary>The numeric report id.</summary>
    public long Id { get; init; }

    /// <summary>The domain the report covers.</summary>
    public string? Domain { get; init; }

    /// <summary>The reporting organisation, e.g. <c>google.com</c>.</summary>
    public string? OrgName { get; init; }

    /// <summary>The reporting organisation's contact address.</summary>
    public string? OrgEmail { get; init; }

    /// <summary>The reporter's own report id.</summary>
    public string? ReportId { get; init; }

    /// <summary>The start of the covered date range.</summary>
    public DateTimeOffset? DateRangeBegin { get; init; }

    /// <summary>The end of the covered date range.</summary>
    public DateTimeOffset? DateRangeEnd { get; init; }

    /// <summary>When the report was ingested.</summary>
    public DateTimeOffset? ReceivedAt { get; init; }

    /// <summary>The number of records in the report.</summary>
    public long RecordCount { get; init; }
}

/// <summary>One page of DMARC aggregate reports.</summary>
public sealed record DmarcReportList
{
    /// <summary>The reports on this page, newest report range first.</summary>
    public IReadOnlyList<DmarcReport> Reports { get; init; } = [];

    /// <summary>Paging information.</summary>
    public Pagination? Pagination { get; init; }
}

/// <summary>A DMARC aggregate report together with its records.</summary>
public sealed record DmarcReportDetails
{
    /// <summary>The report itself.</summary>
    public DmarcReport Report { get; init; } = new();

    /// <summary>The per-source records of the report.</summary>
    public IReadOnlyList<DmarcRecord> Records { get; init; } = [];
}

/// <summary>A single record within a DMARC aggregate report.</summary>
public sealed record DmarcRecord
{
    /// <summary>The numeric record id.</summary>
    public long Id { get; init; }

    /// <summary>The sending IP address.</summary>
    public string? SourceIp { get; init; }

    /// <summary>The number of messages this record covers.</summary>
    public long Count { get; init; }

    /// <summary>The applied disposition: <c>none</c>, <c>quarantine</c> or <c>reject</c>.</summary>
    public string? Disposition { get; init; }

    /// <summary>The DKIM evaluation result, e.g. <c>pass</c>.</summary>
    public string? DkimResult { get; init; }

    /// <summary>The SPF evaluation result, e.g. <c>softfail</c>.</summary>
    public string? SpfResult { get; init; }

    /// <summary>Whether DKIM was aligned with the header From domain.</summary>
    public bool DkimAligned { get; init; }

    /// <summary>Whether SPF was aligned with the header From domain.</summary>
    public bool SpfAligned { get; init; }

    /// <summary>The header From domain.</summary>
    public string? HeaderFrom { get; init; }

    /// <summary>The envelope From domain.</summary>
    public string? EnvelopeFrom { get; init; }
}
