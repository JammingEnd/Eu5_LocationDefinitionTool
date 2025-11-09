using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services.Repository;

/// <summary>
/// Unit of Work implementation that coordinates changes across repositories.
/// Provides automatic change tracking to replace manual _paintedLocations dictionary.
/// Includes transaction support with automatic backup/rollback on failure.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ProvinceRepository _provinceRepository;
    private readonly ChangeTracker<ProvinceInfo, string> _changeTracker;
    private readonly TransactionManager? _transactionManager;
    private bool _disposed;

    public UnitOfWork(ProvinceRepository provinceRepository, TransactionManager? transactionManager = null)
    {
        _provinceRepository = provinceRepository;
        _changeTracker = new ChangeTracker<ProvinceInfo, string>(p => p.Id);
        _transactionManager = transactionManager;
        Provinces = new TrackedProvinceRepository(_provinceRepository, _changeTracker);
    }

    public IRepository<ProvinceInfo, string> Provinces { get; }

    public bool HasChanges => _changeTracker.HasChanges();

    public int ChangeCount => _changeTracker.GetChangeCount();

    public async Task<int> SaveChangesAsync()
    {
        if (!HasChanges)
            return 0;

        // Get all changed provinces
        var changedProvinces = _changeTracker.GetChangedAsDictionary();

        // If transaction manager available, use it for safety
        if (_transactionManager != null)
        {
            try
            {
                // Begin transaction and backup files
                _transactionManager.BeginTransaction();
                await _provinceRepository.BackupFilesForTransactionAsync(_transactionManager);

                // Save via repository
                await _provinceRepository.SaveAsync(changedProvinces);

                // Clear OldName for all saved provinces since they're now persisted with their new names
                foreach (var province in changedProvinces.Values)
                {
                    province.OldName = null;
                }

                // Commit transaction
                _transactionManager.CommitTransaction();
            }
            catch (Exception)
            {
                // Rollback files on error
                if (_transactionManager.IsTransactionInProgress)
                {
                    await _transactionManager.RollbackTransactionAsync();
                }
                throw; // Re-throw to let caller handle
            }
        }
        else
        {
            // No transaction support - just save directly
            await _provinceRepository.SaveAsync(changedProvinces);

            // Clear OldName for all saved provinces since they're now persisted with their new names
            foreach (var province in changedProvinces.Values)
            {
                province.OldName = null;
            }
        }

        // Accept changes (mark all as Unchanged)
        _changeTracker.AcceptChanges();

        return changedProvinces.Count;
    }

    public Task RollbackAsync()
    {
        // Clear all tracked changes in memory
        // (File rollback is handled by TransactionManager if transaction in progress)
        _changeTracker.Clear();
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _changeTracker.Clear();
    }

    public Dictionary<string, ProvinceInfo> GetChangedProvinces()
    {
        return _changeTracker.GetChangedAsDictionary();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            // Cleanup if needed
        }
    }
}

/// <summary>
/// Wrapper repository that integrates with ChangeTracker.
/// Automatically tracks all Add/Update/Delete operations.
/// </summary>
internal class TrackedProvinceRepository : IRepository<ProvinceInfo, string>
{
    private readonly ProvinceRepository _inner;
    private readonly ChangeTracker<ProvinceInfo, string> _changeTracker;

    public TrackedProvinceRepository(ProvinceRepository inner, ChangeTracker<ProvinceInfo, string> changeTracker)
    {
        _inner = inner;
        _changeTracker = changeTracker;
    }

    public Task<ProvinceInfo?> GetByIdAsync(string id) => _inner.GetByIdAsync(id);

    public Task<IEnumerable<ProvinceInfo>> GetAllAsync() => _inner.GetAllAsync();

    public Task<Dictionary<string, ProvinceInfo>> GetAllAsDictionaryAsync() => _inner.GetAllAsDictionaryAsync();

    public Task<IEnumerable<ProvinceInfo>> FindAsync(Func<ProvinceInfo, bool> predicate) => _inner.FindAsync(predicate);

    public Task<bool> ExistsAsync(string id) => _inner.ExistsAsync(id);

    public void Add(ProvinceInfo entity)
    {
        _inner.Add(entity);
        _changeTracker.Track(entity, EntityState.Added);
    }

    public void Update(ProvinceInfo entity)
    {
        _inner.Update(entity);

        // Check if already tracked as Added - if so, keep that state
        if (_changeTracker.GetState(entity) != EntityState.Added)
        {
            _changeTracker.Track(entity, EntityState.Modified);
        }
    }

    public void Delete(string id)
    {
        var entity = _inner.GetByIdAsync(id).Result;
        if (entity != null)
        {
            _inner.Delete(id);
            _changeTracker.Track(entity, EntityState.Deleted);
        }
    }

    public void Delete(ProvinceInfo entity)
    {
        _inner.Delete(entity);
        _changeTracker.Track(entity, EntityState.Deleted);
    }
}
