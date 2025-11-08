using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Eu5_MapTool.cache;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services;

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
    public readonly ModFileWriterService _writerService;
    private AppStorageService _reader;
    public MainWindowViewModel()
    {
        ProvinceId = "Code: ...";
        Topography = "Topography: ...";
        Climate = "Climate: ...";
        Vegetation = "Vegetation: ...";
        Religion = "Religion: ...";
        Culture = "Culture: ...";
        RawMaterial = "Raw Material: ...";
        HarborSuitability = "Harbor Suitability: ...";
        
        _writerService = new ModFileWriterService();
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
    public Dictionary<string, ProvinceInfo> _paintedLocations { get; private set;  } = new Dictionary<string, ProvinceInfo>();
    
    private ProvinceInfo ActiveProvinceInfo { get; set; }

    public async void LoadProvinces()
    {
        try
        {
            Dictionary<string, ProvinceInfo> p = await _reader.LoadModdedAsync();
            Provinces = p;
            Console.WriteLine("Provinces reloaded: " + Provinces.Count);
        }
        catch (Exception e)
        {
            Console.WriteLine(" mama mia, Error loading provinces: " + e.Message);
        }
    }
    public void LoadProvinces(Dictionary<string, ProvinceInfo> p)
    {
        Provinces = p;
        Console.WriteLine("Provinces loaded: " + Provinces.Count);
    }
    
    public async void LoadMapImage(AppStorageService reader)
    {
        _reader = reader;
        _mapImage = await _reader.LoadMapImageAsync();
        OnDoneLoadingMap?.Invoke(this, EventArgs.Empty);
    }
    
    // --------- tool properties ---------
    
    // --------- tool methods ---------
  
    
    public void OnSelect(string provinceId)
    {
        
        var info = Provinces.ContainsKey(provinceId)
            ? Provinces[provinceId]
            : _paintedLocations.ContainsKey(provinceId)
                ? _paintedLocations[provinceId]
                : null;
        
        if(info == null) return;
        
        ActiveProvinceInfo = info;
        ProvinceId = ActiveProvinceInfo.Id;
        Topography = ActiveProvinceInfo.LocationInfo.Topography;
        Vegetation = ActiveProvinceInfo.LocationInfo.Vegetation;
        Religion = ActiveProvinceInfo.LocationInfo.Religion;
        Culture = ActiveProvinceInfo.LocationInfo.Culture;
        RawMaterial = ActiveProvinceInfo.LocationInfo.RawMaterial;
        Climate = ActiveProvinceInfo.LocationInfo.Climate;
        HarborSuitability = ActiveProvinceInfo.LocationInfo.NaturalHarborSuitability;
        FormatLocationInfo();
    }

    public void OnPaint(string provinceId, string topo, string vegetation, string climate, string religion, string culture, string rawMaterial)
    {
        ProvinceInfo info;
        if (_paintedLocations.TryGetValue(provinceId, out ProvinceInfo existingInfo) || Provinces.TryGetValue(provinceId, out existingInfo))
        {
            // Update existing info
            info = existingInfo;
            info.LocationInfo = new ProvinceLocation(
                topo,
                vegetation,
                climate,
                religion,
                culture,
                rawMaterial,
                info.LocationInfo.NaturalHarborSuitability
            );
            _paintedLocations[provinceId] = info;
        }
        else
        {
            // Create new info
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
            _paintedLocations[provinceId] = info;
        }
        ActiveProvinceInfo = info;
        //TODO: fill in the static map with the new values

    }
    
    public void OnPaintPop(string provinceId, List<PopDef> pops)
    {
        provinceId = provinceId.Replace("#", "").Trim();
        
        ProvinceInfo info;
        
        if (_paintedLocations.TryGetValue(provinceId, out ProvinceInfo existingInfo) || Provinces.TryGetValue(provinceId, out existingInfo))
        {
            // Update existing info
            info = existingInfo;
            foreach (var pop in pops)
            {
                info.PopInfo.Pops.Add(pop);
            }
            var updatingpops = MergePopUpdates(info.PopInfo.Pops);
            info.PopInfo.Pops = updatingpops;
            _paintedLocations[provinceId] = info;
        }
        else
        {
            // Create new info
            string randomName = GenerateRandomName();
            info = new ProvinceInfo(randomName, provinceId)
            {
                PopInfo = new ProvincePopInfo
                {
                    Pops = pops
                }
            };
            _paintedLocations[provinceId] = info;
        }
        
        //TODO: fill in the static map with the new values
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
    public string ProvinceId { get; set; }
    public string Topography { get; set; }
    public string Climate { get; set; }
    public string Vegetation { get; set; }
    public string Religion { get; set; }
    public string Culture { get; set; }
    public string RawMaterial { get; set; }
    public string HarborSuitability { get; set; }

    public void FormatLocationInfo()
    {
        ProvinceId = "Code: " + ProvinceId;
        Topography = "Topography: " + Topography;
        Climate = "Climate: " + Climate;
        Vegetation = "Vegetation: " + Vegetation;
        Religion = "Religion: " + Religion;
        Culture = "Culture: " + Culture;
        RawMaterial = "Raw Material: " + RawMaterial;
        HarborSuitability = "Harbor Suitability: " + HarborSuitability;
    }
    // --- Pop info ---
    public string ProvinceName = "";
    public List<PopDef> Pops = new List<PopDef>();
    
    
    // --------- file writing ---------    
    public async Task WriteChanges()
    {
        Console.WriteLine($"Writing {_paintedLocations.Count} painted locations...");

        List<(string, string)> locMap = new List<(string, string)>();
        List<ProvinceInfo> infos = new();
        Dictionary<string, ProvinceInfo> locations = new();
        foreach (var kvp in _paintedLocations)
        {
            string provinceId = kvp.Value.Id; // format requires province name as id
            string provinceName = kvp.Value.Name;
            ProvinceInfo info = kvp.Value;
            
            infos.Add(kvp.Value);
            
            locations[provinceName] = info;
            
            locMap.Add((info.Id, info.Name));
        }
       
        Dictionary<string, ProvinceInfo> poplocations = new(locations);

        await _writerService.WriteLocationMapAsync(locMap);
        
        await _writerService.WriteLocationInfoAsync(locations);
        
        await _writerService.WriteProvincePopInfoAsync(poplocations);
        
        _paintedLocations.Clear();
    }

    public void UpdateProvinceName(string? nameBoxText)
    {
        if (ActiveProvinceInfo == null) return;
        if (string.IsNullOrWhiteSpace(nameBoxText)) return;

        ActiveProvinceInfo.Name = nameBoxText.Trim();
        
        //TODO: correct the logic on updating the name since im using the maps to store provinces now.
        
        _paintedLocations[ActiveProvinceInfo.Id] = ActiveProvinceInfo;
    }
}