using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services.Parsing;
using Eu5_MapTool.Services.Mapping;

namespace Eu5_MapTool.Services.Repository;

/// <summary>
/// Repository for Province entities.
/// Provides CRUD operations and manages in-memory province data.
/// Uses parsers and mappers directly (no dependency on old services).
/// </summary>
public class ProvinceRepository : IRepository<ProvinceInfo, string>
{
    private readonly Dictionary<string, ProvinceInfo> _provinces;
    private readonly string _baseGameDirectory;
    private readonly string _modsDirectory;

    // Parsers
    private readonly KeyValueFileParser _keyValueParser;
    private readonly NestedStructureParser _nestedParser;
    private readonly PopDefinitionParser _popParser;

    // Mappers
    private readonly ProvinceMapper _provinceMapper;

    public ProvinceRepository(string baseGameDirectory, string modsDirectory)
    {
        _baseGameDirectory = baseGameDirectory;
        _modsDirectory = modsDirectory;
        _provinces = new Dictionary<string, ProvinceInfo>(StringComparer.OrdinalIgnoreCase);

        // Initialize parsers
        _keyValueParser = new KeyValueFileParser();
        _nestedParser = new NestedStructureParser();
        _popParser = new PopDefinitionParser();

        // Initialize mappers
        _provinceMapper = new ProvinceMapper();
    }

    /// <summary>
    /// Load all provinces from base game and modded files.
    /// This should be called during application startup.
    /// Uses parsers directly to read files.
    /// </summary>
    public async Task LoadAsync()
    {
        _provinces.Clear();

        Console.WriteLine("Loading provinces using parsers...");

        // Load base game provinces
        var baseGameProvinces = await LoadProvincesFromDirectory(_baseGameDirectory, includePopInfo: false);
        Console.WriteLine($"Loaded {baseGameProvinces.Count} base game provinces");

        foreach (var kvp in baseGameProvinces)
        {
            _provinces[kvp.Key] = kvp.Value;
        }

        // Load modded provinces (will override base game where applicable)
        var moddedProvinces = await LoadProvincesFromDirectory(_modsDirectory, includePopInfo: true);
        Console.WriteLine($"Loaded {moddedProvinces.Count} modded provinces");

        foreach (var kvp in moddedProvinces)
        {
            _provinces[kvp.Key] = kvp.Value;
        }

        Console.WriteLine($"Total provinces in repository: {_provinces.Count}");
    }

    /// <summary>
    /// Load provinces from a specific directory using parsers.
    /// </summary>
    private async Task<Dictionary<string, ProvinceInfo>> LoadProvincesFromDirectory(string directory, bool includePopInfo)
    {
        // 1. Load named locations (hex to name mapping)
        var hexToNameMap = await LoadNamedLocationsAsync(directory);

        // 2. Load location templates (location info)
        var locationData = await LoadLocationTemplatesAsync(directory);

        // 3. Load pop info (if requested - only for modded directory)
        Dictionary<string, LocationPopData> popData = new();
        if (includePopInfo)
        {
            popData = await LoadPopInfoAsync(directory);
        }

        // 4. Use mapper to combine all data into ProvinceInfo entities
        var provinces = _provinceMapper.MapToEntityDictionary(hexToNameMap, locationData, popData);

        return provinces;
    }

    /// <summary>
    /// Load named locations files (hex to name mapping).
    /// </summary>
    private async Task<Dictionary<string, string>> LoadNamedLocationsAsync(string directory)
    {
        string nameLocationsDir = Path.Combine(directory, StaticConstucts.LOCHEXTONAMEPATH);

        if (!Directory.Exists(nameLocationsDir))
        {
            Console.WriteLine($"Warning: Named locations directory not found: {nameLocationsDir}");
            return new Dictionary<string, string>();
        }

        var files = Directory.GetFiles(nameLocationsDir, "*.txt");

        if (files.Length == 0)
        {
            Console.WriteLine($"Warning: No named location files found in: {nameLocationsDir}");
            return new Dictionary<string, string>();
        }

        // Parse all files and combine results
        return await _keyValueParser.ParseFilesAsync(files);
    }

    /// <summary>
    /// Load location templates file (location info like climate, topography, etc.).
    /// </summary>
    private async Task<Dictionary<string, Dictionary<string, string>>> LoadLocationTemplatesAsync(string directory)
    {
        string locationTemplatesFile = Path.Combine(directory, StaticConstucts.MAPDATAPATH, "location_templates.txt");

        if (!File.Exists(locationTemplatesFile))
        {
            Console.WriteLine($"Warning: Location templates file not found: {locationTemplatesFile}");
            return new Dictionary<string, Dictionary<string, string>>();
        }

        return await _nestedParser.ParseFileAsync(locationTemplatesFile);
    }

