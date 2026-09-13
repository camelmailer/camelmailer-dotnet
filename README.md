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
