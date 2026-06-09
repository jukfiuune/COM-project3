using System.Net;
using System.Text;
using System.Text.Json;
using Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests;

public sealed class AiDetectionServiceTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? RequestMessage { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);
        public Exception? ExceptionToThrow { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMessage = request;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(ResponseToReturn);
        }
    }

    private static IConfiguration GetConfig(string url)
    {
        var dict = new Dictionary<string, string?> { { "AiServer:Url", url } };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task DetectTrashAsync_ReturnsDetections_WhenSuccessful()
    {
        var fakeHandler = new FakeHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new[]
                {
                    new { label = "plastic", confidence = 0.9f },
                    new { label = "glass", confidence = 0.8f }
                }), Encoding.UTF8, "application/json")
            }
        };
        var httpClient = new HttpClient(fakeHandler);
        var config = GetConfig("http://ai-server:8000");
        var service = new AiDetectionService(httpClient, config, NullLogger<AiDetectionService>.Instance);

        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.DetectTrashAsync(ms, "test.jpg");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("plastic", result[0].Label);
        Assert.Equal(0.9f, result[0].Confidence);
        Assert.Equal("glass", result[1].Label);
        Assert.Equal(0.8f, result[1].Confidence);
    }

    [Fact]
    public async Task DetectTrashAsync_ReturnsEmpty_WhenHttpFails()
    {
        var fakeHandler = new FakeHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        var httpClient = new HttpClient(fakeHandler);
        var config = GetConfig("http://ai-server:8000");
        var service = new AiDetectionService(httpClient, config, NullLogger<AiDetectionService>.Instance);

        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.DetectTrashAsync(ms, "test.jpg");

        Assert.NotNull(result);
        Assert.Empty(result); // Service should swallow the error and return empty list
    }

    [Fact]
    public async Task DetectTrashAsync_ReturnsEmpty_WhenNetworkException()
    {
        var fakeHandler = new FakeHttpMessageHandler
        {
            ExceptionToThrow = new HttpRequestException("Network down")
        };
        var httpClient = new HttpClient(fakeHandler);
        var config = GetConfig("http://ai-server:8000");
        var service = new AiDetectionService(httpClient, config, NullLogger<AiDetectionService>.Instance);

        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.DetectTrashAsync(ms, "test.jpg");

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
