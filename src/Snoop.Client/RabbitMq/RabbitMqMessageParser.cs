using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rebus.Compression;
using RebusHeaders = Rebus.Messages.Headers;

namespace Snoop.Client.RabbitMq
{
    public static class RabbitMqMessageParser
    {
        public static MessageViewModel ParseMessage(BasicDeliverEventArgs ea, IModel channel)
        {
            try
            {
                var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
                var headers = ParseHeaders(ea.BasicProperties);
                var body = DecodeBody(ea.Body.ToArray(), headers);

                // Extract Rebus-specific information from headers
                var messageType = headers.ContainsKey(RebusHeaders.Type) ? headers[RebusHeaders.Type] : "Unknown";
                var sourceQueue = headers.ContainsKey(RebusHeaders.SourceQueue) ? headers[RebusHeaders.SourceQueue] : ea.RoutingKey;
                var sentTime = ParseSentTime(headers);
                var errorDetails = headers.ContainsKey(RebusHeaders.ErrorDetails) ? headers[RebusHeaders.ErrorDetails] : null;

                // Convert headers to MessageHeaderViewModel list
                var headerViewModels = headers
                    .Where(x => x.Key != RebusHeaders.ErrorDetails)
                    .Select(item => new MessageHeaderViewModel(item.Key, item.Value))
                    .ToList();

                return new MessageViewModel(
                    long.Parse(messageId.GetHashCode().ToString()), // Convert string ID to long for compatibility
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

        public static MessageViewModel ParseMessage(BasicGetResult result, IModel channel)
        {
            try
            {
                var messageId = result.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
                var headers = ParseHeaders(result.BasicProperties);
                var body = DecodeBody(result.Body.ToArray(), headers);

                // Extract Rebus-specific information from headers
                var messageType = headers.ContainsKey(RebusHeaders.Type) ? headers[RebusHeaders.Type] : "Unknown";
                var sourceQueue = headers.ContainsKey(RebusHeaders.SourceQueue) ? headers[RebusHeaders.SourceQueue] : result.RoutingKey;
                var sentTime = ParseSentTime(headers);
                var errorDetails = headers.ContainsKey(RebusHeaders.ErrorDetails) ? headers[RebusHeaders.ErrorDetails] : null;

                // Convert headers to MessageHeaderViewModel list
                var headerViewModels = headers
                    .Where(x => x.Key != RebusHeaders.ErrorDetails)
                    .Select(item => new MessageHeaderViewModel(item.Key, item.Value))
                    .ToList();

                return new MessageViewModel(
                    long.Parse(messageId.GetHashCode().ToString()), // Convert string ID to long for compatibility
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

        private static Dictionary<string, string> ParseHeaders(IBasicProperties properties)
        {
            var headers = new Dictionary<string, string>();

            if (properties.Headers != null)
            {
                foreach (var header in properties.Headers)
                {
                    var key = header.Key;
                    var value = header.Value;

                    if (value is byte[] bytes)
                    {
                        headers[key] = Encoding.UTF8.GetString(bytes);
                    }
                    else if (value is string str)
                    {
                        headers[key] = str;
                    }
                    else
                    {
                        headers[key] = value?.ToString() ?? string.Empty;
                    }
                }
            }

            // Add standard properties as headers
            if (!string.IsNullOrEmpty(properties.ContentType))
                headers[RebusHeaders.ContentType] = properties.ContentType;

            if (!string.IsNullOrEmpty(properties.ContentEncoding))
                headers[RebusHeaders.ContentEncoding] = properties.ContentEncoding;

            if (!string.IsNullOrEmpty(properties.MessageId))
                headers["message-id"] = properties.MessageId;

            if (!string.IsNullOrEmpty(properties.CorrelationId))
                headers["correlation-id"] = properties.CorrelationId;

            if (properties.Timestamp.UnixTime > 0)
                headers["timestamp"] = DateTimeOffset.FromUnixTimeSeconds(properties.Timestamp.UnixTime).ToString("O");

            return headers;
        }

        private static string DecodeBody(byte[] body, Dictionary<string, string> headers)
        {
            try
            {
                if (body == null || body.Length == 0)
                {
                    return "Empty message body";
                }

                var bytes = body;

                // Check if body is compressed
                var isGzipped = headers.ContainsKey(RebusHeaders.ContentEncoding) &&
                               string.Equals(headers[RebusHeaders.ContentEncoding], "gzip", StringComparison.InvariantCultureIgnoreCase);

                if (isGzipped)
                {
                    bytes = new Zipper().Unzip(bytes);
                }

                // Determine encoding
                var contentType = headers.ContainsKey(RebusHeaders.ContentType) ? headers[RebusHeaders.ContentType] : "application/json";
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

        private static Encoding GetEncoding(string contentType)
        {
            var contentTypeSettings = contentType
                .Split(';')
                .Select(token => token.Split('='))
                .Where(tokens => tokens.Length == 2)
                .ToDictionary(tokens => tokens[0].Trim(), tokens => tokens[1].Trim(), StringComparer.InvariantCultureIgnoreCase);

            var encoding = contentTypeSettings.ContainsKey("charset")
                ? Encoding.GetEncoding(contentTypeSettings["charset"])
                : Encoding.UTF8;

            return encoding;
        }

        private static DateTimeOffset ParseSentTime(Dictionary<string, string> headers)
        {
            if (headers.ContainsKey(RebusHeaders.SentTime))
            {
                var sentTimeString = headers[RebusHeaders.SentTime];
                // Remove timezone info if present for parsing
                if (sentTimeString.Contains(':'))
                {
                    sentTimeString = sentTimeString.Substring(0, sentTimeString.LastIndexOf(':'));
                }

                if (DateTimeOffset.TryParse(sentTimeString, out var sentTime))
                {
                    return sentTime;
                }
            }

            if (headers.ContainsKey("timestamp"))
            {
                if (DateTimeOffset.TryParse(headers["timestamp"], out var timestamp))
                {
                    return timestamp;
                }
            }

            return DateTimeOffset.Now;
        }

        private static string FormatJson(string input)
        {
            try
            {
                return JsonConvert.SerializeObject(JsonConvert.DeserializeObject<JObject>(input), Formatting.Indented);
            }
            catch
            {
                try
                {
                    // Try parsing as array
                    return JsonConvert.SerializeObject(JsonConvert.DeserializeObject<JArray>(input), Formatting.Indented);
                }
                catch
                {
                    // Return as-is if not valid JSON
                    return input;
                }
            }
        }
    }
}
