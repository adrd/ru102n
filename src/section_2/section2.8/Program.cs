﻿using StackExchange.Redis;

ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect("localhost");
IDatabase db = muxer.GetDatabase();

// TODO for Coding Challenge Start here on starting-point branch
ITransaction transaction = db.CreateTransaction();

transaction.HashSetAsync("person:1", new HashEntry[]
{
    new ("name", "Steve"),
    new ("age", 32),
    new ("postal_code", "32999")
});
transaction.SortedSetAddAsync("person:name:Steve", "person:1", 0);
transaction.SortedSetAddAsync("person:postal_code:32999", "person:1", 0);
transaction.SortedSetAddAsync("person:age", "person:1", 32);

bool success = transaction.Execute();
Console.WriteLine($"Transaction Successful: {success}");

// add condition that age == 32

transaction.AddCondition(Condition.HashEqual("person:1", "age", 32));
transaction.HashIncrementAsync("person:1", "age");
transaction.SortedSetIncrementAsync("person:age", "person:1", 1);

success = transaction.Execute();
Console.WriteLine($"Transaction Successful: {success}");

// Add a condition that will fail (e.g. age == 31)

transaction.AddCondition(Condition.HashEqual("person:1", "age", 31));
transaction.HashIncrementAsync("person:1", "age");
transaction.SortedSetIncrementAsync("person:age", "person:1", 1);
success = transaction.Execute();

Console.WriteLine($"Transaction Successful: {success}");
// end coding challenge
