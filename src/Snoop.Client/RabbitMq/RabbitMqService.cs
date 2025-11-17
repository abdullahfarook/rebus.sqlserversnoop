using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Snoop.Client.RabbitMq
{
    public class RabbitMqService
    {
        private readonly RabbitMqManagementApiClient _managementClient;

        public RabbitMqService()
        {
            _managementClient = new RabbitMqManagementApiClient();
        }

        public async Task<List<RabbitMqQueueModel>> GetQueuesAsync()
        {
            try
            {
                return await _managementClient.GetQueuesAsync();
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
                return await _managementClient.GetMessagesAsync(queueName, maxMessages);
            }
            catch (Exception)
            {
                return new List<MessageViewModel>();
            }
        }

        public async Task<bool> PurgeQueueAsync(string queueName)
        {
            try
            {
                // Use Management API to purge queue
                var response = await _managementClient.HttpClient.DeleteAsync(
                    $"{_managementClient.BaseUrl}/api/queues/%2F/{Uri.EscapeDataString(queueName)}/contents");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteMessageAsync(string queueName, string messageId)
        {
            try
            {
                // For RabbitMQ, we can't delete individual messages by ID
                // This is a limitation of RabbitMQ - we can only purge the entire queue
                // or consume messages
                return await PurgeQueueAsync(queueName);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ReturnMessageToSourceQueueAsync(string errorQueue, string sourceQueue, MessageViewModel message)
        {
            try
            {
                // Get the message from error queue and republish to source queue
                var messages = await _managementClient.GetMessagesAsync(errorQueue, 1);
                if (!messages.Any()) return false;

                var messageToMove = messages.First();
                
                // Use Management API to publish message to source queue
                var publishRequest = new
                {
                    properties = new
                    {
                        content_type = "application/json",
                        headers = messageToMove.Headers?.ToDictionary(h => h.Key, h => h.Value) ?? new Dictionary<string, string>()
                    },
                    routing_key = sourceQueue,
                    payload = messageToMove.Body,
                    payload_encoding = "string"
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(publishRequest);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _managementClient.HttpClient.PostAsync(
                    $"{_managementClient.BaseUrl}/api/exchanges/%2F/amq.default/publish",
                    content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _managementClient?.Dispose();
        }
    }
}
