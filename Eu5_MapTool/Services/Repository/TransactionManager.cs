using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Eu5_MapTool.Services.Repository;

/// <summary>
/// Manages file-based transactions with backup and rollback capabilities.
/// Ensures atomicity of write operations - all-or-nothing.
/// </summary>
public class TransactionManager
{
    private readonly string _backupDirectory;
    private readonly List<string> _backedUpFiles;
    private bool _transactionInProgress;

    public TransactionManager(string backupDirectory)
    {
        _backupDirectory = backupDirectory;
        _backedUpFiles = new List<string>();
        _transactionInProgress = false;
    }

    /// <summary>
    /// Begin a new transaction.
    /// Creates backup directory if needed.
    /// </summary>
    public void BeginTransaction()
    {
        if (_transactionInProgress)
            throw new InvalidOperationException("A transaction is already in progress.");

        _transactionInProgress = true;
        _backedUpFiles.Clear();

        // Ensure backup directory exists
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    /// <summary>
    /// Backup a file before modifying it.
    /// Only backs up if file exists.
    /// </summary>
    public async Task BackupFileAsync(string filePath)
    {
        if (!_transactionInProgress)
            throw new InvalidOperationException("No transaction in progress. Call BeginTransaction first.");

        if (!File.Exists(filePath))
            return; // Nothing to backup

        // Create backup filename with timestamp
        string fileName = Path.GetFileName(filePath);
        string backupPath = Path.Combine(_backupDirectory, $"{fileName}.backup");

        // Copy file to backup location
        await Task.Run(() => File.Copy(filePath, backupPath, overwrite: true));

        // Track for rollback
        _backedUpFiles.Add(filePath);
    }

    /// <summary>
    /// Backup multiple files.
    /// </summary>
    public async Task BackupFilesAsync(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            await BackupFileAsync(filePath);
        }
    }

    /// <summary>
    /// Commit the transaction.
    /// Clears backups and marks transaction as complete.
    /// </summary>
    public void CommitTransaction()
    {
        if (!_transactionInProgress)
            throw new InvalidOperationException("No transaction in progress.");

        // Delete backup files
        ClearBackups();

        _transactionInProgress = false;
        _backedUpFiles.Clear();
    }

    /// <summary>
    /// Rollback the transaction.
    /// Restores all backed-up files to their original state.
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        if (!_transactionInProgress)
            throw new InvalidOperationException("No transaction in progress.");

        // Restore all backed-up files
        foreach (var filePath in _backedUpFiles)
        {
            string fileName = Path.GetFileName(filePath);
            string backupPath = Path.Combine(_backupDirectory, $"{fileName}.backup");

            if (File.Exists(backupPath))
            {
                await Task.Run(() => File.Copy(backupPath, filePath, overwrite: true));
            }
        }

        // Clear backups
        ClearBackups();

        _transactionInProgress = false;
        _backedUpFiles.Clear();
    }

    private void ClearBackups()
    {
        if (Directory.Exists(_backupDirectory))
        {
            foreach (var file in Directory.GetFiles(_backupDirectory, "*.backup"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }
    }

    /// <summary>
    /// Check if a transaction is currently in progress.
    /// </summary>
    public bool IsTransactionInProgress => _transactionInProgress;
}
