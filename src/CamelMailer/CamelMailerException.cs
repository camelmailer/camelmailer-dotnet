using System.Net;

namespace CamelMailer;

/// <summary>
/// Thrown when the CamelMailer API answers with an error envelope
/// (<c>{"status":"error","error":{"code":…,"message":…}}</c>) or an
/// otherwise unusable response.
/// </summary>
public class CamelMailerException : Exception
{
    /// <summary>Creates an exception without API error details.</summary>
    public CamelMailerException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception carrying the API error code and HTTP status.</summary>
    public CamelMailerException(string message, string? errorCode, HttpStatusCode? statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    /// <summary>Creates an exception carrying error details and an inner exception.</summary>
    public CamelMailerException(string message, string? errorCode, HttpStatusCode? statusCode, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    /// <summary>
    /// The stable API error code (e.g. <c>Unauthorized</c>, <c>NotFound</c>,
    /// <c>ValidationError</c>, <c>ParameterMissing</c>), or <c>null</c> when
    /// the failure produced no error envelope.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>The HTTP status code of the response, or <c>null</c> when no response was received.</summary>
    public HttpStatusCode? StatusCode { get; }
}

/// <summary>
/// Thrown when the request never produced an API response — DNS failures,
/// refused connections, TLS problems or timeouts. Distinct from
/// <see cref="CamelMailerException" /> errors reported by the API itself.
/// </summary>
public sealed class CamelMailerNetworkException : CamelMailerException
{
    /// <summary>Creates a network exception wrapping the underlying transport error.</summary>
    public CamelMailerNetworkException(string message, Exception innerException)
        : base(message, null, null, innerException)
    {
    }
}
