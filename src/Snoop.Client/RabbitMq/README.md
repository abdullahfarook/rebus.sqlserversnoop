# RabbitMQ Implementation for Rebus Snoop

This folder contains the RabbitMQ implementation of the Rebus snooping functionality, providing an alternative to the SQL Server implementation.

## Files

- **RabbitMqRebusService.cs** - Main service class for interacting with RabbitMQ queues and messages
- **RabbitMqMessageParser.cs** - Parser for decoding RabbitMQ messages and extracting Rebus-specific information
- **RabbitMqQueueModel.cs** - Model representing a RabbitMQ queue
- **RabbitMqMessageModel.cs** - Model representing a RabbitMQ message
- **RabbitMqQueueViewModel.cs** - View model for displaying queue information in the UI
- **RabbitMqConnectionViewModel.cs** - View model for managing RabbitMQ connections

## Features

### Queue Management
- List all available RabbitMQ queues
- Display message count and consumer count for each queue
- Purge queues (remove all messages)

### Message Operations
- Peek at messages in queues without consuming them
- Parse and display message headers and body
- Support for compressed messages (gzip)
- JSON formatting for message bodies
- Delete individual messages (via queue purge)

### Rebus Integration
- Parse Rebus-specific headers (Type, SourceQueue, SentTime, ErrorDetails)
- Handle Rebus message format and compression
- Support for error queue management

## Usage

### Connection String Format
```
amqp://username:password@hostname:port/vhost
```

Example:
```
amqp://guest:guest@localhost:5672
```

### Basic Usage
```csharp
var service = new RabbitMqRebusService();
var queues = service.GetValidQueues("amqp://guest:guest@localhost:5672");
var messages = service.GetMessages("amqp://guest:guest@localhost:5672", "error", 10);
```

## Dependencies

- RabbitMQ.Client (6.4.0)
- Rebus.RabbitMq (7.0.0)
- Newtonsoft.Json (for message parsing)
- Rebus (for message headers and compression)

## Limitations

1. **Message Deletion**: RabbitMQ doesn't support direct message deletion by ID. The current implementation uses queue purging as an alternative.

2. **Queue Discovery**: The current implementation uses a simplified approach to discover queues. In production, you might want to use the RabbitMQ Management HTTP API for more comprehensive queue discovery.

3. **Message Visibility**: RabbitMQ doesn't have the same "visible" time concept as SQL Server. Messages are immediately available for consumption.

4. **Error Handling**: The implementation includes basic error handling but may need enhancement for production use.

## Setup

1. Install RabbitMQ server or use Docker:
   ```bash
   docker run -d --hostname my-rabbit --name some-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. Access the management console at `http://localhost:15672` (guest/guest)

3. Ensure your Rebus application is configured to use RabbitMQ transport

## Notes

This implementation provides a foundation for snooping RabbitMQ-based Rebus applications. It may need customization based on your specific Rebus configuration and message patterns.


