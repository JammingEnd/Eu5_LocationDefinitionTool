using System;
using System.Collections.Generic;

namespace Eu5_MapTool.Models;

public static class StaticConstucts
{
    public const string LOCHEXTONAMEPATH = "in_game/map_data/named_locations/";
    public const string MAPDATAPATH = "in_game/map_data/";
    public const string COMMONPATH = "in_game/common/";
    public const string POPINFOPATH = "main_menu/setup/start/";
    
    // Hex is key name is value. used in Writing service
    public static Dictionary<string, string> HEXTONAMEMAP = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    
}