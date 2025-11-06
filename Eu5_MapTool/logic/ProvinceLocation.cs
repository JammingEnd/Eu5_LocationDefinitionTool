namespace Eu5_MapTool.logic;




public struct ProvinceLocation(
    string topography,
    string vegetation,
    string climate,
    string religion,
    string culture,
    string rawMaterial,
    string naturalHarborSuitability = "0.00")
{
    public string Topography = topography;
    public string Vegetation = vegetation;
    public string Climate = climate;
    public string Religion = religion;
    public string Culture = culture;
    public string RawMaterial = rawMaterial;
    public string NaturalHarborSuitability = naturalHarborSuitability;
}