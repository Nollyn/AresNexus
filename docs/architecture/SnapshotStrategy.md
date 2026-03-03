# Snapshot Strategy

## Overview
AresNexus utilizes snapshotting to optimize the performance of loading large event streams. Instead of replaying thousands of events to rebuild the state of an aggregate root, we can load a periodic snapshot and only apply events that occurred after the snapshot.

## Snapshot Triggering
Snapshots are triggered automatically by the `IAccountRepository` during the `SaveAsync` operation.
- **Configurable Interval**: The interval is controlled by the `EventSourcing:SnapshotInterval` configuration setting (default is 100).
- **Condition**: A snapshot is created when the aggregate version crosses an interval boundary (e.g., 100, 200, 300).

## Implementation Details
Aggregates that support snapshotting must implement the `ISnapshotable<TSnapshot>` interface:
```csharp
public interface ISnapshotable<TSnapshot>
{
    TSnapshot CreateSnapshot();
    void LoadFromSnapshot(TSnapshot snapshot);
}
```

Snapshots are persisted in Marten as separate document types, keyed by the `AggregateId`.

## Recovery Speed Improvement
By using snapshots, the time to load an aggregate is reduced from $O(N)$ where $N$ is the number of events, to $O(I)$ where $I$ is the snapshot interval. In a Tier-1 banking environment with high-velocity accounts, this ensures sub-millisecond aggregate reconstruction.

## Storage Growth Model
While snapshots improve read performance, they introduce additional storage overhead.
- **Single Active Snapshot**: We only maintain the latest snapshot per aggregate. Marten's upsert logic ensures we don't keep old, redundant snapshots unless explicitly configured for historical auditing.
- **Size**: Snapshots are compact JSON representations of the aggregate's state.

## Tradeoffs
| Factor | With Snapshots | Without Snapshots |
| :--- | :--- | :--- |
| **Read Latency** | Low/Constant | Increasing with stream length |
| **Write Latency** | Slightly higher (on interval) | Lower |
| **Complexity** | Higher (schema evolution) | Lower |
| **Storage** | Higher | Lower |

## Replay and Snapshots
The `MartenAccountRepository` always attempts to load the latest snapshot first. If found, it fetches only events with a version greater than the snapshot version. If no snapshot is found, it falls back to full replay from version 0.
