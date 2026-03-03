# Concurrency Control

## Overview
In a high-throughput settlement system, multiple processes may attempt to modify the same account simultaneously. AresNexus utilizes **Optimistic Concurrency Control (OCC)** to ensure data integrity without the overhead of heavy-duty locking.

## Optimistic Concurrency
We rely on aggregate versions to detect conflicts:
1.  **Read**: Load the aggregate state and its current version (e.g., version 5).
2.  **Process**: Apply domain logic and generate new events.
3.  **Write**: Attempt to append events to the stream, asserting that the current version in the database is still version 5.

If another process has already updated the account (version is now 6), the write operation will fail with a `ConcurrencyException`.

## Version Conflicts
Marten's event store automatically handles version checking:
-   Each stream has an incremental version.
-   `IDocumentSession.Events.Append(streamId, expectedVersion, events)` ensures that the stream version hasn't drifted since the initial read.

## Retry Boundaries
When a concurrency conflict is detected, the system follows a retry strategy:
-   **Transient Conflicts**: The application tier will catch the `ConcurrencyException`, reload the aggregate state from the database (now version 6), and re-apply the command.
-   **Conflict Resolution Model**: 
    -   **Last-One-Wins**: Avoided in financial transactions.
    -   **Mergeable Changes**: In cases where two operations are independent (e.g., two deposits), we can automatically re-apply.
    -   **Hard Failure**: If the second operation is no longer valid (e.g., two withdrawals that together would exceed the balance), the retry will fail with a domain exception (`InsufficientFunds`).

## Resilience Integration
Concurrency retries are handled at the **Command Handler** level using a dedicated Polly retry policy, separate from the database-level persistence resilience.
