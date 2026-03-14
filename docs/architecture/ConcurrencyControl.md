# Concurrency Control

## Overview
AresNexus handles financial settlements where data consistency and integrity are critical. This document explains our concurrency control mechanisms for managing multiple simultaneous updates to the same aggregate root.

## 1. Optimistic Concurrency
AresNexus uses **Optimistic Concurrency Control (OCC)**. This approach assumes that conflicts are rare and allows multiple operations to proceed in parallel, only checking for conflicts during the final write to the database.
- **Implementation**: Every aggregate root (e.g., `Account`) includes a `Version` property.
- **Write Verification**: When saving changes, Marten verifies that the current version in the database matches the version that was loaded at the start of the transaction.
- **Conflict Detection**: If the versions do not match (meaning another process updated the aggregate in the meantime), Marten throws an `EventStreamUnexpectedMaxEventIdException` (or similar).

## 2. Version Conflicts
When a version conflict occurs, the operation fails to ensure that no state transitions are based on stale data.
- **Client Impact**: The API returns a `409 Conflict` status code to the client.
- **Recommendation**: Clients should implement a retry strategy:
    1. Re-fetch the current state of the aggregate.
    2. Re-apply the desired change to the latest state.
    3. Re-submit the request with the new expected version.

## 3. Retry Boundaries
To simplify client-side logic, AresNexus can implement internal retry boundaries for certain operations.
- **Command Handlers**: If a version conflict is detected within a command handler, the handler can automatically retry the entire operation (load, apply, save) a limited number of times before returning an error to the client.
- **Idempotency**: Retries are safe because all commands are idempotent (checked via `CommandId` or `CorrelationId`).

## 4. Conflict Resolution Model
Our model is **First-Writer Wins**. The first process to successfully commit its changes to the event stream "wins," and any subsequent processes attempting to update the same version will fail.
- **No Automatic Merging**: We do not attempt to automatically merge conflicting changes. Financial transactions require absolute precision, and automatic merging could lead to unintended state transitions (e.g., double-spending or bypassing invariants).
- **Manual Intervention**: If a conflict cannot be resolved through automated retries, it is escalated to the client or a manual reconciliation process.

## 5. Summary
| Mechanism | Strategy | Handling |
| :--- | :--- | :--- |
| **Optimistic Concurrency** | Version-based check at write time. | Exception on mismatch. |
| **Conflict Resolution** | First-Writer Wins | `409 Conflict` returned to client. |
| **Retries** | Exponential backoff on client or internal boundary. | Idempotency ensured via `CorrelationId`. |
