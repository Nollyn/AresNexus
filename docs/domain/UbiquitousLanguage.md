# Ubiquitous Language - Settlement Core

## Core Domain Terms

| Term | Definition | Context |
| :--- | :--- | :--- |
| **Account** | The primary aggregate root representing a customer's financial ledger. | Domain |
| **Aggregate Root** | A cluster of domain objects that can be treated as a single unit. | DDD / Architecture |
| **Event Stream** | An immutable sequence of events for a single aggregate instance. | Event Sourcing |
| **Settlement** | The process of finalizing a financial transaction between two parties. | Business |
| **Deposit** | An operation that increases the balance of an Account. | Business |
| **Withdrawal** | An operation that decreases the balance of an Account. | Business |
| **Lock** | A temporary state where no state-changing operations are allowed on an Account. | Business / Safety |
| **Snapshot** | A point-in-time projection of an aggregate's state used for performance. | Infrastructure |
| **Idempotency** | The property where an operation can be applied multiple times without changing the result beyond the initial application. | Reliability |

## Invariants
1.  **Non-Negative Balance**: An Account's balance cannot drop below zero (unless an explicit overdraft limit is configured).
2.  **Locked Account Immutability**: No deposits or withdrawals are permitted while an account is in a `Locked` state.
3.  **Owner Required**: An Account must always have a valid owner (non-empty string).

## Business Constraints
-   **Traceability**: Every state change MUST be accompanied by a `TraceId` and `CorrelationId` for audit purposes (FINMA/DORA requirements).
-   **At-Rest Encryption**: Sensitive transaction references must be encrypted before being persisted.
-   **Event Immutability**: Once an event is appended to a stream, it cannot be deleted or modified.
