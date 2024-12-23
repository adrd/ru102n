using StackExchange.Redis;

ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect("localhost");
IDatabase db = muxer.GetDatabase();

// TODO for Coding Challenge Start here on starting-point branch
string sensor1 = "sensor:1";
string sensor2 = "sensor:2";

db.KeyDelete(new RedisKey[]{sensor1, sensor2});
Random rnd = new Random();
Task.Run(async () =>
{
    long numInserted = 0;
    int s1Temp = 28;
    int s2Temp = 5;
    int s1Humid = 35;
    int s2Humid = 87;
    while (true)
    {
        await db.StreamAddAsync(sensor1, new[]
        {
            new NameValueEntry("temp", s1Temp),
            new NameValueEntry("humidity", s1Humid)
            
        });

        await db.StreamAddAsync(sensor2, new[]
        {
            new NameValueEntry("temp", s2Temp),
            new NameValueEntry("humidity", s2Humid)
        });

        await Task.Delay(1000);

        numInserted++;
        if (numInserted % 5 == 0)
        {
            s1Temp = s1Temp + rnd.Next(3) - 2;
            s2Temp = s2Temp + rnd.Next(3) - 2;
            s1Humid = Math.Min(s1Humid + rnd.Next(3) - 2, 100);
            s2Humid = Math.Min(s2Humid + rnd.Next(3) - 2, 100);
        }
    }
});

Task.Run(async () =>
{
    Dictionary<string, StreamPosition> positions = new Dictionary<string, StreamPosition>
    {
        { sensor1, new StreamPosition(sensor1, "0-0") },
        { sensor2, new StreamPosition(sensor2, "0-0") }
    };
    
    while (true)
    {
        RedisStream[] readResults = await db.StreamReadAsync(positions.Values.ToArray(), countPerStream: 1);
        if (!readResults.Any(x => x.Entries.Any()))
        {
            await Task.Delay(1000);
            continue;
        }
        foreach (RedisStream stream in readResults)
        {
            foreach (StreamEntry entry in stream.Entries)
            {
                Console.WriteLine($"{stream.Key} - {entry.Id}: {string.Join(", ", entry.Values)}");
                positions[stream.Key!] = new StreamPosition(stream.Key, entry.Id);
            }
        }
    }
});

string groupName = "tempAverage";
db.StreamCreateConsumerGroup(sensor1, groupName, "0-0");
db.StreamCreateConsumerGroup(sensor2, groupName, "0-0");

Task.Run(async()=>
{
    Dictionary<string, double> tempTotals = new Dictionary<string, double> { { sensor1, 0 }, { sensor2, 0 } };

    Dictionary<string, long> messageCountTotals = new Dictionary<string, long>() { { sensor1, 0 }, { sensor2, 0 } };
    string consumerName = "consumer:1";
    Dictionary<string, StreamPosition> positions = new Dictionary<string, StreamPosition>
    {
        { sensor1, new StreamPosition(sensor1, ">") },
        { sensor2, new StreamPosition(sensor2, ">") }
    };
    
    while (true)
    {
        RedisStream[] result = await db.StreamReadGroupAsync(positions.Values.ToArray(), groupName, consumerName, countPerStream: 1);
        if (!result.Any(x => x.Entries.Any()))
        {
            await Task.Delay(1000);
            continue;
        }

        foreach (RedisStream stream in result)
        {
            foreach (StreamEntry entry in stream.Entries)
            {
                int temp = (int)entry.Values.First(x => x.Name == "temp").Value;
                messageCountTotals[stream.Key!]++;
                tempTotals[stream.Key!] += temp;
                double avg = tempTotals[stream.Key!]/messageCountTotals[stream.Key!];
                Console.WriteLine($"{stream.Key} average Temp = {avg:0.###}");
                await db.StreamAcknowledgeAsync(stream.Key, groupName, entry.Id);
            }
        }
    }
});


// end coding challenge
//put all your future code above here!
Console.ReadKey();