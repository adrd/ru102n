// See https://aka.ms/new-console-template for more information

using StackExchange.Redis;

ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect("localhost");
IDatabase db = muxer.GetDatabase();

// TODO for Coding Challenge Start here on starting-point branch
await db.KeyDeleteAsync("bf");
await db.KeyDeleteAsync("cms");
await db.KeyDeleteAsync("topk");

// Likely incomplete list of delimiting characters within the book.
char[] delimiterChars = { ' ', ',', '.', ':', '\t', '\n', '—', '?', '"', ';', '!', '’', '\r', '\'', '(', ')', '”' };

// Pull in text of Moby Dick.
string text = await File.ReadAllTextAsync("data/moby_dick.txt");

// Split words out from text.
string[] words = text.Split(delimiterChars).Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>s.ToLower()).Select(x=>x).ToArray();

// Organize our words into a list to be pushed into the bloom filter in one shot, we could de-duplicate to make the transit shorter,
// but there's nothing inherently wrong with sending duplicates as the filter will filter them out.
HashSet<object> bloomList = words.Aggregate(new HashSet<object> { "bf" }, (list, word) =>
{
    list.Add(word);
    return list;
});

// Reserve our bloom filter.
await db.ExecuteAsync("BF.RESERVE", "bf", 0.01, 20000);

// Add All the Words to our bloom filter.
await db.ExecuteAsync("BF.MADD", bloomList, CommandFlags.FireAndForget);

// Reserve the Top-K.
await db.ExecuteAsync("TOPK.RESERVE", "topk", 10, 20, 10, .925);

// We need to organize the words into a list where each word is followed by the number of occurrences it has in Moby Dick.
List<object> topKList = words.Aggregate(new Dictionary<string, int>(), (dict, word) =>
{
    if (!dict.ContainsKey(word))
    {
        dict.Add(word, 0);
    }

    dict[word]++;
    return dict;
}).Aggregate(new List<object> {"topk"}, (list, kvp) =>
{
   list.Add(kvp.Key);
   list.Add(kvp.Value);
   return list;
});

// Add everything to the Top-K.
await db.ExecuteAsync("TOPK.INCRBY", topKList, CommandFlags.FireAndForget);

// Ask the Bloom Filter and Top-K some questions...
RedisResult doesTheExist = await db.ExecuteAsync("BF.EXISTS", "bf", "the");

int doesTheExistAsInt = (int)doesTheExist;
Console.WriteLine($"Typeof {nameof(doesTheExistAsInt)}: {doesTheExistAsInt.GetType()}");

double doesTheExistAsDouble = (double)doesTheExist;
Console.WriteLine($"Typeof {nameof(doesTheExistAsDouble)}: {doesTheExistAsDouble.GetType()}");

Console.WriteLine($"Type enum for {nameof(doesTheExist)}: {doesTheExist.Type}");
Console.WriteLine($"Does 'the' exist in filter? {doesTheExist}'");

RedisResult res = await db.ExecuteAsync("TOPK.LIST", "topk");
IEnumerable<string> arr = ((RedisResult[])res!).Select(x=>x.ToString());
Console.WriteLine($"Top 10: {string.Join(", ", arr)}");

IEnumerable<string> withCounts = (await db.ExecuteAsync("TOPK.LIST", "topk", "WITHCOUNT")).ToDictionary().Select(x=>$"{x.Key}: {x.Value}");

Console.WriteLine($"Top 10, with counts: {string.Join(", ", withCounts)}");
// end coding challenge