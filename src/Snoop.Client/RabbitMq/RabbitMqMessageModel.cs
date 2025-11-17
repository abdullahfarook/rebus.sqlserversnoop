using System;
using System.Collections.Generic;

namespace Snoop.Client.RabbitMq
{
    public class RabbitMqMessageModel
    {
        public string MessageId { get; set; }
        public Dictionary<string, object> Headers { get; set; }
        public byte[] Body { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public DateTime Timestamp { get; set; }
        public string ContentType { get; set; }
        public string ContentEncoding { get; set; }
        public long DeliveryTag { get; set; }
        public bool Redelivered { get; set; }

        public RabbitMqMessageModel()
        {
            Headers = new Dictionary<string, object>();
        }
    }
}


