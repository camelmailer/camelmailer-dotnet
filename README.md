# Camelmailer .NET SDK

[![CI](https://github.com/camelmailer/camelmailer-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/camelmailer/camelmailer-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/CamelMailer.svg)](https://www.nuget.org/packages/CamelMailer)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

The official .NET SDK for [Camelmailer](https://camelmailer.com) — the
open-source transactional email platform.

## Install

```bash
dotnet add package CamelMailer
```

Requires .NET 8 or later.

## Quickstart

```csharp
using CamelMailer;

using var camelmailer = new CamelMailerClient("cm_xxxx");

var result = await camelmailer.Emails.SendAsync(new SendEmailRequest
{
    From = "billing@acme.com",
    To = ["ada@example.com"],
    Subject = "Your receipt",
    HtmlBody = "<p>Thanks for your purchase.</p>",
});

Console.WriteLine($"Queued message {result.MessageId}");
```

## ASP.NET Core / dependency injection

```csharp
// Program.cs
builder.Services.AddCamelMailer(options =>
{
    options.ApiKey = builder.Configuration["CamelMailer:ApiKey"]!;
    // options.BaseUrl = "https://mail.example.com"; // self-hosted
});
```

```csharp
public class ReceiptService(ICamelMailerClient camelmailer)
{
    public Task SendReceiptAsync(string to, CancellationToken cancellationToken) =>
        camelmailer.Emails.SendAsync(new SendEmailRequest
        {
            From = "billing@acme.com",
            To = [to],
            Subject = "Your receipt",
            TextBody = "Thanks for your purchase.",
        }, cancellationToken);
}
```

`AddCamelMailer` returns the underlying `IHttpClientBuilder`, so you can
attach resilience handlers, timeouts or a custom primary handler.

## Emails

```csharp
// Named addresses, attachments, metadata
await camelmailer.Emails.SendAsync(new SendEmailRequest
{
    From = new EmailAddress { Email = "billing@acme.com", Name = "Acme Billing" },
    To = ["ada@example.com"],
    Subject = "Invoice #1017",
    HtmlBody = "<p>Attached.</p>",
    Attachments = [Attachment.FromBytes("invoice.pdf", "application/pdf", pdfBytes)],
    Metadata = new Dictionary<string, object?> { ["order_id"] = "o_123" },
    Tag = "invoice",
});

// Batch (one result per entry — a failed entry does not fail the batch)
var results = await camelmailer.Emails.SendBatchAsync([request1, request2]);
foreach (var entry in results.Where(r => !r.IsSuccess))
    Console.WriteLine($"{entry.Error!.Code}: {entry.Error.Message}");

// Stored templates
await camelmailer.Emails.SendWithTemplateAsync(new SendTemplateEmailRequest
{
    From = "hello@acme.com",
    To = ["ada@example.com"],
    Template = "welcome",
    TemplateModel = new Dictionary<string, object?> { ["name"] = "Ada" },
});

// Retry-safe sends: the same key with the same body returns the first result
// instead of queuing a second copy, and a different body under the same key is
// refused with InvalidIdempotentRequest. All four send methods take one.
await camelmailer.Emails.SendAsync(request, idempotencyKey: $"order-{orderId}");

// Broadcast to everyone subscribed to a stream. Recipients past the
// per-request cap of 1000 come back as Skipped, so a larger audience wants a
// campaign.
var broadcast = await camelmailer.Emails.SendToStreamAsync("newsletter", new SendToStreamRequest
{
    From = "news@acme.com",
    Subject = "September",
    TextBody = "What shipped this month.",
});

// Inspect messages
var page = await camelmailer.Emails.ListAsync(new ListEmailsOptions { Tag = "invoice" });
var details = await camelmailer.Emails.GetAsync(page.Messages[0].Id);
var opens = await camelmailer.Emails.GetOpensAsync(details.Message.Id);
var raw = await camelmailer.Emails.GetRawAsync(details.Message.Id);
```

## Templates

```csharp
var template = await camelmailer.Templates.CreateAsync(new CreateTemplateRequest
{
    Name = "Welcome",
    Subject = "Welcome, {{ name }}!",
    HtmlBody = "<p>Hi {{ name }}</p>",
});

var rendered = await camelmailer.Templates.RenderAsync(
    template.Permalink!,
    new Dictionary<string, object?> { ["name"] = "Ada" });

await camelmailer.Templates.UpdateAsync(template.Permalink!,
    new UpdateTemplateRequest { Subject = "Hello, {{ name }}!" });
await camelmailer.Templates.ArchiveAsync(template.Permalink!);
```

## Streams

```csharp
var stream = await camelmailer.Streams.CreateAsync(new CreateStreamRequest
{
    Name = "Broadcasts",
    Permalink = "broadcasts",   // the API derives one from the name when unset
    StreamType = "broadcast",
});

await camelmailer.Emails.SendAsync(new SendEmailRequest
{
    From = "news@acme.com",
    To = ["ada@example.com"],
    Subject = "June update",
    TextBody = "...",
    Stream = stream.Permalink,
});
```

## Campaigns

A campaign is content plus an audience. The two ways to create one behave
differently, so pick deliberately: `CreateDraftAsync` writes it and waits,
`CreateAndSendAsync` expands it to the stream's subscribers before the call
returns.

```csharp
// Write it and leave it alone. Without a schedule it stays a draft; with one
// it becomes "scheduled" and the server sends it when due.
var draft = await camelmailer.Campaigns.CreateDraftAsync(new CreateDraftCampaignRequest
{
    Stream = "newsletter",
    From = "news@acme.com",
    Name = "September",
    Subject = "What shipped",
    TextBody = "Hello.",
    // ScheduledAt = DateTimeOffset.Parse("2026-10-01T08:00:00Z"),
});

// Goes out on the spot, no draft and no schedule.
await camelmailer.Campaigns.CreateAndSendAsync("newsletter", new CreateAndSendCampaignRequest
{
    Name = "Status update",
    From = "news@acme.com",
    TextBody = "All clear.",
});

var detail = await camelmailer.Campaigns.GetAsync(draft.Id);   // campaign + stats

// ScheduledAt schedules; ClearSchedule drops it back to a draft. Setting
// neither leaves the schedule standing, so the two are separate.
await camelmailer.Campaigns.UpdateAsync(draft.Id, new UpdateCampaignRequest
{
    ScheduledAt = DateTimeOffset.Parse("2026-10-01T08:00:00Z"),
});
await camelmailer.Campaigns.UpdateAsync(draft.Id, new UpdateCampaignRequest
{
    ClearSchedule = true,
});

await camelmailer.Campaigns.SendAsync(draft.Id);     // now, whatever the schedule said
await camelmailer.Campaigns.CancelAsync(draft.Id);
```

## Subscribers

A broadcast send to an address that is not subscribed is refused, so this list
is the audience.

```csharp
await camelmailer.Subscribers.ListAsync("newsletter");
await camelmailer.Subscribers.AddAsync("newsletter", new AddSubscriberRequest
{
    Address = "ada@example.com",
    Name = "Ada",
});
await camelmailer.Subscribers.ImportAsync("newsletter", ["ada@example.com", "grace@example.com"]);
await camelmailer.Subscribers.ComplaintAsync("newsletter", "ada@example.com");  // suppress + unsubscribe
await camelmailer.Subscribers.RemoveAsync("newsletter", "ada@example.com");
```

## Layouts

A layout wraps every template that uses it. `HtmlWrapper` has to embed the body
with `{{{ content }}}`.

```csharp
await camelmailer.Layouts.CreateAsync(new CreateLayoutRequest
{
    Name = "Default",
    Permalink = "default",
    HtmlWrapper = "<html><body>{{{ content }}}</body></html>",
});
var logo = await camelmailer.Layouts.UploadLogoAsync("default", "data:image/png;base64,...");
await camelmailer.Layouts.DeleteAsync("default");
```

## Inbound and held messages

```csharp
var held = await camelmailer.Inbound.ListAsync(new ListInboundOptions { Status = "held" });
await camelmailer.Inbound.RetryAsync(55);    // back on the delivery queue
await camelmailer.Inbound.BypassAsync(55);   // release past the hold
```

## Logs

Useful when a send did not arrive and the question is whether the request ever
reached the API.

```csharp
var requests = await camelmailer.Logs.ListAsync(perPage: 25);
var tags = await camelmailer.Logs.GetTagsAsync();
```

## Stats, bounces and DMARC

```csharp
var stats = await camelmailer.Stats.GetAsync(
    from: DateTimeOffset.UtcNow.AddDays(-7),
    to: DateTimeOffset.UtcNow);
Console.WriteLine($"{stats.Sent} sent, {stats.Bounced} bounced");

var queue = await camelmailer.Stats.GetDeliveriesAsync();
var bounces = await camelmailer.Bounces.ListAsync(new ListBouncesOptions { Tag = "invoice" });

var dmarc = await camelmailer.Dmarc.GetSummaryAsync(new DmarcQueryOptions { Domain = "acme.com" });
Console.WriteLine($"DMARC pass rate: {dmarc.PassRate:P1}");
```

## Error handling

The SDK throws typed exceptions:

```csharp
try
{
    await camelmailer.Emails.SendAsync(request);
}
catch (CamelMailerNetworkException ex)
{
    // DNS failure, refused connection, timeout — no API response received
    Console.WriteLine($"Network problem: {ex.InnerException?.Message}");
}
catch (CamelMailerException ex)
{
    // The API answered with an error envelope
    Console.WriteLine($"{ex.ErrorCode} (HTTP {(int?)ex.StatusCode}): {ex.Message}");
}
```

`ErrorCode` carries the API's stable error code (`Unauthorized`,
`ValidationError`, `NotFound`, …); `StatusCode` the HTTP status.

## Self-hosted instances

The client talks to the Camelmailer cloud (`https://app.camelmailer.com`)
by default. Point it at your own installation:

```csharp
using var camelmailer = new CamelMailerClient("cm_xxxx", "https://mail.example.com");
```

## Documentation

Full API documentation: [camelmailer.com/docs](https://camelmailer.com/docs)

## License

[MIT](LICENSE)
