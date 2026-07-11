using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CamelMailer;

/// <summary>Registers CamelMailer with <c>Microsoft.Extensions.DependencyInjection</c>.</summary>
public static class CamelMailerServiceCollectionExtensions
{
    /// <summary>The logical name of the <see cref="HttpClient" /> used by the SDK.</summary>
    public const string HttpClientName = "CamelMailer";

    /// <summary>
    /// Registers <see cref="ICamelMailerClient" /> backed by a named
    /// <see cref="HttpClient" /> from <see cref="IHttpClientFactory" />.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the API key and, optionally, the base URL.</param>
    /// <returns>
    /// The <see cref="IHttpClientBuilder" /> of the underlying client, so
    /// resilience handlers, timeouts etc. can be added.
    /// </returns>
    public static IHttpClientBuilder AddCamelMailer(
        this IServiceCollection services,
        Action<CamelMailerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<CamelMailerOptions>().Configure(configure);
        var builder = services.AddHttpClient(HttpClientName);
        services.AddTransient<ICamelMailerClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CamelMailerOptions>>().Value;
            var httpClient = provider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName);
            return new CamelMailerClient(options, httpClient);
        });
        return builder;
    }
}
