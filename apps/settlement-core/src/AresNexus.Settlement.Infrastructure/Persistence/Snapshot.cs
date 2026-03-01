using System.Text.Json;

namespace AresNexus.Settlement.Infrastructure.Persistence;

/// <summary>
/// Snapshot entity for the Infrastructure layer.
/// Used to persist aggregate state snapshots to the database for performance.
/// </summary>
public sealed record Snapshot(
    Guid Id,
    Guid AggregateId,
    string AggregateType,
    string Data,
    int Version,
    DateTime CreatedAt);
