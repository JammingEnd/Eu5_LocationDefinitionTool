using Eu5_MapTool.logic;

namespace Eu5_MapTool.Models;

public class ProvinceInfo
{
    public string Id { get; private set; } // the province id is the same as the hex color
    public string Name { get; set; } // the name of the province like its in the files 
    
    public string OldName { get; set; } // the original name of the province before any changes
    public ProvinceLocation LocationInfo { get; set; } // info for Location.txt
    public ProvincePopInfo PopInfo { get; set; }
    
    public ProvinceInfo(string name, string id)
    {
        Id = id;
        Name = name;
    }
}