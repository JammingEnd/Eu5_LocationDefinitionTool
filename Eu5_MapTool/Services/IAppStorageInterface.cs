using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services;

public interface IAppStorageInterface
{
    Task<Dictionary<string, ProvinceInfo>> LoadBaseGameAsync();
    Task<Dictionary<string, ProvinceInfo>> LoadModdedAsync();
    Task<Bitmap> LoadMapImageAsync();
    
    Task<HashSet<string>> LoadTopographyListAsync(string directoryPath);
    Task<HashSet<string>> LoadClimateListAsync(string directoryPath);
    Task<HashSet<string>> LoadVegetationListAsync(string directoryPath);
    Task<HashSet<string>> LoadReligionListAsync(string directoryPath);
    Task<HashSet<string>> LoadCultureListAsync(string directoryPath);
    Task<HashSet<string>> LoadRawMaterialListAsync(string directoryPath);
    Task<HashSet<string>> LoadPopTypeListAsync(string directoryPath);
}