namespace CamelMailer;

/// <summary>
/// Template layouts. A layout wraps every template that uses it, so header,
/// footer and styling live in one place instead of in each template.
/// </summary>
public interface ILayoutsResource
{
    /// <summary>Lists all layouts of the server.</summary>
    Task<IReadOnlyList<Layout>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a layout. <see cref="CreateLayoutRequest.HtmlWrapper"/> has to
    /// embed the body with <c>{{{ content }}}</c>.
    /// </summary>
    Task<Layout> CreateAsync(
        CreateLayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches a layout by permalink.</summary>
    Task<Layout> GetAsync(string permalink, CancellationToken cancellationToken = default);

    /// <summary>Updates a layout; only the given fields change.</summary>
    Task<Layout> UpdateAsync(
        string permalink,
        UpdateLayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a layout. Templates that referenced it fall back to no wrapper.</summary>
    Task<DeleteResult> DeleteAsync(
        string permalink,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads the layout's logo, given as a <c>data:image/png;base64,…</c> URL,
    /// and returns the absolute URL to reference from the wrapper.
    /// </summary>
    Task<LayoutLogo> UploadLogoAsync(
        string permalink,
        string dataUrl,
        CancellationToken cancellationToken = default);
}

internal sealed class LayoutsResource : ILayoutsResource
{
    private const string BasePath = "/api/v2/server/layouts";

    private readonly ApiConnection _connection;

    internal LayoutsResource(ApiConnection connection) => _connection = connection;

    public async Task<IReadOnlyList<Layout>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<LayoutsData>(BasePath, null, cancellationToken)
            .ConfigureAwait(false);
        return data.Layouts;
    }

    public async Task<Layout> CreateAsync(
        CreateLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PostAsync<LayoutData>(BasePath, request, cancellationToken)
            .ConfigureAwait(false);
        return data.Layout;
    }

    public async Task<Layout> GetAsync(
        string permalink,
        CancellationToken cancellationToken = default)
    {
        var data = await _connection
            .GetAsync<LayoutData>(PathFor(permalink), null, cancellationToken)
            .ConfigureAwait(false);
        return data.Layout;
    }

    public async Task<Layout> UpdateAsync(
        string permalink,
        UpdateLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = await _connection
            .PatchAsync<LayoutData>(PathFor(permalink), request, cancellationToken)
            .ConfigureAwait(false);
        return data.Layout;
    }

    public Task<DeleteResult> DeleteAsync(
        string permalink,
        CancellationToken cancellationToken = default)
        => _connection.DeleteAsync<DeleteResult>(PathFor(permalink), cancellationToken);

    public Task<LayoutLogo> UploadLogoAsync(
        string permalink,
        string dataUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataUrl);
        var body = new LogoBody { DataUrl = dataUrl };
        return _connection.PostAsync<LayoutLogo>(
            $"{PathFor(permalink)}/logo", body, cancellationToken);
    }

    private static string PathFor(string permalink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permalink);
        return $"{BasePath}/{Uri.EscapeDataString(permalink)}";
    }

    internal sealed record LogoBody
    {
        public string DataUrl { get; init; } = string.Empty;
    }

    internal sealed record LayoutsData
    {
        public IReadOnlyList<Layout> Layouts { get; init; } = [];
    }

    internal sealed record LayoutData
    {
        public Layout Layout { get; init; } = new();
    }
}
