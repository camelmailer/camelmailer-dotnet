using System.Net;

namespace CamelMailer.Tests;

public class ErrorHandlingTests
{
    [Fact]
    public async Task ErrorEnvelope_ThrowsTypedExceptionWithCodeAndStatus()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.UnprocessableEntity,
            ResponseBody = TestEnvelope.Error("ValidationError", "The From address is not authorised"),
        };
        using var client = TestClient.Create(handler);

        var exception = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Emails.SendAsync(new SendEmailRequest
            {
                From = "nope@other.com",
                To = ["ada@example.com"],
                TextBody = "x",
            }));

        Assert.Equal("ValidationError", exception.ErrorCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
        Assert.Contains("The From address is not authorised", exception.Message);
    }

    [Fact]
    public async Task Unauthorized_SurfacesCode()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Unauthorized,
            ResponseBody = TestEnvelope.Error("Unauthorized", "Invalid API key"),
        };
        using var client = TestClient.Create(handler);

        var exception = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Stats.GetAsync());

        Assert.Equal("Unauthorized", exception.ErrorCode);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task NotFound_SurfacesCode()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.NotFound,
            ResponseBody = TestEnvelope.Error("NotFound", "No such message"),
        };
        using var client = TestClient.Create(handler);

        var exception = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Emails.GetAsync(999));

        Assert.Equal("NotFound", exception.ErrorCode);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task NonJsonErrorBody_ThrowsWithStatusCodeAndNoErrorCode()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.InternalServerError,
            ResponseBody = "<html>Bad gateway</html>",
            ResponseContentType = "text/html",
        };
        using var client = TestClient.Create(handler);

        var exception = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Stats.GetAsync());

        Assert.Null(exception.ErrorCode);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
    }

    [Fact]
    public async Task ErrorEnvelopeWithOkStatusCode_StillThrows()
    {
        var handler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.OK,
            ResponseBody = TestEnvelope.Error("StorageUnconfigured", "message store not configured"),
        };
        using var client = TestClient.Create(handler);

        var exception = await Assert.ThrowsAsync<CamelMailerException>(
            () => client.Stats.GetAsync());

        Assert.Equal("StorageUnconfigured", exception.ErrorCode);
    }

    [Fact]
    public async Task SuccessEnvelopeWithoutData_Throws()
    {
        var handler = new MockHttpMessageHandler
        {
            ResponseBody = """{"status":"success","time":0.001}""",
        };
        using var client = TestClient.Create(handler);

        await Assert.ThrowsAsync<CamelMailerException>(() => client.Stats.GetAsync());
    }

    [Fact]
    public async Task ConnectionFailure_ThrowsNetworkException()
    {
        var handler = new MockHttpMessageHandler
        {
            ExceptionToThrow = new HttpRequestException("connection refused"),
        };
        using var client = TestClient.Create(handler);

        var exception = await Assert.ThrowsAsync<CamelMailerNetworkException>(
            () => client.Stats.GetAsync());

        Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Null(exception.StatusCode);
        Assert.Null(exception.ErrorCode);
    }

    [Fact]
    public async Task NetworkException_IsACamelMailerException()
    {
        var handler = new MockHttpMessageHandler
        {
            ExceptionToThrow = new HttpRequestException("boom"),
        };
        using var client = TestClient.Create(handler);

        await Assert.ThrowsAsync<CamelMailerNetworkException>(() => client.Stats.GetAsync());

        handler.ExceptionToThrow = new HttpRequestException("boom again");
        var asBase = await Assert.ThrowsAnyAsync<CamelMailerException>(() => client.Stats.GetAsync());
        Assert.IsType<CamelMailerNetworkException>(asBase);
    }

    [Fact]
    public async Task Timeout_ThrowsNetworkException()
    {
        var handler = new MockHttpMessageHandler
        {
            ExceptionToThrow = new TaskCanceledException("timed out", new TimeoutException()),
        };
        using var client = TestClient.Create(handler);

        await Assert.ThrowsAsync<CamelMailerNetworkException>(() => client.Stats.GetAsync());
    }

    [Fact]
    public async Task CallerCancellation_PropagatesAsOperationCanceled()
    {
        var handler = new MockHttpMessageHandler();
        using var client = TestClient.Create(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Stats.GetAsync(cancellationToken: cts.Token));
    }
}
