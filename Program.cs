using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

class Program
{
    private class ClusterConfig
    {
        public string Name { get; set; }
        public ConfigurationOptions Options { get; set; }
        public ConnectionMultiplexer Connection { get; set; }
        public List<PerformanceResult> Results { get; set; } = new List<PerformanceResult>();
    }

    private class PerformanceResult
    {
        public string TestName { get; set; }
        public int OperationCount { get; set; }
        public double DurationMs { get; set; }
        public double OpsPerSecond => OperationCount / (DurationMs / 1000);
    }

    static async Task Main(string[] args)
    {
        var clusters = new[]
        {
            new ClusterConfig
            {
                Name = "Garnet",
                Options = new ConfigurationOptions
                {
                    EndPoints = { "192.168.5.152:7002", "192.168.5.154:7002", "192.168.5.160:7002" }, // change to your Garnet cluster address
                    CommandMap = CommandMap.Default,
                    AllowAdmin = true,
                    AbortOnConnectFail = false
                }
            },
            new ClusterConfig
            {
                Name = "Redis",
                Options = new ConfigurationOptions
                {
                    EndPoints = { "192.168.5.152:7001", "192.168.5.154:7001", "192.168.5.160:7001" }, // change to your Redis cluster address
                    CommandMap = CommandMap.Default,
                    AllowAdmin = true,
                    AbortOnConnectFail = false
                }
            }
        };

        try
        {
            // Connect to clusters
            foreach (var cluster in clusters)
            {
                cluster.Connection = await ConnectionMultiplexer.ConnectAsync(cluster.Options);
                Console.WriteLine($"{cluster.Name} cluster connected.");
            }

            // Run performance tests
            await RunPerformanceTests(clusters);

            // Print results
            PrintResults(clusters);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
        finally
        {
            // Disconnect from clusters
            foreach (var cluster in clusters)
            {
                cluster.Connection?.Dispose();
            }
        }
    }

    static async Task RunPerformanceTests(ClusterConfig[] clusters)
    {
        const int TEST_DURATION_MS = 3000; // 3초
        const int BATCH_SIZE = 1000;

        foreach (var cluster in clusters)
        {
            Console.WriteLine($"\n{cluster.Name} cluster performance test starting...");
            var db = cluster.Connection.GetDatabase();

            // 1. Single operation performance test
            await RunTest("Single operation", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    await db.StringSetAsync($"{{test}}:single:{count}", count.ToString());
                    count++;
                }
                return count;
            }, cluster);

            // 2. Batch operation performance test
            await RunTest("Batch operation", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    var batch = db.CreateBatch();
                    var tasks = new List<Task>();
                    string hashTag = $"{{test:{count / BATCH_SIZE}}}"; // Using hash tag to ensure same slot

                    for (int i = 0; i < 100 && sw.ElapsedMilliseconds < TEST_DURATION_MS; i++)
                    {
                        tasks.Add(batch.StringSetAsync($"{hashTag}:batch:{i}", count.ToString()));
                        count++;
                    }

                    batch.Execute();
                    await Task.WhenAll(tasks);
                }
                return count;
            }, cluster);

            // 3. Large data processing test
            await RunTest("Large data processing", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                var largeData = new string('X', 1024 * 10); // 10KB data

                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    await db.StringSetAsync($"{{test}}:large:{count}", largeData);
                    count++;
                }
                return count;
            }, cluster);

            // 4. Hash operation performance test
            await RunTest("Hash operation", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    var hashFields = new HashEntry[]
                    {
                        new HashEntry("field1", $"value1_{count}"),
                        new HashEntry("field2", $"value2_{count}"),
                        new HashEntry("field3", $"value3_{count}")
                    };
                    await db.HashSetAsync($"{{test}}:hash:{count}", hashFields);
                    count++;
                }
                return count;
            }, cluster);

            // 5. List operation performance test
            await RunTest("List operation", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    await db.ListRightPushAsync($"{{test}}:list:{count / 100}", count.ToString());
                    count++;
                }
                return count;
            }, cluster);

            // 6. Sorted set operation test
            await RunTest("Sorted set", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    await db.SortedSetAddAsync("{{test}}:sortedset", $"member_{count}", count);
                    count++;
                }
                return count;
            }, cluster);

            // 7. Transaction performance test
            await RunTest("Transaction", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    string hashTag = $"{{test:{count}}}"; // Hash tag for using the same slot
                    var tran = db.CreateTransaction();
                    tran.StringSetAsync($"{hashTag}:a", "value1");
                    tran.StringSetAsync($"{hashTag}:b", "value2");
                    tran.StringSetAsync($"{hashTag}:c", "value3");
                    await tran.ExecuteAsync();
                    count++;
                }
                return count;
            }, cluster);

            // 8. Read/Write mixed operation test
            await RunTest("Read/Write mixed", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    await db.StringSetAsync($"{{test}}:rw:{count}", count.ToString());
                    await db.StringGetAsync($"{{test}}:rw:{Math.Max(0, count - 1)}");
                    count++;
                }
                return count;
            }, cluster);

            // 9. Key expiration setting test
            await RunTest("Key expiration setting", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    await db.StringSetAsync($"{{test}}:expire:{count}", count.ToString(), TimeSpan.FromMinutes(5));
                    count++;
                }
                return count;
            }, cluster);

            // 10. Complex JSON processing test
            await RunTest("Complex JSON", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    var complexJson = new JsonObject
                    {
                        ["id"] = count,
                        ["timestamp"] = DateTime.UtcNow.ToString("O"),
                        ["data"] = new JsonObject
                        {
                            ["array"] = new JsonArray { 1, 2, 3, 4, 5 },
                            ["nested"] = new JsonObject
                            {
                                ["field1"] = "value1",
                                ["field2"] = count,
                                ["field3"] = new JsonObject
                                {
                                    ["subfield1"] = "subvalue1",
                                    ["subfield2"] = DateTime.UtcNow.Ticks
                                }
                            }
                        }
                    };
                    await db.StringSetAsync($"{{test}}:complexjson:{count}", complexJson.ToJsonString());
                    count++;
                }
                return count;
            }, cluster);

            // 11. Pipeline performance test
            await RunTest("Pipeline", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    var batch = db.CreateBatch();
                    var tasks = new List<Task>();
                    string hashTag = $"{{test:{count / 100}}}"; // Hash tag for using the same slot

                    for (int i = 0; i < 100 && sw.ElapsedMilliseconds < TEST_DURATION_MS; i++)
                    {
                        tasks.Add(batch.StringSetAsync($"{hashTag}:pipeline:{i}", count.ToString()));
                        count++;
                    }

                    batch.Execute();
                    await Task.WhenAll(tasks);
                }
                return count;
            }, cluster);

            // 12. Search performance test
            await RunTest("Key Search", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                var endpoints = cluster.Connection.GetEndPoints();
                
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    // Search for keys on all servers
                    foreach (var endpoint in endpoints)
                    {
                        var server = cluster.Connection.GetServer(endpoint);
                        if (server.IsConnected)
                        {
                            try
                            {
                                // Use the SCAN command to search for keys
                                await foreach (var key in server.KeysAsync(pattern: "{test}:*", pageSize: 100))
                                {
                                    count++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error scanning keys on {endpoint}: {ex.Message}");
                            }
                        }
                    }

                    // If no keys are found in the first iteration, create test keys
                    if (count == 0)
                    {
                        var db = cluster.Connection.GetDatabase();
                        for (int i = 0; i < 100; i++)
                        {
                            await db.StringSetAsync($"{{test}}:search:dummy:{i}", $"value_{i}");
                        }
                    }
                    
                    // Stop after one search (to prevent infinite loop)
                    break;
                }
                
                return count;
            }, cluster);

            // 13. Pub/Sub performance test
            await RunTest("Pub/Sub", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                var messageCount = 0;
                var subscriber = cluster.Connection.GetSubscriber();
                var channel = "{test}:pubsub:channel";
                var completionSource = new TaskCompletionSource<bool>();

                // Subscribe to test channel
                await subscriber.SubscribeAsync(channel, (_, message) =>
                {
                    messageCount++;
                    if (messageCount >= count)
                    {
                        completionSource.TrySetResult(true);
                    }
                });

                // Publish messages
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    var message = new JsonObject
                    {
                        ["id"] = count,
                        ["timestamp"] = DateTime.UtcNow.Ticks,
                        ["data"] = $"test_message_{count}"
                    }.ToJsonString();

                    await subscriber.PublishAsync(channel, message);
                    count++;

                    // Every 1000 messages, wait for confirmation of receipt
                    if (count % 1000 == 0)
                    {
                        if (await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(1)))
                        {
                            completionSource = new TaskCompletionSource<bool>();
                        }
                    }
                }

                // Wait for final messages to be received
                await Task.WhenAny(
                    completionSource.Task,
                    Task.Delay(1000)
                );

                // Unsubscribe from the channel
                await subscriber.UnsubscribeAsync(channel);

                // Return the number of successful publish operations
                return count;
            }, cluster);

            // 14. Pub/Sub Latency test
            await RunTest("Pub/Sub Latency", async () =>
            {
                var sw = Stopwatch.StartNew();
                int count = 0;
                var subscriber = cluster.Connection.GetSubscriber();
                var channel = "{test}:pubsub:latency";
                var latencySum = 0L;
                var completionSource = new TaskCompletionSource<bool>();

                // Subscribe to test channel
                await subscriber.SubscribeAsync(channel, (_, message) =>
                {
                    var receivedTime = DateTime.UtcNow.Ticks;
                    var messageData = JsonNode.Parse(message.ToString());
                    var sentTime = messageData["timestamp"].GetValue<long>();
                    latencySum += (receivedTime - sentTime) / TimeSpan.TicksPerMillisecond;
                    count++;

                    if (sw.ElapsedMilliseconds >= TEST_DURATION_MS)
                    {
                        completionSource.TrySetResult(true);
                    }
                });

                // Publish messages with timestamps
                while (sw.ElapsedMilliseconds < TEST_DURATION_MS)
                {
                    var message = new JsonObject
                    {
                        ["id"] = count,
                        ["timestamp"] = DateTime.UtcNow.Ticks,
                        ["data"] = $"latency_test_{count}"
                    }.ToJsonString();

                    await subscriber.PublishAsync(channel, message);
                    await Task.Delay(1); // Small delay to prevent overwhelming
                }

                // Wait for final messages
                await Task.WhenAny(
                    completionSource.Task,
                    Task.Delay(1000)
                );

                // Unsubscribe from the channel
                await subscriber.UnsubscribeAsync(channel);

                // Print average latency
                if (count > 0)
                {
                    var avgLatency = latencySum / (double)count;
                    Console.WriteLine($"Average Pub/Sub latency: {avgLatency:F2}ms");
                }

                return count;
            }, cluster);

            // Clean up keys after testing
            try
            {
                var server = cluster.Connection.GetServer(cluster.Connection.GetEndPoints().First());
                await foreach (var key in server.KeysAsync(pattern: "{test}:*"))
                {
                    await db.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred during key cleanup: {ex.Message}");
            }
        }
    }

    static async Task RunTest(string testName, Func<Task<int>> test, ClusterConfig cluster)
    {
        Console.WriteLine($"Running: {testName}...");
        var sw = Stopwatch.StartNew();
        int operations = await test();
        sw.Stop();

        cluster.Results.Add(new PerformanceResult
        {
            TestName = testName,
            OperationCount = operations,
            DurationMs = sw.ElapsedMilliseconds
        });
    }

    static void PrintResults(ClusterConfig[] clusters)
    {
        Console.WriteLine("\nPerformance Test Results Comparison:");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"{"Test Type",-20} | {"Garnet (ops/sec)",-25} | {"Redis (ops/sec)",-25}");
        Console.WriteLine("-".PadRight(80, '-'));

        var testTypes = clusters[0].Results.Select(r => r.TestName).Distinct();
        foreach (var testType in testTypes)
        {
            var garnetResult = clusters[0].Results.First(r => r.TestName == testType);
            var redisResult = clusters[1].Results.First(r => r.TestName == testType);

            Console.WriteLine($"{testType,-20} | {garnetResult.OpsPerSecond,25:N0} | {redisResult.OpsPerSecond,25:N0}");
        }
        Console.WriteLine("=".PadRight(80, '='));

        // Performance difference analysis
        Console.WriteLine("\nPerformance Difference Analysis:");
        foreach (var testType in testTypes)
        {
            var garnetResult = clusters[0].Results.First(r => r.TestName == testType);
            var redisResult = clusters[1].Results.First(r => r.TestName == testType);

            var diff = ((redisResult.OpsPerSecond / garnetResult.OpsPerSecond) - 1) * 100;
            var fasterSystem = diff > 0 ? "Redis" : "Garnet";
            diff = Math.Abs(diff);

            Console.WriteLine($"{testType}: {fasterSystem} is {diff:N1}% faster");
        }
    }
}
