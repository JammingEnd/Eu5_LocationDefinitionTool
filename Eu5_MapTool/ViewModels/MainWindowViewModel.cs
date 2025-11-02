using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            ProvincePopInfo popInfo = new ProvincePopInfo
            {
                Pops = kvp.Value.PopInfo.Pops
            };
           
            locationQueue[provinceId] = info.LocationInfo;
            locMap.Add((info.Id, info.Name));
            popQueue[provinceId] = popInfo;
        }

        await _writerService.WriteLocationMapAsync(locMap);
        
        await _writerService.WriteLocationInfoAsync(locationQueue);

        await _writerService.WriteProvincePopInfoAsync(popQueue);
        
        _paintedLocations.Clear();
    }
}