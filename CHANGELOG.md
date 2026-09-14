# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-09-14

### Added

- `Campaigns`: `CreateDraftAsync`, `CreateAndSendAsync`, `ListAsync`,
  `ListForStreamAsync`, `GetAsync`, `GetForStreamAsync`, `UpdateAsync`,
  `SendAsync`, `CancelAsync`. The two create methods hit different routes:
  `CreateDraftAsync` writes the campaign and waits, while
  `CreateAndSendAsync` expands it to the stream's subscribers before the
  call returns.
- `Subscribers`: `ListAsync`, `AddAsync`, `ImportAsync`, `ComplaintAsync`,
  `RemoveAsync`.
- `Layouts`: `ListAsync`, `CreateAsync`, `GetAsync`, `UpdateAsync`,
  `DeleteAsync`, `UploadLogoAsync`.
- `Inbound`: `ListAsync`, `GetAsync`, `RetryAsync`, `BypassAsync`.
- `Logs`: `ListAsync`, `GetTagsAsync`.
- `Emails.SendToStreamAsync` for broadcasting to a stream's subscribers.
- An `idempotencyKey` parameter on all four send methods. The key travels as
  the `Idempotency-Key` header, because the body is what the server hashes
  to recognise a replay.
- `CreateStreamRequest.Permalink` and `UpdateStreamRequest.Archived`.
  Without the permalink the API derives one from the name, which a caller
  that has to know the permalink up front cannot rely on.

### Changed

- The send methods take `idempotencyKey` before `cancellationToken`. Calls
  that pass the token positionally need to name it; calls that rely on the
  default are unaffected.

## [0.1.0] - 2026-07-12

### Added

- `CamelMailerClient` with configurable base URL for self-hosted instances.
- Emails: send, batch send, send with template (single and batch), get,
  list with filters, deliveries, opens, clicks and raw source.
- Templates: list, create, get, update, archive and render.
- Streams: list, create, get, update and archive.
- Stats: message counters (with time window) and delivery-queue statistics.
- Bounces: list and get.
- DMARC: compliance summary, report list and report details.
- Typed errors: `CamelMailerException` (with `ErrorCode` / `StatusCode`) and
  `CamelMailerNetworkException` for transport failures.
- `services.AddCamelMailer(...)` extension for Microsoft.Extensions.DependencyInjection.

[Unreleased]: https://github.com/camelmailer/camelmailer-dotnet/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/camelmailer/camelmailer-dotnet/releases/tag/v0.2.0
[0.1.0]: https://github.com/camelmailer/camelmailer-dotnet/releases/tag/v0.1.0
