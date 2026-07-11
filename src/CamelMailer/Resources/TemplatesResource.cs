namespace CamelMailer;

/// <summary>Manage stored email templates.</summary>
public interface ITemplatesResource
{
    /// <summary>Lists all templates of the server.</summary>
    Task<IReadOnlyList<Template>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a template.</summary>
    Task<Template> CreateAsync(
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a template by permalink.</summary>
    Task<Template> GetAsync(string permalink, CancellationToken cancellationToken = default);

    /// <summary>Updates a template. Unset fields are left unchanged.</summary>
    Task<Template> UpdateAsync(
        string permalink,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Archives a template so it can no longer be used for sending.</summary>
    Task<Template> ArchiveAsync(string permalink, CancellationToken cancellationToken = default);

    /// <summary>Renders a template against a model without sending anything (preview).</summary>
    Task<RenderedTemplate> RenderAsync(
        string permalink,
        IDictionary<string, object?>? templateModel = null,
        CancellationToken cancellationToken = default);
}

internal sealed class TemplatesResource : ITemplatesResource
{
    private const string BasePath = "/api/v2/server/templates";

    private readonly ApiConnection _connection;

    internal TemplatesResource(ApiConnection connection) => _connection = connection;

    public async Task<IReadOnlyList<Template>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<TemplatesData>(BasePath, null, cancellationToken)
            .ConfigureAwait(false);
        return data.Templates;
    }

    public async Task<Template> CreateAsync(
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PostAsync<TemplateData>(BasePath, request, cancellationToken)
            .ConfigureAwait(false);
        return data.Template;
    }

    public async Task<Template> GetAsync(
        string permalink,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<TemplateData>(PathFor(permalink), null, cancellationToken)
            .ConfigureAwait(false);
        return data.Template;
    }

    public async Task<Template> UpdateAsync(
        string permalink,
        UpdateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PatchAsync<TemplateData>(PathFor(permalink), request, cancellationToken)
            .ConfigureAwait(false);
        return data.Template;
    }

    public async Task<Template> ArchiveAsync(
        string permalink,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .PostAsync<TemplateData>($"{PathFor(permalink)}/archive", null, cancellationToken)
            .ConfigureAwait(false);
        return data.Template;
    }

    public async Task<RenderedTemplate> RenderAsync(
        string permalink,
        IDictionary<string, object?>? templateModel = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .PostAsync<RenderData>(
                $"{PathFor(permalink)}/render",
                new RenderRequest { TemplateModel = templateModel },
                cancellationToken)
            .ConfigureAwait(false);
        return data.Rendered;
    }

    private static string PathFor(string permalink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permalink);
        return $"{BasePath}/{Uri.EscapeDataString(permalink)}";
    }

    internal sealed record TemplatesData
    {
        public IReadOnlyList<Template> Templates { get; init; } = [];
    }

    internal sealed record TemplateData
    {
        public Template Template { get; init; } = new();
    }

    internal sealed record RenderRequest
    {
        public IDictionary<string, object?>? TemplateModel { get; init; }
    }

    internal sealed record RenderData
    {
        public RenderedTemplate Rendered { get; init; } = new();
    }
}
