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
}