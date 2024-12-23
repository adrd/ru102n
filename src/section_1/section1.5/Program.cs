using System.Diagnostics;
using StackExchange.Redis;

ConfigurationOptions options = new ConfigurationOptions
{
    EndPoints = { "localhost:6379" }
};

ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect(options);
IDatabase db = muxer.GetDatabase();

Stopwatch stopwatch = Stopwatch.StartNew();
// TODO for Coding Challenge Start here on starting-point branch
// un-pipelined commands incur the added cost of an extra network round trip
for (int i = 0; i < 1000; i++)
{
    await db.PingAsync();
}

Console.WriteLine($"1000 un-pipelined commands took: {stopwatch.ElapsedMilliseconds}ms to execute");

// If we run out async tasks to StackExchange.Redis concurrently, the library
// will automatically manage pipelining of these commands to Redis, making
// them significantly more performant as we remove most of the network round trips to Redis.
List<Task<TimeSpan>> pingTasks = new List<Task<TimeSpan>>();

//restart stopwatch
stopwatch.Restart();

for (int i = 0; i < 1000; i++)
{
    pingTasks.Add(db.PingAsync());
}

await Task.WhenAll(pingTasks);

Console.WriteLine($"1000 automatically pipelined tasks took: {stopwatch.ElapsedMilliseconds}ms to execute, first result: {pingTasks[0].Result}");

// clear our ping tasks list.
pingTasks.Clear();

// Batches allow you to more intentionally group together the commands that you want to send to Redis.
// If you use a batch, all commands in the batch will be sent to Redis in one contiguous block, with no
// other commands from the client interleaved. Of course, if there are other clients sending commands to Redis, 
// commands from those other clients may be interleaved with your batched commands.
IBatch batch = db.CreateBatch();

// restart stopwatch
stopwatch.Restart();

for (int i = 0; i < 1000; i++)
{
    pingTasks.Add(batch.PingAsync());
}

batch.Execute();
await Task.WhenAll(pingTasks);
Console.WriteLine($"1000 batched commands took: {stopwatch.ElapsedMilliseconds}ms to execute, first result: {pingTasks[0].Result}");
// end Challenge