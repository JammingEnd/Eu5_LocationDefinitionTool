using System.Collections.Generic;

namespace Eu5_MapTool.Services.Mapping;

/// <summary>
/// Generic interface for mapping between file data and domain entities.
/// Provides abstraction for converting parsed file data into domain models and vice versa.
/// </summary>
/// <typeparam name="TEntity">The domain entity type (e.g., ProvinceInfo)</typeparam>
/// <typeparam name="TFileData">The file data structure (e.g., Dictionary)</typeparam>
public interface IEntityMapper<TEntity, TFileData>
{
    /// <summary>
    /// Map file data to domain entity.
    /// </summary>
    /// <param name="fileData">Parsed file data</param>
    /// <returns>Domain entity</returns>
    TEntity MapToEntity(TFileData fileData);

    /// <summary>
    /// Map multiple file data entries to entities.
    /// </summary>
    /// <param name="fileDataCollection">Collection of parsed file data</param>
    /// <returns>Collection of domain entities</returns>
    IEnumerable<TEntity> MapToEntities(IEnumerable<TFileData> fileDataCollection);

    /// <summary>
    /// Map domain entity to file data structure.
    /// </summary>
    /// <param name="entity">Domain entity</param>
    /// <returns>File data structure ready for serialization</returns>
    TFileData MapToFileData(TEntity entity);

    /// <summary>
    /// Map multiple entities to file data structures.
    /// </summary>
    /// <param name="entities">Collection of domain entities</param>
    /// <returns>Collection of file data structures</returns>
    IEnumerable<TFileData> MapToFileDataCollection(IEnumerable<TEntity> entities);
}

/// <summary>
/// Specialized mapper for entities with string identifiers.
/// </summary>
/// <typeparam name="TEntity">The domain entity type</typeparam>
public interface IIdentifiableEntityMapper<TEntity> : IEntityMapper<TEntity, KeyValuePair<string, Dictionary<string, string>>>
{
    /// <summary>
    /// Map a dictionary of file data to a dictionary of entities.
    /// Commonly used for batch operations.
    /// </summary>
    Dictionary<string, TEntity> MapToEntityDictionary(Dictionary<string, Dictionary<string, string>> fileData);

    /// <summary>
    /// Map a dictionary of entities to file data dictionary.
    /// </summary>
    Dictionary<string, Dictionary<string, string>> MapToFileDataDictionary(Dictionary<string, TEntity> entities);
}
