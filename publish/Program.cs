﻿using System;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Routing.TypeBased;

using (var activator = new BuiltinHandlerActivator())
{
    //activator.Register(() => new Handler());

    Configure.With(activator)
             .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
             .Transport(t => t.UseRabbitMq("amqp://guest:guest@localhost:5672", "publisher"))
             .Routing(r => r.TypeBased().MapAssemblyOf<string>("subscriber"))
             .Start();

    // Subscribe to messages BEFORE publishing
    Console.WriteLine("Setting up subscription...");
    await activator.Bus.Subscribe<string>();
    Console.WriteLine("Subscription established!");

    // Wait a moment to ensure subscription is ready
    await Task.Delay(1000);

    Console.WriteLine("Starting to publish 100 messages...");
    
    // Send many messages
    for (int i = 1; i <= 100; i++)
    {
        var message = $"Message number {i} - Hello from Publisher!";
        await activator.Bus.Publish(message);
        Console.WriteLine($"Published: {message}");
        
        // Small delay to avoid overwhelming the system
        await Task.Delay(50);
    }
    
    Console.WriteLine("Finished publishing 100 messages!");
    Console.WriteLine("Waiting for messages to be processed...");
    
    // Wait for all messages to be processed
    await Task.Delay(5000);

    Console.WriteLine("This is Subscriber 2");
    Console.WriteLine("Press ENTER to quit");
    Console.ReadLine();
    Console.WriteLine("Quitting...");
}
class Handler : IHandleMessages<string>
{
    private static int _messageCount = 0;
    
    public Task Handle(string message)
    {
        _messageCount++;
        Console.WriteLine($"[{_messageCount:D3}] Received: {message}");
        
        // If we've received all 100 messages, notify
        if (_messageCount == 100)
        {
            Console.WriteLine("*** All 100 messages have been received! ***");
        }
        
        return Task.CompletedTask;
    }
}