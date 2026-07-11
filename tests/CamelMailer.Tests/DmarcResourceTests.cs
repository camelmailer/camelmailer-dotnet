namespace CamelMailer.Tests;

public class DmarcResourceTests
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsComplianceSummary()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "summary": {
                    "total": 200, "pass": 180, "fail": 20, "pass_rate": 0.9,
                    "by_source": [
                      { "source_ip": "203.0.113.10", "count": 150,
                        "spf_aligned_pct": 98.5, "dkim_aligned_pct": 99.1,
                        "disposition_counts": { "none": 149, "quarantine": 1 } }
                    ],
                    "by_disposition": { "none": 195, "quarantine": 4, "reject": 1 }
                  }
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var summary = await client.Dmarc.GetSummaryAsync();

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/dmarc/summary",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(200, summary.Total);
        Assert.Equal(0.9, summary.PassRate);
        var source = Assert.Single(summary.BySource);
        Assert.Equal("203.0.113.10", source.SourceIp);
        Assert.Equal(99.1, source.DkimAlignedPct);
        Assert.Equal(149, source.DispositionCounts["none"]);
        Assert.Equal(4, summary.ByDisposition["quarantine"]);
    }

    [Fact]
    public async Task GetSummaryAsync_WithQuery_SendsDomainAndWindow()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""{"summary":{"total":0,"pass":0,"fail":0,"pass_rate":0,"by_source":[],"by_disposition":{}}}"""),
        };
        using var client = TestClient.Create(handler);

        await client.Dmarc.GetSummaryAsync(new DmarcQueryOptions
        {
            Domain = "acme.com",
            From = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
        });

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("domain=acme.com", query);
        Assert.Contains("from=2026-06-01T00%3A00%3A00Z", query);
        Assert.Contains("to=2026-07-01T00%3A00%3A00Z", query);
    }

    [Fact]
    public async Task ListReportsAsync_ReturnsReportsWithPagination()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "reports": [
                    { "id": 11, "domain": "acme.com", "org_name": "google.com",
                      "org_email": "noreply-dmarc@google.com", "report_id": "r-1",
                      "date_range_begin": "2026-06-30T00:00:00+00:00",
                      "date_range_end": "2026-06-30T23:59:59+00:00",
                      "received_at": "2026-07-01T04:00:00+00:00", "record_count": 12 }
                  ],
                  "pagination": { "page": 1, "per_page": 25, "total": 1, "total_pages": 1 }
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var list = await client.Dmarc.ListReportsAsync(new DmarcQueryOptions { Page = 1, PerPage = 25 });

        Assert.StartsWith(
            "https://app.camelmailer.com/api/v2/server/dmarc/reports?",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("per_page=25", handler.LastRequest.RequestUri.Query);
        var report = Assert.Single(list.Reports);
        Assert.Equal("google.com", report.OrgName);
        Assert.Equal(12, report.RecordCount);
        Assert.Equal(1, list.Pagination!.TotalPages);
    }

    [Fact]
    public async Task GetReportAsync_ReturnsReportWithRecords()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = TestEnvelope.Success("""
                {
                  "report": {
                    "id": 11, "domain": "acme.com", "org_name": "google.com",
                    "org_email": null, "report_id": "r-1",
                    "date_range_begin": "2026-06-30T00:00:00+00:00",
                    "date_range_end": "2026-06-30T23:59:59+00:00",
                    "received_at": "2026-07-01T04:00:00+00:00", "record_count": 1
                  },
                  "records": [
                    { "id": 21, "source_ip": "203.0.113.10", "count": 3,
                      "disposition": "none", "dkim_result": "pass", "spf_result": "softfail",
                      "dkim_aligned": true, "spf_aligned": false,
                      "header_from": "acme.com", "envelope_from": "bounce.acme.com" }
                  ]
                }
                """),
        };
        using var client = TestClient.Create(handler);

        var details = await client.Dmarc.GetReportAsync(11);

        Assert.Equal(
            "https://app.camelmailer.com/api/v2/server/dmarc/reports/11",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("acme.com", details.Report.Domain);
        var record = Assert.Single(details.Records);
        Assert.True(record.DkimAligned);
        Assert.False(record.SpfAligned);
        Assert.Equal("softfail", record.SpfResult);
    }
}
