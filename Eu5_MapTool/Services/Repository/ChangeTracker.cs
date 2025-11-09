using System;
using System.Collections.Generic;
using System.Linq;

namespace Eu5_MapTool.Services.Repository;

/// <summary>
/// Tracks entity changes within a Unit of Work.
/// Maintains state for entities (Added, Modified, Deleted, Unchanged).
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The key type</typeparam>
public class ChangeTracker<TEntity, TKey> where TEntity : class where TKey : notnull
{
    private readonly Dictionary<TKey, (TEntity Entity, EntityState State)> _trackedEntities;
    private readonly Func<TEntity, TKey> _keySelector;

    public ChangeTracker(Func<TEntity, TKey> keySelector)
    {
        _keySelector = keySelector;
        _trackedEntities = new Dictionary<TKey, (TEntity, EntityState)>();
    }

    /// <summary>
    /// Track an entity with the specified state.
    /// </summary>
    public void Track(TEntity entity, EntityState state)
    {
        var key = _keySelector(entity);
        _trackedEntities[key] = (entity, state);
    }

    /// <summary>
    /// Get the state of an entity.
    /// </summary>
    public EntityState GetState(TEntity entity)
    {
        var key = _keySelector(entity);
        if (_trackedEntities.TryGetValue(key, out var tracked))
        {
            return tracked.State;
        }
        return EntityState.Unchanged;
    }

    /// <summary>
    /// Get the state by key.
    /// </summary>
    public EntityState GetState(TKey key)
    {
        if (_trackedEntities.TryGetValue(key, out var tracked))
        {
            return tracked.State;
        }
        return EntityState.Unchanged;
    }

    /// <summary>
    /// Check if an entity is tracked.
    /// </summary>
    public bool IsTracked(TEntity entity)
    {
        var key = _keySelector(entity);
        return _trackedEntities.ContainsKey(key);
    }

    /// <summary>
    /// Check if a key is tracked.
    /// </summary>
    public bool IsTracked(TKey key)
    {
        return _trackedEntities.ContainsKey(key);
    }

    /// <summary>
    /// Get all entities in a specific state.
    /// </summary>
    public IEnumerable<TEntity> GetEntitiesByState(EntityState state)
    {
        return _trackedEntities.Values
            .Where(t => t.State == state)
            .Select(t => t.Entity);
    }

    /// <summary>
    /// Get all entities with Added state.
    /// </summary>
    public IEnumerable<TEntity> GetAdded() => GetEntitiesByState(EntityState.Added);

    /// <summary>
    /// Get all entities with Modified state.
    /// </summary>
    public IEnumerable<TEntity> GetModified() => GetEntitiesByState(EntityState.Modified);

    /// <summary>
    /// Get all entities with Deleted state.
    /// </summary>
    public IEnumerable<TEntity> GetDeleted() => GetEntitiesByState(EntityState.Deleted);

    /// <summary>
    /// Get all entities that have changed (Added, Modified, or Deleted).
    /// </summary>
    public IEnumerable<TEntity> GetChanged()
    {
        return _trackedEntities.Values
            .Where(t => t.State != EntityState.Unchanged)
            .Select(t => t.Entity);
    }

    /// <summary>
    /// Get all tracked entities as a dictionary.
    /// </summary>
    public Dictionary<TKey, TEntity> GetChangedAsDictionary()
    {
        return _trackedEntities
            .Where(kvp => kvp.Value.State != EntityState.Unchanged)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Entity);
    }

    /// <summary>
    /// Check if there are any tracked changes.
    /// </summary>
    public bool HasChanges()
    {
        return _trackedEntities.Values.Any(t => t.State != EntityState.Unchanged);
    }

    /// <summary>
    /// Get the count of tracked changes (excluding Unchanged).
    /// </summary>
    public int GetChangeCount()
    {
        return _trackedEntities.Values.Count(t => t.State != EntityState.Unchanged);
    }

    /// <summary>
    /// Clear all tracked changes.
    /// </summary>
    public void Clear()
    {
        _trackedEntities.Clear();
    }

    /// <summary>
    /// Accept all changes - sets all entities to Unchanged state.
    /// </summary>
    public void AcceptChanges()
    {
        var keys = _trackedEntities.Keys.ToList();
        foreach (var key in keys)
        {
            var (entity, _) = _trackedEntities[key];
            _trackedEntities[key] = (entity, EntityState.Unchanged);
        }
    }

    /// <summary>
    /// Stop tracking an entity.
    /// </summary>
    public void Untrack(TEntity entity)
    {
        var key = _keySelector(entity);
        _trackedEntities.Remove(key);
    }

    /// <summary>
    /// Stop tracking by key.
    /// </summary>
    public void Untrack(TKey key)
    {
        _trackedEntities.Remove(key);
    }
}
