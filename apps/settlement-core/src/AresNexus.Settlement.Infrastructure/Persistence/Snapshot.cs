using System.Text.Json;

namespace AresNexus.Settlement.Infrastructure.Persistence;

/// <summary>
/// Snapshot entity for the Infrastructure layer.
/// Used to persist aggregate state snapshots to the database for performance.
/// </summary>
/// <param name="AggregateId">The unique identifier of the aggregate.</param>
/// <param name="AggregateType">The type of the aggregate.</param>
/// <param name="Data">The serialized state of the aggregate.</param>
/// <param name="Version">The version of the aggregate when the snapshot was taken.</param>
/// <param name="CreatedAt">The timestamp when the snapshot was created.</param>
public sealed record Snapshot(
    Guid AggregateId,
    string AggregateType,
    string Data,
    int Version,
    DateTime CreatedAt);