    /// <summary>
    /// Load pop info files.
    /// </summary>
    private async Task<Dictionary<string, LocationPopData>> LoadPopInfoAsync(string directory)
    {
        string popInfoDir = Path.Combine(directory, StaticConstucts.POPINFOPATH);

        if (!Directory.Exists(popInfoDir))
        {
            Console.WriteLine($"Warning: Pop info directory not found: {popInfoDir}");
            return new Dictionary<string, LocationPopData>();
        }

        var popFiles = Directory.GetFiles(popInfoDir, "*pops*.txt");

        if (popFiles.Length == 0)
        {
            Console.WriteLine($"Warning: No pop files found in: {popInfoDir}");
            return new Dictionary<string, LocationPopData>();
        }

        // Parse all pop files and combine results
        return await _popParser.ParseFilesAsync(popFiles);
    }

    public Task<ProvinceInfo?> GetByIdAsync(string id)
    {
        _provinces.TryGetValue(id, out var province);
        return Task.FromResult(province);
    }

    public Task<IEnumerable<ProvinceInfo>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<ProvinceInfo>>(_provinces.Values);
    }

    public Task<Dictionary<string, ProvinceInfo>> GetAllAsDictionaryAsync()
    {
        // Return a copy to prevent external modification
        var copy = new Dictionary<string, ProvinceInfo>(_provinces, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(copy);
    }

    public Task<IEnumerable<ProvinceInfo>> FindAsync(Func<ProvinceInfo, bool> predicate)
    {
        var results = _provinces.Values.Where(predicate);
        return Task.FromResult(results);
    }

    public void Add(ProvinceInfo entity)
    {
        _provinces[entity.Id] = entity;
    }

    public void Update(ProvinceInfo entity)
    {
        if (_provinces.ContainsKey(entity.Id))
        {
            _provinces[entity.Id] = entity;
        }
        else
        {
            throw new KeyNotFoundException($"Province with ID '{entity.Id}' not found.");
        }
    }

    public void Delete(string id)
    {
        _provinces.Remove(id);
    }

    public void Delete(ProvinceInfo entity)
    {
        Delete(entity.Id);
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_provinces.ContainsKey(id));
    }

    /// <summary>
    /// Internal method used by UnitOfWork to save changes.
    /// Uses parsers and writers directly (no dependency on old services).
    /// </summary>
    internal async Task SaveAsync(Dictionary<string, ProvinceInfo> changedProvinces)
    {
        Console.WriteLine($"Saving {changedProvinces.Count} provinces using parsers...");

        // 1. Save named locations (hex to name mapping)
        await SaveNamedLocationsAsync(changedProvinces);

        // 2. Save location templates (location info)
        await SaveLocationTemplatesAsync(changedProvinces);

        // 3. Save pop info
        await SavePopInfoAsync(changedProvinces);

        Console.WriteLine("✓ All province files saved successfully");
    }

    /// <summary>
    /// Save named locations (hex to name mapping).
    /// </summary>
    private async Task SaveNamedLocationsAsync(Dictionary<string, ProvinceInfo> changedProvinces)
    {
        string nameLocationsDir = Path.Combine(_modsDirectory, StaticConstucts.LOCHEXTONAMEPATH);

        if (!Directory.Exists(nameLocationsDir))
        {
            Console.WriteLine($"Warning: Named locations directory not found: {nameLocationsDir}");
            return;
        }

        var files = Directory.GetFiles(nameLocationsDir, "*.txt");
        if (files.Length == 0)
        {
            Console.WriteLine($"Warning: No named location files found in: {nameLocationsDir}");
            return;
        }

        string nameHexFile = files[0]; // Use first file found

        // Read existing data
        var existingData = await _keyValueParser.ParseFileAsync(nameHexFile);

        // Handle renames: remove old name entries
        foreach (var province in changedProvinces.Values)
        {
            string oldName = province.OldName ?? province.Name;
            string newName = province.Name;

            if (oldName != newName && existingData.ContainsKey(oldName))
            {
                // Province was renamed - remove the old entry
                existingData.Remove(oldName);
                Console.WriteLine($"  Removing old name entry: {oldName} -> {newName}");
            }
        }

        // Merge changed provinces (name -> hex mapping)
        var hexToNameMap = _provinceMapper.MapToHexNameDictionary(changedProvinces);

        foreach (var kvp in hexToNameMap)
        {
            existingData[kvp.Key] = kvp.Value; // name -> hex
        }

        // Write back to file
        await _keyValueParser.WriteFileAsync(nameHexFile, existingData);

        Console.WriteLine($"✓ Saved named locations to: {Path.GetFileName(nameHexFile)}");
    }

    /// <summary>
    /// Save location templates (location info).
    /// </summary>
    private async Task SaveLocationTemplatesAsync(Dictionary<string, ProvinceInfo> changedProvinces)
    {
        string locationTemplatesFile = Path.Combine(_modsDirectory, StaticConstucts.MAPDATAPATH, "location_templates.txt");

        if (!File.Exists(locationTemplatesFile))
        {
            Console.WriteLine($"Warning: Location templates file not found: {locationTemplatesFile}");
            return;
        }

        // Read existing data
        var existingData = await _nestedParser.ParseFileAsync(locationTemplatesFile);

        // Handle renames: remove old name entries
        foreach (var province in changedProvinces.Values)
        {
            string oldName = province.OldName ?? province.Name;
            string newName = province.Name;

            if (oldName != newName && existingData.ContainsKey(oldName))
            {
                // Province was renamed - remove the old entry
                existingData.Remove(oldName);
                Console.WriteLine($"  Removing old name entry: {oldName} -> {newName}");
            }
        }

        // Map changed provinces to location data (uses province name as key)
        var locationDataToSave = _provinceMapper.MapToLocationDataDictionary(changedProvinces);

        // Merge with existing data (using province name as key)
        foreach (var kvp in locationDataToSave)
        {
            existingData[kvp.Key] = kvp.Value;
        }

        // Write back to file
        await _nestedParser.WriteFileAsync(locationTemplatesFile, existingData);

        Console.WriteLine($"✓ Saved location templates to: {Path.GetFileName(locationTemplatesFile)}");
    }

    /// <summary>
    /// Save pop info.
    /// </summary>
    private async Task SavePopInfoAsync(Dictionary<string, ProvinceInfo> changedProvinces)
    {
        string popInfoDir = Path.Combine(_modsDirectory, StaticConstucts.POPINFOPATH);

        if (!Directory.Exists(popInfoDir))
        {
            Console.WriteLine($"Warning: Pop info directory not found: {popInfoDir}");
            return;
        }

        var popFiles = Directory.GetFiles(popInfoDir, "*pops*.txt");
        if (popFiles.Length == 0)
        {
            Console.WriteLine($"Warning: No pop files found in: {popInfoDir}");
            return;
        }

        string popFile = popFiles[0]; // Use first file found

        // Read existing data
        var existingData = await _popParser.ParseFileAsync(popFile);

        // Handle renames: remove old name entries
        foreach (var province in changedProvinces.Values)
        {
            string oldName = province.OldName ?? province.Name;
            string newName = province.Name;

            if (oldName != newName && existingData.ContainsKey(oldName))
            {
                // Province was renamed - remove the old entry
                existingData.Remove(oldName);
                Console.WriteLine($"  Removing old pop entry: {oldName} -> {newName}");
            }
        }

        // Map changed provinces to pop data
        var popDataToSave = _provinceMapper.MapToPopDataDictionary(changedProvinces);

        // Merge with existing data (using province name as key)
        foreach (var kvp in popDataToSave)
        {
            if (existingData.ContainsKey(kvp.Key))
            {
                // Merge pops (update existing, add new)
                existingData[kvp.Key] = PopDefinitionParser.MergePops(existingData[kvp.Key], kvp.Value);
            }
            else
            {
                // Add new location
                existingData[kvp.Key] = kvp.Value;
            }
        }

        // Write back to file
        await _popParser.WriteFileAsync(popFile, existingData);

        Console.WriteLine($"✓ Saved pop info to: {Path.GetFileName(popFile)}");
    }

    /// <summary>
    /// Backup files that will be modified during save operation.
    /// Used by UnitOfWork for transaction safety.
    /// </summary>
    internal async Task BackupFilesForTransactionAsync(TransactionManager transactionManager)
    {
        if (string.IsNullOrEmpty(_modsDirectory))
            return; // No backup possible without mods directory

        var filesToBackup = new List<string>();

        // Named locations file
        string nameHexDir = Path.Combine(_modsDirectory, StaticConstucts.LOCHEXTONAMEPATH);
        if (Directory.Exists(nameHexDir))
        {
            var nameHexFile = Directory.GetFiles(nameHexDir).FirstOrDefault();
            if (nameHexFile != null)
                filesToBackup.Add(nameHexFile);
        }

        // Location templates file
        string locationTemplatesFile = Path.Combine(_modsDirectory, StaticConstucts.MAPDATAPATH, "location_templates.txt");
        if (File.Exists(locationTemplatesFile))
            filesToBackup.Add(locationTemplatesFile);

        // Pop info file
        string popInfoDir = Path.Combine(_modsDirectory, StaticConstucts.POPINFOPATH);
        if (Directory.Exists(popInfoDir))
        {
            var popInfoFile = Directory.GetFiles(popInfoDir).FirstOrDefault(x => x.Contains("pops"));
            if (popInfoFile != null)
                filesToBackup.Add(popInfoFile);
        }

        // Backup all files
        await transactionManager.BackupFilesAsync(filesToBackup);
    }
}
