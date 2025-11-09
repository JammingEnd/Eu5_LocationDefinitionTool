using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Eu5_MapTool.cache;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services;
using Eu5_MapTool.Services.Repository;

namespace Eu5_MapTool.ViewModels;

public enum ToolType
{
    Select,
    Paint,
    Province
}

public enum PaintType
{
    LocationInfo,
    PopInfo,
}

public partial class MainWindowViewModel : ViewModelBase
{
    public Cache Cache { get; private set; }
    private IUnitOfWork? _unitOfWork;

    public MainWindowViewModel()
    {
        // Properties are initialized with default values above
    }

    /// <summary>
    /// Initialize the Unit of Work for ORM-style data access.
    /// Should be called after services are configured.
    /// </summary>
    public void InitializeUnitOfWork(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public void SetCache(Cache cache)
    {

        Cache = cache;
    }
    
    // --------- map properties ---------

    [ObservableProperty]
    private Bitmap _mapImage;
    public event EventHandler? OnDoneLoadingMap;
    public Dictionary<string, ProvinceInfo> Provinces { get; private set; } // these are provinces loaded from cache

    // Legacy: Keeping for backward compatibility during transition
    // This now returns tracked changes from UnitOfWork if available
    public Dictionary<string, ProvinceInfo> _paintedLocations
    {
        get
        {
            if (_unitOfWork == null || !_unitOfWork.HasChanges)
                return new Dictionary<string, ProvinceInfo>();

            // Return only changed provinces from the change tracker
            return _unitOfWork.GetChangedProvinces();
        }
    }

    private ProvinceInfo? ActiveProvinceInfo { get; set; }

    public async void LoadProvinces()
    {
        if (_unitOfWork == null)
        {
            Console.WriteLine("ERROR: Cannot reload provinces - UnitOfWork not initialized");
            return;
        }

        try
        {
            // Reload from repository
            var allProvinces = await _unitOfWork.Provinces.GetAllAsync();
            Provinces = allProvinces.ToDictionary(p => p.Id, p => p);
            Console.WriteLine("Provinces reloaded: " + Provinces.Count);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error reloading provinces: " + e.Message);
        }
    }
    public void LoadProvinces(Dictionary<string, ProvinceInfo> p)
    {
        Provinces = p;
        Console.WriteLine("Provinces loaded: " + Provinces.Count);
    }
    
    public async void LoadMapImage(string moddedGamePath)
    {
        string imagePath = Path.Combine(moddedGamePath, StaticConstucts.MAPDATAPATH, "locations.png");
        await using var fs = File.OpenRead(imagePath);
        MapImage = new Bitmap(fs);
        OnDoneLoadingMap?.Invoke(this, EventArgs.Empty);
        Console.WriteLine("Map image loaded.");
    }
    
    // --------- tool properties ---------
    
    // --------- tool methods ---------
  
    
    public async Task<ProvinceInfo> OnSelect(string provinceId)
    {
        ProvinceInfo? info = null;

        // First check if UnitOfWork is available and province exists there
        if (_unitOfWork != null)
        {
            info = await _unitOfWork.Provinces.GetByIdAsync(provinceId);
        }

        // Fallback to Provinces dictionary if not found in UnitOfWork
        if (info == null && Provinces.ContainsKey(provinceId))
        {
            info = Provinces[provinceId];
        }

        if (info == null) return null;

        ActiveProvinceInfo = info;
        UpdateProvinceInfoDisplay(info);
        return info;
    }

    public async void OnPaint(string provinceId, string topo, string vegetation, string climate, string religion, string culture, string rawMaterial)
    {
        if (_unitOfWork == null)
        {
            Console.WriteLine("ERROR: UnitOfWork not initialized");
            return;
        }

        ProvinceInfo? info = await _unitOfWork.Provinces.GetByIdAsync(provinceId);

        if (info != null)
        {
            // Update existing province
            info.LocationInfo = new ProvinceLocation(
                topo,
                vegetation,
                climate,
                religion,
                culture,
                rawMaterial,
                info.LocationInfo.NaturalHarborSuitability
            );

            // Automatically tracked by UnitOfWork
            _unitOfWork.Provinces.Update(info);
        }
        else
        {
            // Create new province
            string randomName = GenerateRandomName();
            info = new ProvinceInfo(randomName, provinceId)
            {
                LocationInfo = new ProvinceLocation(
                    topo,
                    vegetation,
                    climate,
                    religion,
                    culture,
                    rawMaterial,
                    "0.00"
                )
            };

            // Automatically tracked by UnitOfWork
            _unitOfWork.Provinces.Add(info);
        }

        ActiveProvinceInfo = info;

        // Update UI to show the painted values
        UpdateProvinceInfoDisplay(info);

        Console.WriteLine($"Province {provinceId} painted. Total changes: {_unitOfWork.ChangeCount}");
    }
    
    public async void OnPaintPop(string provinceId, List<PopDef> pops)
    {
        if (_unitOfWork == null)
        {
            Console.WriteLine("ERROR: UnitOfWork not initialized");
            return;
        }

        provinceId = provinceId.Replace("#", "").Trim();

        ProvinceInfo? info = await _unitOfWork.Provinces.GetByIdAsync(provinceId);

        if (info != null)
        {
            // Update existing province pops
            foreach (var pop in pops)
            {
                info.PopInfo.Pops.Add(pop);
            }

            // Merge duplicate pops (same type/culture/religion)
            var updatedPops = MergePopUpdates(info.PopInfo.Pops);
            info.PopInfo.Pops = updatedPops;

            // Automatically tracked by UnitOfWork
            _unitOfWork.Provinces.Update(info);
        }
        else
        {
            // Create new province with pops
            string randomName = GenerateRandomName();
            info = new ProvinceInfo(randomName, provinceId)
            {
                PopInfo = new ProvincePopInfo
                {
                    Pops = pops
                }
            };

            // Automatically tracked by UnitOfWork
            _unitOfWork.Provinces.Add(info);
        }

        // Update UI to show the province info (including the pops just painted)
        ActiveProvinceInfo = info;
        UpdateProvinceInfoDisplay(info);

        Console.WriteLine($"Province {provinceId} pops painted. Total changes: {_unitOfWork.ChangeCount}");
    }

    private string GenerateRandomName()
    {
        string randomName = "___";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var random = new Random();
        for (int i = 0; i < 10; i++)
        {
            randomName += chars[random.Next(chars.Length)];
        }
        Console.WriteLine("Generating new name for province");
        return randomName;
    }

    // I HATE THIS I HATE THIS I HATE THIS I HATE THIS I HATE THIS I HATE THIS I HATE THIS I HATE THIS
    // thanks GPT :3
    private List<PopDef> MergePopUpdates(List<PopDef> pops)
    {
        // We’ll scan from the end so later entries take priority
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            var newer = pops[i];
            for (int j = 0; j < i; j++)
            {
                var older = pops[j];

                // Same PopType, Culture, and Religion
                if (older.PopType == newer.PopType &&
                    older.Culture == newer.Culture &&
                    older.Religion == newer.Religion)
                {
                    // Update old one’s size with the newer one’s
                    older.Size = newer.Size;

                    // Remove the duplicate newer one
                    pops.RemoveAt(i);
                    break; // exit inner loop since we removed the newer
                }
            }
        }

        return pops;
    }

    
    // --------- infopanel properties ---------

