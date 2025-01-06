Here's a detailed README.md for the Redis vs Garnet benchmark code:

```markdown
# Redis vs Garnet Performance Benchmark

A comprehensive performance testing tool designed to compare Redis and Microsoft Garnet (Redis-compatible interface) clusters across various operations and data structures.

## Overview

This benchmark suite performs comparative testing between Redis and Garnet clusters, measuring performance across multiple operations including basic CRUD, data structures, transactions, and Pub/Sub operations.

## Features

- Parallel testing of Redis and Garnet clusters
- 14 different performance test scenarios
- Automated result comparison and analysis
- Detailed performance metrics including operations per second
- Latency measurements for Pub/Sub operations

## Test Scenarios

1. **Single Operation**: Basic SET/GET operations
2. **Batch Operation**: Multiple operations in batch mode
3. **Large Data Processing**: Handling 10KB data chunks
4. **Hash Operations**: Testing HASH data structure performance
5. **List Operations**: Testing LIST data structure operations
6. **Sorted Set**: Testing ZSET operations
7. **Transaction**: Multi-key transaction performance
8. **Read/Write Mixed**: Combined read and write operations
9. **Key Expiration**: Testing TTL setting performance
10. **Complex JSON**: JSON data structure handling
11. **Pipeline**: Testing pipelined operations
12. **Key Search**: SCAN operation performance
13. **Pub/Sub Performance**: Message publishing throughput
14. **Pub/Sub Latency**: Message delivery latency testing

## Requirements

- .NET 6.0 or higher
- StackExchange.Redis client library
- Access to Redis and Garnet clusters

## Configuration

Update the cluster endpoints in the code:

```csharp
// Garnet cluster configuration
EndPoints = { "192.168.5.152:7002", "192.168.5.154:7002", "192.168.5.160:7002" }

// Redis cluster configuration
EndPoints = { "192.168.5.152:7001", "192.168.5.154:7001", "192.168.5.160:7001" }
```

## Test Parameters

- Test Duration: 3 seconds per test
- Batch Size: 1000 operations
- JSON Data Size: Various (up to complex nested structures)
- Pub/Sub Message Rate: Controlled with 1ms delay
- Key Pattern: Using {test} hash tag for cluster slot alignment

## Output Format

The benchmark provides two types of output:

1. **Detailed Results Table**:
```
Performance Test Results Comparison:
================================================================================
Test Type            | Garnet (ops/sec)          | Redis (ops/sec)
--------------------------------------------------------------------------------
Single operation     |                     4,052 |                     3,843
Batch operation      |                    12,100 |                    10,525
Large data processing |                     2,325 |                     2,052
Hash operation       |                     3,560 |                     3,270
List operation       |                     3,742 |                     3,851
Sorted set           |                     3,723 |                     3,929
Transaction          |                       474 |                       472
Read/Write mixed     |                     1,883 |                     2,256
Key expiration setting |                     3,750 |                     4,364
Complex JSON         |                     3,498 |                     3,996
Pipeline             |                    11,972 |                     8,097
Key Search           |                    35,414 |                    32,180
Pub/Sub              |                     3,529 |                     3,398
Pub/Sub Latency      |                        51 |                        50
================================================================================

Performance Difference Analysis:
Single operation: Garnet is 5.1% faster
Batch operation: Garnet is 13.0% faster
Large data processing: Garnet is 11.7% faster
Hash operation: Garnet is 8.1% faster
List operation: Redis is 2.9% faster
Sorted set: Redis is 5.5% faster
Transaction: Garnet is 0.3% faster
Read/Write mixed: Redis is 19.8% faster
Key expiration setting: Redis is 16.4% faster
Complex JSON: Redis is 14.2% faster
Pipeline: Garnet is 32.4% faster
Key Search: Garnet is 9.1% faster
Pub/Sub: Garnet is 3.7% faster
Pub/Sub Latency: Garnet is 2.4% faster
```

## Key Features Explained

### Hash Tags
- Uses `{test}` hash tag to ensure multi-key operations target the same cluster slot
- Prevents CROSSSLOT errors in cluster mode

### Pub/Sub Testing
- Measures both throughput and latency
- Includes message confirmation mechanism
- Tracks average message delivery time

### Cleanup Mechanism
- Automatically removes test keys after completion
- Uses pattern matching with `{test}:*`
- Handles cleanup errors gracefully

## Best Practices

1. **Cluster Setup**:
   - Ensure both clusters have similar hardware specifications
   - Verify network conditions are consistent
   - Clear cluster data before testing

2. **Test Execution**:
   - Run tests multiple times for consistent results
   - Monitor cluster health during testing
   - Allow cool-down period between test runs

3. **Result Analysis**:
   - Consider both throughput and latency
   - Account for network conditions
   - Look for patterns in performance differences

## Error Handling

The benchmark includes comprehensive error handling for:
- Connection failures
- Operation timeouts
- Key scanning issues
- Pub/Sub communication errors
- Cleanup failures

## Limitations

1. Network conditions can affect results
2. Results may vary based on cluster configuration
3. Some operations may be affected by cluster size
4. Memory usage is not currently monitored

## Future Improvements

- Memory usage monitoring
- Cluster node failure testing
- Custom test scenario support
- Configuration file support
- Extended metrics collection
- Graphical result visualization

## Contributing

Contributions are welcome! Please consider the following:
- Maintain consistent code style
- Add comments for complex operations
- Include tests for new features
- Update documentation as needed

## License

This project is available under the MIT License.

## Acknowledgments

- StackExchange.Redis team
- Microsoft Garnet team
- Redis community

## Contact

For questions and support, please open an issue in the repository.
