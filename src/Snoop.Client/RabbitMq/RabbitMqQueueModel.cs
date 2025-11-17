using System;

namespace Snoop.Client.RabbitMq
{
    public class RabbitMqQueueModel
    {
        public string QueueName { get; set; }
        public uint MessageCount { get; set; }
        public uint ConsumerCount { get; set; }
        public string ConnectionString { get; set; }

        public RabbitMqQueueModel(string queueName, uint messageCount, uint consumerCount, string connectionString)
        {
            QueueName = queueName;
            MessageCount = messageCount;
            ConsumerCount = consumerCount;
            ConnectionString = connectionString;
        }
    }
}


