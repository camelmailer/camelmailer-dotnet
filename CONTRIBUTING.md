# Contributing

Thanks for helping improve the CamelMailer .NET SDK!

## Development setup

- Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Clone the repo and run `dotnet build`.

## Running tests

```bash
dotnet test
```

The unit tests run against a mocked HTTP layer and never touch the network.
To run the integration tests against a real instance, set `CAMELMAILER_API_KEY`
(and optionally `CAMELMAILER_BASE_URL`, `CAMELMAILER_FROM`, `CAMELMAILER_TO`).

## Conventions

- Test-driven: add or adjust a test with every behaviour change.
- Every public member carries an XML doc comment; warnings are errors.
- `dotnet format` must be clean (`dotnet format --verify-no-changes`) — CI
  enforces this.
- Keep commits small and focused.
