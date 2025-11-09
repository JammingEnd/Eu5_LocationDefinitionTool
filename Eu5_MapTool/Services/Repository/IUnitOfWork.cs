using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services.Repository;

/// <summary>
/// Unit of Work pattern interface.
/// Coordinates changes across multiple repositories and manages transactions.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Repository for province operations.
    /// </summary>
    IRepository<ProvinceInfo, string> Provinces { get; }

    /// <summary>
    /// Save all changes made within this unit of work.
    /// This persists all tracked changes to files.
    /// </summary>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Rollback all changes made within this unit of work.
    /// Restores the state before any modifications.
    /// </summary>
    Task RollbackAsync();

    /// <summary>
    /// Check if there are any unsaved changes.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Get the number of tracked changes.
    /// </summary>
    int ChangeCount { get; }

    /// <summary>
    /// Clear all tracked changes without saving.
    /// </summary>
    void Clear();

    /// <summary>
    /// Get all provinces that have been changed (Added, Modified, or Deleted).
    /// Returns a dictionary of changed provinces keyed by province ID.
    /// </summary>
    Dictionary<string, ProvinceInfo> GetChangedProvinces();
}
