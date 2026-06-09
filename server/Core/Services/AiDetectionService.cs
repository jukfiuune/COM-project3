using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace COM_project3.Core.Services
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
                var detections = JsonSerializer.Deserialize<List<TrashDetection>>(responseString, options);

                return detections ?? new List<TrashDetection>();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error occurred during AI trash detection.");
                return new List<TrashDetection>(); // gracefully fallback
            }
        }
    }
}
