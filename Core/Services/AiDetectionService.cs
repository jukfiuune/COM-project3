using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    public class AiDetectionService : IAiDetectionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiDetectionService> _logger;
        private readonly string _aiServerUrl;

        public AiDetectionService(HttpClient httpClient, IConfiguration configuration, ILogger<AiDetectionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _aiServerUrl = configuration["AiServer:Url"] ?? throw new System.InvalidOperationException("AiServer:Url configuration is missing.");
        }

        public async Task<List<TrashDetection>> DetectTrashAsync(Stream imageStream, string fileName)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_aiServerUrl}/detect");
                using var content = new MultipartFormDataContent();
                
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                content.Add(streamContent, "file", fileName);
                
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                var result = JsonSerializer.Deserialize<AiDetectionResponse>(responseString, options);
                if (result?.Detections == null)
                {
                    return new List<TrashDetection>();
                }

                var mapped = new List<TrashDetection>();
                foreach (var d in result.Detections)
                {
                    mapped.Add(new TrashDetection
                    {
                        label = d.Label,
                        confidence = (float)d.Confidence,
                        box = d.Bbox != null ? new List<float> { d.Bbox.X1, d.Bbox.Y1, d.Bbox.X2, d.Bbox.Y2 } : new List<float>(),
                        annotatedImage = result.annotated_image
                    });
                }

                return mapped;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred during AI trash detection.");
                return new List<TrashDetection>(); // gracefully fallback
            }
        }

        private class AiDetectionResponse
        {
            public List<AiDetectionItem>? Detections { get; set; }
            public string? annotated_image { get; set; }
        }

        private class AiDetectionItem
        {
            public string Label { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public AiBoundingBox? Bbox { get; set; }
            public int ClsId { get; set; }
        }

        private class AiBoundingBox
        {
            public int X1 { get; set; }
            public int Y1 { get; set; }
            public int X2 { get; set; }
            public int Y2 { get; set; }
        }
    }
}
