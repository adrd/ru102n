using StackExchange.Redis;

// TODO for Coding Challenge Start here on starting-point branch
ConfigurationOptions options = new ConfigurationOptions
{
    // add and update parameters as needed
    EndPoints = {"localhost:6379"}
};

// initialize a multiplexer with ConnectionMultiplexer.Connect()
ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect(options);

// get an IDatabase here with GetDatabase
IDatabase db = muxer.GetDatabase();

// add ping here
Console.WriteLine($"ping: {db.Ping().TotalMilliseconds} ms");
Console.WriteLine($"database: {db.Database} ");
// end programming challenge

// info about server
IServer server = muxer.GetServer("localhost", 6379);
Console.WriteLine($"server version: {server.Version} ");
Console.WriteLine($"server type {server.ServerType} ");

// info about subscriber
ISubscriber subscriber = muxer.GetSubscriber();
