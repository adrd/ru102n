using StackExchange.Redis;

ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect("localhost");
IDatabase db = muxer.GetDatabase();

// TODO for Coding Challenge Start here on starting-point branch
ISubscriber subscriber = muxer.GetSubscriber();
CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
CancellationToken token = cancellationTokenSource.Token;

ChannelMessageQueue channel = await subscriber.SubscribeAsync("test-channel");

channel.OnMessage(msg =>
{
    Console.WriteLine($"Sequentially received: {msg.Message} on channel: {msg.Channel}");
});

await subscriber.SubscribeAsync("test-channel", (channel, value) =>
{
    Console.WriteLine($"Received: {value} on channel: {channel}");
});

Task basicSendTask = Task.Run(async () =>
{
    int i = 0;
    while (!token.IsCancellationRequested)
    {
        await db.PublishAsync("test-channel", i++);
        await Task.Delay(1000);
    }
});

await subscriber.SubscribeAsync("pattern:*", (channel, value) =>
{
    Console.WriteLine($"Received: {value} on channel: {channel}");
});


Task patternSendTask = Task.Run(async () =>
{
    int i = 0;
    while (!token.IsCancellationRequested)
    {
        await db.PublishAsync($"pattern:{Guid.NewGuid()}", i++);
        await Task.Delay(1000);
    }
});

// put all other producer/subscriber stuff above here.
Console.ReadKey();
// put cancellation & unsubscribe down here.

Console.WriteLine("Unsubscribing to a single channel");
await channel.UnsubscribeAsync();
Console.ReadKey();

Console.WriteLine("Unsubscribing whole subscriber from test-channel");
await subscriber.UnsubscribeAsync("test-channel");
Console.ReadKey();

Console.WriteLine("Unsubscribing from all");
await subscriber.UnsubscribeAllAsync();
Console.ReadKey();
// end coding challenge
