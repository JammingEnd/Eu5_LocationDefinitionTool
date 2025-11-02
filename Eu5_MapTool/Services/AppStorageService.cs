using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services;

public class AppStorageService : IAppStorageInterface
{
    private string _baseGamePath;
    private string _moddedGamePath;

    private const string _LocationHexToNamefile = "";
    
    
    public void SetDirectories(string baseGamePath, string moddedGamePath)
    {
        _baseGamePath = baseGamePath;
        _moddedGamePath = moddedGamePath;
    }
    
    public async Task<Dictionary<string, ProvinceInfo>> LoadBaseGameAsync()
    {
        Dictionary<string, ProvinceInfo> provinceInfos = new();
        
        // reading hex to name mapping 
        string locationHexToNamePath = Path.Combine(_baseGamePath, StaticConstucts._LocationHexToNameDirectory);
        var dirInfo = new DirectoryInfo(locationHexToNamePath);
        foreach (FileInfo locMapFiles in dirInfo.GetFiles("*.txt"))
        {
            var c_lines = await File.ReadAllLinesAsync(locMapFiles.DirectoryName);
            Queue<string> mapLines = new Queue<string>(
                c_lines.Where(line => !string.IsNullOrWhiteSpace(line))
            );
            // line format -> location_name = FFF000
            while (mapLines.Count > 0)
            {
                string line = mapLines.Dequeue();
                int equalIndex = line.IndexOf('=');
                if (equalIndex < 0) continue;
                
                string name = line[..equalIndex].Trim();
                string hex = line[(equalIndex + 1)..].Trim();
                
                ProvinceInfo info = new ProvinceInfo(hex, name);
                
                provinceInfos[hex] = info;
            }   
            
        }
        
        
        
        string locationFilePath = Path.Combine(_baseGamePath + StaticConstucts._ProvinceLocationFileName, "Location.txt");
        
        // read all lines from Location.txt and parsing it to ProvinceInfo objects
        var lines = await File.ReadAllLinesAsync(locationFilePath);
        Queue<string> locationLines = new Queue<string>(
            lines.Where(line => !string.IsNullOrWhiteSpace(line))
        );

        while (locationLines.Count > 0)
        {
            string line = locationLines.Dequeue();
            int idEnd = line.IndexOf('=');
            string id = line[..idEnd].Trim();
            
            string inner = line[(idEnd + 1)..].Trim('{', ' ', '}');
            
            string[] parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var dict = new Dictionary<string, string>();

            for (int i = 0; i < parts.Length - 2; i += 3)
            {
                string key = parts[i];
                string value = parts[i + 2]; // skip '='
                dict[key] = value;
            }

            ProvinceInfo info = provinceInfos[id];
            
              info.LocationInfo = new ProvinceLocation
                {
                    Topography = dict["topography"],
                    Vegetation = dict["vegetation"],
                    Climate = dict["climate"],
                    Religion = dict["religion"],
                    Culture = dict["culture"],
                    RawMaterial = dict["raw_material"]

                };
            
        }
        
        
        
        
        return provinceInfos;
    }
    public async Task<Dictionary<string, ProvinceInfo>> LoadModdedAsync()
    {
        Dictionary<string, ProvinceInfo> provinceInfos = new();
        
        // reading hex to name mapping 
        string locationHexToNamePath = Path.Combine(_moddedGamePath, StaticConstucts._LocationHexToNameDirectory); 
        var dirInfo = new DirectoryInfo(locationHexToNamePath);
        foreach (FileInfo locMapFiles in dirInfo.GetFiles("*.txt"))
        {
            var c_lines = await File.ReadAllLinesAsync(locMapFiles.FullName);
            Queue<string> mapLines = new Queue<string>(
                c_lines.Where(line => !string.IsNullOrWhiteSpace(line))
            );
            // line format -> location_name = FFF000
            while (mapLines.Count > 0)
            {
                string line = mapLines.Dequeue();
                int equalIndex = line.IndexOf('=');
                if (equalIndex < 0) continue;
                
                string name = line[..equalIndex].Trim();
                string hex = line[(equalIndex + 1)..].Trim();
                
                ProvinceInfo info = new ProvinceInfo(hex, name);
                
                provinceInfos[hex] = info;
            }   
            
        }
        
        
        
        string locationFilePath = Path.Combine(_moddedGamePath + StaticConstucts._ProvinceLocationFileName, "Location.txt");
        
        // read all lines from Location.txt and parsing it to ProvinceInfo objects
        var lines = await File.ReadAllLinesAsync(locationFilePath);
        Queue<string> locationLines = new Queue<string>(
            lines.Where(line => !string.IsNullOrWhiteSpace(line))
        );


        while (locationLines.Count > 0)
        {
            string line = locationLines.Dequeue();
            int idEnd = line.IndexOf('=');
            string id = line[..idEnd].Trim();
            
            string inner = line[(idEnd + 1)..].Trim('{', ' ', '}');
            
            string[] parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var dict = new Dictionary<string, string>();

            for (int i = 0; i < parts.Length - 2; i += 3)
            {
                string key = parts[i];
                string value = parts[i + 2]; // skip '='
                dict[key] = value;
            }

            ProvinceInfo info = provinceInfos[id];
            
              info.LocationInfo = new ProvinceLocation
                {
                    Topography = dict["topography"],
                    Vegetation = dict["vegetation"],
                    Climate = dict["climate"],
                    Religion = dict["religion"],
                    Culture = dict["culture"],
                    RawMaterial = dict["raw_material"]

                };
            
        }
        
        
        
        
        return provinceInfos;
    }

    public async Task<Bitmap> LoadMapImageAsync()
    {
        using var fs = File.OpenRead(Path.Combine(_moddedGamePath + StaticConstucts._ProvinceLocationFileName, "locations.png"));
        return new Bitmap(fs);
    }
}