using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eu5_MapTool.Services.Repository;

/// <summary>
/// Generic repository interface for data access operations.
/// Provides CRUD operations and query capabilities for entities.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The key type (typically string for province hex IDs)</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class where TKey : notnull
{
    /// <summary>
    /// Get an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>The entity, or null if not found</returns>
    Task<TEntity?> GetByIdAsync(TKey id);

    /// <summary>
    /// Get all entities.
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Get all entities as a dictionary keyed by ID.
    /// </summary>
    /// <returns>Dictionary of entities</returns>
    Task<Dictionary<TKey, TEntity>> GetAllAsDictionaryAsync();

    /// <summary>
    /// Find entities matching a predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <returns>Collection of matching entities</returns>
    Task<IEnumerable<TEntity>> FindAsync(Func<TEntity, bool> predicate);

    /// <summary>
    /// Add a new entity.
    /// Note: Changes are not persisted until SaveChangesAsync is called on the Unit of Work.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    void Add(TEntity entity);

    /// <summary>
    /// Update an existing entity.
    /// Note: Changes are not persisted until SaveChangesAsync is called on the Unit of Work.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    void Update(TEntity entity);

    /// <summary>
    /// Delete an entity by identifier.
    /// Note: Changes are not persisted until SaveChangesAsync is called on the Unit of Work.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    void Delete(TKey id);

    /// <summary>
    /// Delete an entity.
    /// Note: Changes are not persisted until SaveChangesAsync is called on the Unit of Work.
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    void Delete(TEntity entity);

    /// <summary>
    /// Check if an entity with the given ID exists.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(TKey id);
}