    // --- Location info ---
    [ObservableProperty]
    private string _provinceId = "Code: ...";

    [ObservableProperty]
    private string _topography = "Topography: ...";

    [ObservableProperty]
    private string _climate = "Climate: ...";

    [ObservableProperty]
    private string _vegetation = "Vegetation: ...";

    [ObservableProperty]
    private string _religion = "Religion: ...";

    [ObservableProperty]
    private string _culture = "Culture: ...";

    [ObservableProperty]
    private string _rawMaterial = "Raw Material: ...";

    [ObservableProperty]
    private string _harborSuitability = "Harbor Suitability: ...";

    /// <summary>
    /// Update UI properties from ProvinceInfo data
    /// </summary>
    private void UpdateProvinceInfoDisplay(ProvinceInfo info)
    {
        ProvinceId = $"Code: {info.Id}";
        Topography = $"Topography: {info.LocationInfo.Topography}";
        Vegetation = $"Vegetation: {info.LocationInfo.Vegetation}";
        Climate = $"Climate: {info.LocationInfo.Climate}";
        Religion = $"Religion: {info.LocationInfo.Religion}";
        Culture = $"Culture: {info.LocationInfo.Culture}";
        RawMaterial = $"Raw Material: {info.LocationInfo.RawMaterial}";
        HarborSuitability = $"Harbor Suitability: {info.LocationInfo.NaturalHarborSuitability}";
        
    }
    // --- Pop info ---
    public string ProvinceName = "";
    public List<PopDef> Pops = new List<PopDef>();
    
    
    // --------- file writing ---------
    public async Task WriteChanges()
    {
        if (_unitOfWork == null)
        {
            Console.WriteLine("ERROR: UnitOfWork not initialized. Cannot write changes.");
            return;
        }

        if (!_unitOfWork.HasChanges)
        {
            Console.WriteLine("No changes to write.");
            return;
        }

        Console.WriteLine($"Writing {_unitOfWork.ChangeCount} changed provinces...");

        try
        {
            // Save all changes with transaction safety (automatic backup/rollback)
            int savedCount = await _unitOfWork.SaveChangesAsync();

            Console.WriteLine($"✓ Successfully saved {savedCount} provinces");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to save changes: {ex.Message}");
            Console.WriteLine("Changes have been automatically rolled back.");

            // Rollback in-memory changes as well
            await _unitOfWork.RollbackAsync();

            throw; // Re-throw so UI can handle the error
        }
    }

    public async void UpdateProvinceName(string? nameBoxText)
    {
        if (_unitOfWork == null || ActiveProvinceInfo == null) return;
        if (string.IsNullOrWhiteSpace(nameBoxText)) return;

        string newName = nameBoxText.Trim();
        string provinceId = ActiveProvinceInfo.Id;

        // Get the province from the repository to ensure we have the correct reference
        var info = await _unitOfWork.Provinces.GetByIdAsync(provinceId);
        if (info == null)
        {
            Console.WriteLine($"ERROR: Province {provinceId} not found in repository");
            return;
        }

        // Store the CURRENT name as OldName for tracking the rename
        // This tracks what name exists in the files RIGHT NOW
        // After save, OldName will be cleared since files will match the new name
        if (string.IsNullOrEmpty(info.OldName))
        {
            info.OldName = info.Name;
        }

        // Update the province name
        info.Name = newName;

        // Track the change in UnitOfWork
        _unitOfWork.Provinces.Update(info);

        // Update the active reference
        ActiveProvinceInfo = info;

        // Refresh UI
        UpdateProvinceInfoDisplay(info);

        Console.WriteLine($"Province {provinceId} name updated: {info.OldName} -> {info.Name}");
    }
}