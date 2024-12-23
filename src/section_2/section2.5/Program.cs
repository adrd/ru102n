using StackExchange.Redis;

ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect("localhost");
IDatabase db = muxer.GetDatabase();

// TODO for Coding Challenge Start here on starting-point branch
string person1 = "person:1";
string person2 = "person:2";
string person3 = "person:3";

db.KeyDelete(new RedisKey[]{person1, person2, person3});

db.HashSet(person1, new HashEntry[]
{
    new("name","Alice"),
    new("age", 33),
    new("email","alice@example.com")
});

db.HashSet(person2, new HashEntry[]
{
    new("name","Bob"),
    new("age", 27),
    new("email","robert@example.com")
});

db.HashSet(person3, new HashEntry[]
{
    new("name","Charlie"),
    new("age", 50),
    new("email","chuck@example.com")
});

long newAge = db.HashIncrement(person3, "age");
Console.WriteLine($"person:3 new age: {newAge}");

RedisValue person1Name = db.HashGet(person1, "name");
Console.WriteLine($"person:1 name: {person1Name}");

// HashGetAll
HashEntry[] person2Fields = db.HashGetAll(person2);
Console.WriteLine($"person:2 fields: {string.Join(", ", person2Fields)}");

// HashScan
IEnumerable<HashEntry> person3Fields = db.HashScan(person3);
Console.WriteLine($"person:3 fields: {string.Join(", ", person3Fields)}");
// end coding challenge