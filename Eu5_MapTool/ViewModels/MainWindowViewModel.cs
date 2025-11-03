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
        
        _writerService = new ModFileWriterService();
    }
    
    public void SetCache(Cache cache)
    {
        //TODO: load provinces from cache
        Cache = cache;
    }
    
    // --------- map properties ---------

    [ObservableProperty]
    private Bitmap _mapImage;
    public event EventHandler? OnDoneLoadingMap;
    public Dictionary<string, ProvinceInfo> Provinces { get; private set; } // these are provinces loaded from cache
    public Dictionary<string, ProvinceInfo> _paintedLocations { get; private set;  } = new Dictionary<string, ProvinceInfo>();
    private ProvinceInfo ActiveProvinceInfo { get; set; }
    
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
        FormatLocationInfo();
    }

    public void OnPaint(string provinceId, string topo, string vegetation, string climate, string religion, string culture, string rawMaterial)
    {
        provinceId = provinceId.Replace("#", "").Trim();
        
        ProvinceInfo info;
        if(_paintedLocations.Keys.Contains(provinceId))
        {
            info = _paintedLocations[provinceId];
        }
        else
        {
            info = new ProvinceInfo(provinceId, "PLACEHOLDER_NAME");
        }

        info.LocationInfo = new ProvinceLocation()
        {
            Topography = topo,
            Vegetation = vegetation,
            Climate = climate,
            Religion = religion,
            Culture = culture,
            RawMaterial = rawMaterial
        };

        _paintedLocations[provinceId] = info;
    }
    
    public void OnPaintPop(string provinceId, List<PopDef> pops)
    {
        provinceId = provinceId.Replace("#", "").Trim();
        
        ProvinceInfo info;

        pops = MergePopUpdates(pops);
        
        if(_paintedLocations.Keys.Contains(provinceId))
        {
            info = _paintedLocations[provinceId];
        }
        else
        {
            info = new ProvinceInfo(provinceId, "PLACEHOLDER_NAME");
        }

        info.PopInfo = new ProvincePopInfo()
        {
            Pops = pops
        };
        

        _paintedLocations[provinceId] = info;
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

    public void FormatLocationInfo()
    {
        ProvinceId = "Code: " + ProvinceId;
        Topography = "Topography: " + Topography;
        Climate = "Climate: " + Climate;
        Vegetation = "Vegetation: " + Vegetation;
        Religion = "Religion: " + Religion;
        Culture = "Culture: " + Culture;
        RawMaterial = "Raw Material: " + RawMaterial;
    }
    // --- Pop info ---
    public string ProvinceName = "";
    public List<PopDef> Pops = new List<PopDef>();
    
    
    // --------- file writing ---------    
    public async Task WriteChanges()
    {
        //TODO: implement writing changes to mod files
        Console.WriteLine($"Writing {_paintedLocations.Count} painted locations...");   
        
        Dictionary<string, ProvinceLocation> locationQueue = new Dictionary<string, ProvinceLocation>();
        List<(string, string)> locMap = new List<(string, string)>();
        Dictionary<string, ProvincePopInfo> popQueue = new Dictionary<string, ProvincePopInfo>();
        
        foreach (var kvp in _paintedLocations)
        {
            string provinceId = kvp.Key;
            ProvinceInfo info = kvp.Value;

            var locInfo = info.LocationInfo; 
            if(!new[] { locInfo.Topography, locInfo.Vegetation, locInfo.Climate, locInfo.Culture, locInfo.Religion, locInfo.RawMaterial }.All(string.IsNullOrWhiteSpace))
                locationQueue[provinceId] = locInfo;
            

            if (kvp.Value.PopInfo != null)
            {
                ProvincePopInfo popInfo = new ProvincePopInfo();
                popInfo.Pops = kvp.Value.PopInfo.Pops;
                popQueue[provinceId] = popInfo;
            }
            
           
            locMap.Add((info.Id, info.Name));
        }

        await _writerService.WriteLocationMapAsync(locMap);
        
        if(locationQueue.Count > 0)
            await _writerService.WriteLocationInfoAsync(locationQueue);

        if(popQueue.Count > 0) 
            await _writerService.WriteProvincePopInfoAsync(popQueue);
        
        _paintedLocations.Clear();
    }
}