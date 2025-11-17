# RabbitMQ GUI Implementation

This folder contains the complete WPF GUI implementation for RabbitMQ snooping, mirroring the functionality of the SQL Server implementation.

## Files Created

### View Models
- **RabbitMqMainWindowViewModel.cs** - Main window view model for RabbitMQ connections
- **RabbitMqConnectionViewModel.cs** - Connection management view model
- **RabbitMqQueueViewModel.cs** - Queue management view model (updated)

### Views
- **RabbitMqMainWindow.xaml** - Main window XAML for RabbitMQ interface
- **RabbitMqMainWindow.xaml.cs** - Code-behind for main window
- **LauncherWindow.xaml** - Launcher window to choose between SQL Server and RabbitMQ
- **LauncherWindow.xaml.cs** - Code-behind for launcher

## Features

### Connection Management
- Add new RabbitMQ connections
- Edit existing connection strings
- Save/load connection settings to/from file
- Delete connections

### Queue Operations
- List all available RabbitMQ queues
- Display message count and consumer count for each queue
- Reload messages from queues
- Purge queues (remove all messages)
- Return messages to source queues

### Message Operations
- View messages in a table format
- Display message headers in a separate panel
- Show message body and error details
- Delete individual messages
- Return messages to source queues
- Set visibility (no-op for RabbitMQ compatibility)

## GUI Layout

The RabbitMQ GUI follows the same layout as the SQL Server version:

1. **Left Panel**: Connection management
2. **Top Middle Panel**: Queue list with operations
3. **Bottom Left Panel**: Message list with actions
4. **Top Right Panel**: Message headers
5. **Bottom Right Panel**: Message body and error details

## Usage

1. **Launch Application**: The app now starts with a launcher window
2. **Choose Broker**: Select "RabbitMQ" to open the RabbitMQ snoop interface
3. **Add Connection**: Click "New" and enter your RabbitMQ connection string
4. **Connect**: Click "Save" to connect and load queues
5. **Browse**: Select queues to view messages and perform operations

## Connection String Format

```
amqp://username:password@hostname:port/vhost
```

Example:
```
amqp://guest:guest@localhost:5672
```

## Integration

The RabbitMQ GUI is fully integrated with the existing application:

- Uses the same `BooleanToVisibilityValueConverter`
- Follows the same MVVM pattern with Prism
- Maintains the same UI layout and styling
- Shares the same `MessageViewModel` for consistency

## Settings

Connection settings are saved to `rabbitmqsnoopusersettings.txt` in the application directory, separate from the SQL Server settings file.

## Notes

- The "Set visible now" button is present for interface compatibility but performs no action (RabbitMQ doesn't have visibility concept)
- Message deletion uses queue purging as RabbitMQ doesn't support individual message deletion by ID
- The implementation handles RabbitMQ-specific features like consumer counts




