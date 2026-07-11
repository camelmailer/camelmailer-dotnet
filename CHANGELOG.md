# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/camelmailer/camelmailer-dotnet/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/camelmailer/camelmailer-dotnet/releases/tag/v0.1.0
