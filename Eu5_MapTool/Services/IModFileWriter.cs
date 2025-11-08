using System.Collections.Generic;
using System.Threading.Tasks;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services;

public interface IModFileWriter
{
    Task WriteLocationMapAsync(List<(string, string)> loc_name_hex);
    Task WriteLocationInfoAsync(Dictionary<string, ProvinceInfo> locationInfos);
    Task WriteProvincePopInfoAsync(Dictionary<string, ProvinceInfo> locationPopInfos);
}