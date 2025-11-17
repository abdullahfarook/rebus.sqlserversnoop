using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Snoop.Client.RabbitMq
{
    public class RabbitMqManagementApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        // Expose these for external use
        public HttpClient HttpClient => _httpClient;
        public string BaseUrl => _baseUrl;

        public RabbitMqManagementApiClient(string managementUrl = "http://localhost:15672", string username = "guest", string password = "guest")
        {
            _baseUrl = managementUrl.TrimEnd('/');
            _username = username;
            _password = password;
            
            _httpClient = new HttpClient();
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }

        public async Task<List<RabbitMqQueueModel>> GetQueuesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{_baseUrl}/api/queues");
                var queues = JsonConvert.DeserializeObject<List<RabbitMqQueueInfo>>(response);
                
                return queues.Select(q => new RabbitMqQueueModel(
                    q.Name, 
                    q.Messages, 
                    q.Consumers, 
                    $"amqp://{_username}:{_password}@localhost:5672")).ToList();
            }
            catch (Exception)
            {
                return new List<RabbitMqQueueModel>();
            }
        }

        public async Task<List<MessageViewModel>> GetMessagesAsync(string queueName, int maxMessages = 100)
        {
            try
            {
                var messages = new List<MessageViewModel>();
                
                // Get messages from the queue using the management API
                var requestBody = JsonConvert.SerializeObject(new { count = maxMessages, ackmode = "ack_requeue_true", encoding = "base64" });
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/queues/%2F/{Uri.EscapeDataString(queueName)}/get",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var messageData = JsonConvert.DeserializeObject<List<RabbitMqMessageInfo>>(responseContent);
                    
                    if (messageData != null)
                    {
                        foreach (var msg in messageData)
                        {
                            var message = ParseMessageFromManagementApi(msg);
                            if (message != null)
                            {
                                messages.Add(message);
                            }
                        }
                    }
                }
                else
                {
                    // Log the error for debugging
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"RabbitMQ Management API Error: {response.StatusCode} - {errorContent}");
                }

                return messages;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"RabbitMQ Management API Exception: {ex.Message}");
                return new List<MessageViewModel>();
            }
        }

        private MessageViewModel ParseMessageFromManagementApi(RabbitMqMessageInfo messageInfo)
        {
            try
            {
                var messageId = messageInfo.Properties?.MessageId ?? Guid.NewGuid().ToString();
                var headers = ParseHeadersFromManagementApi(messageInfo.Properties);
                var body = DecodeBodyFromManagementApi(messageInfo.Payload, headers);

                // Extract Rebus-specific information from headers
                var messageType = headers.ContainsKey("rbs2-msg-type") ? headers["rbs2-msg-type"] : "Unknown";
                messageType = messageType.Split(',').FirstOrDefault();
                var sourceQueue = headers.ContainsKey("rbs2-source-queue") ? headers["rbs2-source-queue"] : messageInfo.RoutingKey ?? "Unknown";
                var sentTime = ParseSentTimeFromHeaders(headers);
                var errorDetails = headers.ContainsKey("rbs2-error-details") ? headers["rbs2-error-details"] : null;

                // Convert headers to MessageHeaderViewModel list
                var headerViewModels = headers
                    .Where(x => x.Key != "rbs2-error-details")
                    .Select(item => new MessageHeaderViewModel(item.Key, item.Value))
                    .ToList();

                return new MessageViewModel(
                    long.Parse(messageId.GetHashCode().ToString()),
                    headerViewModels,
                    messageType,
                    sourceQueue,
                    sentTime,
                    null, // RabbitMQ doesn't have visible time concept
                    null, // RabbitMQ doesn't have expiration time in the same way
                    body,
                    errorDetails);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Dictionary<string, string> ParseHeadersFromManagementApi(RabbitMqMessageProperties properties)
        {
            var headers = new Dictionary<string, string>();

            if (properties?.Headers != null)
            {
                foreach (var header in properties.Headers)
                {
                    var key = header.Key;
                    var value = header.Value?.ToString() ?? string.Empty;
                    headers[key] = value;
                }
            }

            // Add standard properties as headers
            if (!string.IsNullOrEmpty(properties?.ContentType))
                headers["content-type"] = properties.ContentType;

            if (!string.IsNullOrEmpty(properties?.ContentEncoding))
                headers["content-encoding"] = properties.ContentEncoding;

            if (!string.IsNullOrEmpty(properties?.MessageId))
                headers["message-id"] = properties.MessageId;

            if (!string.IsNullOrEmpty(properties?.CorrelationId))
                headers["correlation-id"] = properties.CorrelationId;

            return headers;
        }

        private string DecodeBodyFromManagementApi(string payload, Dictionary<string, string> headers)
        {
            try
            {
                if (string.IsNullOrEmpty(payload))
                {
                    return "Empty message body";
                }

                // Decode base64 payload
                var bytes = Convert.FromBase64String(payload);

                // Check if body is compressed
                var isGzipped = headers.ContainsKey("content-encoding") &&
                               string.Equals(headers["content-encoding"], "gzip", StringComparison.InvariantCultureIgnoreCase);

                if (isGzipped)
                {
                    bytes = new Rebus.Compression.Zipper().Unzip(bytes);
                }

                // Determine encoding
                var contentType = headers.ContainsKey("content-type") ? headers["content-type"] : "application/json";
                var encoding = GetEncoding(contentType);
                var str = encoding.GetString(bytes);

                // Try to format as JSON
                return FormatJson(str);
            }
            catch (Exception e)
            {
                return $"Error decoding message body: {e.Message}";
            }
        }

        private System.Text.Encoding GetEncoding(string contentType)
        {
            var contentTypeSettings = contentType
                .Split(';')
                .Select(token => token.Split('='))
                .Where(tokens => tokens.Length == 2)
                .ToDictionary(tokens => tokens[0].Trim(), tokens => tokens[1].Trim(), StringComparer.InvariantCultureIgnoreCase);

            var encoding = contentTypeSettings.ContainsKey("charset")
                ? System.Text.Encoding.GetEncoding(contentTypeSettings["charset"])
                : System.Text.Encoding.UTF8;

            return encoding;
        }

        private DateTimeOffset ParseSentTimeFromHeaders(Dictionary<string, string> headers)
        {
            if (headers.ContainsKey("rbs2-sent-time"))
            {
                var sentTimeString = headers["rbs2-sent-time"];
                if (DateTimeOffset.TryParse(sentTimeString, out var sentTime))
                {
                    return sentTime;
                }
            }

            return DateTimeOffset.Now;
        }

        private string FormatJson(string input)
        {
            try
            {
                return JsonConvert.SerializeObject(JsonConvert.DeserializeObject<JObject>(input), Formatting.Indented);
            }
            catch
            {
                try
                {
                    return JsonConvert.SerializeObject(JsonConvert.DeserializeObject<JArray>(input), Formatting.Indented);
                }
                catch
                {
                    return input;
                }
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/overview");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RabbitMQ Management API Connection Test Failed: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // DTOs for the management API responses
    public class RabbitMqQueueInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("messages")]
        public uint Messages { get; set; }

        [JsonProperty("consumers")]
        public uint Consumers { get; set; }
    }

    public class RabbitMqMessageInfo
    {
        [JsonProperty("payload")]
        public string Payload { get; set; }

        [JsonProperty("routing_key")]
        public string RoutingKey { get; set; }

        [JsonProperty("properties")]
        public RabbitMqMessageProperties Properties { get; set; }
    }

    public class RabbitMqMessageProperties
    {
        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("content_encoding")]
        public string ContentEncoding { get; set; }

        [JsonProperty("message_id")]
        public string MessageId { get; set; }

        [JsonProperty("correlation_id")]
        public string CorrelationId { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, object> Headers { get; set; }
    }
}
