using System.Collections.Generic;

namespace Eu5_MapTool.logic;

public class ProvincePopInfo
{
    public List<PopDef> Pops;   
    
    public ProvincePopInfo()
    {
        Pops = new List<PopDef>();
    }
}
public class PopDef
{
    public string PopType;
    public float Size;
    public string Culture;
    public string Religion;
    
    public void UpdateSize(float newSize)
    {
        Size = newSize;
    }
}